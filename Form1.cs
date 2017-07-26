using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Linq;
using System.Drawing;

namespace SteamHosts
{
    public partial class Form1 : Form
    {
        const string Hostname = "store.steampowered.com";
        const int MaximumConnection = 30;
        const int MaximumTimeout = 4; // seconds
        const string TimeUnitString = " ms";

        Dictionary<String, String> ipList;
        int foundCount;
        int minTime;
        string minIp;

        private delegate void UpdateListViewDelegate(string ip, bool success, int time, string result);

        private delegate void updateCurrentIpResultDelegate(bool success, int time, string result);

        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateListView(string ip, bool success, int time, string result)
        {
            DataGridViewRow row = dataGridView1.Rows
                .Cast<DataGridViewRow>()
                .Where(r => r.Cells["ip"].Value.ToString().Equals(ip))
                .First(); 

            if (success)
            {
                row.Cells["time"].Value = time.ToString() + TimeUnitString;

                if (time < minTime)
                {
                    try
                    {
                        DataGridViewCellStyle style2 = new DataGridViewCellStyle();
                        style2.BackColor = Color.White;

                        DataGridViewRow foundRow = dataGridView1.Rows
                            .Cast<DataGridViewRow>()
                            .Where(r => r.Cells["ip"].Value.ToString().Equals(minIp))
                            .First();
                        foundRow.Selected = false;
                        foundRow.Cells["time"].Style = style2;
                    } catch (Exception)
                    {
                        //
                    }

                    minTime = time;
                    minIp = ip;

                    DataGridViewCellStyle style = new DataGridViewCellStyle();
                    style.BackColor = Color.LightPink;
                    row.Cells["time"].Style = style;

                    dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                    dataGridView1.CurrentCell = dataGridView1.Rows[row.Index].Cells[0];
                    dataGridView1.Refresh();
                    row.Selected = true;
                }
            }
            else
            {
                row.Cells["time"].Value = result;
            }

            //foundCount++;
            Interlocked.Increment(ref foundCount);
            if (foundCount == ipList.Count)
            {
                button1.Enabled = true;
                button3.Enabled = true;
            }
        }

        private void updateCurrentIpResult(bool success, int time, string result)
        {
            if(success)
            {
                label4.ForeColor = Color.Green;
                label4.Text = "（连接成功，耗时：" + time + " ms）";
            } else
            {
                label4.ForeColor = Color.Red;
                label4.Text = "（" + result + "）";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            getCurrentHosts(Hostname);

            initialize();
        }

        private void initialize()
        {
            if (!System.IO.File.Exists("ip.json"))
            {
                MessageBox.Show("无法找到ip.json文件！", "SteamHosts");
                Environment.Exit(0);
            }
            ipList = JsonReader.readIpList("ip.json");

            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();

            foreach (var item in ipList)
            {
                int index = dataGridView1.Rows.Add();
                dataGridView1.Rows[index].Cells[0].Value = item.Key;
                dataGridView1.Rows[index].Cells[1].Value = "";
                dataGridView1.Rows[index].Cells[2].Value = item.Value;
            }

            if (ipList.Count > 0)
            {
                button2.Enabled = true;
            } else
            {
                button2.Enabled = false;
            }
        }

        private void clearData()
        {
            foundCount = 0;
            minTime = 99999;
            minIp = null;

            dataGridView1.FirstDisplayedScrollingRowIndex = 0;
            dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
            dataGridView1.Refresh();

            DataGridViewCellStyle style = new DataGridViewCellStyle();
            style.BackColor = Color.White;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells["time"].Style = style;
                row.Cells["time"].Value = "";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TestHttpSpeed(MaximumConnection, MaximumTimeout);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string hostsResult = "IP null";

            string selectedIP = null;
            try
            {
                selectedIP = dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["ip"].Value.ToString();
            } catch
            {
                //
            }

            if ( selectedIP != null )
            {
                hostsResult = Hosts.ChangeHosts("steamcommunity.com", selectedIP);
                hostsResult = Hosts.ChangeHosts(Hostname, selectedIP);
                if (hostsResult == "OK")
                {
                    MessageBox.Show("设置hosts成功！", "SteamHosts");
                    getCurrentHosts(Hostname);
                    return;
                }
            }
            MessageBox.Show("设置hosts失败，原因：" + hostsResult, "SteamHosts");
        }

        private void testSingleHttpWithIp(string ip, int timeout)
        {
            Task task = Task.Run(() => {
                HttpResult result = HttpHeader.GetHttpConnectionStatus(Hostname, ip, timeout);

                BeginInvoke(new updateCurrentIpResultDelegate(updateCurrentIpResult),
                    result.isSuccess(), result.getTime(), result.getResult());
            });
        }

        private void TestHttpSpeed(int maxConn, int timeout) 
        {
            clearData();
            button1.Enabled = false;
            button3.Enabled = false;

            Task runTask = Task.Run( ()=> {
                Semaphore semaphore = new Semaphore(maxConn, maxConn);

                foreach (var item in ipList) {
                    semaphore.WaitOne();
                
                    Task task = Task.Run( ()=> {
                        HttpResult result = HttpHeader.GetHttpConnectionStatus(Hostname, item.Key, timeout);

                        BeginInvoke(new UpdateListViewDelegate(UpdateListView), 
                            item.Key, result.isSuccess(), result.getTime(), result.getResult());

                        semaphore.Release();
                    } );
                }
            });
        }

        private void getCurrentHosts(string hostname)
        {
            string ip = Hosts.getIPByDomain(hostname);
            label3.Text = ip;

            label4.ForeColor = Color.Black;
            label4.Text = "（测试中...）";

            testSingleHttpWithIp(ip, MaximumTimeout);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Text = "更新中...";
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;

            System.IO.File.Copy("ip.json", "ip.json.bak", true);

            string result = UpdateIp.UpdateIpLibrary();

            if (result.Equals("OK"))
            {
                MessageBox.Show("更新本地IP库文件成功！", "SteamHosts");
            } else
            {
                if (System.IO.File.Exists("ip.json.bak"))
                {
                    System.IO.File.Copy("ip.json.bak", "ip.json", true);
                }
                MessageBox.Show("更新本地IP库文件失败！原因：" + result, "SteamHosts");
            }

            System.IO.File.Delete("ip.json.bak");

            initialize();

            button3.Text = "更新IP列表";
            button1.Enabled = true;
            button3.Enabled = true;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://weibo.com/511200124/");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("确定要清除Steam的hosts吗？", "SteamHosts", MessageBoxButtons.YesNo) 
                == DialogResult.Yes)
            {
                Hosts.removeHostsItem(Hostname);
                string result = Hosts.removeHostsItem("steamcommunity.com");

                if (result.Equals("OK"))
                {
                    MessageBox.Show("清除Steam hosts成功！", "SteamHosts");
                } else
                {
                    MessageBox.Show("清除Steam hosts失败！原因：" + result, "SteamHosts");
                }
                getCurrentHosts(Hostname);
            }
        }
    }
}
