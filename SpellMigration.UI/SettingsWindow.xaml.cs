using MySqlConnector;
using SpellMigration.Core.Configuration;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SpellMigration.UI
{
    /// <summary>
    /// Lets the user view/edit both database connection profiles and
    /// persist them via SettingsStore. "Test" buttons open a real
    /// connection with the CURRENT (possibly unsaved) field values, so the
    /// user can validate before committing to disk.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsStore _store;
        private AppSettingsData _data;

        public SettingsWindow()
        {
            InitializeComponent();
            _store = new SettingsStore();
            _data = _store.Load();
            PopulateFields();
        }

        private void PopulateFields()
        {
            var se = _data.SpellEditorSource;
            SeServerBox.Text = se.Server;
            SePortBox.Text = se.Port.ToString();
            SeDatabaseBox.Text = se.Database;
            SeTableBox.Text = se.TableName;
            SeUserBox.Text = se.UserId;
            SePasswordBox.Password = DecryptForDisplay(se.EncryptedPassword);

            var ac = _data.AcoreWorldTarget;
            AcServerBox.Text = ac.Server;
            AcPortBox.Text = ac.Port.ToString();
            AcDatabaseBox.Text = ac.Database;
            AcTableBox.Text = ac.TableName;
            AcUserBox.Text = ac.UserId;
            AcPasswordBox.Password = DecryptForDisplay(ac.EncryptedPassword);
        }

        private static string DecryptForDisplay(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return "";
            var temp = new PersistedConnectionSettings { EncryptedPassword = encryptedPassword };
            return SettingsStore.ToLiveSettings(temp).Password;
        }

        private DatabaseConnectionSettings ReadSpellEditorFields()
        {
            return new DatabaseConnectionSettings
            {
                Server = SeServerBox.Text.Trim(),
                Port = ParsePort(SePortBox.Text),
                Database = SeDatabaseBox.Text.Trim(),
                TableName = string.IsNullOrWhiteSpace(SeTableBox.Text) ? "spell" : SeTableBox.Text.Trim(),
                UserId = SeUserBox.Text.Trim(),
                Password = SePasswordBox.Password
            };
        }

        private DatabaseConnectionSettings ReadAcoreWorldFields()
        {
            return new DatabaseConnectionSettings
            {
                Server = AcServerBox.Text.Trim(),
                Port = ParsePort(AcPortBox.Text),
                Database = AcDatabaseBox.Text.Trim(),
                TableName = string.IsNullOrWhiteSpace(AcTableBox.Text) ? "spell_dbc" : AcTableBox.Text.Trim(),
                UserId = AcUserBox.Text.Trim(),
                Password = AcPasswordBox.Password
            };
        }

        private static uint ParsePort(string text) =>
            uint.TryParse(text, out var port) ? port : 3306;

        private async void TestSpellEditor_Click(object sender, RoutedEventArgs e)
        {
            await TestConnectionAsync(ReadSpellEditorFields(), "SpellEditor");
        }

        private async void TestAcoreWorld_Click(object sender, RoutedEventArgs e)
        {
            await TestConnectionAsync(ReadAcoreWorldFields(), "AzerothCore world DB");
        }

        private async System.Threading.Tasks.Task TestConnectionAsync(
            DatabaseConnectionSettings settings, string label)
        {
            StatusText.Text = $"Testing {label}...";
            try
            {
                await using var connection = new MySqlConnection(settings.BuildConnectionString());
                await connection.OpenAsync();
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = $"{label}: connected successfully.";
            }
            catch (Exception ex)
            {
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                StatusText.Text = $"{label}: {ex.Message}";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var seLive = ReadSpellEditorFields();
            var acLive = ReadAcoreWorldFields();

            _data.SpellEditorSource = SettingsStore.ToPersisted(seLive);
            _data.AcoreWorldTarget = SettingsStore.ToPersisted(acLive);

            _store.Save(_data);
            DialogResult = true;
            Close();
        }
    }
}