namespace SpellMigration.Core.Configuration
{
    /// <summary>
    /// Connection details for a single MySQL database. Two instances of this
    /// exist in a typical setup: one pointing at SpellEditor's DB (schema
    /// name varies per user — e.g. "CustomSpells"), and one pointing at
    /// AzerothCore's world DB (commonly "acore_world", but not guaranteed).
    /// 
    /// Table names for SpellEditor's `spell` table and AzerothCore's
    /// `spell_dbc` table are NOT hardcoded as string literals elsewhere in
    /// Core — they're read from here — because SpellEditor's export tooling
    /// has been observed naming its own table both "spell" and "spell_dbc"
    /// depending on export settings, so this must not be assumed fixed.
    /// </summary>
    public sealed class DatabaseConnectionSettings
    {
        public string Server { get; set; } = "127.0.0.1";
        public uint Port { get; set; } = 3306;
        public string Database { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Password { get; set; } = "";

        /// <summary>The spell table name within this database. Defaults to
        /// "spell" for a SpellEditor source; set to "spell_dbc" for an
        /// AzerothCore world DB target.</summary>
        public string TableName { get; set; } = "spell";

        public string BuildConnectionString()
        {
            var builder = new MySqlConnector.MySqlConnectionStringBuilder
            {
                Server = Server,
                Port = Port,
                Database = Database,
                UserID = UserId,
                Password = Password
            };
            return builder.ConnectionString;
        }
    }
}