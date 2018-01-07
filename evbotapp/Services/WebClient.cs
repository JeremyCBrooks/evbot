using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace evbotapp.Services
{
    public class WebClient
    {
        private CookieContainer cookieJar = new CookieContainer();

        public async Task<HttpWebResponse> Submit(Uri uri, string method, IEnumerable<string> headers, string data, IWebProxy proxy)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
            req.CookieContainer = cookieJar;
            if (null != proxy)
            {
                req.Proxy = proxy;
            }

            req.Method = method;

            if (null != headers)
            {
                req.Headers = new WebHeaderCollection();
                foreach (var header in headers)
                {
                    req.Headers.Add(header.ToString());
                }
            }

            HttpWebResponse response = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(data))
                {
                    using (var stream = await req.GetRequestStreamAsync())
                    using (StreamWriter sw = new StreamWriter(stream))
                    {
                        sw.Write(data);
                    }
                }

                response = await req.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException we)
            {
                response = we.Response as HttpWebResponse;
            }

            return response;
        }

        public static string ParseResponse(HttpWebResponse response)
        {
            if (response != null)
            {
                var sb = new StringBuilder();
                try
                {
                    using (var stream = response.GetResponseStream())
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        return sr.ReadToEnd();
                    }
                }
                catch { }
            }

            return null;
        }
    }
}
