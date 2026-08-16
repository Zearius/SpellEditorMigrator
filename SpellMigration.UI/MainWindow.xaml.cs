using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using SpellMigration.Core.Configuration;
using SpellMigration.Core.Data;
using SpellMigration.Core.Models;
using SpellMigration.Core.Validation;

namespace SpellMigration.UI
{
    /// <summary>
    /// Main workflow window: enter a spell ID, fetch it from SpellEditor's DB,
    /// run validation, map it to spell_dbc column names, preview the
    /// resulting INSERT, and optionally apply it to acore_world.
    /// 
    /// Settings are re-loaded from disk on every fetch/apply rather than
    /// cached at startup, so changes made in SettingsWindow take effect
    /// immediately without restarting the app.
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpellRecord? _mappedRecord;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private async void FetchAndValidate_Click(object sender, RoutedEventArgs e)
        {
            ResetResultState();

            if (!int.TryParse(SpellIdBox.Text.Trim(), out int spellId))
            {
                SetStatus("Enter a valid numeric spell ID.", isError: true);
                return;
            }

            var settingsData = new SettingsStore().Load();
            var seSettings = SettingsStore.ToLiveSettings(settingsData.SpellEditorSource);

            SetStatus($"Fetching spell {spellId} from SpellEditor...", isError: false);

            SpellRecord? record;
            try
            {
                var seRepo = new SpellEditorRepository(seSettings);
                record = await seRepo.GetSpellByIdAsync(spellId);
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to connect to SpellEditor DB: {ex.Message}", isError: true);
                return;
            }

            if (record is null)
            {
                SetStatus($"No spell with ID {spellId} found in SpellEditor's table.", isError: true);
                return;
            }

            SpellNameText.Text = string.IsNullOrWhiteSpace(record.Name)
                ? $"(spell {spellId})"
                : record.Name;

            var validation = SpellValidator.Validate(record);
            PopulateIssues(validation);

            if (validation.HasErrors)
            {
                SetStatus("Validation failed. Fix the source spell before mapping — see errors above.",
                          isError: true);
                return;
            }

            _mappedRecord = SpellMapper.MapToSpellDbc(record);

            var acSettings = SettingsStore.ToLiveSettings(settingsData.AcoreWorldTarget);
            var acRepo = new AcoreWorldRepository(acSettings);
            SqlPreviewBox.Text = acRepo.BuildPreviewSql(_mappedRecord);

            ApplyButton.IsEnabled = true;
            SetStatus(validation.HasWarnings
                ? "Mapped with warnings — review above before applying."
                : "Mapped cleanly. Review the preview, then Apply when ready.",
                isError: false);
        }

        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (_mappedRecord is null)
                return;

            var settingsData = new SettingsStore().Load();
            var acSettings = SettingsStore.ToLiveSettings(settingsData.AcoreWorldTarget);
            var acRepo = new AcoreWorldRepository(acSettings);

            int spellId = _mappedRecord.Id;

            bool exists;
            try
            {
                exists = await acRepo.SpellExistsAsync(spellId);
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to check acore_world: {ex.Message}", isError: true);
                return;
            }

            if (exists)
            {
                var result = MessageBox.Show(
                    $"Spell ID {spellId} already exists in {acSettings.TableName}. " +
                    "This tool does not currently support overwrite/replace — " +
                    "applying will fail with a duplicate-key error.\n\nProceed anyway?",
                    "Spell already exists",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            ApplyButton.IsEnabled = false;
            SetStatus("Applying to acore_world...", isError: false);

            try
            {
                await acRepo.ApplyAsync(_mappedRecord);
                SetStatus($"Spell {spellId} written to {acSettings.TableName}.", isError: false);
            }
            catch (Exception ex)
            {
                SetStatus($"Apply failed: {ex.Message}", isError: true);
                ApplyButton.IsEnabled = true;
            }
        }

        private void PopulateIssues(ValidationResult validation)
        {
            var display = new ObservableCollection<string>();

            if (validation.Issues.Count == 0)
            {
                display.Add("No issues found.");
            }
            else
            {
                foreach (var issue in validation.Issues)
                    display.Add(issue.ToString());
            }

            IssuesList.ItemsSource = display;
        }

        private void ResetResultState()
        {
            _mappedRecord = null;
            SpellNameText.Text = "";
            IssuesList.ItemsSource = null;
            SqlPreviewBox.Text = "";
            ApplyButton.IsEnabled = false;
        }

        private void SetStatus(string message, bool isError)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError ? Brushes.Red : Brushes.DarkGreen;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            SpellIdBox.Text = "";
            ResetResultState();
            SetStatus("", isError: false);
        }
    }
}