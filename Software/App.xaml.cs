using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Software.Logging;

namespace Software
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static NotifyIcon notifyIcon = new NotifyIcon();

        [STAThread]
        static void Main(string[] args)
        {
            notifyIcon.Icon = Software.Properties.Resources.SearchingIcon;
            notifyIcon.Text = "Loading...";

            var logger = new SerilogLoggingProvider();
            logger.Info("Software of Greatness starting.");

            var cancellationTokenSource = new CancellationTokenSource();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("&Reload config", null, (s, e) => { MainLoop._shouldReloadConfig = true; });
            contextMenu.Items.Add("E&xit", null, (s, e) => { cancellationTokenSource.Cancel(); });
            notifyIcon.ContextMenuStrip = contextMenu;

            new Thread(() => MainLoop.Run(cancellationTokenSource, logger)).Start();
            cancellationTokenSource.Token.Register(() => { Application.Exit(); });

            notifyIcon.Visible = true;

            // Standard message loop to catch click-events on notify icon
            // Code after this method will be running only after Application.Exit()
            Application.Run();
            logger.Info("Software of Greatness exiting.");
            notifyIcon.Visible = false;
        }
    }
}
