using System;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class PerformanceViewModel : NotifyPropertyChangedViewModel
    {
        public BehaviourViewModel Behaviour { get; } = new();

        public IEnumerable<PowerPlan> PowerPlans { get; } = PowerPlanEx.Selections;

        public PowerPlan SelectedPowerPlan
        {
            get => App.Settings.Prop.PowerPlan == PowerPlan.Disabled
                ? PowerPlan.Balanced
                : App.Settings.Prop.PowerPlan;
            set => App.Settings.Prop.PowerPlan = value;
        }

        public ICommand ApplyPowerPlanCommand => new RelayCommand(ApplyPowerPlan);

        private void ApplyPowerPlan()
        {
            const string LOG_IDENT = "PerformanceViewModel::ApplyPowerPlan";

            App.Logger.WriteLine(LOG_IDENT, $"Applying power plan '{SelectedPowerPlan}'");
            SelectedPowerPlan.TryApply();
        }
    }
}