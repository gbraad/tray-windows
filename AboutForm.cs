﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace tray_windows
{
    public partial class AboutForm : Form
    {
        private VersionResult version;
        public AboutForm()
        {
            InitializeComponent();
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            Bitmap bm = new Bitmap(Resource.ocp_logo);
            Icon = Icon.FromHandle(bm.GetHicon());
            Text = @"About";
            FormClosing += AboutForm_FormClosing;
            TrayVersion.Text = Application.ProductVersion;
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Shown += GetVersion;
        }

        private void GetVersion(object sender, EventArgs e)
        {
            var d = new Daemon.DaemonCommander();
            try
            {
                var r = d.GetVersion();
                version = JsonConvert.DeserializeObject<VersionResult>(r);
                if (version.Success) {
                    CrcVersionLabel.Text = String.Format("{0}+{1}", version.CrcVersion, version.CommitSha);
                    OcpVersion.Text = version.OpenshiftVersion;
                }
                else
                {
                    DisplayMessageBox.Warn("Unable to fetch version information from daemon");
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                this.Hide();
                DisplayMessageBox.Warn(ex.Message);
            }
        }
        private void AboutForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true; // this cancels the close event.
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var crcVersionWithGitSha = version.CrcVersion.Split('+');                  
            var v = crcVersionWithGitSha[0].Substring(0, crcVersionWithGitSha[0].Length - 2);
            var docsUrl = string.Format("https://access.redhat.com/documentation/en-us/red_hat_codeready_containers/{0}/", v);
            System.Diagnostics.Process.Start(docsUrl);
        }

        private void linkLabel2_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/code-ready/tray-windows");
        }
    }
}
