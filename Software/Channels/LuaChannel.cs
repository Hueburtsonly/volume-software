using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using System.Diagnostics;

namespace Software.Channels
{
    class LuaChannel : Channel
    {
        private DynValue config;
        double period = 0;
        private Stopwatch stopwatch = null;
        private double nextMatchMs = 0;

        public LuaChannel(DynValue config)
        {
            Trace.Assert(config.Type == DataType.Function);
            this.config = config;
        }

        public static DynValue ConstructForLua(Script script, DynValue config, out LuaChannel channel)
        {
            LuaChannel nc = new LuaChannel(config);
            channel = nc;
            DynValue ret = DynValue.NewTable(script);
            ret.Table["SetPeriod"] =
                (Func<double, DynValue>)((double newPeriod) => { nc.period = newPeriod; return ret; });
            return ret;
        }

        String prevString = null;

        public void HandleFrame(sbyte encoderDelta, byte buttonState, ushort touchReading, ushort ambientReading, out byte[] ledState, out byte[] lcdImage)
        {
            if (period != 0)
            {
                if (stopwatch == null)
                {
                    stopwatch = new Stopwatch();
                    stopwatch.Start();
                }
                else if (stopwatch.ElapsedMilliseconds < nextMatchMs)
                {
                    ledState = null;
                    lcdImage = null;
                    return;
                }
                
                nextMatchMs += period;
            }

            DynValue ret = config.Function.Call(encoderDelta, buttonState, touchReading, ambientReading);
            Trace.Assert(ret.Type == DataType.Tuple);
            Trace.Assert(ret.Tuple[0].Type == DataType.String);
            String newString = ret.Tuple[0].String;

            if (prevString != newString)
            {
                lcdImage = ImageUtil.GenImageStream(newString);
                prevString = newString;
            }
            else
            {
                lcdImage = null;
            }

            Trace.Assert(ret.Tuple[1].Type == DataType.Table);
            ledState = new byte[21];
            for (int i = 0; i < 21; i++)
            {
                ledState[i] = (byte)(ret.Tuple[1].Table.Get(i+1).Number);
            }

            ; ; ;
        }
    }
}
