using MediaBrowser.Controller.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Notifications;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    class BlurNNotificationType : INotificationTypeFactory
    {
        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            var knownTypes = new List<NotificationTypeInfo>()
            {
                new NotificationTypeInfo()
                {
                    Category = "BlurN",
                    DefaultDescription = "Year: {Year}, IMDb Rating: {IMDbRating}",
                    DefaultTitle = "[BlurN] New movie released: {Title}",
                    Enabled = true,
                    IsBasedOnUserEvent = false,
                    Name = "New release notification",
                    Type = "BlurNNewRelease",
                    Variables = new List<string> {"Title", "Year", "IMDbRating" }
                }
            };
            return knownTypes;
        }
    }
}
