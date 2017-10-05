namespace Software.Channels
{
    interface Channel
    {
        void HandleFrame(sbyte encoderDelta, byte buttonState, out byte[] ledState, out byte[] lcdImage);
    }
}
