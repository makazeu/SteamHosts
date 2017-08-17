using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Linq;
using System.Drawing;
using System.Text.RegularExpressions;

namespace SteamHosts
{
    public partial class Form1 : Form
    {
        const string Hostname = "store.steampowered.com";
        const int MaximumConnection = 30;
        const int MaximumTimeout = 4; // seconds
        const string TimeUnitString = " ms";
        const int WatchModeTimeInterval = 15;// seconds
        Regex IPRegex = new Regex(@"\d+\.\d+\.\d+\.\d+");

        System.Timers.Timer t;
        Dictionary<String, String> ipList;
        int foundCount;
        int minTime;
        string minIp;
        bool isTesting = false;

        private delegate void UpdateListViewDelegate(string ip, bool success, int time, string result);

        private delegate void updateCurrentIpResultDelegate(bool success, int time, string result);

        private delegate void OnWatchModeDelegate();

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
                    }
                    catch (Exception)
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
                isTesting = false;
                //开启监视模式
                if (button5.Text == "禁用监视")
                {
                    Hosts.ChangeHosts("steamcommunity.com", minIp);
                    Hosts.ChangeHosts(Hostname, minIp);
                    getCurrentHosts(Hostname);
                    t.Start();
                }
                //监视模式被放弃
                if (button5.Text == "等待结束...")
                {
                    t.Stop();
                    button5.Text = "启用监视";
                    button5.Enabled = true;
                }
            }
        }

        private void updateCurrentIpResult(bool success, int time, string result)
        {
            if (success)
            {
                label4.ForeColor = Color.Green;
                label4.Text = "（连接成功，耗时：" + time + " ms）";
            }
            else
            {
                label4.ForeColor = Color.Red;
                label4.Text = "（" + result + "）";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            getCurrentHosts(Hostname);

            initialize();

            //初始化Timer
            t = new System.Timers.Timer(WatchModeTimeInterval * 1000);
            t.Elapsed += (tsender, te) =>
            {
                BeginInvoke(new OnWatchModeDelegate(OnWatchMode));
                //TODO:记录数据到json
            };
            t.AutoReset = true;
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
            }
            else
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
            }
            catch
            {
                //
            }

            if (selectedIP != null)
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
            Task task = Task.Run(() =>
            {
                HttpResult result = HttpHeader.GetHttpConnectionStatus(Hostname, ip, timeout);

                BeginInvoke(new updateCurrentIpResultDelegate(updateCurrentIpResult),
                    result.isSuccess(), result.getTime(), result.getResult());
            });
        }

        private void TestHttpSpeed(int maxConn, int timeout)
        {
            isTesting = true;
            clearData();
            button1.Enabled = false;
            button3.Enabled = false;

            Task runTask = Task.Run(() =>
            {
                Semaphore semaphore = new Semaphore(maxConn, maxConn);

                foreach (var item in ipList)
                {
                    semaphore.WaitOne();

                    Task task = Task.Run(() =>
                    {
                        HttpResult result = HttpHeader.GetHttpConnectionStatus(Hostname, item.Key, timeout);

                        BeginInvoke(new UpdateListViewDelegate(UpdateListView),
                            item.Key, result.isSuccess(), result.getTime(), result.getResult());

                        semaphore.Release();
                    });
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
            }
            else
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
            if (MessageBox.Show("确定要清除Steam的hosts吗？", "SteamHosts", MessageBoxButtons.YesNo)
                == DialogResult.Yes)
            {
                Hosts.removeHostsItem(Hostname);
                string result = Hosts.removeHostsItem("steamcommunity.com");

                if (result.Equals("OK"))
                {
                    MessageBox.Show("清除Steam hosts成功！", "SteamHosts");
                }
                else
                {
                    MessageBox.Show("清除Steam hosts失败！原因：" + result, "SteamHosts");
                }
                getCurrentHosts(Hostname);
            }
        }

        /// <summary>
        /// 监视模式
        /// </summary>
        /// <remarks>
        /// 原理为每隔WatchModeTimeInterval时间检测一次当前hosts
        /// 如果连接成功，则继续使用
        /// 如果连接失败，则停止timer计时，启用一个临时可用的hosts，重启全部测试
        /// 在全部测试完成后timer会自动开启，重新开始监视模式循环
        /// </remarks>
        private void OnWatchMode()
        {
            HttpResult result = HttpHeader.GetHttpConnectionStatus(Hostname, minIp, MaximumTimeout);

            updateCurrentIpResult(result.isSuccess(), result.getTime(), result.getResult());

            if (!result.isSuccess())
            {
                //停止timer计时，等待下一次全部测试完成后启动
                t.Stop();
                //临时使用一个可用的hosts
                var ips = from DataGridViewRow row in dataGridView1.Rows
                          where row.Cells["ip"].Value is string && IPRegex.IsMatch(row.Cells["ip"].Value.ToString())
                          orderby row.Cells["time"].Value.ToString()
                          select row.Cells["ip"].Value.ToString();
                foreach (var ip in ips)
                {
                    HttpResult res = HttpHeader.GetHttpConnectionStatus(Hostname, ip, MaximumTimeout);
                    if (res.isSuccess())
                    {
                        //更新hosts
                        String hostsResult;
                        hostsResult = Hosts.ChangeHosts("steamcommunity.com", ip);
                        hostsResult = Hosts.ChangeHosts(Hostname, ip);
                        getCurrentHosts(Hostname);
                        break;
                    }
                    else
                        continue;
                }
                //重启全部测试
                TestHttpSpeed(MaximumConnection, MaximumTimeout);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (button5.Text == "启用监视")
            {
                TestHttpSpeed(MaximumConnection, MaximumTimeout);
                button5.Text = "禁用监视";
            }
            else
            {
                if (isTesting)
                {
                    button5.Text = "等待结束...";
                    button5.Enabled = false;
                }
                else
                {
                    t.Stop();
                    button5.Text = "启用监视";
                }
            }
        }
    }
}
