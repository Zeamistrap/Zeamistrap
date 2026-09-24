using Bloxstrap.Utility;
using Bloxstrap.Properties;
using System;
using System.Configuration;
using System.Windows.Automation;
using Windows.Win32.Foundation;

namespace Bloxstrap.RobloxInterfaces
{
    public static class Deployment
    {
        public const string DefaultRobloxDomain = "roblox.com";

        public const string DefaultChannel = "production";
        
        private const string VersionStudioHash = "version-012732894899482c";


        public static EventHandler<string>? ChannelChanged;
        private static string _channel = App.Settings.Prop.Channel;
        public static string Channel {
            get => _channel;
            set
            {
                bool channelChanged = !String.Equals(_channel, value, StringComparison.OrdinalIgnoreCase);
                bool settingChanged = !String.Equals(App.Settings.Prop.Channel, value, StringComparison.OrdinalIgnoreCase);

                if (!channelChanged && !settingChanged)
                    return;

                string previousChannel = _channel;
                string previousSetting = App.Settings.Prop.Channel;

                _channel = value;
                App.Settings.Prop.Channel = _channel;
                bool saved = App.Settings.Save();

                if (!saved)
                {
                    _channel = previousChannel;
                    App.Settings.Prop.Channel = previousSetting;
                }
                else if (channelChanged)
                {
                    ChannelChanged?.Invoke(null, value);
                }
            }
        }

        public static string ChannelToken = string.Empty;

        public static string BinaryType = "WindowsPlayer";

        public static string RobloxDomain => App.Settings.Prop.RobloxDomain;

        public static bool IsDefaultChannel => Channel.Equals(DefaultChannel, StringComparison.OrdinalIgnoreCase) || Channel.Equals("live", StringComparison.OrdinalIgnoreCase);
        public static bool IsDefaultRobloxDomain => RobloxDomain.Equals(DefaultRobloxDomain, StringComparison.OrdinalIgnoreCase);

        public static string BaseUrl { get; private set; } = null!;

        public static readonly List<HttpStatusCode?> BadChannelCodes = new()
        {
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound
        };

        private static readonly Dictionary<string, (ClientVersion Version, DateTimeOffset ExpiresAt)> ClientVersionCache = new();
        private static readonly TimeSpan ClientVersionCacheLifetime = TimeSpan.FromSeconds(30);

        // a list of roblox deployment locations that we check for, in case one of them don't work
        // these are all weighted based on their priority, so that we pick the most optimal one that we can. 0 = highest
        private static readonly Dictionary<string, int> BaseUrls = new()
        {
            { "https://setup.rbxcdn.com", 0 },
            { "https://setup-aws.rbxcdn.com", 2 },
            { "https://setup-ak.rbxcdn.com", 2 },
            { "https://roblox-setup.cachefly.net", 2 },
            { "https://s3.amazonaws.com/setup.roblox.com", 4 }
        };

