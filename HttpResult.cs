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
        private string result;
        private int time;

        public bool isSuccess(){
            return flag;
        }

        public void setFlag(bool flag) {
            this.flag = flag;
        }

        public void setResult(string result) {
            this.result = result;
        }

        public string getResult() {
            return result;
        }

        public int getTime() {
            return time;
        }

        public void setTime(int time) {
            this.time = time;
        }
    }
}
