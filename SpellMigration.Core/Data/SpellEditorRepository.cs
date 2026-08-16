using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using SpellMigration.Core.Configuration;
using SpellMigration.Core.Mapping;
using SpellMigration.Core.Models;

namespace SpellMigration.Core.Data
{
    /// <summary>
    /// Reads spell rows from a SpellEditor-managed MySQL database, keyed by
    /// SpellEditor's own column names (as defined in ColumnMap.Pairs). This
    /// replaces the manual "export SQL insert, paste it, regex-parse it"
    /// workflow entirely — reading structured columns from ADO.NET removes
    /// the exact failure mode that corrupted the first Prestillence export
    /// (a silently dropped value in a bare VALUES(...) list).
    /// </summary>
    public sealed class SpellEditorRepository
    {
        private readonly DatabaseConnectionSettings _settings;

        public SpellEditorRepository(DatabaseConnectionSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Fetches a single spell by ID. Returns null if no row
        /// matches — the caller should treat that as "spell ID not found",
        /// not as an empty/invalid record.</summary>
        public async Task<SpellRecord?> GetSpellByIdAsync(int id)
        {
            var results = await GetSpellsByIdsAsync(new[] { id });
            return results.Count > 0 ? results[0] : null;
        }

        /// <summary>Fetches multiple spells by ID in a single round trip.
        /// Preferred over calling GetSpellByIdAsync in a loop when migrating
        /// a batch, since it avoids one connection/query per spell.</summary>
        public async Task<List<SpellRecord>> GetSpellsByIdsAsync(IReadOnlyCollection<int> ids)
        {
            var records = new List<SpellRecord>();
            if (ids.Count == 0)
                return records;

            string columnList = string.Join(", ",
                Array.ConvertAll(ColumnListSpellEditorNames(), c => $"`{c}`"));

            var paramNames = new List<string>();
            var idList = new List<int>(ids);
            for (int i = 0; i < idList.Count; i++)
                paramNames.Add($"@id{i}");

            string sql = $"SELECT {columnList} FROM `{_settings.TableName}` " +
                         $"WHERE `ID` IN ({string.Join(", ", paramNames)})";

            await using var connection = new MySqlConnection(_settings.BuildConnectionString());
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            for (int i = 0; i < idList.Count; i++)
                command.Parameters.AddWithValue(paramNames[i], idList[i]);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var values = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string colName = reader.GetName(i);
                    object? val = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    values[colName] = val;
                }
                records.Add(new SpellRecord(values));
            }

            return records;
        }

        private static string[] ColumnListSpellEditorNames()
        {
            var names = new List<string>(ColumnMap.ExpectedColumnCount);
            foreach (var (seName, _) in ColumnMap.Pairs)
                names.Add(seName);
            return names.ToArray();
        }
    }
}