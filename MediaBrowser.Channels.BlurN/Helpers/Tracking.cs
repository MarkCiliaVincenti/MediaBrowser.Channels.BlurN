using MediaBrowser.Channels.BlurN.ScheduledTasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    class Tracking
    {
        public static async void Track(IHttpClient _httpClient, IApplicationHost _appHost, IServerConfigurationManager _serverConfigurationManager, string sessionControl, string task, CancellationToken cancellationToken)
        {
            var config = Plugin.Instance.Configuration;
            if (string.IsNullOrEmpty(config.InstallationID))
            {
                config.InstallationID = Guid.NewGuid().ToString();
                Plugin.Instance.SaveConfiguration();
            }

            try
            {
                string version = Plugin.Instance.Version.ToString();

                var values = new Dictionary<string, string>
                    {
                        { "v", "1" },
                        { "t", "event" },
                        { "tid", "UA-92060336-1" },
                        { "cid", config.InstallationID },
                        { "ec", task },
                        { "ea", version },
                        { "el", config.ChannelRefreshCount.ToString() },
                        { "an", "BlurN" },
                        { "aid", "MediaBrowser.Channels.BlurN" },
                        { "av", version },
                        { "ds", "app" },
                        { "sc", sessionControl },
                        { "ul", _serverConfigurationManager.Configuration.UICulture.ToLower() },
                        { "z", new Random().Next(1,2147483647).ToString() }
                    };

                var options = new HttpRequestOptions
                {
                    Url = "https://www.google-analytics.com/collect",
                    CancellationToken = cancellationToken,
                    LogRequest = false,
                    LogErrors = false,
                    BufferContent = false,
                    LogErrorResponseBody = false,
                    EnableDefaultUserAgent = true
                };

                options.SetPostData(values);

                using (var response = await _httpClient.SendAsync(options, "POST").ConfigureAwait(false))
                {

                }
            }
            catch (Exception ex)
            {
                    Plugin.DebugLogger($"Failed to track usage with GA: {ex.Message}");
            }
        }
    }
}
