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
        public const string NewRelease = "BlurNNewRelease";

        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            return new List<NotificationTypeInfo>()
            {
                new NotificationTypeInfo()
                {
                    Category = "BlurN",
                    DefaultDescription = "Year: {Year}, IMDb Rating: {IMDbRating} ({IMDbVotes} votes)",
                    DefaultTitle = "[BlurN] New movie released: {Title}",
                    Enabled = true,
                    IsBasedOnUserEvent = false,
                    Name = "New release notification",
                    Type = NewRelease,
                    Variables = new List<string> {"Title", "Year", "IMDbRating", "IMDbVotes" }
                }
            };
        }
    }
}
