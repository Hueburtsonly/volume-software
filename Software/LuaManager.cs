using MoonSharp.Interpreter;
using Software.Channels;
using System;
using System.Collections.Generic;
using Software.Logging;
using System.IO;
using System.Windows.Forms;
using Software.Configuration;

namespace Software
{
    class LuaManager
    {
        public static Channel[] StartLua(LoggingProvider logger, SoftwareConfiguration configuration)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            List<Channel> channels = new List<Channel>(8);
            for (int i = 0; i < 8; i++)
            {
                channels.Add(new VolumeChannel("XXX", "XXX", logger));
            }

            Script script = new Script();

            script.Globals["AddVolumeChannel"] = (Func<int, String, String, int>)((int c, String displayName, String exeSuffix) => { channels[c] = new VolumeChannel(displayName, exeSuffix, logger); return 0; });
            script.Globals["AddLuaChannel"] = (Func<int, DynValue, DynValue>)((int c, DynValue config) => { LuaChannel nc; DynValue dv = LuaChannel.ConstructForLua(script, config, out nc); channels[c] = nc; return dv; });

            script.DoString(Software.Properties.Resources.BuiltInLua);

            if (!File.Exists(configuration.ConfigFilePath))
            {
                File.WriteAllText(configuration.ConfigFilePath, Software.Properties.Resources.DefaultConfigLua);
            }

            try
            {
                script.DoFile(configuration.ConfigFilePath);
            }
            catch (ScriptRuntimeException e)
            {
                MessageBox.Show(e.DecoratedMessage, "Error parsing config.lua", MessageBoxButtons.OK);
                throw e;
            }

            //DynValue res = script.Call(script.Globals["fact"], 4);

            //return res.Number;

            return channels.ToArray();
        }
    }
}
