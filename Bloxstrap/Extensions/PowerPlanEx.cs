using System.ComponentModel;

namespace Bloxstrap.Extensions
{
    static class PowerPlanEx
    {
        private const string LOG_IDENT = "PowerPlanEx::TryApply";

        public static IReadOnlyCollection<PowerPlan> Selections => new PowerPlan[]
        {
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

        public static bool TryApply(this PowerPlan powerPlan)
        {
            if (powerPlan == PowerPlan.Disabled)
                return false;

            string? schemeGuid = powerPlan.GetSchemeGuid();
            if (schemeGuid is null)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Power plan '{powerPlan}' has no known scheme GUID");
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = $"/setactive {schemeGuid}",
                UseShellExecute = true
            };

            if (!Utilities.IsAdministrator)
                startInfo.Verb = "runas";

            try
            {
                Process.Start(startInfo);
                App.Logger.WriteLine(LOG_IDENT, $"Set active power plan to '{powerPlan}'");
                return true;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                App.Logger.WriteLine(LOG_IDENT, "Power plan change was cancelled (UAC prompt dismissed)");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return false;
        }
    }
}