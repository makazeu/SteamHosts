using System;
using System.Net;
using System.Diagnostics;

namespace SteamHosts
{
    class HttpHeader
    {
        private static string ERROR_TIMEOUT = "连接超时";
        private static string ERROR_RESET = "连接被重置";
        private static string ERROR_OTHER = "连接失败";

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
            } catch (WebException e) when (e.Status == WebExceptionStatus.Timeout)
            {
                result.setResult(ERROR_TIMEOUT);
            } catch (WebException e) when (e.Status == WebExceptionStatus.ReceiveFailure)
            {
                result.setResult(ERROR_RESET);
            } catch (WebException e)
            {
                Console.WriteLine(e);
                result.setResult(ERROR_OTHER);
            }

            return result;
        }
    }
}
