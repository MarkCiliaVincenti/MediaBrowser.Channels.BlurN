using MediaBrowser.Channels.BlurN.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MediaBrowser.Channels.BlurN
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        #region Class Members
        private Guid _id = new Guid("08D13692-D214-47DF-B9BD-2868870C5961");
        private IApplicationPaths _appPaths;
        private static ILogger Logger;
        public static INotificationManager NotificationManager;
        #endregion Class Members

        #region Properties
        public static string StaticName => "BlurN";
        public override string Description => "BlurN will list and notify on newly released movies on Blu-Ray when a movie matches a number of filters.";
        public static Plugin Instance { get; private set; }
        public IApplicationPaths AppPaths => _appPaths;
        public PluginConfiguration PluginConfiguration => Configuration;
        public ImageFormat ThumbImageFormat => ImageFormat.Png;
        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
        };
        public override Guid Id => _id;
        public override string Name => StaticName;
        #endregion Properties

        #region Methods
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogManager logManager, INotificationManager notificationManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _appPaths = applicationPaths;
            Logger = logManager.GetLogger(GetType().Name);
            NotificationManager = notificationManager;
        }

        public static void DebugLogger(string message, params object[] paramList)
        {
            if (Instance.Configuration.EnableDebugLogging)
                Logger.Debug($"[BlurN] {message}", paramList);
        }

        public override void OnUninstalling()
        {
            base.OnUninstalling();
        }

        public Stream GetThumbImage()
        {
            return GetType().GetTypeInfo().Assembly.GetManifestResourceStream($"{GetType().Namespace}.Images.thumb.png");
        }
        #endregion Methods
    }
}
