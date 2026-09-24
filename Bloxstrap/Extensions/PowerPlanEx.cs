namespace Bloxstrap.Extensions
{
    static class PowerPlanEx
    {
        public static IReadOnlyCollection<PowerPlan> Selections => new PowerPlan[]
        {
            PowerPlan.Disabled,
            PowerPlan.HighPerformance,
            PowerPlan.UltimatePerformance,
            PowerPlan.Balanced,
            PowerPlan.PowerSaver
        };

        public static string? GetSchemeGuid(this PowerPlan powerPlan) => powerPlan switch
        {
            PowerPlan.HighPerformance => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
            PowerPlan.UltimatePerformance => "e9a42b02-d5df-448d-aa00-03f14749eb61",
            PowerPlan.Balanced => "381b4222-f694-41f0-9685-ff5bb260df2e",
            PowerPlan.PowerSaver => "a1841308-3541-4fab-bc81-f71556f20b4a",
            _ => null
        };
    }
}