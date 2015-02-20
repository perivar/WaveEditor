using System;

namespace aybe.AudioObjects
{
	/// <summary>
	///     Represents an audio stream.
	/// </summary>
	public interface IAudioStream : IDisposable
	{
		/// <summary>
		///     Gets the bit-depth of this stream.
		/// </summary>
		int BitDepth { get; }

		/// <summary>
		///     Gets the original bit-depth of this stream.
		/// </summary>
		/// <remarks>
		///     This property provides a mean to know the original bit-depth of a stream when for instance it is being resampled on
		///     the fly by the underlying process that decodes audio data.
		///     <para>
		///         If the stream is not being resampled on the fly then value of this property will be identical to
		///         <see cref="BitDepth" />.
		///     </para>
		/// </remarks>
		int BitDepthOriginal { get; }

		/// <summary>
		///     Gets the number of channels of this stream.
		/// </summary>
		int Channels { get; }

		/// <summary>
		///     Gets the length in bytes of this stream.
		/// </summary>
		long Length { get; }

		/// <summary>
		///     Gets the position in bytes of this stream.
		/// </summary>
		long Position { get; set; }

		/// <summary>
		///     Gets the length in samples of this stream.
		/// </summary>
		long Samples { get; }

		/// <summary>
		///     Gets if this instance has been disposed.
		/// </summary>
		bool IsDisposed { get; }

		/// <summary>
		///     Gets the sample rate in Hz of this stream.
		/// </summary>
		int Samplerate { get; }

		/// <summary>
		///     Gets peak data for this stream.
		/// </summary>
		/// <param name="ratio">
		///     A positive number, multiple of 2 which defines the number of samples that will participate in
		///     detecting a peak min/max pair.
		/// </param>
		/// <param name="numberOfSamples">
		///     Number of samples to get the peaks for, does not have to be a multiple of
		///     <paramref name="ratio" />.
		/// </param>
		/// <param name="position">Position in samples where to start reading from in this stream.</param>
		/// <returns>
		///     An array where the peaks are laid out as in a PCM stream e.g. for 2 channels it will be Lmin, Lmax, Rmin,
		///     Rmax.
		/// </returns>
		float[] GetPeaks(int ratio, int numberOfSamples, int position);

		/// <summary>
		///     Reads a number samples from specified position.
		/// </summary>
		/// <param name="position">Position in samples where to start reading from.</param>
		/// <param name="buffer">Buffer where samples will be written to.</param>
		/// <param name="count">Number of samples to read.</param>
		/// <returns>The number of samples read, can be less than <paramref name="count" />.</returns>
		int ReadSamples(int position, float[] buffer, int count);

		/// <summary>
		///     Plays this stream.
		/// </summary>
		/// <remarks>
		///     An implementation does not necessarily supports this method.
		/// </remarks>
		void Play();

		/// <summary>
		///     Pauses this stream.
		/// </summary>
		/// <remarks>
		///     An implementation does not necessarily supports this method.
		/// </remarks>
		void Pause();
	}
}