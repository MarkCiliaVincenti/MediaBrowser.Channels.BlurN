using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using System.Collections.Generic;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    class BlurNNotificationType : INotificationTypeFactory
    {
        public const string NewRelease = "BlurNNewRelease";

        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            yield return new NotificationTypeInfo()
            {
                Category = "BlurN",
                Enabled = true,
                IsBasedOnUserEvent = false,
                Name = "New release notification",
                Type = NewRelease
            };
        }
    }
}
