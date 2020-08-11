using System;
using MoonSharp.Interpreter;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.ClearScript;

namespace Software.Channels
{
    class ScriptChannel : Channel
    {
        private ScriptChannelCallback config;
        double period = 100;
        private Stopwatch stopwatch = null;
        private double nextMatchMs = 0;

        public ScriptChannel(ScriptChannelCallback config)
        {
            //Trace.Assert(config.Type == DataType.Function);
            this.config = config;
        }

        public static ScriptChannel ConstructForScript(ScriptChannelCallback config, out ScriptChannel channel)
        {
            ScriptChannel nc = new ScriptChannel(config);
            channel = nc;
            return nc;
        }

        String prevString = null;

        public ScriptChannel SetPeriod(double newPeriod)
        {
            // TODO: Make this accessible from the Script.
            this.period = newPeriod;
            return this;
        }

        Int16 accumEncoderDelta = 0;

        public void HandleFrame(sbyte encoderDelta, byte buttonState, ushort touchReading, ushort ambientReading, out byte[] ledState, out byte[] lcdImage)
        {
            accumEncoderDelta += encoderDelta;
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

            dynamic dynReturnedObject = config(accumEncoderDelta, buttonState, touchReading, ambientReading);
            accumEncoderDelta = 0;

            String newString = dynReturnedObject.text; // (String)(ret.GetProperty("text")); // ret.Tuple[0].String;

            dynamic dynleds = dynReturnedObject.leds;

          

            if (prevString != newString)
            {
                lcdImage = ImageUtil.RenderPlainText(newString);
                prevString = newString;
            }
            else
            {
                lcdImage = null;
            }

            //Trace.Assert(ret.Tuple[1].Type == DataType.Table);
            ledState = new byte[21];
            for (int i = 0; i < 21; i++)
            {
                ledState[i] = (byte)(dynleds[i]);
            }

            ; ; ;
        }
    }
}
