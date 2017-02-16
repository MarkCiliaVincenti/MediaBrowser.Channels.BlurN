using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MediaBrowser.Channels.BlurN.Helpers
{
    class HTTP
    {
        //private const string userAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";

        public static async Task<string> GetPage(string uri, string userAgent)
        {
            string result;
            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage request = new HttpRequestMessage())
            {
                request.Headers.Add("User-Agent", userAgent);
                request.RequestUri = new Uri(uri);
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    using (HttpContent content = response.Content)
                        result = await content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        string error = string.Format("Error accessing {0} ({1} {2})", uri, (int)response.StatusCode, response.StatusCode.ToString());

                        if (Plugin.Instance.Configuration.EnableDebugLogging)
                            Plugin.Logger.Debug(string.Format("[BlurN] {0}: {1}", error, result));
                        throw new HttpRequestException(error, new Exception(result));
                    }
                }
            }

            return result;
        }
    }
}
