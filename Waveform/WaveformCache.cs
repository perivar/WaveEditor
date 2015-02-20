using System;
using System.Collections.Generic;
using System.Diagnostics;
using aybe.AudioObjects;

namespace aybe.Waveform
{
    public sealed class WaveformCache : IWaveformCache
    {
        private readonly IAudioStream _audioStream;
        private readonly int _initialRatio;
        private readonly Dictionary<int, float[]> _dictionary;

        private WaveformCache()
        {
            _dictionary = new Dictionary<int, float[]>();
        }

        public WaveformCache( IAudioStream audioStream, int initialRatio)
            : this()
        {
            if (audioStream == null) throw new ArgumentNullException("audioStream");
            if (initialRatio < 2) throw new ArgumentOutOfRangeException("initialRatio");
            if (initialRatio % 2 != 0) throw new ArgumentOutOfRangeException("initialRatio");
            _audioStream = audioStream;
            _initialRatio = initialRatio;
        }

        public int InitialRatio
        {
            get { return _initialRatio; }
        }

        public IAudioStream AudioStream
        {
            get { return _audioStream; }
        }

        public float[] GetPeaks(int ratio)
        {
            if (ratio < InitialRatio) throw new ArgumentOutOfRangeException("ratio");
            if (ratio % 2 != 0) throw new ArgumentOutOfRangeException("ratio");
            if (!_dictionary.ContainsKey(ratio))
            {
                var floats = AudioStream.GetPeaks(ratio,(int) AudioStream.Samples,0);
                Debug.Assert(AudioStream.Position == AudioStream.Length);
                _dictionary.Add(ratio, floats);
            }
            return _dictionary[ratio];
        }
    }
}