using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class MainWindowViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand SaveSettingsCommand => new RelayCommand(SaveSettings);

        public ICommand SaveAndLaunchSettingsCommand => new RelayCommand(SaveAndLaunchSettings);

        public ICommand CloseWindowCommand => new RelayCommand(CloseWindow);

        public EventHandler? RequestSaveNoticeEvent;

        public EventHandler? RequestCloseWindowEvent;

        public bool GBSEnabled = App.GlobalSettings.Loaded;

        public bool TestModeEnabled
        {
            get => App.LaunchSettings.TestModeFlag.Active;
            set
            {
                if (value && !App.State.Prop.TestModeWarningShown)
                {
                    var result = Frontend.ShowMessageBox(Strings.Menu_TestMode_Prompt, MessageBoxImage.Information, MessageBoxButton.YesNo);

                    if (result != MessageBoxResult.Yes)
                        return;

                    App.State.Prop.TestModeWarningShown = true;
                }

                App.LaunchSettings.TestModeFlag.Active = value;
            }
        }

        private void CloseWindow() => RequestCloseWindowEvent?.Invoke(this, EventArgs.Empty);

        private bool TrySaveSettings()
        {
            const string LOG_IDENT = "MainWindowViewModel::SaveSettings";

            bool settingsSaved = App.Settings.Save();
            bool stateSaved = App.State.Save();
            bool flagsSaved = App.FastFlags.Save();

            if (!settingsSaved || !stateSaved || !flagsSaved)
            {
                App.Logger.WriteLine(LOG_IDENT, "One or more settings files could not be saved; pending tasks were not executed.");
                return false;
            }

            App.GlobalSettings.Save();

            foreach (var pair in App.PendingSettingTasks)
            {
                var task = pair.Value;

                if (task.Changed)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Executing pending task '{task}'");
                    task.Execute();
                }
            }

            App.PendingSettingTasks.Clear();

            RequestSaveNoticeEvent?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private void SaveSettings()
        {
            TrySaveSettings();
        }

        public void SaveAndLaunchSettings()
        {
            if (!TrySaveSettings())
                return;

            if (!App.LaunchSettings.TestModeFlag.Active) // test mode already launches an instance
                Process.Start(Paths.Application, "-player");
            else
                CloseWindow();
        }
    }
}
