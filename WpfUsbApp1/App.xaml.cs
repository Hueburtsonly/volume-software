using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
//using System.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace WpfUsbApp1
{


    public partial class App : System.Windows.Application
    {
        static NotifyIcon notifyIcon = new NotifyIcon();
        static bool Visible = false;

        [STAThread]
        static void Main(string[] args)
        {

            MainWindow win = new MainWindow();

            notifyIcon.DoubleClick += (s, e) =>
            {
                
                //SetConsoleWindowVisibility(Visible);
            };
            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Visible = true;
            notifyIcon.Text = Application.ProductName;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show Window", null, (s, e) => { Visible = !Visible; if (Visible) win.Show(); else win.Hide(); });
            contextMenu.Items.Add("Exit", null, (s, e) => { win.stopUsbLoop();  });
            win.Closed += new EventHandler((s, e) => {  Application.Exit(); });
            notifyIcon.ContextMenuStrip = contextMenu;

            Console.WriteLine("Running!");

            // Standard message loop to catch click-events on notify icon
            // Code after this method will be running only after Application.Exit()
            Application.Run();

            notifyIcon.Visible = false;
        }
    }
}
