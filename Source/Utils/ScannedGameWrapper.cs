using System.ComponentModel;
using System.Runtime.CompilerServices;
using Playnite.SDK.Models;

namespace AutoImportPlugin
{
    public class ScannedGameWrapper : INotifyPropertyChanged
    {
        private bool isSelected = false;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged();

                    if (isSelected) IsIgnored = false;
                }
            }
        }

        private bool isIgnored = false;
        public bool IsIgnored
        {
            get => isIgnored;
            set
            {
                if (isIgnored != value)
                {
                    isIgnored = value;
                    OnPropertyChanged();

                    if (isIgnored) IsSelected = false;
                }
            }
        }

        public GameMetadata GameData { get; set; }
        public string Name => GameData?.Name;

        public string ExecutablePath
        {
            get
            {
                if (GameData?.GameActions != null && GameData.GameActions.Count > 0)
                    return GameData.GameActions[0].Path;
                return "Unknown Path";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}