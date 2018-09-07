using MediaBrowser.Channels.BlurN.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Channels.BlurN
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
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

        private Guid _id = new Guid("08D13692-D214-47DF-B9BD-2868870C5961");
        public override Guid Id
        {
            get { return _id; }
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
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
