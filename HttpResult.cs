using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamHosts
{
    class HttpResult
    {
        private bool flag;  // true: has response  false: no response
        private int time;

        public bool isSuccess(){
            return flag;
        }

        public void setFlag(bool flag) {
            this.flag = flag;
        }

        public int getTime() {
            return time;
        }

        public void setTime(int time) {
            this.time = time;
        }
    }
}
