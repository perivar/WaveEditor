namespace aybe.AudioObjects
{
    public static class Extensions
    {
        public static int ToSamples(this int bytes, int bits, int channels)
        {
            return Converters.BytesToSamples(bytes, bits, channels);
        }

        public static long ToSamples(this long bytes, int bits, int channels)
        {
            return Converters.BytesToSamples(bytes, bits, channels);
        }
    }
}