using MediaBrowser.Channels.BlurN.ScheduledTasks;
using MediaBrowser.Common;
using MediaBrowser.Controller.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    class Tracking
    {
        public static async void Track(IApplicationHost _appHost, IServerConfigurationManager _serverConfigurationManager, string sessionControl, string task)
        {
            var config = Plugin.Instance.Configuration;
            if (string.IsNullOrEmpty(config.InstallationID))
            {
                config.InstallationID = Guid.NewGuid().ToString();
                Plugin.Instance.SaveConfiguration();
            }

            try
            {
                using (var client = new HttpClient())
                {
                    string version = typeof(RefreshNewReleases).GetTypeInfo().Assembly.GetName().Version.ToString();

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
                        { "ua", HTTP.EmbyUserAgent(_appHost) },
                        { "sc", sessionControl },
                        { "ul", _serverConfigurationManager.Configuration.UICulture.ToLower() },
                        { "z", new Random().Next(1,2147483647).ToString() }
                    };

                    var content = new FormUrlEncodedContent(values);
                    var response = await client.PostAsync("https://www.google-analytics.com/collect", content);
                    var responseString = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                if (config.EnableDebugLogging)
                    Plugin.Logger.Debug("[BlurN] Failed to track usage with GA: " + ex.Message);
            }
        }
    }
}
