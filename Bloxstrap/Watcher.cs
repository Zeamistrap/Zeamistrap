using Bloxstrap.AppData;
using Bloxstrap.Integrations;

namespace Bloxstrap
{
    public class Watcher : IDisposable
    {
        private readonly InterProcessLock _lock = new("Watcher");
        private readonly CancellationTokenSource _disposeCancellation = new();

        private readonly WatcherData? _watcherData;
        
        private readonly NotifyIconWrapper? _notifyIcon;

        public readonly ActivityWatcher? ActivityWatcher;

        public readonly WindowManipulation? WindowManipulation;

        public readonly DiscordRichPresence? RichPresence;

        public Watcher()
        {
            const string LOG_IDENT = "Watcher";


            if (!_lock.IsAcquired)
            {
                App.Logger.WriteLine(LOG_IDENT, "Watcher instance already exists");
                return;
            }

            string? watcherDataArg = App.LaunchSettings.WatcherFlag.Data;

            if (String.IsNullOrEmpty(watcherDataArg))
            {
#if DEBUG
                string path = new RobloxPlayerData().ExecutablePath;
                if (!File.Exists(path))
                    throw new ApplicationException("Roblox player is not been installed");

                using var gameClientProcess = Process.Start(path);

                while (gameClientProcess.MainWindowHandle == IntPtr.Zero)
                    Thread.Sleep(100);

                _watcherData = new() { ProcessId = gameClientProcess.Id, Handle = gameClientProcess.MainWindowHandle.ToInt64() };
#else
                throw new Exception("Watcher data not specified");
#endif
            }
            else
            {
                _watcherData = JsonSerializer.Deserialize<WatcherData>(Encoding.UTF8.GetString(Convert.FromBase64String(watcherDataArg)));
            }

            if (_watcherData is null)
                throw new Exception("Watcher data is invalid");

            if (App.Settings.Prop.EnableWindowManipulation && _watcherData.Handle != 0)
                WindowManipulation = new(_watcherData.Handle, _watcherData.ProcessId);

            if (App.Settings.Prop.EnableActivityTracking)
            {
                ActivityWatcher = new(_watcherData.LogFile);

                if (App.Settings.Prop.UseDisableAppPatch)
                {
                    ActivityWatcher.OnAppClose += delegate
                    {
                        App.Logger.WriteLine(LOG_IDENT, "Received desktop app exit, closing Roblox");
                        using var process = Process.GetProcessById(_watcherData.ProcessId);
                        process.CloseMainWindow();
                    };
                }

                if (App.Settings.Prop.UseDiscordRichPresence && !App.State.Prop.WatcherRunning)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Running rpc");
                    RichPresence = new(ActivityWatcher);
                }
            }

            _notifyIcon = new(this);
        }

        public void KillRobloxProcess() => CloseProcess(_watcherData!.ProcessId, true);

        private static Process? GetProcessIfRunning(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);

                if (process.HasExited)
                {
                    process.Dispose();
                    return null;
                }

                return process;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public void CloseProcess(int pid, bool force = false)
        {
            const string LOG_IDENT = "Watcher::CloseProcess";

            try
            {
                using var process = Process.GetProcessById(pid);

                App.Logger.WriteLine(LOG_IDENT, $"Killing process '{process.ProcessName}' (pid={pid}, force={force})");

                if (process.HasExited)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"PID {pid} has already exited");
                    return;
                }

                if (force)
                    process.Kill();
                else
                    process.CloseMainWindow();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"PID {pid} could not be closed");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public async Task Run()
        {
            if (!_lock.IsAcquired || _watcherData is null)
                return;

            Task? activityTask = ActivityWatcher?.StartAsync();

            try
            {
                WindowManipulation?.Start();

                using Process? process = GetProcessIfRunning(_watcherData.ProcessId);

                if (process is not null)
                {
                    try
                    {
                        await process.WaitForExitAsync(_disposeCancellation.Token);
                    }
                    catch (OperationCanceledException) when (_disposeCancellation.IsCancellationRequested)
                    {
                        // Normal shutdown path.
                    }
                }
            }
            finally
            {
                ActivityWatcher?.Dispose();

                if (activityTask is not null)
                {
                    try
                    {
                        await activityTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal shutdown path.
                    }
                    catch (ObjectDisposedException)
                    {
                        // Normal shutdown path.
                    }
                }
            }

            if (_watcherData.AutoclosePids is not null)
            {
                foreach (int pid in _watcherData.AutoclosePids)
                    CloseProcess(pid);
            }

            if (App.LaunchSettings.TestModeFlag.Active)
                Process.Start(Paths.Process, "-settings -testmode");
        }

        public void Dispose()
        {
            App.Logger.WriteLine("Watcher::Dispose", "Disposing Watcher");

            _disposeCancellation.Cancel();
            ActivityWatcher?.Dispose();
            _notifyIcon?.Dispose();
            RichPresence?.Dispose();
            _lock.Dispose();

            App.State.Prop.WatcherRunning = false;

            GC.SuppressFinalize(this);
        }
    }
}
