using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamHosts
{
    class MyWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 6000;
            return w;
        }
    }

    class UpdateIp
    {
        private static string url = "https://stip.m21.win/ip";

        public static string UpdateIpLibrary()
        {
            try
            {
                MyWebClient webClient = new MyWebClient();
                webClient.Encoding = Encoding.UTF8;
                webClient.DownloadFile(url, "ip.json");
                return "OK";
            } catch (Exception e)
            {
                return e.Message;
            }
            
        }
    }
}
