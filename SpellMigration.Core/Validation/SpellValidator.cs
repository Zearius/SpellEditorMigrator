using System.Collections.Generic;
using SpellMigration.Core.Mapping;
using SpellMigration.Core.Models;

namespace SpellMigration.Core.Validation
{
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    public sealed class ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public string Message { get; }

        public ValidationIssue(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public override string ToString() => $"[{Severity}] {Message}";
    }

    public sealed class ValidationResult
    {
        public List<ValidationIssue> Issues { get; } = new();

        public bool HasErrors => Issues.Exists(i => i.Severity == ValidationSeverity.Error);
        public bool HasWarnings => Issues.Exists(i => i.Severity == ValidationSeverity.Warning);

        /// <summary>True only if there are zero Errors. Warnings do not block
        /// mapping/apply — they're surfaced to the user for a manual call,
        /// the same way we caught Frozen Hurricane's trigger dependency and
        /// the passive/proc mismatch by inspection rather than a hard stop.</summary>
        public bool IsValid => !HasErrors;

        public void AddError(string message) =>
            Issues.Add(new ValidationIssue(ValidationSeverity.Error, message));

        public void AddWarning(string message) =>
            Issues.Add(new ValidationIssue(ValidationSeverity.Warning, message));
    }

    /// <summary>
    /// Encodes the specific failure patterns found during manual testing of
    /// the SpellEditor -> spell_dbc migration: a dropped-value column-count
    /// mismatch (Prestillence's first export), a half-configured secondary
    /// effect slot (Prestillence/Soul Tear's bad template), and an
    /// EffectTriggerSpell reference that needs to be confirmed to exist
    /// (Frozen Hurricane -> 42209, Cosmic Infusion -> 9800).
    /// </summary>
    public static class SpellValidator
    {
        private const int SpellAttr0Passive = 0x40;

        public static ValidationResult Validate(SpellRecord record)
        {
            var result = new ValidationResult();

            CheckColumnCount(record, result);
            CheckStrayEffect(record, result, effectIndex: 2);
            CheckStrayEffect(record, result, effectIndex: 3);
            CheckDanglingTriggerSpells(record, result);
            CheckProcPassiveCoherence(record, result);

            return result;
        }

        /// <summary>Hard failure: a record with the wrong number of populated
        /// columns cannot be trusted to map correctly at all. This is the
        /// check that would have caught the Prestillence bug (232 values
        /// into a 234-column table) before it ever reached spell_dbc.</summary>
        private static void CheckColumnCount(SpellRecord record, ValidationResult result)
        {
            if (record.ColumnCount != ColumnMap.ExpectedColumnCount)
            {
                result.AddError(
                    $"Spell {record.Id}: expected {ColumnMap.ExpectedColumnCount} columns, " +
                    $"found {record.ColumnCount}. Do not map or apply this record — " +
                    "a count mismatch means every field after the gap is silently shifted.");
            }
        }

        /// <summary>Warning: an effect slot with a non-zero Effect type but no
        /// target and no base points is very likely a leftover default from
        /// whatever base template was duplicated in SpellEditor, not an
        /// intentional second effect.</summary>
        private static void CheckStrayEffect(SpellRecord record, ValidationResult result, int effectIndex)
        {
            string effectCol = $"Effect{effectIndex}";
            string targetACol = $"EffectImplicitTargetA{effectIndex}";
            string basePointsCol = $"EffectBasePoints{effectIndex}";

            if (!record.HasColumn(effectCol))
                return;

            int effectType = record.GetValue<int>(effectCol);
            if (effectType == 0)
                return;

            int targetA = record.GetValue<int>(targetACol);
            int basePoints = record.GetValue<int>(basePointsCol);

            if (targetA == 0 && basePoints == 0)
            {
                result.AddWarning(
                    $"Spell {record.Id}: {effectCol} = {effectType} but {targetACol} and " +
                    $"{basePointsCol} are both 0. This looks like a stray, half-configured " +
                    "effect slot carried over from a base template rather than an intentional " +
                    "effect — confirm before relying on it.");
            }
        }

        /// <summary>Warning: EffectTriggerSpell references cannot be verified
        /// to exist from this record alone. Flag every non-zero trigger so
        /// the caller can confirm the target spell is present in acore_world
        /// before applying — a silently-missing trigger spell means the proc
        /// or periodic effect fires and does nothing.</summary>
        private static void CheckDanglingTriggerSpells(SpellRecord record, ValidationResult result)
        {
            for (int i = 1; i <= 3; i++)
            {
                string col = $"EffectTriggerSpell{i}";
                if (!record.HasColumn(col))
                    continue;

                int triggerId = record.GetValue<int>(col);
                if (triggerId != 0)
                {
                    result.AddWarning(
                        $"Spell {record.Id}: {col} = {triggerId}. Confirm spell {triggerId} " +
                        "exists in acore_world before applying — this record depends on it.");
                }
            }
        }

        /// <summary>Warning: proc configuration on a non-passive spell is
        /// unusual and worth a second look — most custom procs are meant to
        /// be passively-attached auras (SPELL_ATTR0_PASSIVE), not something
        /// the player casts directly.</summary>
        private static void CheckProcPassiveCoherence(SpellRecord record, ValidationResult result)
        {
            int procChance = record.GetValue<int>("ProcChance");
            int procTypeMask = record.GetValue<int>("ProcTypeMask");

            if (procChance == 0 && procTypeMask == 0)
                return;

            int attributes = record.GetValue<int>("Attributes");
            bool isPassive = (attributes & SpellAttr0Passive) != 0;

            if (!isPassive)
            {
                result.AddWarning(
                    $"Spell {record.Id}: ProcChance/ProcTypeMask are set but " +
                    "SPELL_ATTR0_PASSIVE (0x40) is not set on Attributes. Most proc-based " +
                    "spells are passive auras — confirm this is intentional.");
            }
        }
    }
}