using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

namespace SteamHosts
{
    public partial class Form1 : Form
    {
        const string Hostname = "store.steampowered.com";
        const int MaximumConnection = 30;
        const int MaximumTimeout = 3; // seconds

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
            ListViewItem item = listView1.FindItemWithText(ip, true, 0);

            if (success)
            {
                item.SubItems[2].Text = time.ToString();

                if(time < minTime)
                {
                    try
                    {
                        listView1.FindItemWithText(minIp, true, 0).BackColor = Color.White;
                    } catch(Exception)
                    {
                        //
                    }

                    minTime = time;
                    minIp = ip;

                    item.BackColor = Color.LightPink;
                    listView1.TopItem = item;

                    button2.Enabled = true;
                }
            }
            else
                item.SubItems[2].Text = "连接失败";

            //foundCount++;
            Interlocked.Increment(ref foundCount);
            if (foundCount == ipList.Count)
            {
                button1.Enabled = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ipList = JsonReader.readIpList("ip.json");
            int index = 1;
            listView1.BeginUpdate();
            foreach (var item in ipList)
            {
                ListViewItem lvItem = new ListViewItem();
                lvItem.Text = index.ToString();
                lvItem.SubItems.Add(item.Key);
                lvItem.SubItems.Add("");
                lvItem.SubItems.Add(item.Value);

                listView1.Items.Add(lvItem);
                index++;
            }
            listView1.EndUpdate();
        }

        private void clearData()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            foundCount = 0;
            minTime = 99999;
            minIp = null;

            listView1.BeginUpdate();
            foreach(ListViewItem item in listView1.Items)
            {
                item.BackColor = Color.White;
                item.SubItems[2].Text = "";
            }
            listView1.EndUpdate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TestHttpSpeed(MaximumConnection, MaximumTimeout);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if( minIp != null )
            {
                bool hostsResult = Hosts.ChangeHosts(Hostname, minIp);
                if (hostsResult)
                {
                    MessageBox.Show("设置hosts成功！");
                    return;
                }
            }
            MessageBox.Show("设置hosts失败！");
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
    }
}
