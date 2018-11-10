﻿using LibUsbDotNet;
using LibUsbDotNet.Main;
using Software.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Software.Logging;

namespace Software
{
    class MainLoop
    {
        private const byte ChannelCount = 4;
        private static LoggingProvider _logger;
        private static CancellationTokenSource _cancellationTokenSource;

        public static void Run(CancellationTokenSource cancellationTokenSource, LoggingProvider logger)
        {
            _cancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Channel[] channels = LuaManager.StartLua();

            var vid = 0x6b56;
            var pid = 0x8802;
            _logger.Info($"Connecting to VID: {vid} PID: {pid}");

            UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(vid, pid);
            var MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);

            // If the device is open and ready
            if (MyUsbDevice == null)
            {
                _logger.Error("Device Not Found.");
                ExitTray();
                return;
            }

            // If this is a "whole" usb device (libusb-win32, linux libusb)
            // it will have an IUsbDevice interface. If not (WinUSB) the 
            // variable will be null indicating this is an interface of a 
            // device.
            IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
            if (!ReferenceEquals(wholeUsbDevice, null))
            {
                // This is a "whole" USB device. Before it can be used, 
                // the desired configuration and interface must be selected.

                // Select config #1
                wholeUsbDevice.SetConfiguration(1);

                // Claim interface #2.
                wholeUsbDevice.ClaimInterface(2);
            }


            UsbEndpointWriter Writer3 = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep03);
            UsbEndpointWriter Writer4 = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep04);
            UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep03);

            //Channel[] channels = { new VolumeChannel("Teamspeak", "ts3client_win64.exe"), new VolumeChannel("Rocket", "RocketLeague.exe"), new VolumeChannel("Chrome", "chrome.exe"), new VolumeChannel("XXX", "XXX") };
            
            int[] enc = new int[ChannelCount];
            byte[][] wantedLedState = new byte[ChannelCount][]; // 21
            byte[][] wantedLcdImage = new byte[ChannelCount][]; // 512
            byte[][] actualLedState = new byte[ChannelCount][];
            byte[][] actualLcdImage = new byte[ChannelCount][];
            byte ledCursor = 0;
            byte lcdCursor = 0;

            do
            {
                int transferredIn;
                byte[] readBuffer = new byte[38];
                ErrorCode ecRead = reader.Transfer(readBuffer, 0, readBuffer.Length, 100, out transferredIn);
                if (ecRead != ErrorCode.None)
                {
                    _logger.Error($"Submit Async Read Failed. ErrorCode: {ecRead}");
                    return;
                }


                if (transferredIn > 0)
                {

                    ushort touchReading = (ushort)((ushort)readBuffer[2] | (ushort)(((ushort)readBuffer[3]) << 8));
                    ushort ambientReading = (ushort)((ushort)readBuffer[4] | (ushort)(((ushort)readBuffer[5]) << 8));

                    for (int i = 0; i < ChannelCount; i++)
                    {
                        enc[i] += (sbyte)(readBuffer[6 + 2 * i]);
                        byte[] newLedState, newLcdImage;
                        channels[i].HandleFrame((sbyte)readBuffer[6 + 2 * i], readBuffer[7 + 2 * i], touchReading, ambientReading, out newLedState, out newLcdImage);
                        if (newLedState != null) wantedLedState[i] = newLedState;
                        if (newLcdImage != null) wantedLcdImage[i] = newLcdImage;
                    }

                    {
                        IEnumerable<byte> buffer = new byte[0];
                        for (int i = 0; i < ChannelCount && buffer.Count() < 52; i++)
                        {
                            if (wantedLedState[ledCursor] != null && (actualLedState[ledCursor] == null || !wantedLedState[ledCursor].SequenceEqual(actualLedState[ledCursor])))
                            {
                                byte[] wanted = wantedLedState[ledCursor];
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
                            ErrorCode ecWrite = Writer4.Transfer(bytesToSend, 0, bytesToSend.Length, 100, out transferredOut);
                            if (ecWrite != ErrorCode.None)
                            {
                                // usbReadTransfer.Dispose();
                                _logger.Error($"Submit Async Write Failed on Writer4. ErrorCode: {ecWrite}");
                                return;
                            }
                        }
                    }
                    {
                        for (int i = 0; i < ChannelCount; i++)
                        {
                            if (wantedLcdImage[lcdCursor] != null && (actualLcdImage[lcdCursor] == null || !wantedLcdImage[lcdCursor].SequenceEqual(actualLcdImage[lcdCursor])))
                            {
                                byte[] bytesToSend = wantedLcdImage[lcdCursor];
                                actualLcdImage[lcdCursor] = bytesToSend;

                                bytesToSend = new byte[] { 4, 2, lcdCursor, 0 }.Concat(bytesToSend).ToArray();

                                int transferredOut;
                                ErrorCode ecLcdWrite = Writer3.Transfer(bytesToSend, 0, bytesToSend.Length, 900, out transferredOut);
                                if (ecLcdWrite != ErrorCode.None)
                                {
                                    // usbReadTransfer.Dispose();
                                    _logger.Error($"Submit Async Write Failed on Writer3. ErrorCode: {ecLcdWrite}");
                                    return;
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

            } while (!(cancellationTokenSource.Token.IsCancellationRequested));

        }

        private static void ExitTray()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
