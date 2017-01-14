using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Channels.BlurN.Helpers;
using MediaBrowser.Model.Notifications;
using System.Xml.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Channels;

namespace MediaBrowser.Channels.BlurN.ScheduledTasks
{
    class ResetDatabase : IScheduledTask
    {
        public string Category
        {
            get
            {
                return "BlurN";
            }
        }

        public string Description
        {
            get
            {
                return "Resets the BlurN database, retaining the settings.";
            }
        }

        public string Key
        {
            get
            {
                return "BlurNResetDatabase";
            }
        }

        public string Name
        {
            get
            {
                return "Reset BlurN database";
            }
        }


        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var config = Plugin.Instance.Configuration;
            config.LastPublishDate = DateTime.MinValue;
            config.Items = new OMDBList();
            Plugin.Instance.SaveConfiguration();
            progress.Report(100);
            return;
        }



        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Until we can vary these default triggers per server and MBT, we need something that makes sense for both
            return new[] {             
                new TaskTriggerInfo {Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromDays(365).Ticks }
            };
        }
    }
}
