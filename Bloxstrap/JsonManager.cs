using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Linq;

namespace Bloxstrap
{
    public class JsonManager<T> where T : class, new()
    {
        public T OriginalProp { get; set; } = new();

        public T Prop { get; set; } = new();

        /// <summary>
        /// The file hash when last retrieved from disk
        /// </summary>
        public string? LastFileHash { get; private set; }

        private DateTime _fileLastWriteTimeUtc = DateTime.MinValue;
        private long _fileLength = 0;

        public bool Loaded { get; set; } = false;

        public virtual string ClassName => typeof(T).Name;
        
        public virtual string ProfilesLocation => Path.Combine(Paths.Base, $"Profiles.json");

        public virtual string FileLocation => Path.Combine(Paths.Base, $"{ClassName}.json");

        public virtual string LOG_IDENT_CLASS => $"JsonManager<{ClassName}>";

        public virtual void Load(bool alertFailure = true)
        {
            
            string LOG_IDENT = $"{LOG_IDENT_CLASS}::Load";

            App.Logger.WriteLine(LOG_IDENT, $"Loading from {FileLocation}...");

            try
            {
                string contents = File.ReadAllText(FileLocation);

                T? settings = JsonSerializer.Deserialize<T>(contents);

                if (settings is null)
                    throw new ArgumentNullException("Deserialization returned null");

                Prop = settings;
                Loaded = true;
                LastFileHash = MD5Hash.FromString(contents);
                _fileLastWriteTimeUtc = File.GetLastWriteTimeUtc(FileLocation);
                _fileLength = new FileInfo(FileLocation).Length;

                App.Logger.WriteLine(LOG_IDENT, "Loaded successfully!");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to load!");
                App.Logger.WriteException(LOG_IDENT, ex);

                if (alertFailure)
                {
                    string message = "";

                    if (ClassName == nameof(Settings))
                        message = Strings.JsonManager_SettingsLoadFailed;
                    else if (ClassName == nameof(FastFlagManager))
                        message = Strings.JsonManager_FastFlagsLoadFailed;

                    if (!String.IsNullOrEmpty(message))
                        Frontend.ShowMessageBox($"{message}\n\n{ex.Message}", System.Windows.MessageBoxImage.Warning);

                    try
                    {
                        // Create a backup of loaded file
                        File.Copy(FileLocation, FileLocation + ".bak", true);
                    }
                    catch (Exception copyEx)
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to create backup file: {FileLocation}.bak");
                        App.Logger.WriteException(LOG_IDENT, copyEx);
                    }
                }

                Save();
            }
        }

        public virtual bool Save()
        {
            string LOG_IDENT = $"{LOG_IDENT_CLASS}::Save";
            string? temporaryFile = null;

            App.Logger.WriteLine(LOG_IDENT, $"Saving to {FileLocation}...");

            Directory.CreateDirectory(Path.GetDirectoryName(FileLocation)!);

            try
            {
                // Serialize saves for the same state file across the foreground
                // bootstrapper, watcher, and background updater processes.
                using var saveLock = new InterProcessLock($"Json-{ClassName}", TimeSpan.FromSeconds(5));

                if (!saveLock.IsAcquired)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Timed out waiting for the state-file lock; skipping save.");
                    return false;
                }

                string contents = JsonSerializer.Serialize(Prop, new JsonSerializerOptions { WriteIndented = true });
                string contentsHash = MD5Hash.FromString(contents);

                // If another process changed the file after this instance loaded it,
                // do not overwrite that newer state with a stale in-memory copy.
                if (LastFileHash is not null && File.Exists(FileLocation) && HasFileOnDiskChanged())
                {
                    App.Logger.WriteLine(LOG_IDENT, "State file changed on disk; skipping stale save.");
                    return false;
                }

                // Avoid rewriting unchanged state files. This is especially important
                // during startup, where multiple components may save the same state.
                if (File.Exists(FileLocation) && LastFileHash == contentsHash)
                {
                    App.Logger.WriteLine(LOG_IDENT, "No changes detected.");
                    return true;
                }

                // Write beside the target and replace it only after the complete
                // JSON document has been flushed by File.WriteAllText.
                temporaryFile = $"{FileLocation}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
                File.WriteAllText(temporaryFile, contents);
                File.Move(temporaryFile, FileLocation, overwrite: true);
                temporaryFile = null;

                LastFileHash = contentsHash;
                _fileLastWriteTimeUtc = File.GetLastWriteTimeUtc(FileLocation);
                _fileLength = new FileInfo(FileLocation).Length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to save");
                App.Logger.WriteException(LOG_IDENT, ex);

                string errorMessage = string.Format(Resources.Strings.Bootstrapper_JsonManagerSaveFailed, ClassName, ex.Message);
                Frontend.ShowMessageBox(errorMessage, System.Windows.MessageBoxImage.Warning);

                return false;
            }
            finally
            {
                if (temporaryFile is not null)
                {
                    try
                    {
                        File.Delete(temporaryFile);
                    }
                    catch (IOException)
                    {
                        // Best-effort cleanup; the next save can replace stale temp files.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Best-effort cleanup.
                    }
                }
            }

            App.Logger.WriteLine(LOG_IDENT, "Save complete!");
            return true;
        }

        /// <summary>
        /// Is the file on disk different to the one deserialised during this session?
        /// </summary>
        public bool HasFileOnDiskChanged()
        {
            FileInfo fileInfo = new(FileLocation);

            if (!fileInfo.Exists)
                return true;

            if (fileInfo.LastWriteTimeUtc == _fileLastWriteTimeUtc && fileInfo.Length == _fileLength)
                return false;

            return LastFileHash != MD5Hash.FromFile(FileLocation);
        }
    }
}
