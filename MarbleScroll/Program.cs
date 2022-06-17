using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MarbleScroll
{
    static class Program
    {

        private static MarbleScroll marbleScroll;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            marbleScroll = new MarbleScroll();
            marbleScroll.Start();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += new EventHandler(Exit);
            MarbleForm form = new MarbleForm();
            Application.Run(); 
        }

        static void Exit(object sender, EventArgs e)
        {
            marbleScroll.Stop();
        }
    }
}
