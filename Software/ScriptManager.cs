using MoonSharp.Interpreter;
using Software.Channels;
using System;
using System.Collections.Generic;
using Software.Logging;
using System.IO;
using System.Windows.Forms;
using Software.Configuration;
using Microsoft.ClearScript.V8;
using Microsoft.ClearScript;

namespace Software
{
    class ScriptManager : IDisposable
    {

        public List<Channel> channels = new List<Channel>(8);
        private V8ScriptEngine _engine = new V8ScriptEngine("ghello");
        private bool disposedValue;

        public ScriptManager(LoggingProvider logger, SoftwareConfiguration configuration)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            
            for (int i = 0; i < 8; i++)
            {
                channels.Add(new VolumeChannel("XXX", "XXX", logger));
            }


            try
            {
                // TODO: consider using the advice in https://github.com/Microsoft/ClearScript/issues/65 instead of using WeakReferences.

                WeakReference<List<Channel>> weakChannelList = new WeakReference<List<Channel>>(channels);

                _engine.AddHostObject("host", new ExtendedHostFunctions());

                _engine.AddHostObject("AddVolumeChannel", (Func<int, String, String, int>)((int c, String displayName, String exeSuffix) => {
                    List<Channel> localChannels;
                    if (weakChannelList.TryGetTarget(out localChannels))
                    {
                        localChannels[c] = new VolumeChannel(displayName, exeSuffix, logger);
                    }
                    return 0;
                }));

                _engine.AddHostType("ScriptChannelCallback", typeof(ScriptChannelCallback));
                _engine.AddHostObject("AddScriptChannelInternal", (Func<int, ScriptChannelCallback, ScriptChannel>)((int c, ScriptChannelCallback config) => {
                    ScriptChannel nc;
                    ScriptChannel dv = ScriptChannel.ConstructForScript(config, out nc);
                    List<Channel> localChannels;
                    if (weakChannelList.TryGetTarget(out localChannels))
                    {
                        localChannels[c] = nc;
                    }
                    return dv;
                }));

                _engine.Execute("builtin.js", Software.Properties.Resources.BuiltInScript);

                if (!File.Exists(configuration.ConfigFilePath))
                {
                    File.WriteAllText(configuration.ConfigFilePath, Software.Properties.Resources.DefaultConfigScript);
                }

                _engine.Execute(configuration.ConfigFilePath, File.ReadAllText(configuration.ConfigFilePath));

            }
            catch (ScriptEngineException e)
            {
                MessageBox.Show(e.ErrorDetails, "Error executing config.js", MessageBoxButtons.OK);
                throw e;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                channels.Clear();
                _engine.Dispose();
                _engine = null;

                disposedValue = true;
            }
        }

        ~ScriptManager()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
