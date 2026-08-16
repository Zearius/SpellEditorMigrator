using System;
using System.Collections.Generic;

namespace SpellMigration.Core.Models
{
    /// <summary>
    /// Represents a single spell's data as a column-name-keyed bag of values.
    /// Used for both SpellEditor-sourced rows (keyed by SpellEditor column names)
    /// and spell_dbc-mapped rows (keyed by spell_dbc column names) — the same
    /// shape works for both sides of the pipeline, just with different keys.
    /// 
    /// A Dictionary is used instead of 234 discrete properties because the
    /// column set is defined by ColumnMap, not by this class, and because most
    /// consumers (validator, mapper, SQL writer) just need to iterate or look
    /// up by name rather than touch fields individually.
    /// </summary>
    public sealed class SpellRecord
    {
        private readonly Dictionary<string, object?> _values;

        public SpellRecord()
        {
            _values = new Dictionary<string, object?>();
        }

        public SpellRecord(IDictionary<string, object?> values)
        {
            _values = new Dictionary<string, object?>(values);
        }

        /// <summary>Number of columns currently populated on this record.</summary>
        public int ColumnCount => _values.Count;

        public IReadOnlyDictionary<string, object?> Values => _values;

        public object? this[string columnName]
        {
            get => _values.TryGetValue(columnName, out var v) ? v : null;
            set => _values[columnName] = value;
        }

        public bool HasColumn(string columnName) => _values.ContainsKey(columnName);

        /// <summary>Strongly-typed accessor. Handles the common case where a
        /// numeric value came back from MySqlConnector as a different numeric
        /// type than expected (e.g. long vs int), which is a frequent source
        /// of silent bugs when reading raw ADO.NET results.</summary>
        public T GetValue<T>(string columnName, T defaultValue = default!)
        {
            if (!_values.TryGetValue(columnName, out var raw) || raw is null || raw is DBNull)
                return defaultValue;

            if (raw is T typed)
                return typed;

            try
            {
                return (T)Convert.ChangeType(raw, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>Spell ID convenience accessor — every record needs this
        /// constantly (logging, lookups, error messages).</summary>
        public int Id => GetValue<int>("ID");

        /// <summary>Human-readable spell name, if the record has one under
        /// either the SpellEditor or spell_dbc naming convention.</summary>
        public string? Name =>
            GetValue<string?>("SpellName0") ?? GetValue<string?>("Name_Lang_enUS");
    }
}