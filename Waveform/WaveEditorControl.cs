using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

using System.Windows.Forms.VisualStyles;
using aybe.Waveform;
using aybe.AudioObjects;
using aybe.Graphics;

namespace WaveEditor.Waveform
{
	/// <summary>
	/// Description of WaveEditorControl.
	/// </summary>
	public partial class WaveEditorControl : UserControl, INotifyPropertyChanged
	{
		#region Private constants
		private const int DefaultScale = 1;
		private const int SliderSmallChange = 1;
		private const int SliderLargeChange = 32;

		public const int HorizontalMovementFast = 1024;
		public const int HorizontalMovementNormal = 256;
		public const int HorizontalMovementSlow = 1;
		#endregion

		#region Private fields
		private IAudioStream _audioStream;
		private long _mousePositionInSamples;
		private TimelineUnit _timelineUnit;
		private int _zoomInteger;

		private Bitmap _offlineBitmap;
		
		IWaveformTheme _theme;
		IWaveformCache _waveformCache;
		#endregion
		
		public WaveEditorControl()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
			              ControlStyles.OptimizedDoubleBuffer, true);
			InitializeComponent();
			this.DoubleBuffered = true;
			
			VerticalZoom = DefaultScale;
			ZoomInteger = 0; // yeah :D
			HScrollbar.SmallChange = SliderSmallChange;
			HScrollbar.LargeChange = SliderLargeChange;
			_theme = new WaveformThemeWavelab();
		}

		#region Private properties
		private double VerticalZoom { get; set; }
		#endregion

		#region Public properties
		public IAudioStream AudioStream
		{
			get { return _audioStream; }
			set
			{
				if (Equals(value, _audioStream)) return;
				_audioStream = value;

				if (value != null)
				{
					_waveformCache = new WaveformCache(value, 128);
					long samples = Converters.BytesToSamples(value.Length, value.BitDepth, value.Channels);
					HScrollbar.Maximum = (int) (samples - 1);
				}
				else
				{
					HScrollbar.Maximum = 0;
				}
				RefreshWaveforms();
				
				OnPropertyChanged();
			}
		}
		
		/// <summary>
		///     Gets the position in samples over the mouse cursor.
		/// </summary>
		public long MouseSamplePosition
		{
			get { return _mousePositionInSamples; }
			private set
			{
				if (value == _mousePositionInSamples) return;
				_mousePositionInSamples = value;
				this.lblSelectionInfo.Text = String.Format("{0}", _mousePositionInSamples);
				OnPropertyChanged();
			}
		}

		/// <summary>
		///     Gets or sets the current timeline unit.
		/// </summary>
		public TimelineUnit TimelineUnit
		{
			get { return _timelineUnit; }
			set
			{
				if (value == _timelineUnit) return;
				_timelineUnit = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		///     Gets the current zoom integer. 0 means 1:1, 1 means 1:2, -1 means 2:1 etc ...
		/// </summary>
		public int ZoomInteger
		{
			get { return _zoomInteger; }
			set
			{
				if (value == _zoomInteger) return;
				_zoomInteger = value;
				OnPropertyChanged();
				OnPropertyChanged("ZoomRatio");
				OnPropertyChanged("ZoomRatioString");
			}
		}

		/// <summary>
		///     Gets the current zoom ratio denominator.
		/// </summary>
		public int ZoomRatio
		{
			get
			{
				int zoom = ZoomInteger;
				int ratio = (int)Math.Pow(2.0, Math.Abs(zoom)) * (Math.Sign(zoom) | 1);
				return ratio;
			}
		}

		public string ZoomRatioString
		{
			get
			{
				int ratioFromZoom = ZoomRatio;
				int a;
				int b;
				if (ratioFromZoom < 0)
				{
					a = -ratioFromZoom;
					b = 1;
				}
				else
				{
					a = 1;
					b = ratioFromZoom;
				}
				string s = string.Format("{0}:{1}", a, b);
				return s;
			}
		}

		#endregion
		
		#region INotifyPropertyChanged implementation
		
		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
		
		#region Controls events
		private void WaveEditorControlResize(object sender, EventArgs e)
		{
			var control = pnlWaveforms;
			
			if (control.Width > 0 && control.Height > 0)
			{
				var width = (int)control.Width;
				var height = (int)control.Height - HScrollbar.Height;

				_offlineBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
				WaveformImage.Image = _offlineBitmap;
				
				RefreshWaveforms();
			}
			else
			{
				_offlineBitmap = null;
			}
		}
		
		void WaveformImageMouseMove(object sender, MouseEventArgs e)
		{
			UpdateMouseSamplePosition(e.Location);
		}
		
		void Slider1ValueChanged(object sender, EventArgs e)
		{
			RefreshWaveforms();
		}
		
		#endregion
		
		#region Private methods
		private void RefreshWaveforms()
		{
			if (AudioStream != null)
			{
				var samples = (int)HScrollbar.Value;
				int bytes = Converters.SamplesToBytes(samples, AudioStream.BitDepth, AudioStream.Channels);
				AudioStream.Position = bytes;

				var ratio = ZoomRatio;
				var zoom = VerticalZoom;
				
				Draw(AudioStream, _theme, _offlineBitmap, ratio, samples, _waveformCache, zoom);
				//DrawTest(AudioStream, _theme, _offlineBitmap, ratio, samples, _waveformCache, zoom);
				
				// force redraw
				WaveformImage.Invalidate();
				
				lblZoomText.Text = ZoomRatioString;
				lblAudioInfo.Text = AudioStream.ToString();
				//lblSelectionInfo.Text =
			}
		}

		private void UpdateMouseSamplePosition(Point position)
		{
			int zoomRatio = ZoomRatio;
			int abs = Math.Abs(zoomRatio);
			var value = (long)HScrollbar.Value;
			var x = (int)position.X;
			long sample = zoomRatio < 0 ? x / abs : x * abs;
			long arg0 = value + sample;
			MouseSamplePosition = arg0;
		}
		
		private static void DrawTest(IAudioStream AudioStream, IWaveformTheme theme, Bitmap bitmap, int ratio, int positionSamples, IWaveformCache waveformCache, double verticalZoom)
		{
			int bitmapWidth = bitmap.Width;
			int bitmapHeight = bitmap.Height;
			
			int pixelsPerSample = Math.Abs(ratio);
			var samples = (int)Math.Ceiling((double)bitmapWidth / pixelsPerSample);

			int channels = AudioStream.Channels;
			int channelHeight = bitmapHeight / channels;
			int channelCenter = channelHeight / 2;

			int positionX = positionSamples / ratio;

			const int minMax = 2;
			int hop = channels * minMax;
			
			var font = new Font("Tahoma", 8, FontStyle.Regular);
			using (var canvas = Graphics.FromImage(bitmap)) {
				canvas.Clear(Color.Aqua);
				canvas.DrawString(String.Format("pixelsPerSample: {0}", pixelsPerSample), font, Brushes.Black, new PointF(0, 0));
				canvas.DrawString(String.Format("samples: {0}", samples), font, Brushes.Black, new PointF(0, 10));
				canvas.DrawString(String.Format("channels: {0}", channels), font, Brushes.Black, new PointF(0, 20));
				canvas.DrawString(String.Format("channelHeight: {0}", channelHeight), font, Brushes.Black, new PointF(0, 30));
				canvas.DrawString(String.Format("channelCenter: {0}", channelCenter), font, Brushes.Black, new PointF(0, 40));
				canvas.DrawString(String.Format("positionSamples: {0}", positionSamples), font, Brushes.Black, new PointF(0, 50));
				canvas.DrawString(String.Format("positionX: {0}", positionX), font, Brushes.Black, new PointF(0, 60));
				canvas.DrawString(String.Format("verticalZoom: {0}", verticalZoom), font, Brushes.Black, new PointF(0, 70));
				canvas.DrawString(String.Format("ratio: {0}", ratio), font, Brushes.Black, new PointF(0, 80));
				canvas.DrawString(String.Format("samples: {0}", AudioStream.Samples), font, Brushes.Black, new PointF(0, 90));
				canvas.DrawString(String.Format("length: {0}", AudioStream.Length), font, Brushes.Black, new PointF(0, 100));
			}
		}

		private static void DrawLineBresenham(Graphics canvas, int x1, int y1, int x2, int y2, LineDash[] lineDash) {
			var dashedPen = new Pen(Color.FromArgb(lineDash[0].Color), 1.0F);
			dashedPen.DashStyle = DashStyle.Dot;
			canvas.DrawLine(dashedPen, x1, y1, x2, y2);
		}

		private static void DrawLineBresenham(Graphics canvas, int x1, int y1, int x2, int y2, int lineDash) {
			var dashedPen = new Pen(Color.FromArgb(lineDash), 1.0F);
			dashedPen.DashStyle = DashStyle.Dot;
			
			canvas.DrawLine(dashedPen, x1, y1, x2, y2);
		}
		
		private static void DrawLine(Graphics canvas, int x1, int y1, int x2, int y2, LineDash[] lineDash) {
			var pen = new Pen(Color.FromArgb(lineDash[0].Color), 1.0F);
			canvas.DrawLine(pen, x1, y1, x2, y2);
		}
		
		private static void DrawLine(Graphics canvas, int x1, int y1, int x2, int y2, int line) {
			var pen = new Pen(Color.FromArgb(line), 1.0F);
			canvas.DrawLine(pen, x1, y1, x2, y2);
		}

		private static void Draw(IAudioStream AudioStream, IWaveformTheme theme, Bitmap bitmap, int ratio, int positionSamples, IWaveformCache waveformCache, double verticalZoom)
		{
			int bitmapWidth = bitmap.Width;
			int bitmapHeight = bitmap.Height;
			
			Graphics canvas = Graphics.FromImage(bitmap);
			
			int samplePixels = Math.Abs(ratio);
			var samples = (int)Math.Ceiling((double)bitmapWidth / samplePixels);

			const int xMin = 0;
			const int yMin = 0;
			int xMax = bitmapWidth - 1;
			int yMax = bitmapHeight - 1;
			var color6dBLevel = theme.Color6dBLevel;
			int colorBackground = theme.ColorBackground;
			int colorDCLevel = theme.ColorDCLevel;
			int colorForm = theme.ColorForm;
			int colorEnvelope = theme.ColorEnvelope;
			int colorSeparationLine = theme.ColorSeparationLine;
			bool draw6dBLevel = theme.Draw6dBLevel;
			bool drawBackground = theme.DrawBackground;
			bool drawDCLevel = theme.DrawDCLevel;
			bool drawEnvelope = theme.DrawEnvelope;
			bool drawForm = theme.DrawForm;
			bool drawSeparationLine = theme.DrawSeparationLine;

			int channels = AudioStream.Channels;
			int channelHeight = bitmapHeight / channels;
			int channelCenter = channelHeight / 2;

			int positionX = positionSamples / ratio;

			const int minMax = 2;
			int hop = channels * minMax;

			#region Background

			//bitmap.Clear(drawBackground ? colorBackground : Colors.Transparent.ToInt32());
			using (Graphics g = Graphics.FromImage(bitmap)) {
				g.Clear(Color.FromArgb(colorBackground));
			}
			
			#endregion

			#region 6dB, DC and separation lines

			if (draw6dBLevel)
			{
				LineDash[] dashes6dBLevel =
				{
					new LineDash(3, color6dBLevel),
					new LineDash(3, Color.Transparent.ToArgb())
				};
				for (int c = 0; c < channels; c++)
				{
					int y1 = Transform(+0.5f, channelHeight, c, 1.0d);
					int y2 = Transform(-0.5f, channelHeight, c, 1.0d);
					
					DrawLineBresenham(canvas, xMin, y1, xMax, y1, dashes6dBLevel);
					DrawLineBresenham(canvas, xMin, y2, xMax, y2, dashes6dBLevel);
				}
			}

			if (drawDCLevel)
			{
				LineDash[] lineDashesDcLevel =
				{
					new LineDash(3, colorDCLevel),
					new LineDash(3, Color.Transparent.ToArgb())
				};
				for (int c = 0; c < channels; c++)
				{
					int y = channelHeight * c + channelCenter;
					DrawLineBresenham(canvas, xMin, y, xMax, y, lineDashesDcLevel);
				}
			}

			if (drawSeparationLine)
			{
				if (channels > 1)
				{
					int y = bitmapHeight / 2 - 1;
					DrawLineBresenham(canvas, xMin, y, xMax, y, colorSeparationLine);
				}
			}

			#endregion

			#region Wave form

			LineDash[] lineDashesEndIndicator =
			{
				new LineDash(3, theme.ColorEndIndicator),
				new LineDash(3, Color.FromArgb(0xF0, 0xF0, 0xF0).ToArgb())
			};

			bool drawEndIndicator = theme.DrawEndIndicator;
			if (ratio < 0) // OK !
			{
				#region Draw N:1

				var buffer3 = new float[samples * channels]; // visible samples
				int readSamples = AudioStream.ReadSamples(positionSamples, buffer3, samples);
				var buffer2 = new float[channels]; // next sample for correct drawing
				if (positionSamples + readSamples < AudioStream.Samples)
					AudioStream.ReadSamples(positionSamples + readSamples, buffer2, 1);
				for (int c = 0; c < channels; c++) // draw !
				{
					float f1;
					f1 = buffer3[c];
					for (int x = 0; x < readSamples - 1; x++)
					{
						float f2 = buffer3[(x + 1) * channels + c];
						int x1 = x * samplePixels;
						int y1 = Transform(f1, channelHeight, c, verticalZoom);
						int x2 = (x + 1) * samplePixels;
						int y2 = Transform(f2, channelHeight, c, verticalZoom);
						DrawLine(canvas, x1, y1, x2, y1, colorEnvelope); // horizontal
						DrawLine(canvas, x2, y1, x2, y2, colorEnvelope); // vertical
						f1 = f2;
					}
					int x3 = (readSamples - 1) * samplePixels; // last sample !
					int y3 = Transform(f1, channelHeight, c, verticalZoom);
					DrawLineBresenham(canvas, x3, y3, x3 + samplePixels, y3, colorEnvelope);
					DrawLineBresenham(canvas, x3 + samplePixels, y3, x3 + samplePixels,
					                  Transform(buffer2[c], channelHeight, c, verticalZoom), colorEnvelope);
				}
				if (readSamples < samples) // draw end lines
				{
					int x = (readSamples) * samplePixels + 1;
					if (drawEndIndicator)
					{
						DrawLineBresenham(canvas, x, 0, x, bitmapHeight - 1, lineDashesEndIndicator);
					}
					if (drawDCLevel)
					{
						for (int i = 0; i < channels; i++)
						{
							int y = channelHeight * i + channelCenter;
							DrawLineBresenham(canvas, x, y, bitmapWidth - 1, y, theme.ColorEnvelope);
						}
					}
				}

				#endregion
			}
			else if (ratio == 1) // OK !
			{
				#region Draw 1:1

				var buffer3 = new float[samples * channels]; // visible samples
				int readSamples = AudioStream.ReadSamples(positionSamples, buffer3, samples);
				var int32 = colorEnvelope;
				for (int c = 0; c < channels; c++)
				{
					float f1 = buffer3[c];
					for (int x = 0; x < readSamples; x++)
					{
						float f2 = buffer3[x * channels + c];
						int x1 = (x - 1) * samplePixels;
						int y1 = Transform(f1, channelHeight, c, verticalZoom);
						int x2 = x * samplePixels;
						int y2 = Transform(f2, channelHeight, c, verticalZoom);
						DrawLine(canvas, x1, y1, x2, y2, int32);
						f1 = f2;
					}
				}

				if (readSamples < samples) // draw end lines
				{
					int x = (readSamples) * samplePixels + 1;
					if (drawEndIndicator)
					{
						LineDash[] lineDashes =
						{
							new LineDash(3, theme.ColorEndIndicator),
							new LineDash(3, Color.FromArgb(0xF0, 0xF0, 0xF0).ToArgb())
						};
						DrawLine(canvas, x, 0, x, bitmapHeight - 1, lineDashes);
					}
					if (drawDCLevel)
					{
						for (int i = 0; i < channels; i++)
						{
							int y = channelHeight * i + channelCenter;
							DrawLine(canvas, x, y, bitmapWidth - 1, y, theme.ColorEnvelope);
						}
					}
				}

				#endregion
			}
			else
			{
				var buffer = new float[bitmapWidth * hop];
				int peaksWidth; // pixels
				if (ratio < waveformCache.InitialRatio) // low overhead, just grab samples at this ratio
				{
					int position = positionSamples;
					int numberOfSamples = bitmapWidth * ratio;
					float[] floats = AudioStream.GetPeaks(ratio, numberOfSamples, position);
					Array.Copy(floats, 0, buffer, 0, floats.Length);
					peaksWidth = floats.Length / hop;
				}
				else // high overhead, grab portion of samples to draw from cache array
				{
					// OK !
					float[] peaks = waveformCache.GetPeaks(ratio);
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
						int y1 = Transform(minPrev, channelHeight, c, verticalZoom);
						int y2 = Transform(maxPrev, channelHeight, c, verticalZoom);
						int y3 = Transform(min, channelHeight, c, verticalZoom);
						int y4 = Transform(max, channelHeight, c, verticalZoom);
						if (drawForm)
						{
							if (!drawEnvelope) DrawLineBresenham(canvas, x - 1, y2, x, y4, colorEnvelope);
							DrawLineBresenham(canvas, x, y3, x, y4, colorForm);
						}
						if (drawEnvelope)
						{
							DrawLineBresenham(canvas, x - 1, y1, x, y3, colorEnvelope);
							DrawLineBresenham(canvas, x - 1, y2, x, y4, colorEnvelope);
						}
						maxPrev = max;
						minPrev = min;
					}
				}
				if (peaksWidth < bitmapWidth) // end indicator
				{
					int x = peaksWidth + 1;
					DrawLineBresenham(canvas, x, yMin, x, yMax, lineDashesEndIndicator);
				}
			}

			#endregion
			
			canvas.Dispose();
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
		
		#endregion
		
		#region Zoom handlers

		private void ZoomHorizontal(int i)
		{
			// todo behavior is ok but needs cleanup
			// todo move zoom bounds to property
			// Update zoom
			if (i > 0)
			{
				if (ZoomInteger > -5)
				{
					ZoomInteger--;
				}
			}
			else if (i < 0)
			{
				if (ZoomInteger < 16)
				{
					ZoomInteger++;
				}
			}
			else if (i == 0)
			{
				ZoomInteger = 0;
			}

			// Update slider
			int ratio = ZoomRatio;
			HScrollbar.SmallChange = ratio <= 0 ? 1 : Math.Abs(ratio);
			HScrollbar.LargeChange = HScrollbar.SmallChange * SliderLargeChange;
			//Slider1.TickFrequency = ratio < 0 ? 1 : ratio;

			if (ratio > 1)
			{
				// clamp to ratio
				var value = (int)HScrollbar.Value;
				int mod = value % ratio;
				int newValue = value - mod;
				HScrollbar.Value = newValue;
			}
			
			// TODO do refresh one time only
			RefreshWaveforms();
		}

		private void ZoomVertical(int value)
		{
			if (value > 0)
			{
				VerticalZoom *= 2.0d;
			}
			else if (value < 0)
			{
				VerticalZoom /= 2.0d;
			}
			else if (value == 0)
			{
				VerticalZoom = 1.0d;
			}
			RefreshWaveforms();
		}

		#endregion
		
		#region Mouse and Key Events
		/// <summary>Keys which can generate OnKeyDown event.</summary>
		private static readonly Keys[] InputKeys = new []
		{ Keys.Left, Keys.Up, Keys.Right, Keys.Down, Keys.Oemcomma, Keys.Home, Keys.OemPeriod, Keys.End };
		
		protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
		{
			if(Array.IndexOf<Keys>(InputKeys, e.KeyCode) != -1)
			{
				e.IsInputKey = true;
			}
			base.OnPreviewKeyDown(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			
			if (e.KeyCode == Keys.Up) {
				// increase the amplitude
				ZoomVertical(+1);
			} else if (e.KeyCode == Keys.Down) {
				// decrease the amplitude
				ZoomVertical(-1);
			} else if (e.KeyCode == Keys.Right) {
				//var increment = (int)e.Parameter;
				var increment = 5;
				int ratio = ZoomRatio <= 0 ? 1 : ZoomRatio;
				HScrollbar.Value += ratio * increment;
			} else if (e.KeyCode == Keys.Left) {
				//var increment = (int)e.Parameter;
				var increment = 5;
				int ratio = ZoomRatio <= 0 ? 1 : ZoomRatio;
				HScrollbar.Value -= ratio * increment;
			} else if (e.KeyCode == Keys.Oemcomma || e.KeyCode == Keys.Home) {
				HScrollbar.Value = HScrollbar.Minimum;
			} else if (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.End) {
				HScrollbar.Value = HScrollbar.Maximum;
			}
		}
		#endregion
		
		#region ZoomButtons
		void BtnZoomInClick(object sender, EventArgs e)
		{
			ZoomHorizontal(+1);
		}
		void BtnZoomOutClick(object sender, EventArgs e)
		{
			ZoomHorizontal(-1);
		}
		void BtnZoomOnSelectionClick(object sender, EventArgs e)
		{
			//
		}
		void BtnZoomInAmpClick(object sender, EventArgs e)
		{
			ZoomVertical(+1);
		}
		void BtnZoomOutAmpClick(object sender, EventArgs e)
		{
			ZoomVertical(-1);
		}
		void BtnZoomMul2Click(object sender, EventArgs e)
		{
			//
		}
		void BtnZoomDiv2Click(object sender, EventArgs e)
		{
			//
		}
		#endregion
	}
	
}
