using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Software
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        static NotifyIcon notifyIcon = new NotifyIcon();

        [STAThread]
        static void Main(string[] args)
        {
            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Text = "Tray Icon of Greatness";

            var exitToken = new CancellationTokenSource();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Exit", null, (s, e) => { exitToken.Cancel(); });
            notifyIcon.ContextMenuStrip = contextMenu;

            new Thread(() => MainLoop.Run(exitToken.Token)).Start();
            exitToken.Token.Register(() => { Application.Exit(); });

            notifyIcon.Visible = true;

            // Standard message loop to catch click-events on notify icon
            // Code after this method will be running only after Application.Exit()
            Application.Run();

            notifyIcon.Visible = false;
        }
    }
}
