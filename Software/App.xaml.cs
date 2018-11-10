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
        static NotifyIcon notifyIcon = new NotifyIcon();

        [STAThread]
        static void Main(string[] args)
        {
            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Text = "Tray Icon of Greatness";

            var logger = new SerilogLoggingProvider();
            logger.Info("Software of Greatness starting.");

            var cancellationTokenSource = new CancellationTokenSource();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Exit", null, (s, e) => { cancellationTokenSource.Cancel(); });
            notifyIcon.ContextMenuStrip = contextMenu;

            new Thread(() => MainLoop.Run(cancellationTokenSource, logger)).Start();
            cancellationTokenSource.Token.Register(() => { Application.Exit(); });

            notifyIcon.Visible = true;

            // Standard message loop to catch click-events on notify icon
            // Code after this method will be running only after Application.Exit()
            Application.Run();
            logger.Info("Softwware of Greatness exiting.");
            notifyIcon.Visible = false;
        }
    }
}
