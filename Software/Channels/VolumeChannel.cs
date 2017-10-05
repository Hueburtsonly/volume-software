using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Software.Channels
{
    class VolumeChannel : Channel
    {
        static MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        static MMDevice device;
        static AudioSessionManager sessManager;

        static bool shouldRefreshSessions = true;

        static VolumeChannel()
        {
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            sessManager = device.AudioSessionManager;

            //sessManager.AudioSessionControl.RegisterEventClient(this);
            sessManager.OnSessionCreated += new AudioSessionManager.SessionCreatedDelegate(OnSessionCreated);

            for (int i = 0; i < 10; i++)
            {
                ledThresholds[i] = (float)(Math.Pow(1.2, i - 10));
                timesToLowerPip[i] = (150 - (9 - i) * (9 - i)) / 5;
                if (i > 0 && timesToLowerPip[i] <= timesToLowerPip[i - 1])
                    timesToLowerPip[i] = timesToLowerPip[i - 1] + 1;
            }
            ledThresholds[0] = 0;

        }

        public static void OnSessionCreated(object sender, IAudioSessionControl newSession)
        {
            shouldRefreshSessions = true;
        }

        public static void MaybeRefreshSessions()
        {
            if (shouldRefreshSessions)
            {
                shouldRefreshSessions = false;
                sessManager.RefreshSessions();

                Console.WriteLine("");
                int count = sessManager.Sessions.Count;

                for (int i = 0; i < count; i++)
                {
                    AudioSessionControl session = sessManager.Sessions[i];

                    if (session.GetProcessID == 0)
                    {
                        continue;
                    }

                    AudioSessionState state = session.State;

                    String fn = "";

                    try
                    {
                        Process p = Process.GetProcessById((int)(session.GetProcessID));
                        fn = ProcessExecutablePath(p);
                    }
                    catch (Exception e) { }

                    if (state != AudioSessionState.AudioSessionStateExpired)
                    {
                        newSessionEvent(session, fn);



                        Console.WriteLine("\"" + fn + "\" " + session.GetProcessID + " -- " + session.AudioMeterInformation.MasterPeakValue + ((state == AudioSessionState.AudioSessionStateActive) ? " (ACTIVE)" : ""));
                    }
                }
            }
        }

        static private string ProcessExecutablePath(Process process)
        {
            try
            {
                return process.MainModule.FileName;
            }
            catch
            {
                string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject item in searcher.Get())
                {
                    object id = item["ProcessID"];
                    object path = item["ExecutablePath"];

                    if (path != null && id.ToString() == process.Id.ToString())
                    {
                        return path.ToString();
                    }
                }
            }

            return "oopsie";
        }

        // Whenever a new session is created, this event is fired for every non-expired session.
        private static event NewSessionHandler newSessionEvent;
        private delegate void NewSessionHandler(AudioSessionControl session, String filename);

        private static float[] ledThresholds = new float[10];

        // Controls the descent of the red "pip" which stays high for a little while, and then
        // "falls". For each value x in this array, if the pip hasn't been raised for x frames,
        // it is moved down by one LED.
        private static int[] timesToLowerPip = new int[10];

        // Current location of the peak-tracking red "pip", and the number of frames since the 
        // last time it moved.
        private int pipLocation = -1;
        private int timeSincePipRaised = 0;
        private int prevIsActive = -1;

        // Keeps track of the highest peak value seen from this session, slowly decays over time.
        // Used to enable quieter apps to still use the full range of the LED display.
        private float recentMaximum = 0;

        // Counts down the number of frames of silence remaining until this channel is considered
        // inactive.
        private int silenceTimeout = -1;

        private AudioSessionControl session = null;
        private String exeSuffix;
        private String displayName;
        private NewSessionHandler newSessionHandler;

        public VolumeChannel(String displayName, String exeSuffix)
        {
            this.displayName = displayName;
            this.exeSuffix = exeSuffix;
            newSessionEvent += newSessionHandler = new NewSessionHandler(HandleNewSession);
        }

        ~VolumeChannel()
        {
            newSessionEvent -= newSessionHandler;
        }

        private void HandleNewSession(AudioSessionControl newSession, String filename)
        {
            if (filename.EndsWith(exeSuffix))
            {
                session = newSession;
            }
        }

        public void HandleFrame(sbyte encoderDelta, byte buttonState, ushort touchReading, ushort ambientReading, out byte[] ledState, out byte[] lcdImage)
        {
            MaybeRefreshSessions();

            float peak = 0;
            bool isActive = false;
            if (session != null)
            {
                if (encoderDelta != 0) session.SimpleAudioVolume.Volume += encoderDelta / 20.0f;
                peak = session.AudioMeterInformation.MasterPeakValue;
                isActive = (session.State == AudioSessionState.AudioSessionStateActive);
            }
            if (isActive)
            {
                if (peak == 0)
                {
                    if (silenceTimeout < 0)
                    {
                        isActive = false;
                    }
                    else
                    {
                        silenceTimeout--;
                    }
                }
                else
                {
                    silenceTimeout = 300;
                }
            }


            recentMaximum *= 0.998f;
            if (peak > recentMaximum)
            {
                recentMaximum = peak;
            }
            int cpiploc = -1;
            for (int i = 9; i >= 0; i--)
            {
                if (peak > ledThresholds[i] * recentMaximum)
                {
                    cpiploc = i;
                    break;
                }
            }
            if (cpiploc >= pipLocation)
            {
                pipLocation = cpiploc;
                timeSincePipRaised = 0;
            }
            else
            {
                ++timeSincePipRaised;
                for (int i = 0; i < 10; i++)
                {
                    if (timeSincePipRaised == timesToLowerPip[i]) --pipLocation;
                }
            }

            int newIsActive = isActive ? 1 : 0;
            if (prevIsActive != newIsActive)
            {
                lcdImage = ImageUtil.GenImageStream(isActive ? displayName : "");
                prevIsActive = newIsActive;
            } else
            {
                lcdImage = null;
            }

            ledState = new byte[21];
            if (isActive)
            {
                ledState[20] = 0x80;
            }

            //*
            for (int i = 0; i < 10; i++)
            {
                ledState[10 + i] = (peak > ledThresholds[i] * recentMaximum) ? (byte)((i + 1) * 25) : (byte)0;
                    
            }
            for (int i = 9; i >= 0; i--)
            {
                if (i == pipLocation)
                {
                    ledState[i] = (byte)((i + 1) * 8);
                    break;
                }
            }
            /*/
            if (isActive)
            for (int i = 0; i < 10; i++)
            {
                ledState[10 + i] = (byte)(Math.Abs(cycle) * (i+1));
            }
            cycle++;
            if (cycle == 26) cycle = -24;
            // */
        }
    }
}
