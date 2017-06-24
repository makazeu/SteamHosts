using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections;

namespace SteamHosts
{
    class JsonReader
    {
        public static Dictionary<String,String> readIpList(String filename)
        {
            Dictionary<String, String> ipList = new Dictionary<String, String>();
            using (StreamReader sr = new StreamReader(filename))
            {
                try
                {
                    String data = sr.ReadToEnd();
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
                } catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            return null;
        }
    }
}
