using MediaBrowser.Controller.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Notifications;
using System.Reflection;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    class BlurNNotificationType : INotificationTypeFactory
    {
        public const string NewRelease = "BlurNNewRelease";

        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            string[] variablesStringArray = new string[] { "Title", "Year", "IMDbRating", "IMDbVotes", "IMDbURL" };
            List<string> variablesStringList = new List<string>() { "Title", "Year", "IMDbRating", "IMDbVotes", "IMDbURL" };

            dynamic variablesDynamic;
            var version = typeof(NotificationTypeInfo).GetTypeInfo().Assembly.GetName().Version;
            var v3_2_27_0 = new Version(3, 2, 27, 0);
            if (version.CompareTo(v3_2_27_0) > 0)
                variablesDynamic = variablesStringArray;
            else
                variablesDynamic = variablesStringList;

            yield return new NotificationTypeInfo()
            {
                Category = "BlurN",
                DefaultDescription = "Year: {Year}, IMDb Rating: {IMDbRating} ({IMDbVotes} votes)",
                DefaultTitle = "[BlurN] New movie released: {Title}",
                Enabled = true,
                IsBasedOnUserEvent = false,
                Name = "New release notification",
                Type = NewRelease,
                Variables = variablesDynamic
            };
        }
    }
}
