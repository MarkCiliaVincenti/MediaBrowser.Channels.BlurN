using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Channels.BlurN.Configuration;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Plugins;
using System.Collections.Generic;

namespace MediaBrowser.Channels.BlurN
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        IApplicationPaths _appPaths;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogManager logManager, INotificationManager notificationManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _appPaths = applicationPaths;
            Logger = logManager.GetLogger(GetType().Name);
            NotificationManager = notificationManager;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
            };
        }

        public override string Name
        {
            get { return StaticName; }
        }

        public static string StaticName
        {
            get { return "BlurN"; }
        }

        public override string Description
        {
            get
            {
                return "BlurN will list and notify on newly released movies on Blu-Ray when a movie matches a number of filters.";
            }
        }

        private static ILogger Logger;
        public static INotificationManager NotificationManager;

        public static void DebugLogger(string message, params object[] paramList)
        {
            if (Instance.Configuration.EnableDebugLogging)
                Logger.Debug($"[BlurN] {message}", paramList);
        }

        public override void OnUninstalling()
        {
            base.OnUninstalling();
        }

        public static Plugin Instance { get; private set; }

        public IApplicationPaths AppPaths
        {
            get { return _appPaths; }
        }

        public PluginConfiguration PluginConfiguration
        {
            get { return Configuration; }
        }
    }
}
