using LibUsbDotNet;
using LibUsbDotNet.Main;
using Software.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Software
{
    class MainLoop
    {
        private const byte ChannelCount = 4;

        public static void Run(CancellationToken exitToken)
        {

            Channel[] channels = LuaManager.StartLua();

            UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x6b56, 0x8802);
            UsbDevice MyUsbDevice;


            MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);

            // If the device is open and ready
            if (MyUsbDevice == null) throw new Exception("Device Not Found.");

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
                if (ecRead != ErrorCode.None) throw new Exception("Submit Async Read Failed.");


                if (transferredIn > 0)
                {

                    string hex = BitConverter.ToString(readBuffer);

                    for (int i = 0; i < ChannelCount; i++)
                    {
                        enc[i] += (sbyte)(readBuffer[6 + 2 * i]);
                        byte[] newLedState, newLcdImage;
                        channels[i].HandleFrame((sbyte)readBuffer[6 + 2 * i], readBuffer[7 + 2 * i], out newLedState, out newLcdImage);
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
                            Debug.Assert(buffer.Count() == 52);

                            byte[] bytesToSend = buffer.ToArray();

                            int transferredOut;
                            ErrorCode ecWrite = Writer4.Transfer(bytesToSend, 0, bytesToSend.Length, 100, out transferredOut);
                            if (ecWrite != ErrorCode.None)
                            {
                                // usbReadTransfer.Dispose();
                                throw new Exception("Submit Async Write Failed.");
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
                                    throw new Exception("Submit Async Write Failed.");
                                }
                                else
                                {
                                    Console.WriteLine("Wrote to LCD {0}", lcdCursor);
                                }
                                break;
                            }
                            lcdCursor = (byte)((lcdCursor + 1) % ChannelCount);
                        }
                    }


                }
                else
                {
                    Console.WriteLine("Didn't get an interrupt packet?????");
                }

            } while (!(exitToken.IsCancellationRequested));

        }
    }
}
