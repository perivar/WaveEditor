using System;

namespace aybe.AudioObjects
{
	/// <summary>
	///     Contains methods for converting between different type of units.
	/// </summary>
	public static class Converters
	{
		#region FMOD
		/// <summary>
		///     Converts bytes to samples.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="bits"></param>
		/// <param name="channels"></param>
		/// <returns></returns>
		public static int BytesToSamples(int bytes, int bits, int channels)
		{
			return bytes*8/bits/channels;
		}

		/// <summary>
		///     Converts bytes to samples.
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="bits"></param>
		/// <param name="channels"></param>
		/// <returns></returns>
		public static long BytesToSamples(long bytes, int bits, int channels)
		{
			return bytes*8/bits/channels;
		}

		/// <summary>
		///     Converts Milliseconds to samples.
		/// </summary>
		/// <param name="samplerate"></param>
		/// <param name="ms"></param>
		/// <returns></returns>
		public static int MillisecondsToSamples(int samplerate, int ms)
		{
			return ms*samplerate/1000;
		}

		/// <summary>
		///     Converts Milliseconds to samples.
		/// </summary>
		/// <param name="samplerate"></param>
		/// <param name="ms"></param>
		/// <returns></returns>
		public static long MillisecondsToSamples(long samplerate, long ms)
		{
			return ms*samplerate/1000;
		}

		/// <summary>
		///     Converts samples to bytes.
		/// </summary>
		/// <param name="samples"></param>
		/// <param name="bits"></param>
		/// <param name="channels"></param>
		/// <returns></returns>
		public static int SamplesToBytes(int samples, int bits, int channels)
		{
			return samples*bits*channels/8;
		}

		/// <summary>
		///     Converts samples to bytes.
		/// </summary>
		/// <param name="samples"></param>
		/// <param name="bits"></param>
		/// <param name="channels"></param>
		/// <returns></returns>
		public static long SamplesToBytes(long samples, long bits, long channels)
		{
			return samples*bits*channels/8;
		}

		/// <summary>
		///     Converts samples to Milliseconds.
		/// </summary>
		/// <param name="samples"></param>
		/// <param name="samplerate"></param>
		/// <returns></returns>
		public static int SamplesToMilliseconds(int samples, int samplerate)
		{
			return samples*1000/samplerate;
		}

		/// <summary>
		///     Converts samples to Milliseconds.
		/// </summary>
		/// <param name="samples"></param>
		/// <param name="samplerate"></param>
		/// <returns></returns>
		public static long SamplesToMilliseconds(long samples, int samplerate)
		{
			return samples*1000/samplerate;
		}


		/// <summary>
		///     Converts samples to sample rate.
		/// </summary>
		/// <param name="samples"></param>
		/// <param name="ms"></param>
		/// <returns></returns>
		public static int SamplesToSamplerate(int samples, int ms)
		{
			return samples*1000/ms;
		}

		/// <summary>
		///     Converts samples to sample rate.
		/// </summary>
		/// <param name="samples"></param>
		/// <param name="ms"></param>
		/// <returns></returns>
		public static long SamplesToSamplerate(long samples, long ms)
		{
			return samples*1000/ms;
		}

		#endregion

		/// <summary>
		///     Converts bytes to a human-friendly string.
		/// </summary>
		/// <param name="bytes">Value to convert.</param>
		/// <param name="siUnits">Use SI units.</param>
		/// <returns>A string that is human-friendly, e.g a length of 1048576 bytes will return "1MiB".</returns>
		public static string BytesToString(long bytes, bool siUnits)
		{
			//http://stackoverflow.com/questions/10420352/converting-file-size-in-bytes-to-human-readable
			int threshold = siUnits ? 1000 : 1024;
			if (bytes < threshold)
			{
				string format = String.Format("{0} B", bytes);
				return format;
			}

			string[] units = siUnits
				? new[] {"kB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"}
				: new[] {"KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB"};
			int u = -1;
			do
			{
				bytes /= threshold;
				++u;
			} while (bytes >= threshold);

			string fileSize = String.Format("{0} {1}", Math.Round((double) bytes, 1), units[u]);
			return fileSize;
		}
	}
}