using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using SpellMigration.Core.Configuration;
using SpellMigration.Core.Mapping;
using SpellMigration.Core.Models;

namespace SpellMigration.Core.Data
{
    /// <summary>
    /// The write side of the pipeline: takes a SpellRecord already mapped to
    /// spell_dbc column names (see SpellMapper) and either previews the
    /// resulting INSERT as text (dry-run, matches the manual review step
    /// that caught every real bug during testing) or applies it to
    /// acore_world for real.
    /// 
    /// Preview and Apply are intentionally separate methods rather than a
    /// single "insert with a dryRun flag" — the caller (UI) should have to
    /// explicitly choose to apply, never fall into it via a default
    /// parameter.
    /// </summary>
    public sealed class AcoreWorldRepository
    {
        private readonly DatabaseConnectionSettings _settings;

        public AcoreWorldRepository(DatabaseConnectionSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Returns true if a spell with this ID already exists in
        /// spell_dbc. The caller should check this before Apply and decide
        /// whether to warn, block, or offer a REPLACE — this repository
        /// does not make that call on its own.</summary>
        public async Task<bool> SpellExistsAsync(int spellId)
        {
            await using var connection = new MySqlConnection(_settings.BuildConnectionString());
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1 FROM `{_settings.TableName}` WHERE `ID` = @id LIMIT 1";
            command.Parameters.AddWithValue("@id", spellId);

            var result = await command.ExecuteScalarAsync();
            return result != null;
        }

        /// <summary>Builds the human-readable INSERT statement for a mapped
        /// record, using explicit column names (never a bare positional
        /// VALUES list — that's what corrupted the first Prestillence
        /// export). This is for review/preview before applying, and can
        /// also be saved to a .sql file the same way manual testing did.</summary>
        public string BuildPreviewSql(SpellRecord mappedRecord)
        {
            var columnNames = ColumnListSpellDbcNames();

            var columnList = new StringBuilder();
            var valueList = new StringBuilder();

            for (int i = 0; i < columnNames.Length; i++)
            {
                if (i > 0)
                {
                    columnList.Append(",\n ");
                    valueList.Append(",\n ");
                }
                columnList.Append($"`{columnNames[i]}`");
                valueList.Append(FormatSqlLiteral(mappedRecord[columnNames[i]]));
            }

            return $"INSERT INTO `{_settings.Database}`.`{_settings.TableName}`\n" +
                   $"({columnList})\nVALUES\n({valueList});";
        }

        /// <summary>Actually writes the mapped record to acore_world, using
        /// a parameterized query (not string concatenation) for the real
        /// write path — the preview text above is for human review only and
        /// is never executed directly.</summary>
        public async Task ApplyAsync(SpellRecord mappedRecord)
        {
            var columnNames = ColumnListSpellDbcNames();

            var columnList = new StringBuilder();
            var paramList = new StringBuilder();

            for (int i = 0; i < columnNames.Length; i++)
            {
                if (i > 0)
                {
                    columnList.Append(", ");
                    paramList.Append(", ");
                }
                columnList.Append($"`{columnNames[i]}`");
                paramList.Append($"@p{i}");
            }

            string sql = $"INSERT INTO `{_settings.TableName}` ({columnList}) VALUES ({paramList})";

            await using var connection = new MySqlConnection(_settings.BuildConnectionString());
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            for (int i = 0; i < columnNames.Length; i++)
            {
                object? value = mappedRecord[columnNames[i]] ?? DBNull.Value;
                command.Parameters.AddWithValue($"@p{i}", value);
            }

            await command.ExecuteNonQueryAsync();
        }

        private static string[] ColumnListSpellDbcNames()
        {
            var names = new List<string>(ColumnMap.ExpectedColumnCount);
            foreach (var (_, dbcName) in ColumnMap.Pairs)
                names.Add(dbcName);
            return names.ToArray();
        }

        /// <summary>Formats a single value as a SQL literal for the preview
        /// text: numbers pass through as-is, strings are quoted with
        /// embedded quotes escaped, null becomes the NULL keyword.</summary>
        private static string FormatSqlLiteral(object? value)
        {
            if (value is null)
                return "NULL";

            if (value is string s)
                return "'" + s.Replace("'", "''") + "'";

            if (value is bool b)
                return b ? "1" : "0";

            // Numeric types (int, long, decimal, double, float, etc.) format
            // via their own ToString — culture-invariant to avoid decimal
            // commas on non-US locales silently breaking the SQL.
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL";
        }
    }
}