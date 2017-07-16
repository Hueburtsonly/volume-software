using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Management;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfUsbApp1
{
    class VolumeTemp 
    {

        MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        MMDevice device;
        AudioSessionManager sessManager;

        bool shouldRefreshSessions = true;

        public VolumeTemp()
        {
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            sessManager = device.AudioSessionManager;

            //sessManager.AudioSessionControl.RegisterEventClient(this);
            sessManager.OnSessionCreated += new AudioSessionManager.SessionCreatedDelegate(OnSessionCreated);



        }

        AudioSessionControl ascTeamspeak = null;
        AudioSessionControl ascRocketLeague = null;
        AudioSessionControl ascChrome = null;


        public int update(int delta0, int delta1, int delta2, out float[] peaks, out int[] states)
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
                    catch { }

                    if (state != AudioSessionState.AudioSessionStateExpired)
                    {

                        if (fn.EndsWith("ts3client_win64.exe"))
                        {
                            ascTeamspeak = session;
                            fn = "TEAMSPEAK!";
                        } else if (fn.EndsWith("RocketLeague.exe"))
                        {
                            ascRocketLeague = session;
                            fn = "ROCKETLEAGUE!";
                        }
                        else if (fn.EndsWith("chrome.exe"))
                        {
                            ascChrome = session;
                            fn = "CHROME!";
                        }


                        Console.WriteLine("\"" + fn + "\" " + session.GetProcessID + " -- " + session.AudioMeterInformation.MasterPeakValue + ((state == AudioSessionState.AudioSessionStateActive) ? " (ACTIVE)":""));
                    }
                }
            }
            peaks = new float[] { 0, 0, 0, 0 };
            states = new int[] { 0, 0, 0, 0 };
            if (ascTeamspeak != null)
            {
                if (delta0 != 0) ascTeamspeak.SimpleAudioVolume.Volume += delta0 / 20.0f;
                peaks[0] = ascTeamspeak.AudioMeterInformation.MasterPeakValue;
                if (ascTeamspeak.State == AudioSessionState.AudioSessionStateActive) states[0] = 1;
            }
            if (ascRocketLeague != null)
            {
                if (delta1 != 0) ascRocketLeague.SimpleAudioVolume.Volume += delta1 / 20.0f;
                peaks[1] = ascRocketLeague.AudioMeterInformation.MasterPeakValue;
                if (ascRocketLeague.State == AudioSessionState.AudioSessionStateActive) states[1] = 1;
            }
            if (ascChrome != null)
            {
                if (delta2 != 0) ascChrome.SimpleAudioVolume.Volume += delta2 / 20.0f;
                peaks[2] = ascChrome.AudioMeterInformation.MasterPeakValue;
                if (ascChrome.State == AudioSessionState.AudioSessionStateActive) states[2] = 1;
            }
            
            return sessManager.Sessions.Count;
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



        public void OnSessionCreated(object sender, IAudioSessionControl newSession)
        {
            Console.WriteLine("Session added");
            shouldRefreshSessions = true;
        }



    }
}
