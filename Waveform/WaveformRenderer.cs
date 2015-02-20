using System;
using aybe.AudioObjects;
using aybe.Graphics;

namespace aybe.Waveform
{
	public abstract class WaveformRenderer : Renderer
	{
		public IWaveformTheme Theme { get; set; }
		public IWaveformCache Cache { get; set; }

		public void Draw(int position, int ratio, double zoom)
		{
			IWaveformCache cache = Cache;
			if (cache == null) throw new InvalidOperationException();
			var renderer = this;
			if (renderer == null) throw new InvalidOperationException();
			IWaveformTheme theme = Theme;
			if (theme == null) throw new InvalidOperationException();

			IAudioStream audioStream = cache.AudioStream;
			int bitmapWidth;
			int bitmapHeight;
			renderer.GetBitmapSize(out bitmapWidth, out bitmapHeight);

			int pixelsPerSample = Math.Abs(ratio);
			var samples = (int)Math.Ceiling((double)bitmapWidth / pixelsPerSample);

			const int xMin = 0;
			const int yMin = 0;
			int xMax = bitmapWidth - 1;
			int yMax = bitmapHeight - 1;

			int channels = audioStream.Channels;
			int channelHeight = bitmapHeight / channels;
			int channelCenter = channelHeight / 2;

			int positionX = position / ratio;

			const int minMax = 2;
			int hop = channels * minMax;
			const int colorTransparent = 0x00000000;

			using (var context = renderer.GetContext())
			{
				#region Background
				context.Clear(theme.DrawBackground ? theme.ColorBackground : colorTransparent);
				#endregion

				#region 6dB, DC and separation lines
				if (theme.Draw6dBLevel)
				{
					LineDash[] dashes6dBLevel =
					{
						new LineDash(3, theme.Color6dBLevel),
						new LineDash(3, colorTransparent)
					};
					for (int c = 0; c < channels; c++)
					{
						int y1 = Transform(+0.5f, channelHeight, c, 1.0d);
						int y2 = Transform(-0.5f, channelHeight, c, 1.0d);
						context.DrawLine(xMin, y1, xMax, y1, dashes6dBLevel);
						context.DrawLine(xMin, y2, xMax, y2, dashes6dBLevel);
					}
				}

				if (theme.DrawDCLevel)
				{
					LineDash[] lineDashesDcLevel =
					{
						new LineDash(3, theme.ColorDCLevel),
						new LineDash(3, colorTransparent)
					};
					for (int c = 0; c < channels; c++)
					{
						int y = channelHeight * c + channelCenter;
						context.DrawLine(xMin, y, xMax, y, lineDashesDcLevel);
					}
				}

				if (theme.DrawSeparationLine)
				{
					if (channels > 1)
					{
						int y = bitmapHeight / 2 - 1;
						context.DrawLine(xMin, y, xMax, y, theme.ColorSeparationLine);
					}
				}

				#endregion

				#region Wave form
				LineDash[] lineDashesEndIndicator =
				{
					new LineDash(3, theme.ColorEndIndicator),
					new LineDash(3, unchecked ((int) 0xFFF0F0F0))
				};

				bool drawEndIndicator = theme.DrawEndIndicator;
				if (ratio < 0) // OK !
				{
					#region Draw N:1
					//var buffer1 = new float[channels]; // previous sample for correct drawing
					//if (position > 0) AudioStream.ReadSamples(position - 1, buffer1, 1);
					var buffer3 = new float[samples * channels]; // visible samples
					int readSamples = audioStream.ReadSamples(position, buffer3, samples);
					var buffer2 = new float[channels]; // next sample for correct drawing
					if (position + readSamples < audioStream.Samples)
						audioStream.ReadSamples(position + readSamples, buffer2, 1);
					for (int c = 0; c < channels; c++) // draw !
					{
						float f1;
						//f1 = buffer1[c];
						f1 = buffer3[c];
						for (int x = 0; x < readSamples - 1; x++)
						{
							float f2 = buffer3[(x + 1) * channels + c];
							int x1 = x * pixelsPerSample;
							int y1 = Transform(f1, channelHeight, c, zoom);
							int x2 = (x + 1) * pixelsPerSample;
							int y2 = Transform(f2, channelHeight, c, zoom);
							context.DrawLine(x1, y1, x2, y1, theme.ColorEnvelope); // horizontal
							context.DrawLine(x2, y1, x2, y2, theme.ColorEnvelope); // vertical
							f1 = f2;
						}
						int x3 = (readSamples - 1) * pixelsPerSample; // last sample !
						int y3 = Transform(f1, channelHeight, c, zoom);
						context.DrawLine(x3, y3, x3 + pixelsPerSample, y3, theme.ColorEnvelope);
						context.DrawLine(x3 + pixelsPerSample, y3, x3 + pixelsPerSample,
						                 Transform(buffer2[c], channelHeight, c, zoom), theme.ColorEnvelope);
					}
					if (readSamples < samples) // draw end lines
					{
						int x = (readSamples) * pixelsPerSample + 1;
						if (drawEndIndicator)
						{
							context.DrawLine(x, 0, x, bitmapHeight - 1, lineDashesEndIndicator);
						}
						if (theme.DrawDCLevel)
						{
							for (int i = 0; i < channels; i++)
							{
								int y = channelHeight * i + channelCenter;
								context.DrawLine(x, y, bitmapWidth - 1, y, theme.ColorEnvelope);
							}
						}
					}

					#endregion
				}
				else if (ratio == 1) // OK !
				{
					#region Draw 1:1
					//float[] data = AudioStream.ReadSamples(samples);

					var buffer3 = new float[samples * channels]; // visible samples
					int readSamples = audioStream.ReadSamples(position, buffer3, samples);
					for (int c = 0; c < channels; c++)
					{
						float f1 = buffer3[c];
						for (int x = 0; x < readSamples; x++)
						{
							float f2 = buffer3[x * channels + c];
							int x1 = (x - 1) * pixelsPerSample;
							int y1 = Transform(f1, channelHeight, c, zoom);
							int x2 = x * pixelsPerSample;
							int y2 = Transform(f2, channelHeight, c, zoom);
							context.DrawLine(x1, y1, x2, y2, theme.ColorEnvelope);
							f1 = f2;
						}
					}

					if (readSamples < samples) // draw end lines
					{
						int x = (readSamples) * pixelsPerSample + 1;
						if (drawEndIndicator)
						{
							LineDash[] lineDashes =
							{
								new LineDash(3, theme.ColorEndIndicator),
								new LineDash(3, unchecked ((int) 0xFFF0F0F0))
							};
							context.DrawLine(x, 0, x, bitmapHeight - 1, lineDashes);
						}
						if (theme.DrawDCLevel)
						{
							for (int i = 0; i < channels; i++)
							{
								int y = channelHeight * i + channelCenter;
								context.DrawLine(x, y, bitmapWidth - 1, y, theme.ColorEnvelope);
							}
						}
					}

					#endregion
				}
				else
				{
					var buffer = new float[bitmapWidth * hop];
					int peaksWidth; // pixels
					if (ratio < cache.InitialRatio) // low overhead, just grab samples at this ratio
					{
						int numberOfSamples = bitmapWidth * ratio;
						float[] floats = audioStream.GetPeaks(ratio, numberOfSamples, position);
						Array.Copy(floats, 0, buffer, 0, floats.Length);
						peaksWidth = floats.Length / hop;
					}
					else // high overhead, grab portion of samples to draw from cache array
					{
						// OK !
						float[] peaks = cache.GetPeaks(ratio);
						int sourceIndex = positionX * hop;
						int sourceLength = bitmapWidth * hop;
						if (sourceIndex + sourceLength > peaks.Length)
							sourceLength = peaks.Length - sourceIndex;
						Array.Copy(peaks, sourceIndex, buffer, 0, sourceLength);
						peaksWidth = sourceLength / hop;
					}
					for (int c = 0; c < channels; c++)
					{
						int offsetMinPrev = c * channels + 0;
						int offsetMaxPrev = c * channels + 1;
						float minPrev = buffer[offsetMinPrev];
						float maxPrev = buffer[offsetMaxPrev];
						for (int x = 0; x < bitmapWidth; x++) // draw only vicible pixels
						{
							int offsetMin = x * channels * minMax + c * channels + 0;
							int offsetMax = x * channels * minMax + c * channels + 1;
							float min = buffer[offsetMin];
							float max = buffer[offsetMax];
							int y1 = Transform(minPrev, channelHeight, c, zoom);
							int y2 = Transform(maxPrev, channelHeight, c, zoom);
							int y3 = Transform(min, channelHeight, c, zoom);
							int y4 = Transform(max, channelHeight, c, zoom);
							if (theme.DrawForm)
							{
								if (!theme.DrawEnvelope) context.DrawLine(x - 1, y2, x, y4, theme.ColorEnvelope);
								context.DrawLine(x, y3, x, y4, theme.ColorForm);
							}
							if (theme.DrawEnvelope)
							{
								context.DrawLine(x - 1, y1, x, y3, theme.ColorEnvelope);
								context.DrawLine(x - 1, y2, x, y4, theme.ColorEnvelope);
							}
							maxPrev = max;
							minPrev = min;
						}
					}
					if (peaksWidth < bitmapWidth) // end indicator
					{
						int x = peaksWidth + 1;
						context.DrawLine(x, yMin, x, yMax, lineDashesEndIndicator);
					}
				}
				#endregion

			}
		}

		/// <summary>
		///     Transforms a value in the range of -1.0 to +1.0
		/// </summary>
		/// <param name="peak"></param>
		/// <param name="channelHeight"></param>
		/// <param name="channelIndex"></param>
		/// <param name="zoom"></param>
		/// <returns></returns>
		private static int Transform(float peak, int channelHeight, int channelIndex, double zoom)
		{
			int top = channelIndex * channelHeight;
			int bottom = top + channelHeight;
			var i = (int)((0.5d + 0.5d * -peak * zoom) * channelHeight + channelHeight * channelIndex);
			return i < top ? top : i > bottom ? bottom : i;
		}
	}
}