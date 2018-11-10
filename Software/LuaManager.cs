using MoonSharp.Interpreter;
using Software.Channels;
using System;
using System.Collections.Generic;
using Software.Logging;

namespace Software
{
    class LuaManager
    {
        public static Channel[] StartLua(LoggingProvider logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            List<Channel> channels = new List<Channel>(4);
            for (int i = 0; i < 4; i++)
            {
                channels.Add(new VolumeChannel("XXX", "XXX", logger));
            }

            Script script = new Script();

            script.Globals["AddVolumeChannel"] = (Func<int, String, String, int>)((int c, String displayName, String exeSuffix) => { channels[c] = new VolumeChannel(displayName, exeSuffix, logger); return 0; });
            script.Globals["AddLuaChannel"] = (Func<int, DynValue, DynValue>)((int c, DynValue config) => { LuaChannel nc; DynValue dv = LuaChannel.ConstructForLua(script, config, out nc); channels[c] = nc; return dv; });

            script.DoString(Software.Properties.Resources.BuiltInLua);


            //DynValue res = script.Call(script.Globals["fact"], 4);

            //return res.Number;

            return channels.ToArray();
        }
    }
}
