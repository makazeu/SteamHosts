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
        const int MaximumTimeout = 3; // seconds
        const string UnableToConnect = "连接失败";
        const string TimeUnitString = " ms";

        Dictionary<String, String> ipList;
        int foundCount;
        int minTime;
        string minIp;

        private delegate void UpdateListViewDelegate(string ip, bool success, int time);

        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateListView(string ip, bool success, int time)
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

                    button2.Enabled = true;
                }
            }
            else
            {
                row.Cells["time"].Value = UnableToConnect;
            }

            //foundCount++;
            Interlocked.Increment(ref foundCount);
            if (foundCount == ipList.Count)
            {
                button1.Enabled = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            getCurrentHosts(Hostname);
            ipList = JsonReader.readIpList("ip.json");

            foreach (var item in ipList)
            {
                int index = dataGridView1.Rows.Add();
                dataGridView1.Rows[index].Cells[0].Value = item.Key;
                dataGridView1.Rows[index].Cells[1].Value = "";
                dataGridView1.Rows[index].Cells[2].Value = item.Value;
            }
        }

        private void clearData()
        {
            button1.Enabled = false;
            button2.Enabled = false;
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
            if ( minIp != null )
            {
                hostsResult = Hosts.ChangeHosts("steamcommunity.com", dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["ip"].Value.ToString());
                hostsResult = Hosts.ChangeHosts(Hostname, dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells["ip"].Value.ToString());
                if (hostsResult == "OK")
                {
                    MessageBox.Show("设置hosts成功！");
                    getCurrentHosts(Hostname);
                    return;
                }
            }
            MessageBox.Show("设置hosts失败，原因：" + hostsResult);
        }

        private void TestHttpSpeed(int maxConn, int timeout) 
        {
            clearData();

            Task runTask = Task.Run( ()=> {
                Semaphore semaphore = new Semaphore(maxConn, maxConn);

                foreach (var item in ipList) {
                    semaphore.WaitOne();
                
                    Task task = Task.Run( ()=> {
                        HttpResult result = HttpHeader.GetHttpConnectionStatus(Hostname, item.Key, timeout);

                        if (result.isSuccess()) {
                            //Console.Write(item.Key + "  ");
                            //Console.WriteLine("Success! " + result.getTime());
                        }

                        BeginInvoke(new UpdateListViewDelegate(UpdateListView), 
                            item.Key, result.isSuccess(), result.getTime());

                        semaphore.Release();
                    } );
                }
            });
        }

        private void getCurrentHosts(string hostname)
        {
            string ip = Hosts.getIPByDomain(hostname);
            label3.Text = ip;
        } 
    }
}
