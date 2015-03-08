using System;
namespace CommonUtils.Audio
{
	/// <summary>
	/// Provides access to sound player functionality needed to
	/// generate a Waveform.
	/// The original interface idea copyright (C) 2011 - 2012, Jacob Johnston
	/// </summary>
	public interface IWaveformPlayer : ISoundPlayer, IDisposable
	{
		/// <summary>
		/// Return the file path, can be null
		/// </summary>
		string FilePath { get; }
		
		/// <summary>
		/// Gets or sets the current sound streams playback position.
		/// </summary>
		int ChannelSamplePosition { get; set; }

		/// <summary>
		/// Gets the number of Channels (2 = stereo)
		/// </summary>
		int Channels { get; }
		
		/// <summary>
		/// Return the sample length per Channel (i.e. if the waveform is stereo, this is half the total sample length)
		/// </summary>
		int ChannelSampleLength { get; }
		
		/// <summary>
		/// Return number of bits per sample (e.g. 16, 32, etc.)
		/// </summary>
		int BitsPerSample { get; }

		/// <summary>
		/// Return the total sample length (i.e. if the waveform is stereo, this is double the channel sample length)
		/// </summary>
		int TotalSampleLength { get; }
		
		/// <summary>
		/// Gets the raw level data for the waveform.
		/// </summary>
		/// <remarks>
		/// Level data should be structured in an array where each sucessive index
		/// alternates between left or right channel data, starting with left. Index 0
		/// should be the first left level, index 1 should be the first right level, index
		/// 2 should be the second left level, etc.
		/// </remarks>
		float[] WaveformData { get; }

		/// <summary>
		/// Gets or sets the starting time for a section of repeat/looped audio.
		/// </summary>
		int SelectionSampleBegin { get; set; }

		/// <summary>
		/// Gets or sets the ending time for a section of repeat/looped audio.
		/// </summary>
		int SelectionSampleEnd { get; set; }
		
		/// <summary>
		/// Read from file at a specific frequency rate
		/// </summary>
		/// <param name="filename">Filename to read from</param>
		/// <param name="samplerate">Sample rate</param>
		/// <param name="milliseconds">Milliseconds to read</param>
		/// <param name="startmilliseconds">Start at a specific millisecond range</param>
		/// <returns>Array with data</returns>
		float[] ReadMonoFromFile(string filename, int samplerate, int milliseconds, int startmilliseconds);
		
		/// <summary>
		/// Open File using passed path
		/// </summary>
		/// <param name="path">path to audio file</param>
		void OpenFile(string path);	}
}
