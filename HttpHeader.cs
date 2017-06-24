using System;
using System.Net;
using System.Diagnostics;

namespace SteamHosts
{
    class HttpHeader
    {
        public static HttpResult GetHttpConnectionStatus(
            string hostname, string ip, int timeout)
        {
            HttpResult result = new HttpResult();
            result.setFlag(false);

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + hostname + "/");
                request.Timeout = 1000 * timeout;

                if (! string.IsNullOrEmpty(ip))
                {
                    WebProxy proxy = new WebProxy(ip);
                    request.Proxy = proxy;
                }
                
                Stopwatch timer = new Stopwatch();
                timer.Start();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();

                timer.Stop();
                TimeSpan timeSpan = timer.Elapsed;
                result.setTime((int) timeSpan.TotalMilliseconds);
                result.setFlag(true);
            } catch (Exception)
            {
                //Console.WriteLine(e.ToString());
            }

            return result;
        }
    }
}
