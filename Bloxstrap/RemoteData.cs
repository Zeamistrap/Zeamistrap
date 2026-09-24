using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Bloxstrap
{
    public class RemoteDataManager : JsonManager<RemoteDataBase>
    {
        public override string ClassName => nameof(RemoteDataManager);

        public override string LOG_IDENT_CLASS => ClassName;

        public override string FileLocation => Path.Combine(Paths.Base, "Data.json");

        public bool Changed => !OriginalProp.Equals(Prop);

        public GenericTriState LoadedState = GenericTriState.Unknown;

        private readonly TaskCompletionSource<GenericTriState> _loadedCompletion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _stateLock = new();

        public event EventHandler DataLoaded = null!;

        public void Subscribe(EventHandler Handler)
        {
            bool invokeImmediately;

            lock (_stateLock)
            {
                invokeImmediately = LoadedState != GenericTriState.Unknown;

                if (!invokeImmediately)
                    DataLoaded += Handler;
            }

            if (invokeImmediately)
                InvokeOnDispatcher(Handler);
        }

        private void InvokeOnDispatcher(EventHandler handler)
        {
            void Invoke()
            {
                try
                {
                    handler(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("RemoteDataManager::DataLoaded", ex);
                }
            }

            var dispatcher = System.Windows.Application.Current?.Dispatcher;

            if (dispatcher is null || dispatcher.CheckAccess())
            {
                Invoke();
                return;
            }

            try
            {
                dispatcher.BeginInvoke((Action)Invoke);
            }
            catch (InvalidOperationException)
            {
                // The UI is shutting down; there is no useful notification target.
            }
        }

        private void InvokeHandlers()
        {
            EventHandler? handlers;

            lock (_stateLock)
                handlers = DataLoaded;

            if (handlers is null)
                return;

            foreach (EventHandler handler in handlers.GetInvocationList())
                InvokeOnDispatcher(handler);
        }

        private void SetLoadedState(GenericTriState state)
        {
            lock (_stateLock)
                LoadedState = state;
        }

        public async Task<RemoteDataBase> GetSnapshot()
        {
            await WaitUntilDataFetched();

            lock (_stateLock)
                return Prop;
        }

        public async Task WaitUntilDataFetched()
        {
            lock (_stateLock)
            {
                if (LoadedState != GenericTriState.Unknown)
                    return;
            }

            Task timeout = Task.Delay(TimeSpan.FromSeconds(3));
            Task completed = await Task.WhenAny(_loadedCompletion.Task, timeout);

            if (completed == _loadedCompletion.Task)
                await _loadedCompletion.Task;
        }

        private static void Validate(RemoteDataBase data)
        {
            if (data.PackageMaps is null ||
                data.PackageMaps.CommonPackageMap is null ||
                data.PackageMaps.PlayerPackageMap is null ||
                data.PackageMaps.StudioPackageMap is null ||
                data.IgnoredPackages is null ||
                data.PackageMaps.CommonPackageMap.Any(x => x.Value is null) ||
                data.PackageMaps.PlayerPackageMap.Any(x => x.Value is null) ||
                data.PackageMaps.StudioPackageMap.Any(x => x.Value is null))
            {
                throw new JsonException("Remote data is missing required package map fields.");
            }
        }

        private void LoadLocalFallback(string logIdentifier)
        {
            this.Load(false);

            try
            {
                Validate(Prop);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(logIdentifier, "Local data is invalid; using defaults.");
                App.Logger.WriteException(logIdentifier, ex);
                Prop = new RemoteDataBase();
            }
        }

        // remember that our data isnt necessary, we can fetch it in the background 
        public async Task LoadData()
        {
            const string LOG_IDENT = $"{nameof(RemoteDataManager)}::LoadData";
            if (App.Settings.Prop.ForceLocalData || App.LaunchSettings.WatcherFlag.Active)
            {
                App.Logger.WriteLine(LOG_IDENT, "Force loading local data");
                LoadLocalFallback(LOG_IDENT);

                SetLoadedState(GenericTriState.Successful); // we treat it as successful to simulate the production data
            }
            else
            {
                // Keep a valid local snapshot available while the remote request
                // is in flight. The bootstrapper can then use stale-but-safe data
                // if the network request exceeds its three-second budget.
                App.Logger.WriteLine(LOG_IDENT, "Loading local data as stale fallback");
                LoadLocalFallback(LOG_IDENT);

                try
                {
                    Uri remoteDataUri = new(App.ProjectRemoteDataLink);
                    RemoteDataBase remoteData = await Http.GetJson<RemoteDataBase>(remoteDataUri);
                    Validate(remoteData);

                    lock (_stateLock)
                        Prop = remoteData;

                    SetLoadedState(GenericTriState.Successful);
                    App.Logger.WriteLine(LOG_IDENT, "Remote data loaded");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Could not load remote data");
                    App.Logger.WriteException(LOG_IDENT, ex);
                    SetLoadedState(GenericTriState.Failed);
                }
            }

            GenericTriState loadedState;

            lock (_stateLock)
                loadedState = LoadedState;

            _loadedCompletion.TrySetResult(loadedState);
            InvokeHandlers();

            if (loadedState == GenericTriState.Successful)
                this.Save();

            App.Logger.WriteLine(LOG_IDENT, $"Loading finished with status: {loadedState}");
        }
    }
}
