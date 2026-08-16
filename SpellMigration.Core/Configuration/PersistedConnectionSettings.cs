namespace SpellMigration.Core.Configuration
{
    /// <summary>
    /// The on-disk representation of a DatabaseConnectionSettings profile.
    /// Everything is plaintext except the password, which is stored as a
    /// DPAPI-encrypted, base64-encoded blob (see SettingsStore) rather than
    /// as raw text — this is a niche community tool, not a security product,
    /// but there's no reason to leave a MySQL password sitting in plaintext
    /// JSON when encrypting it costs almost nothing.
    /// </summary>
    public sealed class PersistedConnectionSettings
    {
        public string Server { get; set; } = "127.0.0.1";
        public uint Port { get; set; } = 3306;
        public string Database { get; set; } = "";
        public string UserId { get; set; } = "";
        public string TableName { get; set; } = "spell";

        /// <summary>Base64-encoded, DPAPI-protected password. Never the raw
        /// password. Empty string if no password has been set yet.</summary>
        public string EncryptedPassword { get; set; } = "";
    }

    /// <summary>The full persisted settings file: one profile for the
    /// SpellEditor source database, one for the AzerothCore world DB
    /// target.</summary>
    public sealed class AppSettingsData
    {
        public PersistedConnectionSettings SpellEditorSource { get; set; } = new()
        {
            TableName = "spell"
        };

        public PersistedConnectionSettings AcoreWorldTarget { get; set; } = new()
        {
            TableName = "spell_dbc"
        };
    }
}