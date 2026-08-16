using SpellMigration.Core.Mapping;
using SpellMigration.Core.Models;

namespace SpellMigration.Core.Data
{
    /// <summary>
    /// Translates a SpellEditor-keyed SpellRecord (from SpellEditorRepository)
    /// into a spell_dbc-keyed SpellRecord (for AcoreWorldRepository), using
    /// the verified 1:1 column mapping in ColumnMap. This is a pure,
    /// side-effect-free transform — no DB access, no validation — so it can
    /// be unit tested trivially and reused anywhere a translated record is
    /// needed.
    /// 
    /// Values are copied as-is; nothing is renamed, transformed, or
    /// reinterpreted here. Every SpellEditor -> spell_dbc pair confirmed
    /// during testing (Deep Ice Bolt, Frozen Hurricane, Soul Tear,
    /// Restorative Grasp, Cosmic Infusion) was a straight value copy under
    /// a renamed column — no field required actual value translation.
    /// </summary>
    public static class SpellMapper
    {
        /// <summary>Maps a single SpellEditor-keyed record to a spell_dbc-keyed
        /// record. Run SpellValidator against the SOURCE record before calling
        /// this — mapping a record that already failed validation (e.g. wrong
        /// column count) just carries the corruption forward under new names.</summary>
        public static SpellRecord MapToSpellDbc(SpellRecord spellEditorRecord)
        {
            var mapped = new SpellRecord();

            foreach (var (seName, dbcName) in ColumnMap.Pairs)
            {
                mapped[dbcName] = spellEditorRecord[seName];
            }

            return mapped;
        }
    }
}