        private static async Task<string?> TestConnection(string url, int priority, CancellationToken token)
        {
            string LOG_IDENT = $"Deployment::TestConnection<{url}>";

            await Task.Delay(priority * 1000, token);

            App.Logger.WriteLine(LOG_IDENT, "Connecting...");

            try
            {
                using var response = await App.HttpClient.GetAsync($"{url}/versionStudio", token);

                response.EnsureSuccessStatusCode();

                // versionStudio is the version hash for the last MFC studio to be deployed.
                // the response body should always be "version-012732894899482c".
                string content = await response.Content.ReadAsStringAsync(token);

                if (content != VersionStudioHash)
                    throw new InvalidHTTPResponseException($"versionStudio response does not match (expected \"{VersionStudioHash}\", got \"{content}\")");
            }
            catch (TaskCanceledException)
            {
                App.Logger.WriteLine(LOG_IDENT, "Connectivity test cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                throw;
            }

            return url;
        }

        /// <summary>
        /// This function serves double duty as the setup mirror enumerator, and as our connectivity check.
        /// Returns null for success.
        /// </summary>
        /// <returns></returns>
        public static async Task<Exception?> InitializeConnectivity()
        {
            const string LOG_IDENT = "Deployment::InitializeConnectivity";

            var tokenSource = new CancellationTokenSource();

            var exceptions = new List<Exception>();
            var tasks = (from entry in BaseUrls select TestConnection(entry.Key, entry.Value, tokenSource.Token)).ToList();

            App.Logger.WriteLine(LOG_IDENT, "Testing connectivity...");

            while (tasks.Any() && String.IsNullOrEmpty(BaseUrl))
            {
                var finishedTask = await Task.WhenAny(tasks);

                tasks.Remove(finishedTask);

                if (finishedTask.IsFaulted)
                    exceptions.Add(finishedTask.Exception!.InnerException!);
                else if (!finishedTask.IsCanceled)
                    BaseUrl = finishedTask.Result;
            }

            // stop other running connectivity tests
            tokenSource.Cancel();

            if (string.IsNullOrEmpty(BaseUrl))
            {
                if (exceptions.Any())
                    return exceptions[0];

                // task cancellation exceptions don't get added to the list
                return new TaskCanceledException("All connection attempts timed out.");
            }

            App.Logger.WriteLine(LOG_IDENT, $"Got {BaseUrl} as the optimal base URL");

            return null;
        }

        public static string GetLocation(string resource)
        {
            return GetLocation(resource, Channel);
        }

        private static string GetLocation(string resource, string channel)
        {
            string location = BaseUrl;

            if (!channel.Equals(DefaultChannel, StringComparison.OrdinalIgnoreCase) &&
                !channel.Equals("live", StringComparison.OrdinalIgnoreCase))
                location += "/channel/common";

            location += resource;

            return location;
        }

        public async static Task<UserChannel?> GetUserChannel(string binaryType)
        {
            const string LOG_IDENT = "Deployment::GetUserChannel";
            try
            {
                Uri apiUrl = UrlBuilder.BuildApiUrl("clientsettings", "v2/user-channel?binaryType=" + binaryType);
                using HttpResponseMessage response = await App.Cookies.AuthGet(apiUrl);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                UserChannel channelInfo = JsonSerializer.Deserialize<UserChannel>(content)!;

                return channelInfo;
            }
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to get user channel");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public static async Task<bool> IsChannelPrivate(string channel)
        {
            if (channel == "production")
                channel = "live";

            if (channel == "live")
                return false;

            try
            {
                Uri apiUrl = UrlBuilder.BuildApiUrl("clientsettingscdn", "v2/client-version/WindowsPlayer/channel/" + channel);
                using var response = await App.HttpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                if (BadChannelCodes.Contains(ex.StatusCode))
                    return true;
            }

            return false;
        }

        public static async Task<DateTime?> GetVersionTimestamp(string version, string? channel = null)
        {
            const string LOG_IDENT = "Deployment::GetVersionTimestamp";
            const string header = "last-modified";

            channel ??= Channel;

            // since we arent getting the timestamp during launch there shouldnt be any collisions
            if (string.IsNullOrEmpty(BaseUrl))
                await InitializeConnectivity();

            try
            {
                string location = GetLocation($"/{version}-rbxPkgManifest.txt", channel);
                using var response = await App.HttpClient.GetAsync(location);
                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.TryGetValues(header, out var values))
                {
                    string lastModified = values.First();
                    DateTime dateTime = DateTime.Parse(lastModified);

                    return dateTime;
                }
            } 
            catch (HttpRequestException ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to get timestamp for {version}");
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            return null;
        }

        public static async Task<ClientVersion> GetInfo(
            string? channel = null,
            bool behindProductionCheck = false,
            bool includeTimestamp = false,
            string? contextDomain = null,
            string? contextBinaryType = null,
            string? contextToken = null)
        {
            const string LOG_IDENT = "Deployment::GetInfo";

            if (String.IsNullOrEmpty(channel))
                channel = Channel;

            bool isDefaultChannel = String.Compare(channel, DefaultChannel, StringComparison.OrdinalIgnoreCase) == 0;
            string domain = contextDomain ?? RobloxDomain;
            string binaryType = contextBinaryType ?? BinaryType;
            string channelToken = contextToken ?? ChannelToken;

            App.Logger.WriteLine(LOG_IDENT, $"Getting deploy info for channel {channel}");

            string cacheKey = $"{domain}|{binaryType}|{channel}|{channelToken}|{behindProductionCheck}|{includeTimestamp}";

            string path = $"v2/client-version/{binaryType}";

            if (!isDefaultChannel)
                path += $"/channel/{channel}";

            async Task<ClientVersion> SendVersionRequest(string service)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, UrlBuilder.BuildApiUrl(service, path, domain));

                if (!string.IsNullOrEmpty(channelToken))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Got Roblox-Channel-Token");
                    request.Headers.Add("Roblox-Channel-Token", channelToken);
                }

                return await Http.SendJson<ClientVersion>(request);
            }

            ClientVersion clientVersion;

            bool hasCachedVersion;
            (ClientVersion Version, DateTimeOffset ExpiresAt)? cachedVersion = null;

            lock (ClientVersionCache)
            {
                hasCachedVersion = ClientVersionCache.TryGetValue(cacheKey, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow;
                if (hasCachedVersion)
                    cachedVersion = entry;
            }

            if (hasCachedVersion && cachedVersion is not null)
            {
                App.Logger.WriteLine(LOG_IDENT, "Deploy information is cached");
                clientVersion = cachedVersion.Value.Version;
            }
            else
            {
                try
                {
                    clientVersion = await SendVersionRequest("clientsettingscdn");
                }
                catch (HttpRequestException httpEx)
                when (!isDefaultChannel && BadChannelCodes.Contains(httpEx.StatusCode))
                {
                    throw new InvalidChannelException(httpEx.StatusCode);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Failed to contact clientsettingscdn! Falling back to clientsettings...");
                    App.Logger.WriteException(LOG_IDENT, ex);

                    try
                    {
                        clientVersion = await SendVersionRequest("clientsettings");
                    }
                    catch (HttpRequestException httpEx)
                    when (!isDefaultChannel && BadChannelCodes.Contains(httpEx.StatusCode))
                    {
                        throw new InvalidChannelException(httpEx.StatusCode);
                    }
                }

                // check if channel is behind LIVE
                if (!isDefaultChannel && behindProductionCheck)
                {
                    var defaultClientVersion = await GetInfo(
                        DefaultChannel,
                        behindProductionCheck: false,
                        includeTimestamp: false,
                        contextDomain: domain,
                        contextBinaryType: binaryType,
                        contextToken: channelToken);

                    if (Utilities.CompareVersions(clientVersion.Version, defaultClientVersion.Version) == VersionComparison.LessThan)
                        clientVersion.IsBehindDefaultChannel = true;
                }
                else
                    clientVersion.IsBehindDefaultChannel = false;

                if (includeTimestamp && clientVersion.Timestamp is null)
                    clientVersion.Timestamp = await GetVersionTimestamp(clientVersion.VersionGuid, channel);

                lock (ClientVersionCache)
                    ClientVersionCache[cacheKey] = (clientVersion, DateTimeOffset.UtcNow.Add(ClientVersionCacheLifetime));
            }

            return clientVersion;
        }
    }
}
