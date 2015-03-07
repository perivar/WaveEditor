using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using CommonUtils.FFT; // Audio Analyzer, TimeLineUnit and DrawingProperties
using CommonUtils.Audio;

namespace CommonUtils.GUI
{
	/// <summary>
	/// Control for viewing waveforms
	/// </summary>
	public sealed class CustomWaveViewer : UserControl, INotifyPropertyChanged
	{
		#region Fields
		IWaveformPlayer _soundPlayer;
		Bitmap _offlineBitmap;

		// Sizes
		const int SIDE_MARGIN = 30;		// waveform area side margin
		const int TOP_MARGIN = 15; 		// waveform area top margin
		int _waveformDrawingWidth = 0; 	// the width where the waveform is drawn (excluding the margins)
		int _waveformDrawingHeight = 0; 	// the height where the waveform is drawn (excluding the margins)

		// Progress
		int _progressSample = 0; 		// what sample position is the progress/play head at

		// Loop
		Rectangle _loopRegion = new Rectangle();
		int _startLoopXPosition = -1;
		int _previousStartLoopXPosition = -1;
		int _endLoopXPosition = -1;
		int _previousEndLoopXPosition = -1;
		int _startLoopSamplePosition = -1;
		int _endLoopSamplePosition = -1;

		// Zoom
		int _startZoomSamplePosition = 0;
		int _endZoomSamplePosition = 0;
		int _previousStartZoomSamplePosition = 0;

		int _amplitude = 1; // 1 = default amplitude
		
		double _samplesPerPixel = 128;
		
		// Mouse variables
		const int MOUSE_MOVE_TOLERANCE = 3;
		bool _isMouseDown = false;
		bool _isZooming = false;
		Point _mouseDownPoint;
		Point _currentPoint;
		
		// current point
		int _currentPointSamplePos = -1;
		double _currentPointTimePos = -1;
		
		// time line unit and drawing properties
		TimelineUnit _timelineUnit = TimelineUnit.Time;
		DrawingProperties _drawingProperties = DrawingProperties.Blue;

		// setup image attributes for color negation
		ImageAttributes _imageAttributes = new ImageAttributes();
		#endregion

		#region Properties
		public TimelineUnit TimelineUnit {
			get {
				return _timelineUnit;
			}
			set {
				TimelineUnit oldValue = _timelineUnit;
				_timelineUnit = value;
				if (oldValue != TimelineUnit) {
					NotifyPropertyChanged("TimelineUnit");
					UpdateWaveform();
				}
			}
		}

		public double SamplesPerPixel {
			get {
				return _samplesPerPixel;
			}
			set {
				double oldValue = _samplesPerPixel;
				_samplesPerPixel = value;
				if (oldValue != _samplesPerPixel) {
					NotifyPropertyChanged("SamplesPerPixel");
					NotifyPropertyChanged("ZoomRatioString");
				}
			}
		}

		public string ZoomRatioString
		{
			get
			{
				if (SamplesPerPixel < 1) {
					return string.Format("{0:0}:1", 1/SamplesPerPixel);
				} else {
					return string.Format("1:{0:0}", SamplesPerPixel);
				}
			}
		}

		public int StartLoopSamplePosition {
			get {
				return _startLoopSamplePosition;
			}
			set {
				int oldValue = _startLoopSamplePosition;
				_startLoopSamplePosition = value;
				if (oldValue != _startLoopSamplePosition)
					NotifyPropertyChanged("StartLoopSamplePosition");
			}
		}

		public int EndLoopSamplePosition {
			get {
				return _endLoopSamplePosition;
			}
			set {
				int oldValue = _endLoopSamplePosition;
				_endLoopSamplePosition = value;
				if (oldValue != _endLoopSamplePosition)
					NotifyPropertyChanged("EndLoopSamplePosition");
			}
		}

		public int WaveformDrawingWidth {
			get {
				return _waveformDrawingWidth;
			}
			set {
				int oldValue = _waveformDrawingWidth;
				_waveformDrawingWidth = value;
				if (oldValue != _waveformDrawingWidth)
					NotifyPropertyChanged("WaveformDrawingWidth");
			}
		}

		public int WaveformDrawingHeight {
			get {
				return _waveformDrawingHeight;
			}
			set {
				int oldValue = _waveformDrawingHeight;
				_waveformDrawingHeight = value;
				if (oldValue != _waveformDrawingHeight)
					NotifyPropertyChanged("WaveformDrawingHeight");
			}
		}

		public int StartZoomSamplePosition {
			get {
				return _startZoomSamplePosition;
			}
			set {
				int oldValue = _startZoomSamplePosition;
				_startZoomSamplePosition = value;
				if (oldValue != _startZoomSamplePosition)
					NotifyPropertyChanged("StartZoomSamplePosition");
			}
		}

		public int EndZoomSamplePosition {
			get {
				return _endZoomSamplePosition;
			}
			set {
				int oldValue = _endZoomSamplePosition;
				_endZoomSamplePosition = value;
				if (oldValue != _endZoomSamplePosition)
					NotifyPropertyChanged("EndZoomSamplePosition");
			}
		}

		public int PreviousStartZoomSamplePosition {
			get {
				return _previousStartZoomSamplePosition;
			}
			set {
				int oldValue = _previousStartZoomSamplePosition;
				_previousStartZoomSamplePosition = value;
				if (oldValue != _previousStartZoomSamplePosition)
					NotifyPropertyChanged("PreviousStartZoomSamplePosition");
			}
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new WaveViewer control
		/// </summary>
		public CustomWaveViewer()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
			              ControlStyles.OptimizedDoubleBuffer, true);
			this.DoubleBuffered = true;
			
