using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Threading;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    class Library
    {
        public static Dictionary<string, BaseItem> BuildLibraryDictionary(CancellationToken cancellationToken, ILibraryManager libraryManager, InternalItemsQuery query)
        {
            IEnumerable<BaseItem> library;
            Dictionary<string, BaseItem> libDict = new Dictionary<string, BaseItem>();

            library = libraryManager.GetItemList(query);

            cancellationToken.ThrowIfCancellationRequested();

            foreach (BaseItem libItem in library)
            {
                if (libItem.GetTopParent() is Channel)
                    continue;
                string libIMDbId = libItem.GetProviderId(MetadataProviders.Imdb);
                if (!string.IsNullOrEmpty(libIMDbId) && !libDict.ContainsKey(libIMDbId))
                    libDict.Add(libIMDbId, libItem);
            }

            return libDict;
        }
    }
}
