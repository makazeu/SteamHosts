using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSHostsFile;

namespace SteamHosts
{
    class Hosts
    {
        public static bool ChangeHosts(String hostname, String ipAddr)
        {
            try
            {
                HostsFile.Set(hostname, ipAddr);
                return true;
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }
    }
}
