using LibUsbDotNet;
using LibUsbDotNet.Main;
using LibUsbDotNet.LibUsb;
using Software.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Software.Configuration;
using Software.Logging;

namespace Software
{
    class MainLoop
    {
        private const byte ChannelCount = 8;
        private static LoggingProvider _logger;
        private static CancellationTokenSource _cancellationTokenSource;
        private static ScriptManager _scriptManager = null;
        private static byte[][] _wantedLedState = new byte[ChannelCount][]; // 21
        private static Renderable[] _wantedLcdRenderable = new Renderable[ChannelCount];
        private static Boolean _shouldLogConnection = true;
        public static bool _shouldReloadConfig = true;

        public static void Run(CancellationTokenSource cancellationTokenSource, LoggingProvider logger)
        {
            _cancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var config = new SoftwareConfiguration(new AppConfigurationValueProvider());

            while (_shouldReloadConfig)
            {
                _shouldReloadConfig = false;
                try
                {
                    if (_scriptManager != null)
                    {
                        _scriptManager.Dispose();
                    }
                    _scriptManager = new ScriptManager(_logger, config);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    _cancellationTokenSource.Cancel();
                }

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        App.notifyIcon.Text = "Trying to connect...";
                        App.notifyIcon.Icon = Software.Properties.Resources.SearchingIcon;
                        TheActualLoop();
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                        System.Threading.Thread.Sleep(1000);
                    }
                    if (_shouldReloadConfig)
                    {
                        break;
                    }
                }
            }
        }

        private static void TheActualLoop()
        {

            var vid = 0x6b56;
            var pid = 0x8802;
            if (_shouldLogConnection)
            {
                _logger.Info($"Connecting to VID: {vid} PID: {pid}");
            }

            using var usbContext = new UsbContext();
            // Disposed before usbContext (reverse declaration order). Left to the
            // finalizer, the device would be unreferenced after its context is gone,
            // which crashes the process.
            using var MyUsbDevice = usbContext.Find(d => d.VendorId == vid && d.ProductId == pid);

            // If the device is open and ready
            if (MyUsbDevice == null || MyUsbDevice.TryOpen() == false)
            {
                if (_shouldLogConnection)
                {
                    _logger.Warn("Device Not Found.");
                    _shouldLogConnection = false;
                }
                System.Threading.Thread.Sleep(1000);
                return;
            }

            _shouldLogConnection = true;
            _logger.Info("Connected with great success.");
            App.notifyIcon.Text = "Tray Icon of Greatness";
            App.notifyIcon.Icon = Software.Properties.Resources.MainIcon;

            // Select config #1. Not every driver lets libusb-1.0 change the
            // configuration; where it refuses, config #1 is already active.
            try
            {
                MyUsbDevice.SetConfiguration(1);
            }
            catch (UsbException e)
            {
                _logger.Debug($"SetConfiguration(1) not applied: {e.Message}");
            }

            // Claim interface #0.
            MyUsbDevice.ClaimInterface(0);


            UsbEndpointWriter Writer3 = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep03);
            UsbEndpointWriter Writer4 = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep04);
            UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep03);

            sbyte[] enc = new sbyte[ChannelCount];
            byte[][] actualLedState = new byte[ChannelCount][];
            Renderable[] actualLcdRenderable = new Renderable[ChannelCount];
            byte ledCursor = 0;
            byte lcdCursor = 0;

            bool firstLoop = true;

            do
            {
                int transferredIn;
                byte[] readBuffer = new byte[38];
                Error ecRead = reader.Read(readBuffer, 0, readBuffer.Length, 1000, out transferredIn);
                if (ecRead != Error.Success)
                {
                    throw new Exception($"Submit Async Read Failed. ErrorCode: {ecRead}");
                }

                if (transferredIn > 0)
                {
                    ushort touchReading = (ushort)((ushort)readBuffer[2] | (ushort)(((ushort)readBuffer[3]) << 8));
                    ushort ambientReading = (ushort)((ushort)readBuffer[4] | (ushort)(((ushort)readBuffer[5]) << 8));
                    ambientReading = readBuffer[1];
                    for (int i = 0; i < ChannelCount; i++)
                    {
                        sbyte newenc = (sbyte)(readBuffer[6 + 2 * i]);
                        sbyte encdiff = (sbyte)(firstLoop ? 0 : newenc - enc[i]);
                        enc[i] = newenc;
                        byte[] newLedState;
                        Renderable newLcdRenderable;
                        _scriptManager.channels[i].HandleFrame(encdiff, readBuffer[7 + 2 * i], touchReading, ambientReading, out newLedState, out newLcdRenderable);
                        if (newLedState != null) _wantedLedState[i] = newLedState;
                        if (newLcdRenderable != null) _wantedLcdRenderable[i] = newLcdRenderable;
                    }

                    {
                        IEnumerable<byte> buffer = new byte[0];
                        for (int i = 0; i < ChannelCount && buffer.Count() < 52; i++)
                        {
                            if (_wantedLedState[ledCursor] != null && (actualLedState[ledCursor] == null || !_wantedLedState[ledCursor].SequenceEqual(actualLedState[ledCursor])))
                            {
                                byte[] wanted = _wantedLedState[ledCursor];
                                buffer = buffer.Concat(new byte[] {
                                    ledCursor, 0,
                                    wanted[0], wanted[1], wanted[2], wanted[3], wanted[4], wanted[5], wanted[6], wanted[7], wanted[8], wanted[9], wanted[20], wanted[20],
                                    wanted[10], wanted[11], wanted[12], wanted[13], wanted[14], wanted[15], wanted[16], wanted[17], wanted[18], wanted[19], wanted[20], wanted[20]});
                                actualLedState[ledCursor] = wanted;
                            }
                            ledCursor = (byte)((ledCursor + 1) % ChannelCount);
                        }
                        if (buffer.Count() != 0)
                        {
                            if (buffer.Count() == 26)
                            {
                                buffer = buffer.Concat(buffer);
                            }

                            byte[] bytesToSend = buffer.ToArray();

                            int transferredOut;
                            Error ecWrite = Writer4.Write(bytesToSend, 0, bytesToSend.Length, 100, out transferredOut);
                            if (ecWrite != Error.Success)
                            {
                                // usbReadTransfer.Dispose();
                                throw new Exception($"Submit Async Write Failed on Writer4. ErrorCode: {ecWrite}");
                            }
                        }
                    }
                    {
                        for (int i = 0; i < ChannelCount; i++)
                        {
                            if (_wantedLcdRenderable[lcdCursor] != null && (actualLcdRenderable[lcdCursor] == null || !_wantedLcdRenderable[lcdCursor].Equals(actualLcdRenderable[lcdCursor])))
                            {
                                byte[] bytesToSend = (actualLcdRenderable[lcdCursor] = _wantedLcdRenderable[lcdCursor]).Render();

                                bytesToSend = new byte[] { 8, 2, lcdCursor, 0 }.Concat(bytesToSend).Concat(new byte[] { 0, 0, 0, 0 }).ToArray();

                                int transferredOut;
                                Error ecLcdWrite = Writer3.Write(bytesToSend, 0, bytesToSend.Length, 900, out transferredOut);
                                if (ecLcdWrite != Error.Success)
                                {
                                    // usbReadTransfer.Dispose();
                                    throw new Exception($"Submit Async Write Failed on Writer3. ErrorCode: {ecLcdWrite}");
                                }
                                else
                                {
                                    _logger.Info($"Wrote to LCD {lcdCursor}");
                                }
                                break;
                            }
                            lcdCursor = (byte)((lcdCursor + 1) % ChannelCount);
                        }
                    }

                }
                else
                {
                    _logger.Warn("Didn't get an interrupt packet?????");
                }

                firstLoop = false;
            } while (!_cancellationTokenSource.Token.IsCancellationRequested && !_shouldReloadConfig);

            MyUsbDevice.ReleaseInterface(0);
            MyUsbDevice.Close();
        }
    }
}
