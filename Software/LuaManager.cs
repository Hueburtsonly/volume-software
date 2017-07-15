﻿using MoonSharp.Interpreter;
using Software.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Software
{
    class LuaManager
    {

        private static int Mul(int a, int b, DynValue v)
        {
            return (int)v.Function.Call(a, b).Number;
        }


        public static Channel[] StartLua()
        {
            List<Channel> channels = new List<Channel>(4);
            for (int i = 0; i < 4; i++)
            {
                channels.Add(new VolumeChannel("XXX", "XXX"));
            }


            Script script = new Script();

            script.Globals["AddVolumeChannel"] = (Func<int, String, String, int>)((int c, String displayName, String exeSuffix) => { channels[c] = new VolumeChannel(displayName, exeSuffix); return 0; });
            script.Globals["AddLuaChannel"] = (Func<int, DynValue, int>)((int c, DynValue config) => { channels[c] = new LuaChannel(config); return 0; });

            script.DoString(Software.Properties.Resources.BuiltInLua);


            //DynValue res = script.Call(script.Globals["fact"], 4);

            //return res.Number;

            return channels.ToArray();
        }
    }
}