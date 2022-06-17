using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MarbleScroll
{
    public partial class MarbleForm : Form
    {
        public MarbleForm()
        {
            InitializeComponent();
            InitializeSystray();

            Hide();
        }

        protected override void OnLoad(EventArgs e)
        {
            this.Hide();
            base.OnLoad(e);
        }

        private void InitializeSystray()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MarbleForm));
            Container components = new Container();

            ToolStripMenuItem Exit = new ToolStripMenuItem();
            Exit.Text = "Exit";
            Exit.Click += new EventHandler(Exit_Click);

            ContextMenuStrip trayMenu = new ContextMenuStrip(components);
            trayMenu.ResumeLayout(false);
            trayMenu.Items.AddRange(new ToolStripMenuItem[1] {
                Exit
            });

            NotifyIcon trayIcon = new NotifyIcon(components);
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Icon = (Icon) resources.GetObject("$this.Icon");
            trayIcon.Text = "MarbleScroll";
            trayIcon.Visible = true;
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Close();
            Application.Exit();
        }

    }
}
