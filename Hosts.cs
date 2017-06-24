using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSHostsFile;
using System.Net;

namespace SteamHosts
{
    class Hosts
    {
        public static string ChangeHosts(String hostname, String ipAddr)
        {
            try
            {
                HostsFile.Set(hostname, ipAddr);
                return "OK";
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return e.Message.ToString();
            }
        }

        public static string getIPByDomain(String hostname)
        {
            try
            {
                IPHostEntry hostInfo = Dns.GetHostEntry(hostname);
                IPAddress[] IPAddr = hostInfo.AddressList;
                return IPAddr[0].ToString();
            } catch (Exception e)
            {
                return e.Message.ToString();
            }
        }
    }
}
