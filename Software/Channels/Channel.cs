using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Software.Channels
{
    interface Channel
    {
        void HandleFrame(sbyte encoderDelta, byte buttonState, ushort touchReading, ushort ambientReading, out byte[] ledState, out byte[] lcdImage);
    }
}