			// set drawing properties for the waveform
			_drawingProperties.DrawRaw = true;
			_drawingProperties.DisplayDebugBox = false;

			// TODO: change the way the double buffering is done, e.g.
			// http://inchoatethoughts.com/custom-drawing-controls-in-c-manual-double-buffering
			
			// Setup Color Matrix for inverting the selection
			float[][] colorMatrixElements = {
				new float[] {-1, 0,  0,  0,  0},
				new float[] {0, -1,  0,  0,  0},
				new float[] {0,  0, -1,  0,  0},
				new float[] {0,  0,  0,  1,  0},
				new float[] {1,  1,  1,  0,  1}};
			var matrix = new ColorMatrix(colorMatrixElements);
			_imageAttributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
		}
		#endregion
		
		#region Event Overrides
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			_waveformDrawingWidth = this.Width - (2 * SIDE_MARGIN);
			_waveformDrawingHeight = this.Height - (2 * TOP_MARGIN);
			
			FitToScreen();
		}
		
		protected override void OnPaint(PaintEventArgs e)
		{
			if (_offlineBitmap != null) {

				// create a blank bitmap the same size as original
				var tempImage = new Bitmap(_offlineBitmap.Width, _offlineBitmap.Height);
				
				// get a graphics object from the new image
				Graphics gNewBitmap = Graphics.FromImage(tempImage);

				// draw the offline bitmap
				gNewBitmap.DrawImage(_offlineBitmap, 0, 0);
				
				// draw marker
				using (var markerPen = new Pen(Color.Blue, 1))
				{
					// what samples are we showing?
					if (_progressSample >= _startZoomSamplePosition && _progressSample <= _endZoomSamplePosition) {
						int xLocation = SamplePositionToXPosition(_progressSample - _startZoomSamplePosition, _samplesPerPixel);
						gNewBitmap.DrawLine(markerPen, xLocation, TOP_MARGIN - 1, xLocation, _waveformDrawingHeight + TOP_MARGIN - 1);
					}
					
					using (var currentFont = new Font("Arial", 7)) {
						string currentText = String.Format("[{0}:{1}]  {2}  {3:hh\\:mm\\:ss\\.fff}", _currentPoint.X, _currentPoint.Y, _currentPointSamplePos, TimeSpan.FromSeconds(_currentPointTimePos));
						SizeF currentTextSize = gNewBitmap.MeasureString(currentText, currentFont);
						gNewBitmap.DrawString(currentText, currentFont, Brushes.Black, SIDE_MARGIN + _waveformDrawingWidth - currentTextSize.Width, TOP_MARGIN + _waveformDrawingHeight + 1);
					}
				}
				
				// draw the temporary bitmap onto the main canvas
				e.Graphics.DrawImageUnscaled(tempImage, 0, 0);
				
				// setup the region we want to invert (select region)
				var destination = new Rectangle(_startLoopXPosition, TOP_MARGIN - 1, _loopRegion.Width, _loopRegion.Height - 2 * TOP_MARGIN);
				var region = new Region(destination);
				e.Graphics.Clip = region;

				// invert the select region
				e.Graphics.DrawImage(tempImage, destination, destination.Left, destination.Top,
				                     destination.Width, destination.Height, GraphicsUnit.Pixel, _imageAttributes);

				
				// dispose of the temporary Graphics objects
				gNewBitmap.Dispose();
				tempImage.Dispose();
			}
			
			// Calling the base class OnPaint
			base.OnPaint(e);
		}
		#endregion
		
		#region Public Methods
		/// <summary>
		/// Register a sound player from which the waveform timeline
		/// can get the necessary playback data.
		/// </summary>
		/// <param name="soundPlayer">A sound player that provides waveform data through the IWaveformPlayer interface methods.</param>
		public void RegisterSoundPlayer(IWaveformPlayer soundPlayer)
		{
			this._soundPlayer = soundPlayer;
			soundPlayer.PropertyChanged += soundPlayer_PropertyChanged;
		}

		public void SelectAll() {
			
			// if everything is already selected, unselect
			if (_soundPlayer.SelectionBegin == TimeSpan.FromSeconds(0) &&
			    _soundPlayer.SelectionEnd == TimeSpan.FromSeconds(_soundPlayer.ChannelLength)) {

				// fit to screen also clears loop regions
				FitToScreen();
			} else {
				// fit to screen also clears loop regions
				FitToScreen();

				_startLoopXPosition = SIDE_MARGIN;
				_endLoopXPosition = SIDE_MARGIN + _waveformDrawingWidth;
				
				// TODO: select everything visible instead of the whole song?
				_soundPlayer.SelectionBegin = TimeSpan.FromSeconds(0);
				_soundPlayer.SelectionEnd = TimeSpan.FromSeconds(_soundPlayer.ChannelLength);
				_soundPlayer.ChannelPosition = 0;
			}
			
			UpdateLoopRegion();
		}

		#endregion
		
		#region Public Zoom Methods
		public void ZoomHorizontal(int i)
		{
			if (_soundPlayer != null && _soundPlayer.ChannelSampleLength > 1)
			{
				int drawingWidth = _waveformDrawingWidth;
				int startZoomSamplePos = _startZoomSamplePosition;
				int endZoomSamplePos = _endZoomSamplePosition;
				double samplesPerPixel = _samplesPerPixel;
				int rangeInSamples = Math.Abs(endZoomSamplePos - startZoomSamplePos);

				// Update zoom
				if (i > 0)
				{
					// don't exceed 32:1 (2^5)
					const float min = (float)1/32;
					if (samplesPerPixel > min)
					{
						samplesPerPixel /= 2;
						endZoomSamplePos = (int) (startZoomSamplePos + (rangeInSamples / 2));
					}
				}
				else if (i < 0)
				{
					// don't exceed 1:65536 (2^16)
					if (samplesPerPixel < 65536)
					{
						samplesPerPixel *= 2;
						endZoomSamplePos = (int) (startZoomSamplePos + (rangeInSamples * 2));
						if (endZoomSamplePos > _soundPlayer.ChannelSampleLength) {
							// shift start zoom position forwards
							int shiftSamples = endZoomSamplePos - _soundPlayer.ChannelSampleLength;
							startZoomSamplePos -= shiftSamples;
							endZoomSamplePos = _soundPlayer.ChannelSampleLength;
						}
					}
				}
				else if (i == 0)
				{
					samplesPerPixel = 1;
					endZoomSamplePos = (int) (startZoomSamplePos + (rangeInSamples));
				}
				
				Zoom(startZoomSamplePos, endZoomSamplePos);
			}
		}
		
		public void Zoom(int startZoomSamplePos, int endZoomSamplePos)
		{
			if (_soundPlayer != null && _soundPlayer.ChannelSampleLength > 1)
			{
				double startSeconds = SamplePositionToSeconds(startZoomSamplePos, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength);
				double endSeconds = SamplePositionToSeconds(endZoomSamplePos, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength);
				
				// make sure the zoom start and zoom end is correct
				if (startZoomSamplePos < 0) {
					startZoomSamplePos = 0;
				}
				if (endZoomSamplePos > _soundPlayer.ChannelSampleLength || endZoomSamplePos < 0) {
					endZoomSamplePos = _soundPlayer.ChannelSampleLength;
				}
				
				// Check that we are not zooming too much
				int rangeInSamples = Math.Abs(endZoomSamplePos - startZoomSamplePos);
				if (rangeInSamples <= 10) {
					return;
				}
				
				StartZoomSamplePosition = startZoomSamplePos;
				EndZoomSamplePosition = endZoomSamplePos;
				PreviousStartZoomSamplePosition = startZoomSamplePos;
				SamplesPerPixel = (float) (endZoomSamplePos - startZoomSamplePos) / (float) _waveformDrawingWidth;

				
				#region Update Loop Region After Zooming
				
				// TODO: only calculate loop start and end x position when painting
				
				// if both start and stop loop pos is before the zoom window
				if (_startLoopSamplePosition < _startZoomSamplePosition
				    && _endLoopSamplePosition < _startZoomSamplePosition) {
					
					_startLoopXPosition = -1;
					_endLoopXPosition = -1;
					
					// if both start and stop loop pos is after the zoom window
				} else if (_startLoopSamplePosition > _endZoomSamplePosition
				           && _endLoopSamplePosition > _endZoomSamplePosition) {

					_startLoopXPosition = -1;
					_endLoopXPosition = -1;
					
					// if start loop pos is before zoom window and end pos is within the zoom window
				} else if (_startLoopSamplePosition < _startZoomSamplePosition
				           && Between(_endLoopSamplePosition, _startZoomSamplePosition, _endZoomSamplePosition)) {
					
					_startLoopXPosition = SIDE_MARGIN;

					int endRangeInSamples = Math.Abs(_endLoopSamplePosition - _startZoomSamplePosition);
					_endLoopXPosition = SamplePositionToXPosition(endRangeInSamples, _samplesPerPixel);
					
					// if start loop pos is within the zoom window and the end pos is after
				} else if (Between(_startLoopSamplePosition, _startZoomSamplePosition, _endZoomSamplePosition)
				           && _endLoopSamplePosition > _endZoomSamplePosition) {

					_endLoopXPosition = SIDE_MARGIN + _waveformDrawingWidth;

					int startRangeInSamples = Math.Abs(_startZoomSamplePosition - _startLoopSamplePosition);
					_startLoopXPosition = SamplePositionToXPosition(startRangeInSamples, _samplesPerPixel);
					
					// if both loop pos is within zoom window
				} else if (Between(_startLoopSamplePosition, _startZoomSamplePosition, _endZoomSamplePosition)
				           && Between(_endLoopSamplePosition, _startZoomSamplePosition, _endZoomSamplePosition)) {
					
					int startRangeInSamples = Math.Abs(_startLoopSamplePosition - _startZoomSamplePosition);
					_startLoopXPosition = SamplePositionToXPosition(startRangeInSamples, _samplesPerPixel);
					
					int loopRangeInSamples = Math.Abs(_endLoopSamplePosition - _startLoopSamplePosition);
					int len = SamplePositionToXPosition(loopRangeInSamples, _samplesPerPixel, false);
					
					_endLoopXPosition = _startLoopXPosition + len;
				}
				_previousStartLoopXPosition = _startLoopXPosition;
				_previousEndLoopXPosition = _endLoopXPosition;
				
				UpdateLoopRegion(false);
				#endregion
				
				// Update the waveform
				UpdateWaveform();
			}
		}
		
		public void FitToScreen()
		{
			if (_soundPlayer != null && _soundPlayer.ChannelSampleLength > 1)
			{
				int numberOfChannelSamples = _soundPlayer.ChannelSampleLength;
				SamplesPerPixel = (float) numberOfChannelSamples / (float) _waveformDrawingWidth;
				PreviousStartZoomSamplePosition = 0;
				StartZoomSamplePosition = 0;
				EndZoomSamplePosition = numberOfChannelSamples;
			}

			// remove select region after zooming
			ClearLoopRegion();
			
			// reset amplitude
			this._amplitude = 1;
			
			UpdateWaveform();
		}
		
		public void ZoomInAmplitude() {
			// increase the amplitude
			if (_amplitude * 2 < 5000) {
				_amplitude*=2;
				UpdateWaveform();
			}
		}
		
		public void ZoomOutAmplitude() {
			// decrease the amplitude
			_amplitude/=2;
			if (_amplitude < 1) _amplitude = 1;
			UpdateWaveform();
		}
		
		public void ZoomSelection() {
			_startZoomSamplePosition = _startLoopSamplePosition;
			_endZoomSamplePosition = _endLoopSamplePosition;
			
			Zoom(StartLoopSamplePosition, EndLoopSamplePosition);
		}
		
		public void IncreaseSelection() {
			// keep start selection pos, but double the length
			if (StartLoopSamplePosition >= 0 && EndLoopSamplePosition > 0) {
				int rangeInSamples = Math.Abs(EndLoopSamplePosition - StartLoopSamplePosition);
				int rangeInPixles = Math.Abs(_endLoopXPosition - _startLoopXPosition);
				
				rangeInSamples *= 2;
				rangeInPixles *= 2;
				
				_endLoopXPosition = Math.Min(this.WaveformDrawingWidth + SIDE_MARGIN, _startLoopXPosition + rangeInPixles);
				EndLoopSamplePosition = Math.Min(_soundPlayer.ChannelSampleLength, _startLoopSamplePosition + rangeInSamples);
				
				if (_soundPlayer != null && _soundPlayer.WaveformData != null) {
					_soundPlayer.SelectionEnd = TimeSpan.FromSeconds(SamplePositionToSeconds(_endLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength));
				}
				
				UpdateLoopRegion();
			}
		}
		
		public void DecreaseSelection() {
			// keep start selection pos, but half the length
			if (StartLoopSamplePosition >= 0 && EndLoopSamplePosition > 0) {
				int rangeInSamples = Math.Abs(EndLoopSamplePosition - StartLoopSamplePosition);
				int rangeInPixles = Math.Abs(_endLoopXPosition - _startLoopXPosition);
				
				rangeInSamples /= 2;
				rangeInPixles /= 2;
				
				_endLoopXPosition = Math.Min(this.WaveformDrawingWidth + SIDE_MARGIN, _startLoopXPosition + rangeInPixles);
				EndLoopSamplePosition = Math.Min(_soundPlayer.ChannelSampleLength, _startLoopSamplePosition + rangeInSamples);
				
				if (_soundPlayer != null && _soundPlayer.WaveformData != null) {
					_soundPlayer.SelectionEnd = TimeSpan.FromSeconds(SamplePositionToSeconds(_endLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength));
				}
				
				UpdateLoopRegion();
			}
		}
		
		#endregion
		
		#region Private Drawing Methods
		private void UpdateWaveform()
		{
			if (_soundPlayer == null || _soundPlayer.WaveformData == null)
				return;
			
			if (_soundPlayer.ChannelSampleLength > 1) {
				_drawingProperties.TimeLineUnit = _timelineUnit;
				this._offlineBitmap = AudioAnalyzer.DrawWaveform(_soundPlayer.WaveformData,
				                                                 new Size(this.Width, this.Height),
				                                                 _amplitude,
				                                                 _startZoomSamplePosition, _endZoomSamplePosition,
				                                                 -1, -1,
				                                                 -1,
				                                                 _soundPlayer.SampleRate,
				                                                 _soundPlayer.Channels,
				                                                 _drawingProperties);

				// force redraw
				this.Invalidate();
			}
		}

		private void UpdateProgressIndicator()
		{
			if (_soundPlayer != null && _soundPlayer.ChannelSampleLength != 0)
			{
				_progressSample = SecondsToSamplePosition(_soundPlayer.ChannelPosition, _soundPlayer.ChannelLength, _soundPlayer.ChannelSampleLength);
				
				int rangeInSamples = Math.Abs(_endZoomSamplePosition - _startZoomSamplePosition);
				
				// if the progress is before the zoom window
				if (_progressSample < _startZoomSamplePosition) {
					Zoom(0, _progressSample + rangeInSamples);
					
					// or if the progress is after half the zoom window
				} else if (_progressSample > (_endZoomSamplePosition - rangeInSamples/2)) {
					
					// don't zoom if we are in the last scroll frame
					if (_progressSample > _soundPlayer.ChannelSampleLength - rangeInSamples) {
						// force redraw
						this.Invalidate();
					} else {
						// keep the progress line in the center while playing
						if (_soundPlayer.IsPlaying) {
							Zoom(_progressSample - rangeInSamples/2, _progressSample + rangeInSamples/2);
						}
					}
					// or if the progress is within the zoom window
				} else {
					// force redraw
					this.Invalidate();
				}
			}
		}
		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// CustomWaveViewer
			// 
			this.Name = "CustomWaveViewer";
			this.Size = new System.Drawing.Size(600, 200);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CustomWaveViewerMouseDown);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CustomWaveViewerMouseMove);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CustomWaveViewerMouseUp);
			this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.CustomWaveViewerMouseWheel);
			this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.CustomWaveViewerMouseDoubleClick);
			this.ResumeLayout(false);
		}
		#endregion
		
		#region Mouse Events
		void CustomWaveViewerMouseWheel(object sender, MouseEventArgs e)
		{
			// read in the current zoom sample positions and sample length
			int oldStartZoomSamplePosition = _startZoomSamplePosition;
			int oldEndZoomSamplePosition = _endZoomSamplePosition;
			int channelSampleLength = _soundPlayer.ChannelSampleLength;
			
			// most of the mouse wheel zoom logic is taken from BlueberryThing Source
			int midpoint;
			int delta;
			int newStartZoomSamplePosition;
			int newEndZoomSamplePosition;
			float hitpointFraction;
			
			// calculate the range
			int rangeInSamples = oldEndZoomSamplePosition - oldStartZoomSamplePosition;
			
			// Scroll the display left/right
			if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
				delta = rangeInSamples / 20;
				
				// If scrolling right (forward in time on the waveform)
				if (e.Delta > 0) {
					delta = MathUtils.LimitInt(delta, 0, channelSampleLength - oldEndZoomSamplePosition);
					newStartZoomSamplePosition = oldStartZoomSamplePosition + delta;
					newEndZoomSamplePosition = oldEndZoomSamplePosition + delta;
				}
				
				// If scrolling left (backward in time on the waveform)
				else
				{
					delta = MathUtils.LimitInt(delta, 0, oldStartZoomSamplePosition);
					newStartZoomSamplePosition = oldStartZoomSamplePosition - delta;
					newEndZoomSamplePosition = oldEndZoomSamplePosition - delta;
				}
			}

			// change the amplitude up or down
			else if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) {
				
				// If right (increase the amplitude)
				if (e.Delta > 0) {
					// increase the amplitude
					if (_amplitude * 2 < 5000) {
						_amplitude*=2;
						UpdateWaveform();
					}
				}
				
				// If left (decrease the amplitude)
				else {
					_amplitude/=2;
					if (_amplitude < 1) _amplitude = 1;
				}
				
				UpdateWaveform();
				return;
			}
			
			// Zoom the display in/out
			else {
				midpoint = oldStartZoomSamplePosition + (rangeInSamples / 2);
				hitpointFraction = (float)e.X / (float)this.Width;
				if (hitpointFraction < 0.0f)
					hitpointFraction = 0.0f;
				if (hitpointFraction > 1.0f)
					hitpointFraction = 1.0f;
				
				if (e.Delta > 0) {
					// Zoom in
					delta = rangeInSamples / 4;
					newStartZoomSamplePosition = (int) (oldStartZoomSamplePosition + (delta * hitpointFraction));
					newEndZoomSamplePosition = (int) (oldEndZoomSamplePosition - (delta * (1.0 - hitpointFraction)));
					
					// only allow zooming if samples are more than 10
					int samplesSelected = newEndZoomSamplePosition - newStartZoomSamplePosition;
					if (samplesSelected <= 10) {
						return;
					}
				} else {
					// Zoom out
					delta = rangeInSamples / 3; // must use a higher delta than zoom in to make sure we can zoom out again
					newStartZoomSamplePosition = (int) (oldStartZoomSamplePosition - (delta * hitpointFraction));
					newEndZoomSamplePosition = (int) (oldEndZoomSamplePosition + (delta * (1.0 - hitpointFraction)));
				}
				
				// Limit the view
				if (newStartZoomSamplePosition < 0)
					newStartZoomSamplePosition = 0;
				if (newStartZoomSamplePosition > midpoint)
					newStartZoomSamplePosition = midpoint;
				if (newEndZoomSamplePosition < midpoint)
					newEndZoomSamplePosition = midpoint;
				if (newEndZoomSamplePosition > channelSampleLength)
					newEndZoomSamplePosition = channelSampleLength;
			}
			
			// If there a change in the view, then refresh the display
			if ((newStartZoomSamplePosition != oldStartZoomSamplePosition)
			    || (newEndZoomSamplePosition != oldEndZoomSamplePosition))
			{
				// Zoom
				Zoom(newStartZoomSamplePosition, newEndZoomSamplePosition);
			}
		}
		
		void CustomWaveViewerMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left) {
				_isMouseDown = true;
				_mouseDownPoint = e.Location;
				
				if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
					// Control key is being pressed
					_isZooming = true;
				} else {
					_isZooming = false;
				}
			}
			else if (e.Button == MouseButtons.Right) {
				FitToScreen();
			}
		}
		
		void CustomWaveViewerMouseMove(object sender, MouseEventArgs e)
		{
			_currentPoint = e.Location;
			
			int currentXPosition = _currentPoint.X;
			int mouseDownXPosition = _mouseDownPoint.X;
			int startLoopXPos = _startLoopXPosition;
			int endLoopXPos = _endLoopXPosition;
			int prevStartXPos = _previousStartLoopXPosition;
			int prevEndXPos = _previousEndLoopXPosition;
			
			if (_soundPlayer.WaveformData == null) return;
			
			if (_isMouseDown) {
				// dragging left anchor
				if (Between(mouseDownXPosition, prevStartXPos - MOUSE_MOVE_TOLERANCE, prevStartXPos + MOUSE_MOVE_TOLERANCE)) {
					
					// test if current left x is bigger than right
					if (currentXPosition > _endLoopXPosition) {
						_startLoopXPosition = ClosestSampleAccurateXPosition(_previousEndLoopXPosition, _samplesPerPixel);
						_endLoopXPosition = ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel);
					} else {
						_startLoopXPosition = ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel);
						_endLoopXPosition = ClosestSampleAccurateXPosition(_previousEndLoopXPosition, _samplesPerPixel);
					}
					
					// dragging right anchor
				} else if (Between(mouseDownXPosition, prevEndXPos - MOUSE_MOVE_TOLERANCE, prevEndXPos + MOUSE_MOVE_TOLERANCE)) {
					
					// test if current right x is less than left
					if (currentXPosition < _startLoopXPosition) {
						_startLoopXPosition = ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel);
						_endLoopXPosition = ClosestSampleAccurateXPosition(_previousStartLoopXPosition, _samplesPerPixel);
					} else {
						_startLoopXPosition = ClosestSampleAccurateXPosition(_previousStartLoopXPosition, _samplesPerPixel);
						_endLoopXPosition = ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel);
					}
				} else {
					_startLoopXPosition = Math.Min(ClosestSampleAccurateXPosition(mouseDownXPosition, _samplesPerPixel), ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel));
					_endLoopXPosition = Math.Max(ClosestSampleAccurateXPosition(mouseDownXPosition, _samplesPerPixel), ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel));
				}
				
				// limit start and end to waveform drawing space
				if (_startLoopXPosition < SIDE_MARGIN) _startLoopXPosition = SIDE_MARGIN;
				if (_endLoopXPosition > SIDE_MARGIN + _waveformDrawingWidth) _endLoopXPosition = SIDE_MARGIN + _waveformDrawingWidth;
				
				GetCurrentPoint();
				
				// update loop region and redraw
				UpdateLoopRegion();
			} else {
				
				GetCurrentPoint();
				
				// redraw
				this.Invalidate();
			}

			// Display the appropriate cursor.
			if (IsLoopBoundry(_currentPoint.X, _startLoopXPosition, _endLoopXPosition)) {
				this.Cursor = Cursors.VSplit;
			} else {
				this.Cursor = null;
			}
		}
		
		void CustomWaveViewerMouseDoubleClick(object sender, MouseEventArgs e) {
			SelectAll();
		}
		
		/// <summary>
		/// Determine if the mouse location is at one of the loop ends (boundries)
		/// </summary>
		/// <param name="mouseXLocation">mouse x position</param>
		/// <param name="startLoopXPos">start loop X position</param>
		/// <param name="endLoopXPos">end loop X position</param>
		/// <returns></returns>
		static bool IsLoopBoundry(int mouseXLocation, int startLoopXPos, int endLoopXPos) {
			
			if (Between(mouseXLocation, startLoopXPos - MOUSE_MOVE_TOLERANCE, startLoopXPos + MOUSE_MOVE_TOLERANCE)
			    || Between(mouseXLocation, endLoopXPos - MOUSE_MOVE_TOLERANCE, endLoopXPos + MOUSE_MOVE_TOLERANCE)) {
				return true;
			} else {
				return false;
			}
		}
		
		/// <summary>
		/// Store the information underneath the current mouse position
		/// I.e. Sample Position and Time Position
		/// </summary>
		void GetCurrentPoint() {
			_currentPointSamplePos = _startZoomSamplePosition + XPositionToSamplePosition(_currentPoint.X, _samplesPerPixel, true);
			_currentPointTimePos = SamplePositionToSeconds(_currentPointSamplePos, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength);
		}
		
		void CustomWaveViewerMouseUp(object sender, MouseEventArgs e)
		{
			if (!_isMouseDown || _soundPlayer.WaveformData == null)
				return;
			
			_isMouseDown = false;

			if (_isZooming) {
				int startZoomSamplePos = Math.Max(_previousStartZoomSamplePosition + XPositionToSamplePosition(_startLoopXPosition, _samplesPerPixel), 0);
				int endZoomSamplePos = Math.Min(_previousStartZoomSamplePosition + XPositionToSamplePosition(_endLoopXPosition, _samplesPerPixel), _soundPlayer.ChannelSampleLength);
				
				// only allow zooming if samples are more than 10
				int samplesSelected = endZoomSamplePos - startZoomSamplePos;
				if (samplesSelected > 10) {
					// Zoom
					Zoom(startZoomSamplePos, endZoomSamplePos);
				}
				return;
			}

			bool doUpdateLoopRegion = false;

			if (Math.Abs(_currentPoint.X - _mouseDownPoint.X) < MOUSE_MOVE_TOLERANCE) {
				// if we did not select a new loop range but just clicked
				int curSamplePosition = _previousStartZoomSamplePosition + XPositionToSamplePosition(_mouseDownPoint.X, _samplesPerPixel);
				
				if (PointInLoopRegion(curSamplePosition)) {
					_soundPlayer.ChannelPosition = SamplePositionToSeconds(curSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength);
				} else {
					_soundPlayer.SelectionBegin = TimeSpan.Zero;
					_soundPlayer.SelectionEnd = TimeSpan.Zero;
					_soundPlayer.ChannelPosition = SamplePositionToSeconds(curSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength);
					
					StartLoopSamplePosition = -1;
					EndLoopSamplePosition = -1;
					
					doUpdateLoopRegion = true;
				}
			} else {
				_previousStartLoopXPosition = _startLoopXPosition;
				_previousEndLoopXPosition = _endLoopXPosition;

				StartLoopSamplePosition = Math.Max(_previousStartZoomSamplePosition + XPositionToSamplePosition(_startLoopXPosition, _samplesPerPixel), 0);
				EndLoopSamplePosition = Math.Min(_previousStartZoomSamplePosition + XPositionToSamplePosition(_endLoopXPosition, _samplesPerPixel), _soundPlayer.ChannelSampleLength);

				_soundPlayer.SelectionBegin = TimeSpan.FromSeconds(SamplePositionToSeconds(_startLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength));
				_soundPlayer.SelectionEnd = TimeSpan.FromSeconds(SamplePositionToSeconds(_endLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength));
				_soundPlayer.ChannelPosition = SamplePositionToSeconds(_startLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength);
			}
			
			if (doUpdateLoopRegion) {
				_startLoopXPosition = 1;
				_endLoopXPosition = -1;
				UpdateLoopRegion();
			}
		}

		#endregion
		
		#region Key Events
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
				ZoomInAmplitude();
			} else if (e.KeyCode == Keys.Down) {
				ZoomOutAmplitude();
			} else if (e.KeyCode == Keys.Right) {
				ScrollTime(true);
			} else if (e.KeyCode == Keys.Left) {
				ScrollTime(false);
			} else if (e.KeyCode == Keys.Oemcomma || e.KeyCode == Keys.Home) {
				_soundPlayer.ChannelPosition = 0;
				
				// keep zoom level and set position to 0
				int rangeInSamples = Math.Abs(_endZoomSamplePosition - _startZoomSamplePosition);
				Zoom(0, rangeInSamples);
				
			} else if (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.End) {
				_soundPlayer.ChannelPosition = _soundPlayer.ChannelLength;

				// keep zoom level and set position to last range
				int rangeInSamples = Math.Abs(_endZoomSamplePosition - _startZoomSamplePosition);
				Zoom(_soundPlayer.ChannelSampleLength - rangeInSamples, _soundPlayer.ChannelSampleLength);
			}
		}
		#endregion
		
		#region Private Static Util Methods
		
		/// <summary>
		/// Determine if value is between or equal to left and right values
		/// </summary>
		/// <param name="value">value to check</param>
		/// <param name="left">leftmost value</param>
		/// <param name="right">rightmost value</param>
		/// <returns>true if between</returns>
		private static bool Between(int value, int left, int right)
		{
			return value >= left && value <= right;
		}
		
		/// <summary>
		/// Return a x position which closes to an actual sample
		/// </summary>
		/// <param name="xPosition">x position to correct</param>
		/// <param name="samplesPerPixel">samples per pixel</param>
		/// <returns>a sample accurate x position</returns>
		private static int ClosestSampleAccurateXPosition(int xPosition, double samplesPerPixel) {
			return SamplePositionToXPosition(XPositionToSamplePosition(xPosition, samplesPerPixel), samplesPerPixel);
		}
		
		/// <summary>
		/// Convert a sample position to a x position
		/// </summary>
		/// <param name="samplePosition">sample position</param>
		/// <param name="samplesPerPixel">samples per pixel</param>
		/// <param name="useMargin">whether to use a margin</param>
		/// <returns>x position</returns>
		private static int SamplePositionToXPosition(int samplePosition, double samplesPerPixel, bool useMargin=true) {
			double pixelPosition = samplePosition / samplesPerPixel;
			int pixelPos = (int) pixelPosition;
			return (useMargin ? SIDE_MARGIN : 0) + pixelPos;
		}

		/// <summary>
		/// Convert a x position to a sample position
		/// </summary>
		/// <param name="xPosition">x position</param>
		/// <param name="samplesPerPixel">samples per pixel</param>
		/// <param name="useMargin">whether to use a margin</param>
		/// <returns>sample position</returns>
		private static int XPositionToSamplePosition(int xPosition, double samplesPerPixel, bool useMargin=true) {
			double samplePos = -1;
			if (useMargin) {
				samplePos = samplesPerPixel * (double) (xPosition - SIDE_MARGIN);
			} else {
				samplePos = samplesPerPixel * xPosition;
			}
			return (int) samplePos;
		}
		
		/// <summary>
		/// Convert a sample position to a time in seconds
		/// </summary>
		/// <param name="samplePosition">sample position</param>
		/// <param name="totalSamples">total number of samples</param>
		/// <param name="totalDurationSeconds">total duration in seconds</param>
		/// <returns>Time in seconds</returns>
		public static double SamplePositionToSeconds(int samplePosition, int totalSamples, double totalDurationSeconds) {
			double positionPercent = (double) samplePosition / (double) totalSamples;
			double position = (totalDurationSeconds * positionPercent);
			return Math.Min(totalDurationSeconds, Math.Max(0, position));
		}

		/// <summary>
		/// Convert a time in seconds to sample position
		/// </summary>
		/// <param name="channelPositionSeconds">time in seconds</param>
		/// <param name="totalDurationSeconds">total duration in seconds</param>
		/// <param name="totalSamples">total number of samples</param>
		/// <returns>Sample position</returns>
		public static int SecondsToSamplePosition(double channelPositionSeconds, double totalDurationSeconds, int totalSamples) {
			double progressPercent = channelPositionSeconds / totalDurationSeconds;
			// TODO: make this more precise
			if (progressPercent > 0.999) {
				// set to 100%
				progressPercent = 1;
			}
			int position = (int) (totalSamples * progressPercent);
			return Math.Min(totalSamples, Math.Max(0, position));
		}
		#endregion
		
		#region Public Scroll methods
		public void ScrollTime(bool doScrollRight, int startZoomSamplePosition) {
			if (_soundPlayer.WaveformData == null) return;

			// read in the current zoom sample positions and sample length
			int oldStartZoomSamplePosition = _startZoomSamplePosition;
			int oldEndZoomSamplePosition = _endZoomSamplePosition;
			int channelSampleLength = _soundPlayer.ChannelSampleLength;
			
			// find delta
			int delta = (int) Math.Abs(oldStartZoomSamplePosition - startZoomSamplePosition);
			
			int newStartZoomSamplePosition = -1;
			int newEndZoomSamplePosition = -1;
			
			// If scrolling right (forward in time on the waveform)
			if (doScrollRight) {
				delta = MathUtils.LimitInt(delta, 0, channelSampleLength - oldEndZoomSamplePosition);
				newStartZoomSamplePosition = oldStartZoomSamplePosition + delta;
				newEndZoomSamplePosition = oldEndZoomSamplePosition + delta;
			} else {
				// If scrolling left (backward in time on the waveform)
				delta = MathUtils.LimitInt(delta, 0, oldStartZoomSamplePosition);
				newStartZoomSamplePosition = oldStartZoomSamplePosition - delta;
				newEndZoomSamplePosition = oldEndZoomSamplePosition - delta;
			}
			
			// If there a change in the view, then refresh the display
			if ((newStartZoomSamplePosition != oldStartZoomSamplePosition)
			    || (newEndZoomSamplePosition != oldEndZoomSamplePosition))
			{
				// Zoom
				Zoom(newStartZoomSamplePosition, newEndZoomSamplePosition);
			}
		}
		
		public void ScrollTime(bool doScrollRight) {

			if (_soundPlayer.WaveformData == null) return;

			// read in the current zoom sample positions and sample length
			int oldStartZoomSamplePosition = _startZoomSamplePosition;
			int oldEndZoomSamplePosition = _endZoomSamplePosition;
			int channelSampleLength = _soundPlayer.ChannelSampleLength;
			
			int newStartZoomSamplePosition = -1;
			int newEndZoomSamplePosition = -1;
			
			// calculate the range
			int rangeInSamples = oldEndZoomSamplePosition - oldStartZoomSamplePosition;
			
			// find delta
			int delta = rangeInSamples / 32;
			if (delta == 0) delta = 1;
			
			// If scrolling right (forward in time on the waveform)
			if (doScrollRight) {
				delta = MathUtils.LimitInt(delta, 0, channelSampleLength - oldEndZoomSamplePosition);
				newStartZoomSamplePosition = oldStartZoomSamplePosition + delta;
				newEndZoomSamplePosition = oldEndZoomSamplePosition + delta;
			} else {
				// If scrolling left (backward in time on the waveform)
				delta = MathUtils.LimitInt(delta, 0, oldStartZoomSamplePosition);
				newStartZoomSamplePosition = oldStartZoomSamplePosition - delta;
				newEndZoomSamplePosition = oldEndZoomSamplePosition - delta;
			}
			
			// If there a change in the view, then refresh the display
			if ((newStartZoomSamplePosition != oldStartZoomSamplePosition)
			    || (newEndZoomSamplePosition != oldEndZoomSamplePosition))
			{
				// Zoom
				Zoom(newStartZoomSamplePosition, newEndZoomSamplePosition);
			}
		}
		#endregion
		
		#region Loop methods
		/// <summary>
		/// Check if a given sample position is within the current set loop
		/// </summary>
		/// <param name="samplePosition">sample position to check</param>
		/// <returns>boolean that tells if the given sample position is within the current selection (loop)</returns>
		private bool PointInLoopRegion(int samplePosition) {
			if (_soundPlayer.ChannelLength == 0)
				return false;

			double loopStartSamples = (_soundPlayer.SelectionBegin.TotalSeconds / _soundPlayer.ChannelLength) * _soundPlayer.ChannelSampleLength;
			double loopEndSamples = (_soundPlayer.SelectionEnd.TotalSeconds / _soundPlayer.ChannelLength) * _soundPlayer.ChannelSampleLength;
			
			return (samplePosition >= loopStartSamples && samplePosition < loopEndSamples);
		}
		
		private void UpdateLoopRegion(bool redraw=true)
		{
			if (_startLoopXPosition > 0 && _endLoopXPosition > 0) {
				_loopRegion.Width = _endLoopXPosition - _startLoopXPosition;
				_loopRegion.Height = TOP_MARGIN + WaveformDrawingHeight;
			} else {
				_loopRegion.Width = 0;
				_loopRegion.Height = 0;
			}
			
			if (redraw) {
				this.Invalidate();
			}
		}
		
		private void ClearLoopRegion() {
			_startLoopXPosition = -1;
			_endLoopXPosition = -1;
			_loopRegion.Width = 0;
			_loopRegion.Height = 0;
			
			StartLoopSamplePosition = -1;
			EndLoopSamplePosition = -1;
			
			if (_soundPlayer != null && _soundPlayer.WaveformData != null) {
				_soundPlayer.SelectionBegin = TimeSpan.Zero;
				_soundPlayer.SelectionEnd = TimeSpan.Zero;
			}
			
			// force redraw
			this.Invalidate();
		}
		#endregion

		#region Event Handlers
		private void soundPlayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "SelectionBegin":
					StartLoopSamplePosition = SecondsToSamplePosition(_soundPlayer.SelectionBegin.TotalSeconds, _soundPlayer.ChannelLength, _soundPlayer.ChannelSampleLength);
					break;
				case "SelectionEnd":
					EndLoopSamplePosition = SecondsToSamplePosition(_soundPlayer.SelectionEnd.TotalSeconds, _soundPlayer.ChannelLength, _soundPlayer.ChannelSampleLength);
					break;
				case "WaveformData":
					FitToScreen();
					break;
				case "ChannelPosition":
					UpdateProgressIndicator();
					break;
				case "ChannelLength":
					StartLoopSamplePosition = -1;
					EndLoopSamplePosition = -1;
					break;
			}
		}
		#endregion

		#region INotifyPropertyChanged implementation
		
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
		
		#region Audio Processing Methods
		
		// taken from WaveEdit.cs WavePad Source file
		public bool FindZeroCrossing(float[] audioData, int chans, bool Reverse, out int framePosition)
		{
			int frames = audioData.Length;
			int start = 0;
			if (Reverse) // if searching backwards
			{
				start = frames - 1;
				for (int iFrame = start - 1; iFrame >= 0; iFrame--) // for each frame
				{
					for (int iChan = 0; iChan < chans; iChan++) // for each channel
					{
						if (audioData[iChan*iFrame] < 0 && audioData[iChan*iFrame + 1] >= 0) // if span crosses zero
						{
							framePosition = iFrame;
							return true;
						}
					}
				}
			} // searching forward
			else
			{
				start = 0;
				for (int iFrame = start; iFrame < frames - 1; iFrame++) // for each frame
				{
					for (int iChan = 0; iChan < chans; iChan++) // for each channel
					{
						if (audioData[iChan*iFrame] < 0 && audioData[iChan*iFrame + 1] >= 0) // if span crosses zero
						{
							framePosition = iFrame;
							return true;
						}
					}
				}
			}
			framePosition = -1;
			return false;
		}
		#endregion
	}
}