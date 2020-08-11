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

    public delegate Object ScriptChannelCallback(Int16 encoderDelta, byte buttonState, ushort touchReading, ushort ambientReading);

    class ScriptManager
    {
        public static Channel[] StartScript(LoggingProvider logger, SoftwareConfiguration configuration)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            List<Channel> channels = new List<Channel>(8);
            for (int i = 0; i < 8; i++)
            {
                channels.Add(new VolumeChannel("XXX", "XXX", logger));
            }


            var engine = new V8ScriptEngine("ghello");
            try
            {
                engine.AddHostObject("host", new ExtendedHostFunctions());

                engine.AddHostObject("AddVolumeChannel", (Func<int, String, String, int>)((int c, String displayName, String exeSuffix) => { channels[c] = new VolumeChannel(displayName, exeSuffix, logger); return 0; }));

                engine.AddHostType("ScriptChannelCallback", typeof(ScriptChannelCallback));
                engine.AddHostObject("AddScriptChannelInternal", (Func<int, ScriptChannelCallback, ScriptChannel>)((int c, ScriptChannelCallback config) => { ScriptChannel nc; ScriptChannel dv = ScriptChannel.ConstructForScript(config, out nc); channels[c] = nc; return dv; }));

                engine.Execute("builtin.js", Software.Properties.Resources.BuiltInScript);

                if (!File.Exists(configuration.ConfigFilePath))
                {
                    File.WriteAllText(configuration.ConfigFilePath, Software.Properties.Resources.DefaultConfigScript);
                }

                engine.Execute(configuration.ConfigFilePath, File.ReadAllText(configuration.ConfigFilePath));

            }
            catch (ScriptEngineException e)
            {
                MessageBox.Show(e.ErrorDetails, "Error executing config.js", MessageBoxButtons.OK);
                throw e;
            }

            return channels.ToArray();
        }
    }
}
