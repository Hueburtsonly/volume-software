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

        public LuaChannel(DynValue config)
        {
            Trace.Assert(config.Type == DataType.Function);
            this.config = config;
        }

        String prevString = null;

        public void HandleFrame(sbyte encoderDelta, byte buttonState, out byte[] ledState, out byte[] lcdImage)
        {
            DynValue ret = config.Function.Call(encoderDelta, buttonState);
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
