using System;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class PerformanceViewModel : NotifyPropertyChangedViewModel
    {
        public BehaviourViewModel Behaviour { get; } = new();

        public IEnumerable<PowerPlan> PowerPlans { get; } = PowerPlanEx.Selections;

        public PowerPlan SelectedPowerPlan
        {
            get => App.Settings.Prop.PowerPlan;
            set => App.Settings.Prop.PowerPlan = value;
        }
    }
}