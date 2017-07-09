using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibUsbDotNet;
using LibUsbDotNet.Internal;
using LibUsbDotNet.Main;
using LibUsbDotNet.LudnMonoLibUsb;
using System.Threading;

namespace WpfUsbApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {



        public MainWindow()
        {



            InitializeComponent();

            Status.Content = "Ready.";

            new Thread(EncoderMonitor).Start();
        }

        private void EncoderMonitor()
        {

            UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x6b56, 0x8802);
            UsbDevice MyUsbDevice;
            UsbEndpointWriter Writer3;
            UsbEndpointWriter Writer4;
            VolumeTemp volumeTemp = new VolumeTemp();

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


            Writer3 = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep03);
            Writer4 = MyUsbDevice.OpenEndpointWriter(WriteEndpointID.Ep04/*, EndpointType.Isochronous*/);

            int enc0 = 0;
            int enc1 = 0;
            int enc2 = 0;
            int enc3 = 0;

            //int counter = 0;

            uint cdisp = 0;
            int[] dispstate = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            float[] slowpeaks = new float[] {0, 0, 0, 0, 0, 0, 0, 0};
            int[] pips = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            int[] ttph = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };

            int[] ttphl = new int[10];
            float[] limits = new float[10];
            for (int i = 0; i < 10; i++)
            {
                limits[i] = (float)(Math.Pow(1.2, i - 10));
                ttphl[i] = (150 - (9-i)*(9-i)) / 5;
                if (i > 0 && ttphl[i] <= ttphl[i - 1])
                    ttphl[i] = ttphl[i - 1] + 1;
            }
            limits[0] = 0;

            ErrorCode ecRead;
            UsbTransfer usbReadTransfer;
            UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep03);
            int transferredIn;
            do
            {

                byte[] readBuffer = new byte[38];
                ecRead = reader.Transfer(readBuffer, 0, readBuffer.Length, 100, out transferredIn);
                if (ecRead != ErrorCode.None) throw new Exception("Submit Async Read Failed.");

                //WaitHandle.WaitAll(new WaitHandle[] { usbReadTransfer.AsyncWaitHandle }, 200, false);
                //if (!usbReadTransfer.IsCompleted) usbReadTransfer.Cancel();

                //ecRead = usbReadTransfer.Wait(out transferredIn);

                //usbReadTransfer.Dispose();

                if (transferredIn > 0)
                {

                    string hex = BitConverter.ToString(readBuffer);

                    enc0 += (sbyte)(readBuffer[6]);
                    enc1 += (sbyte)(readBuffer[8]);
                    enc2 += (sbyte)(readBuffer[10]);
                    enc3 += (sbyte)(readBuffer[12]);

                    float[] peaks;
                    int[] newstate;

                    String msg = "" + volumeTemp.update((sbyte)(readBuffer[6]), (sbyte)(readBuffer[8]), (sbyte)(readBuffer[10]), out peaks, out newstate);

                    //peak1 = 0.5f;

                    
                    this.Dispatcher.Invoke(() =>
                    {
                        Status.Content = "Behold: " + hex;
                        Enc0.Content = (((readBuffer[7] & 1) == 1) ? "PRESS " : "") + ((readBuffer[7] + 1) / 2) + " " + enc0;
                        Enc1.Content = (((readBuffer[9] & 1) == 1) ? "PRESS " : "") + ((readBuffer[9] + 1) / 2) + " " + enc1;
                        Enc2.Content = (((readBuffer[11] & 1) == 1) ? "PRESS " : "") + ((readBuffer[11] + 1) / 2) + " " + enc2;
                        Enc3.Content = (((readBuffer[13] & 1) == 1) ? "PRESS " : "") + ((readBuffer[13] + 1) / 2) + " " + enc3;
                    });  // */


                    //UsbTransfer[] usbWriteTransfers = new UsbTransfer[2];

                    for (int ch = 0; ch < 4; ch++)
                    {
                        slowpeaks[ch] *= 0.998f;
                        if (peaks[ch] > slowpeaks[ch])
                        {
                            slowpeaks[ch] = peaks[ch];
                        }
                        int cpiploc = -1;
                        for (int i = 9; i >= 0; i--)
                        {
                            if (peaks[ch] > limits[i] * slowpeaks[ch])
                            {
                                cpiploc = i;
                                break;
                            }
                        }
                        if (cpiploc >= pips[ch])
                        {
                            pips[ch] = cpiploc;
                            ttph[ch] = 0;
                        }
                        else
                        {
                            ++ttph[ch];
                            for (int i = 0; i < 10; i++) { 
                                if (ttph[ch] == ttphl[i]) --pips[ch];
                            }
                        }
                    }

                    for (int packet = 0; packet < 2; packet++)
                    {

                        ErrorCode ecWrite;
                        var bytesToSend = new byte[50];
                        for (int i = 0; i < 50; i++)
                        {
                            bytesToSend[i] = (((i - 12) % 12) == 0 && i >= 2 && i < 126 - packet * 100) ? (byte)0xff : (byte)0;
                        }
                        bytesToSend[0] = (byte)(packet * 2);
                        for (int i = 0; i < 10; i++)
                        {
                            bytesToSend[2 + 12 + i] = (peaks[0 + packet * 2] > limits[i] * slowpeaks[0 + packet * 2]) ? (byte)0xff : (byte)0;
                            bytesToSend[2 + 36 + i] = (peaks[1 + packet * 2] > limits[i] * slowpeaks[1 + packet * 2]) ? (byte)0xff : (byte)0;
                        }
                        for (int i = 9; i >= 0; i--)
                        {
                            if (i == pips[0 + packet * 2])
                            {
                                bytesToSend[2 + 0 + i] = 0xff;
                                break;
                            }
                        }
                        for (int i = 9; i >= 0; i--)
                        {
                            if (i == pips[1 + packet * 2])
                            {
                                bytesToSend[2 + 24 + i] = 0xff;
                                break;
                            }
                        }

                        int eh;
                        ecWrite = Writer4.Transfer(bytesToSend, 0, bytesToSend.Length, 100, out eh);
                        if (ecWrite != ErrorCode.None)
                        {
                            // usbReadTransfer.Dispose();
                            throw new Exception("Submit Async Write Failed.");
                        }
                    }

                    String dispString = "";
                    if (cdisp < 3 && dispstate[cdisp] != newstate[cdisp])
                    {
                        dispstate[cdisp] = newstate[cdisp];
                        if (newstate[cdisp] == 1)
                        {
                            switch (cdisp)
                            {
                                case 0:
                                    dispString = "Teamspeak";
                                    break;
                                case 1:
                                    dispString = "Rocket League";
                                    break;
                                case 2:
                                    dispString = "Chrome";
                                    break;
                                default:
                                    dispString = "Impossible";
                                    break;
                            }
                        }
                        ErrorCode ecLcdWrite;
                        byte[] bytesLcdToSend = ImageTemp.GenImageStream((byte)cdisp, dispString);
                        int usbLcdWriteTransfer;

                        ecLcdWrite = Writer3.Transfer(bytesLcdToSend, 0, bytesLcdToSend.Length, 100, out usbLcdWriteTransfer);
                        if (ecLcdWrite != ErrorCode.None)
                        {
                            // usbReadTransfer.Dispose();
                            throw new Exception("Submit Async Write Failed.");
                        }
                    }



                } else {
                    Console.WriteLine("Didn't get an interrupt packet?????");
                }

                cdisp = (cdisp + 1) % 8;
            } while (true);
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("If you're seeing this, you are already connected, probably. Hoorah! (probably)");
        }

        private void Ping3_Click(object sender, RoutedEventArgs e)
        {

            //ErrorCode ecLcdWrite;
            //byte[] bytesLcdToSend = ImageTemp.GenImageStream(2, "Rocket League");//new byte[] {4,2,1,0, 15, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 192, 192, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 15, 0, 0, 0, 0, 0, 0, 252, 252, 0, 0, 0, 0, 0, 0, 0, 0, 252, 252, 0, 0, 224, 248, 152, 140, 140, 140, 140, 156, 152, 144, 0, 0, 120, 252, 204, 204, 140, 140, 152, 248, 252, 4, 0, 0, 224, 248, 28, 12, 12, 12, 24, 252, 252, 0, 0, 255, 255, 24, 12, 12, 12, 28, 248, 224, 0, 0, 252, 252, 0, 0, 0, 0, 0, 252, 252, 0, 0, 224, 248, 24, 12, 12, 12, 12, 24, 248, 224, 0, 0, 252, 252, 0, 0, 0, 0, 0, 252, 252, 0, 0, 224, 248, 152, 140, 140, 140, 140, 156, 152, 144, 0, 0, 16, 152, 140, 140, 140, 204, 204, 248, 112, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 6, 6, 6, 6, 6, 6, 6, 6, 255, 255, 0, 0, 3, 15, 13, 25, 25, 25, 25, 13, 15, 3, 0, 0, 12, 12, 24, 24, 25, 25, 25, 31, 15, 0, 0, 0, 3, 15, 28, 24, 24, 24, 12, 255, 255, 0, 0, 31, 31, 12, 24, 24, 24, 28, 15, 3, 0, 0, 255, 255, 12, 24, 24, 24, 28, 15, 7, 0, 0, 3, 15, 12, 24, 24, 24, 24, 12, 15, 3, 0, 0, 31, 31, 12, 24, 24, 24, 28, 15, 7, 0, 0, 3, 15, 13, 25, 25, 25, 25, 13, 15, 3, 0, 0, 7, 15, 25, 25, 25, 24, 24, 12, 4, 0, 0, 0, 0, 0, 0, 0, 240, 128, 128, 128, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128, 128, 128, 240 };
            //UsbTransfer usbLcdWriteTransfer;

            //ecLcdWrite = Writer3.SubmitAsyncTransfer(bytesLcdToSend, 0, bytesLcdToSend.Length, 100, out usbLcdWriteTransfer);
            //if (ecLcdWrite != ErrorCode.None)
            //{
            //    // usbReadTransfer.Dispose();
            //    throw new Exception("Submit Async Write Failed.");
            //}

            //WaitHandle.WaitAll(new WaitHandle[] { usbLcdWriteTransfer.AsyncWaitHandle/*, usbReadTransfer.AsyncWaitHandle*/ }, 200, false);
            //if (!usbLcdWriteTransfer.IsCompleted) usbLcdWriteTransfer.Cancel();


        }

     /*   ~MainWindow()
        {
            if (MyUsbDevice != null)
            {
                if (MyUsbDevice.IsOpen)
                {
                    // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                    // it exposes an IUsbDevice interface. If not (WinUSB) the 
                    // 'wholeUsbDevice' variable will be null indicating this is 
                    // an interface of a device; it does not require or support 
                    // configuration and interface selection.
                    IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                    if (!ReferenceEquals(wholeUsbDevice, null))
                    {
                        // Release interface #2.
                        wholeUsbDevice.ReleaseInterface(2);
                    }

                    MyUsbDevice.Close();
                }
                MyUsbDevice = null;

                // Free usb resources
                UsbDevice.Exit();
            }
            //MessageBox.Show("Destructor successful");
        }
        */
        private void Relist_Click(object sender, RoutedEventArgs e)
        {
           // volumeTemp.OnSessionCreated(null, null);
        }

        

        private void UpdateLeds_Click(object sender, RoutedEventArgs e)
        {
            
            //ErrorCode ecWrite;
            //var bytesToSend = new byte[50];
            //for (int i = 0; i < bytesToSend.Length; i++)
            //{
            //    bytesToSend[i] = 0x00;
            //    if (i >= 2 && i < (2 + upto))
            //    {
            //        bytesToSend[i] = 255;
            //    }
            //}
            //UsbTransfer usbWriteTransfer;
            //int transferredOut;

            //upto += 1;

            //ecWrite = Writer4.SubmitAsyncTransfer(bytesToSend, 0, bytesToSend.Length, 100, out usbWriteTransfer);
            //if (ecWrite != ErrorCode.None)
            //{
            //    // usbReadTransfer.Dispose();
            //    throw new Exception("Submit Async Write Failed.");
            //}

            //WaitHandle.WaitAll(new WaitHandle[] { usbWriteTransfer.AsyncWaitHandle/*, usbReadTransfer.AsyncWaitHandle*/ }, 200, false);
            //if (!usbWriteTransfer.IsCompleted) usbWriteTransfer.Cancel();

            //ecWrite = usbWriteTransfer.Wait(out transferredOut);


            //////////////////////

            ////UsbTransferQueue queue = new UsbTransferQueue()
        }
    }
}
