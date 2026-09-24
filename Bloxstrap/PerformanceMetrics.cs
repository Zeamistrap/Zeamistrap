using System.Diagnostics;

namespace Bloxstrap
{
    /// <summary>
    /// Lightweight process/runtime measurements for launch and update diagnostics.
    /// Values are emitted as key=value pairs so logs can be aggregated externally.
    /// </summary>
    public static class PerformanceMetrics
    {
        public static void Mark(string stage, long startedAt)
        {
            double durationMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            Write(stage, durationMs);
        }

        public static void Snapshot(string stage)
        {
            Write(stage, null);
        }

        private static void Write(string stage, double? durationMs)
        {
            string resources = "unavailable";

            try
            {
                using var process = Process.GetCurrentProcess();
                process.Refresh();

                resources = FormattableString.Invariant(
                    $"working_set_mb={process.WorkingSet64 / 1024d / 1024d:F2};private_mb={process.PrivateMemorySize64 / 1024d / 1024d:F2};handles={process.HandleCount};threads={process.Threads.Count}").ToString();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("Performance::Snapshot", ex);
            }

            App.Logger.WriteLine(
                "Performance",
                FormattableString.Invariant(
                    $"stage={stage};duration_ms={(durationMs.HasValue ? durationMs.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : "-1")};{resources}").ToString());
        }
    }
}
