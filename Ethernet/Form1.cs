using SharpPcap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Ethernet
{
    public partial class Form1 : Form
    {
        #region 属性和字段

        private ICaptureDevice device;//当前设备

        private Thread backgroundThread;//封装原始包的进程

        private bool backgroundThreadStop;//是否手动停止后台线程

        private object QueueLock = new object();

        private List<RawCapture> PacketQueue = new List<RawCapture>();//接收的原始包数据 

        private int packetCount;//接收包数量和序号

        private ICaptureStatistics captureStatistics;//统计信息 

        private bool statisticsUiNeedsUpdate = false;
        #endregion
        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string ver = SharpPcap.Version.VersionString;
            //Console.WriteLine("SharpPcap {0}, Example1.IfList.cs", ver);
            txtTips.AppendText(string.Format("SharpPcap {0}, Example1.IfList.cs\r\n", ver));
            // Retrieve the device list
            var devices = CaptureDeviceList.Instance;
            // If no devices were found print an error
            if (devices.Count < 1)
            {
                txtTips.AppendText(string.Format("No devices were found on this machine\r\n"));
                return;
            }
            txtTips.AppendText(string.Format("\r\nThe following devices are available on this machine:"));
            txtTips.AppendText(string.Format("----------------------------------------------------\r\n"));
            /* Scan the list printing every entry */
            foreach (var dev in devices)
            {
                txtTips.AppendText(string.Format("{0}\r\n\r\n\r\n", dev.ToString()));
                EthernetPort.Items.Add("" + dev.Description.Substring(16));
            }
             
            //Console.ReadLine();
            //————————————————
            //版权声明：本文为CSDN博主「gaohuaid」的原创文章，遵循 CC 4.0 BY - SA 版权协议，转载请附上原文出处链接及本声明。
            //原文链接：https://blog.csdn.net/gaohuaid/article/details/8853587
        }

        private void OpenNet_Click(object sender, EventArgs e)
        {
            if(EthernetPort.SelectedIndex>-1)
            {
                StartCapture(EthernetPort.SelectedIndex);
            }
            else
            {
                MessageBox.Show("请选择网口");
            }
        }

        private void StartCapture(int itemIndex)
        {
            device = CaptureDeviceList.Instance[itemIndex];
            OpenNet.Enabled = false;
            CloseNet.Enabled = true;
            device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
             

            device.Open(DeviceMode.Promiscuous);        //打开混杂模式
            device.StartCapture();
            if (device.Started)
            { 
                backgroundThread = new Thread(BackgroundThread); 
            }

        }
        //后台线程进行贞处理
        private void BackgroundThread()
        {
            while (!backgroundThreadStop)
            {
                bool shouldSleep = true;

                lock (QueueLock)
                {
                    if (PacketQueue.Count != 0)
                    {
                        shouldSleep = false;
                    }
                } 
                if (shouldSleep)
                {
                    Thread.Sleep(250);
                }
                else // should process the queue
                {

                }
            }
        }
        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            // print out periodic statistics about this device
            var Now = DateTime.Now;
            byte[] data = e.Packet.Data;
            StringBuilder ret = new StringBuilder();
            foreach (byte b in data)
            {
                //{0:X2} 大写
                ret.AppendFormat("{0:x2}", b);
            }
            var hex = ret.ToString(); 
            txtRecv.AppendText(Now+":-"+ data .Length+ "bytes.\r\n" + ret.ToString() + "\r\n");
            
        }

        private void CloseNet_Click(object sender, EventArgs e)
        {
            try
            {
                OpenNet.Enabled = true;
                CloseNet.Enabled = false;
                device.StopCapture(); 
                device.Close();
            }catch(Exception exp)
            {
                device.Close();
            }
        }
    }
}
