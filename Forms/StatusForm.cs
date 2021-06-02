using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using CRCTray.Helpers;
using CRCTray.Communication;

namespace CRCTray
{
    public partial class StatusForm : Form
    {
        delegate void UpdateStatusCallback(StatusResult status);
        Timer UpdateStatusTimer;

        public StatusForm()
        {
            InitializeComponent();

            TaskHandlers.StatusReceived += UpdateStatus;
        }

        private void StatusForm_Load(object sender, EventArgs e)
        {
            // deal with async behaviour
            const string _3dot = "...";
            CrcStatus.Text = _3dot;
            OpenShiftStatus.Text = _3dot;
            DiskUsage.Text = _3dot;
            CacheUsage.Text = _3dot;
            CacheFolder.Text = _3dot;

            Bitmap bm = new Bitmap(Resource.ocp_logo);
            Icon = Icon.FromHandle(bm.GetHicon());
            Text = @"Status and Logs";

            this.FormClosing += StatusForm_Closing;
            this.Activated += StatusForm_Activated;

            UpdateStatusTimer = new Timer();
            UpdateStatusTimer.Interval = 3000; // 3 seconds
            UpdateStatusTimer.Enabled = true;
            UpdateStatusTimer.Tick += Timer_Tick;
        }

        private void StatusForm_Activated(object sender, EventArgs e)
        {
            UpdateStatusTimer.Enabled = true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("Updating status and logs");

            // Run as task to make sure it doesn't block this window
            Task.Run(TaskHandlers.Status);
            GetLogs();
        }

        private void StatusForm_Closing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            UpdateStatusTimer.Enabled = false;
            e.Cancel = true;
        }

        private void UpdateStatus(StatusResult status)
        {
            if (status != null)
            {
                if (CrcStatus.InvokeRequired)
                {
                    UpdateStatusCallback c = UpdateStatus;
                    Invoke(c, status);
                }
                else
                {
                    var cacheFolderPath = string.Format("{0}\\.crc\\cache", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

                    if (status.CrcStatus != "")
                        CrcStatus.Text = status.CrcStatus;

                    if (status.OpenshiftStatus != "")
                        OpenShiftStatus.Text = StatusText(status);

                    DiskUsage.Text = string.Format("{0} of {1} (Inside the CRC VM)", FileSize.HumanReadable(status.DiskUse), FileSize.HumanReadable(status.DiskSize));
                    CacheUsage.Text = FileSize.HumanReadable(GetFolderSize.SizeInBytes(cacheFolderPath));
                    CacheFolder.Text = cacheFolderPath;
                }
            }
        }
        
        private static string StatusText(StatusResult status)
        {
            var ret = "";
            if (!string.IsNullOrEmpty(status.OpenshiftStatus))
                ret += status.OpenshiftStatus;
            if (!string.IsNullOrEmpty(status.OpenshiftVersion))
                ret += string.Format(" (v{0})", status.OpenshiftVersion);
            return ret;
        }


        private async void GetLogs()
        {
            var logs = await Task.Run(TaskHandlers.GetDaemonLogs);
            if (logs != null)
            {
                var messages = string.Join("\r\n", logs.Messages);

                if (logsTextBox.Text == messages)
                    return;

                logsTextBox.Text = messages;
                logsTextBox.SelectionStart = logsTextBox.Text.Length;
                logsTextBox.ScrollToCaret();
            }
        }
    }

    public static class FileSize
    {
        // Load all suffixes in an array  
        static readonly string[] suffixes =
        { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public static string HumanReadable(long bytes)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

    }

    public static class GetFolderSize
    {
        public static long SizeInBytes(string path)
        {
            long size = 0;

            var dirInfo = new DirectoryInfo(path);
            try
            {
                foreach (FileInfo fi in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    size += fi.Length;
                }
            }
            catch (Exception e)
            {
                TrayIcon.NotifyError(string.Format("Unexpected Error, did you run 'crc setup'? Error: {0}", e.Message));
            }
            return size;
        }
    }
}
