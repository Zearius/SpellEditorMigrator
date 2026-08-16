using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpellMigration.Core.Configuration
{
    /// <summary>
    /// Reads and writes AppSettingsData to a JSON file under the current
    /// user's AppData\Roaming folder — NOT next to the executable. An app
    /// installed to Program Files typically cannot write to its own install
    /// directory without elevation, so settings must live somewhere the
    /// user's account can always write to regardless of install location.
    /// 
    /// The password field is encrypted with Windows DPAPI (CurrentUser
    /// scope) before being written to disk, and decrypted only in memory
    /// when building a live DatabaseConnectionSettings for a repository to
    /// use. DPAPI keys are tied to the Windows user account, so this file
    /// is not portable between machines/users by design — that's expected
    /// for this kind of protection.
    /// </summary>
    public sealed class SettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly string _filePath;

        public SettingsStore()
        {
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpellMigrationTool");
            Directory.CreateDirectory(appDataDir);
            _filePath = Path.Combine(appDataDir, "settings.json");
        }

        /// <summary>Explicit-path constructor, mainly for testing without
        /// touching the real AppData folder.</summary>
        public SettingsStore(string filePath)
        {
            _filePath = filePath;
        }

        public string FilePath => _filePath;

        /// <summary>Loads settings from disk, or returns defaults if the
        /// file doesn't exist yet (e.g. first run).</summary>
        public AppSettingsData Load()
        {
            if (!File.Exists(_filePath))
                return new AppSettingsData();

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettingsData>(json, JsonOptions)
                   ?? new AppSettingsData();
        }

        public void Save(AppSettingsData data)
        {
            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(_filePath, json);
        }

        /// <summary>Converts a persisted profile (encrypted password) into a
        /// live DatabaseConnectionSettings (decrypted password, in memory
        /// only) for a repository to actually connect with.</summary>
        public static DatabaseConnectionSettings ToLiveSettings(PersistedConnectionSettings persisted)
        {
            return new DatabaseConnectionSettings
            {
                Server = persisted.Server,
                Port = persisted.Port,
                Database = persisted.Database,
                UserId = persisted.UserId,
                TableName = persisted.TableName,
                Password = DecryptPassword(persisted.EncryptedPassword)
            };
        }

        /// <summary>Converts a live settings object (e.g. freshly entered in
        /// the Settings UI) into its persisted form, encrypting the password
        /// before it's serialized to disk.</summary>
        public static PersistedConnectionSettings ToPersisted(DatabaseConnectionSettings live)
        {
            return new PersistedConnectionSettings
            {
                Server = live.Server,
                Port = live.Port,
                Database = live.Database,
                UserId = live.UserId,
                TableName = live.TableName,
                EncryptedPassword = EncryptPassword(live.Password)
            };
        }

        private static string EncryptPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
                return "";

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainPassword);
            byte[] encryptedBytes = ProtectedData.Protect(
                plainBytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        private static string DecryptPassword(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64))
                return "";

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
                byte[] plainBytes = ProtectedData.Unprotect(
                    encryptedBytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                // Encrypted under a different Windows user/machine, or the
                // file was hand-edited/corrupted. Fail safe: treat as no
                // password rather than crashing settings load.
                return "";
            }
        }
    }
}