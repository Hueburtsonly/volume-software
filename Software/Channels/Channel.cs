namespace Software.Channels
{
    interface Channel
    {
        void HandleFrame(sbyte encoderDelta, byte buttonState, ushort touchReading, ushort ambientReading, out byte[] ledState, out byte[] lcdImage);
    }
}
