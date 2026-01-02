using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace AutoImportPlugin
{
    public class AutoImport : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private AutoImportSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("92c96e54-069b-4fc4-bbaa-35ac3064f85a");
        public override string Name => "AutoImport";

        public AutoImport(IPlayniteAPI api) : base(api)
        {
            settings = new AutoImportSettingsViewModel(this);
            Properties = new LibraryPluginProperties { HasSettings = true };
        }

        public override ISettings GetSettings(bool firstRunSettings) => settings;
        public override System.Windows.Controls.UserControl GetSettingsView(bool firstRunSettings) => new AutoImportSettingsView();

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            return ScanAndSelectGames();
        }

        public void RunManualImport()
        {
            var games = ScanAndSelectGames();
            if (games.Count > 0)
            {
                int addedCount = 0;
                foreach (var gameData in games)
                {
                    var result = PlayniteApi.Database.ImportGame(gameData, this);
                    if (result != null) addedCount++;
                }
                PlayniteApi.Dialogs.ShowMessage($"Successfully imported {addedCount} games.", "Scan Complete");
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return path.ToLowerInvariant().Trim().Replace("/", "\\");
        }

        private List<GameMetadata> ScanAndSelectGames()
        {
            var allFoundGames = new List<ScannedGameWrapper>();

            if (settings.Settings.ScanFolders == null) return new List<GameMetadata>();

            var blockedSet = new HashSet<string>();
            if (settings.Settings.BlockedPaths != null)
            {
                foreach (var path in settings.Settings.BlockedPaths)
                {
                    blockedSet.Add(NormalizePath(path));
                }
            }

            var existingSet = new HashSet<string>();
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.IsInstalled)
                {
                    if (!string.IsNullOrEmpty(game.InstallDirectory))
                        existingSet.Add(NormalizePath(game.InstallDirectory));

                    if (game.GameActions != null)
                    {
                        foreach (var action in game.GameActions)
                        {
                            if (action.Type == GameActionType.File && !string.IsNullOrEmpty(action.Path))
                            {
                                existingSet.Add(NormalizePath(action.Path));
                            }
                        }
                    }
                }
            }

            foreach (var folder in settings.Settings.ScanFolders)
            {
                if (Directory.Exists(folder))
                {
                    allFoundGames.AddRange(ScanFolderLimited(folder, blockedSet, existingSet));
                }
            }

            if (allFoundGames.Count == 0) return new List<GameMetadata>();

            List<GameMetadata> finalSelection = new List<GameMetadata>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new GameSelectionWindow(allFoundGames);
                if (Application.Current.MainWindow != null) window.Owner = Application.Current.MainWindow;

                if (window.ShowDialog() == true)
                {
                    finalSelection = window.SelectedGames.Select(g => g.GameData).ToList();

                    var newlyIgnored = allFoundGames
                        .Where(g => g.IsIgnored)
                        .Select(g => g.ExecutablePath)
                        .ToList();

                    if (newlyIgnored.Count > 0)
                    {
                        foreach (var path in newlyIgnored)
                        {
                            if (!settings.BlockedPathsUI.Contains(path))
                            {
                                settings.BlockedPathsUI.Add(path);
                            }
                        }
                        settings.EndEdit();
                    }
                }
            });

            return finalSelection;
        }

        private IEnumerable<ScannedGameWrapper> ScanFolderLimited(string rootPath, HashSet<string> blockedSet, HashSet<string> existingSet)
        {
            var results = new List<ScannedGameWrapper>();
            results.AddRange(GetExecutablesInDir(rootPath, blockedSet, existingSet));

            try
            {
                foreach (var subDir in Directory.GetDirectories(rootPath))
                {
                    results.AddRange(GetExecutablesInDir(subDir, blockedSet, existingSet));
                }
            }
            catch { }
            return results;
        }

        private IEnumerable<ScannedGameWrapper> GetExecutablesInDir(string dirPath, HashSet<string> blockedSet, HashSet<string> existingSet)
        {
            var list = new List<ScannedGameWrapper>();
            try
            {
                var files = Directory.GetFiles(dirPath, "*.exe");
                foreach (var file in files)
                {
                    string normalizedFile = NormalizePath(file);
                    string normalizedDir = NormalizePath(dirPath);

                    bool isIgnored = blockedSet.Contains(normalizedFile) || blockedSet.Contains(normalizedDir);
                    if (isIgnored) continue;

                    bool alreadyExists = existingSet.Contains(normalizedFile) || existingSet.Contains(normalizedDir);
                    if (alreadyExists) continue;



                    if (IsGameExecutable(file))
                    {
                        var fileInfo = new FileInfo(file);
                        var metadata = new GameMetadata
                        {
                            Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                            GameId = fileInfo.FullName.GetHashCode().ToString(),
                            InstallDirectory = fileInfo.DirectoryName,
                            IsInstalled = true,
                            Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                            Source = new MetadataNameProperty("AutoImport"),
                            GameActions = new List<GameAction>
                            {
                                new GameAction
                                {
                                    Type = GameActionType.File,
                                    Path = fileInfo.FullName,
                                    WorkingDir = fileInfo.DirectoryName,
                                    Name = "Play"
                                }
                            }
                        };
                        list.Add(new ScannedGameWrapper { GameData = metadata });
                    }
                }
            }
            catch { }
            return list;
        }

        private bool IsGameExecutable(string path)
        {
            string fileName = Path.GetFileName(path).ToLower();
            return !(fileName.Contains("uninstall") || fileName.Contains("setup") || fileName.Contains("config") || fileName.Contains("crash"));
        }
    }
}