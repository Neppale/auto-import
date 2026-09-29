using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input; 

namespace AutoImportPlugin
{
    public class AutoImportSettings : ObservableObject
    {
        private List<string> scanFolders = new List<string>();
        public List<string> ScanFolders { get => scanFolders; set => SetValue(ref scanFolders, value); }

        private List<string> blockedPaths = new List<string>();
        public List<string> BlockedPaths
        {
            get => blockedPaths;
            set => SetValue(ref blockedPaths, value);
        }
    }


    public class AutoImportSettingsViewModel : ObservableObject, ISettings
    {
        private readonly AutoImport plugin;
        private AutoImportSettings editingClone { get; set; }

        private AutoImportSettings settings;
        public AutoImportSettings Settings
        {
            get => settings;
            set { settings = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> BlockedPathsUI { get; set; } = new ObservableCollection<string>();

        private string newBlockEntry;
        public string NewBlockEntry { get => newBlockEntry; set => SetValue(ref newBlockEntry, value); }

        public RelayCommand<string> RemoveBlockCommand { get; }
        public RelayCommand AddBlockCommand { get; }

        public AutoImportSettingsViewModel(AutoImport plugin)
        {
            this.plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<AutoImportSettings>();
            Settings = savedSettings ?? new AutoImportSettings();

            foreach (var path in Settings.BlockedPaths) BlockedPathsUI.Add(path);

            RemoveBlockCommand = new RelayCommand<string>((path) =>
            {
                if (path != null && BlockedPathsUI.Contains(path))
                {
                    BlockedPathsUI.Remove(path);
                }
            });

            AddBlockCommand = new RelayCommand(() =>
            {
                var entry = NewBlockEntry?.Trim();
                if (string.IsNullOrEmpty(entry) || BlockedPathsUI.Contains(entry)) return;

                if (!Path.IsPathRooted(entry))
                {
                    try { _ = new Regex(entry, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)); }
                    catch (ArgumentException)
                    {
                        plugin.PlayniteApi.Dialogs.ShowErrorMessage($"Invalid pattern: {entry}", "AutoImport");
                        return;
                    }
                }

                BlockedPathsUI.Add(entry);
                NewBlockEntry = string.Empty;
            });
        }

        public void BeginEdit() { editingClone = Serialization.GetClone(Settings); }
        public void CancelEdit()
        {
            Settings = editingClone;
            BlockedPathsUI.Clear();
            foreach (var path in Settings.BlockedPaths) BlockedPathsUI.Add(path);
        }

        public void EndEdit()
        {
            Settings.BlockedPaths = BlockedPathsUI.ToList();
            plugin.SavePluginSettings(Settings);
        }
        public bool VerifySettings(out List<string> errors) { errors = null; return true; }
    }

    public class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string> list)
            {
                return string.Join(Environment.NewLine, list);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .Distinct()
                           .ToList();
            }
            return new List<string>();
        }
    }
}