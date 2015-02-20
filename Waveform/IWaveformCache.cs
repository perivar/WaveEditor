using aybe.AudioObjects;

namespace aybe.Waveform
{
	public interface IWaveformCache
	{
		int InitialRatio { get; }
		IAudioStream AudioStream { get; }
		float[] GetPeaks(int ratio);
	}
}