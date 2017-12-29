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
            string[] variablesStringArray = new string[] { "Title", "Year", "IMDbRating", "IMDbVotes", "IMDbURL" };

            yield return new NotificationTypeInfo()
            {
                Category = "BlurN",
                DefaultDescription = "Year: {Year}, IMDb Rating: {IMDbRating} ({IMDbVotes} votes)",
                DefaultTitle = "[BlurN] New movie released: {Title}",
                Enabled = true,
                IsBasedOnUserEvent = false,
                Name = "New release notification",
                Type = NewRelease,
                Variables = variablesStringArray
            };
        }
    }
}
