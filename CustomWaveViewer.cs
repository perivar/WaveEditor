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
		MouseButtons _lastButtonUp = MouseButtons.None;
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

		// Snap
		bool _snapToZeroCrossing = false;
		
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

		public bool SnapToZeroCrossing {
			get {
				return _snapToZeroCrossing;
			}
			set {
				bool oldValue = _snapToZeroCrossing;
				_snapToZeroCrossing = value;
				if (oldValue != _snapToZeroCrossing)
					NotifyPropertyChanged("SnapToZeroCrossing");
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
						string currentText = String.Format("[X:{0},Y:{1}]  Sample Index: {2}, Sample Time: {3:hh\\:mm\\:ss\\.fff}, Total time: {4:hh\\:mm\\:ss\\.fff}", _currentPoint.X, _currentPoint.Y, _currentPointSamplePos, TimeSpan.FromSeconds(_currentPointTimePos), TimeSpan.FromSeconds(_soundPlayer.ChannelLength));
						SizeF currentTextSize = gNewBitmap.MeasureString(currentText, currentFont);
						gNewBitmap.DrawString(currentText, currentFont, Brushes.Black, SIDE_MARGIN + _waveformDrawingWidth - currentTextSize.Width, TOP_MARGIN + _waveformDrawingHeight + 1);
					}
				}
				
				// draw the temporary bitmap onto the main canvas
				e.Graphics.DrawImageUnscaled(tempImage, 0, 0);
				
				// setup the region we want to invert (select region)
				var destination = new Rectangle(_startLoopXPosition, TOP_MARGIN - 1, _loopRegion.Width, _loopRegion.Height);
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
			if (_soundPlayer.SelectionSampleBegin == 0 &&
			    _soundPlayer.SelectionSampleEnd == _soundPlayer.ChannelSampleLength - 1) {

				// fit to screen also clears loop regions
				FitToScreen();
			} else {
				_startLoopXPosition = SIDE_MARGIN;
				_endLoopXPosition = SIDE_MARGIN + _waveformDrawingWidth;
				
				_soundPlayer.SelectionSampleBegin = _startZoomSamplePosition;
				_soundPlayer.SelectionSampleEnd = _endZoomSamplePosition;
				_soundPlayer.ChannelSamplePosition = _startZoomSamplePosition;
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
				int rangeInSamples = Math.Abs(endZoomSamplePos - startZoomSamplePos) + 1;

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
				// make sure the zoom start and zoom end is correct
				if (startZoomSamplePos < 0) {
					startZoomSamplePos = 0;
				}
				// Ensure that endZoomSamplePosition is 0-index based, e.g. the last index is max length - 1
				if (endZoomSamplePos >= _soundPlayer.ChannelSampleLength || endZoomSamplePos < 0) {
					endZoomSamplePos = _soundPlayer.ChannelSampleLength - 1;
				}
				
				// Check that we are not zooming too much
				int rangeInSamples = Math.Abs(endZoomSamplePos - startZoomSamplePos) + 1;
				if (rangeInSamples <= 10) {
					return;
				}
				
				StartZoomSamplePosition = startZoomSamplePos;
				EndZoomSamplePosition = endZoomSamplePos;
				PreviousStartZoomSamplePosition = startZoomSamplePos;
				// add 1 since the zoom sample positions are 0-index based
				// don't add 1 since we want x number of samples to cover the whole screen
				SamplesPerPixel = (double) (endZoomSamplePos - startZoomSamplePos) / (double) _waveformDrawingWidth;

				#region Update Loop Region After Zooming
				
				// TODO: only calculate loop start and end x position when painting
				
				if (_startLoopSamplePosition < _startZoomSamplePosition
				    && _endLoopSamplePosition < _startZoomSamplePosition) {
					
					// both start and stop loop position are before the zoom window
					
					_startLoopXPosition = -1;
					_endLoopXPosition = -1;
					
					
				} else if (_startLoopSamplePosition > _endZoomSamplePosition
				           && _endLoopSamplePosition > _endZoomSamplePosition) {
					// both start and stop loop positions are after the zoom window

					_startLoopXPosition = -1;
					_endLoopXPosition = -1;
					
				} else if (_startLoopSamplePosition < _startZoomSamplePosition
				           && IsBetween(_endLoopSamplePosition, _startZoomSamplePosition, _endZoomSamplePosition)) {
					
					// start loop position is before zoom window and end position is within the zoom window
					_startLoopXPosition = SIDE_MARGIN;

					int endRangeInSamples = Math.Abs(_endLoopSamplePosition - _startZoomSamplePosition);
					_endLoopXPosition = SamplePositionToXPosition(endRangeInSamples, _samplesPerPixel);
					
					
				} else if (IsBetween(_startLoopSamplePosition, _startZoomSamplePosition, _endZoomSamplePosition)
				           && _endLoopSamplePosition > _endZoomSamplePosition) {

					// start loop position is within the zoom window and the end position is after
					
					_endLoopXPosition = SIDE_MARGIN + _waveformDrawingWidth;

					int startRangeInSamples = Math.Abs(_startZoomSamplePosition - _startLoopSamplePosition);
					_startLoopXPosition = SamplePositionToXPosition(startRangeInSamples, _samplesPerPixel);
					
					
				} else if (IsBetween(_startLoopSamplePosition, _startZoomSamplePosition, _endZoomSamplePosition)
				           && IsBetween(_endLoopSamplePosition, _startZoomSamplePosition, _endZoomSamplePosition)) {
					
					// both loop positions are within the zoom window
					int startRangeInSamples = Math.Abs(_startLoopSamplePosition - _startZoomSamplePosition);
					_startLoopXPosition = SamplePositionToXPosition(startRangeInSamples, _samplesPerPixel);
					
					int loopRangeInSamples = Math.Abs(_endLoopSamplePosition - _startLoopSamplePosition);
					int len = SamplePositionToXPosition(loopRangeInSamples, _samplesPerPixel, false);
					
					_endLoopXPosition = _startLoopXPosition + len;
				} else {
					// both loop positions are oustide the zoom window
					_startLoopXPosition = SIDE_MARGIN;
					_endLoopXPosition = SIDE_MARGIN + _waveformDrawingWidth;
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
				StartZoomSamplePosition = 0;
				EndZoomSamplePosition = numberOfChannelSamples - 1;
				PreviousStartZoomSamplePosition = 0;
				SamplesPerPixel = (double) numberOfChannelSamples / (double)  _waveformDrawingWidth;
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
				int rangeInSamples = Math.Abs(EndLoopSamplePosition - StartLoopSamplePosition) + 1;
				int rangeInPixles = Math.Abs(_endLoopXPosition - _startLoopXPosition);
				
				rangeInSamples *= 2;
				rangeInPixles *= 2;
				
				_endLoopXPosition = Math.Min(this.WaveformDrawingWidth + SIDE_MARGIN, _startLoopXPosition + rangeInPixles);
				EndLoopSamplePosition = Math.Min(_soundPlayer.ChannelSampleLength, _startLoopSamplePosition + rangeInSamples);
				
				if (_soundPlayer != null && _soundPlayer.WaveformData != null) {
					_soundPlayer.SelectionSampleEnd = EndLoopSamplePosition;
				}
				
				UpdateLoopRegion();
			}
		}
		
		public void DecreaseSelection() {
			// keep start selection pos, but half the length
			if (StartLoopSamplePosition >= 0 && EndLoopSamplePosition > 0) {
				int rangeInSamples = Math.Abs(EndLoopSamplePosition - StartLoopSamplePosition) + 1;
				int rangeInPixles = Math.Abs(_endLoopXPosition - _startLoopXPosition);
				
				rangeInSamples /= 2;
				rangeInPixles /= 2;
				
				_endLoopXPosition = Math.Min(this.WaveformDrawingWidth + SIDE_MARGIN, _startLoopXPosition + rangeInPixles);
				EndLoopSamplePosition = Math.Min(_soundPlayer.ChannelSampleLength, _startLoopSamplePosition + rangeInSamples);
				
				if (_soundPlayer != null && _soundPlayer.WaveformData != null) {
					_soundPlayer.SelectionSampleEnd = EndLoopSamplePosition;
				}
				
				UpdateLoopRegion();
			}
		}
		
		#endregion
		
		#region Private Methods
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
				_progressSample = _soundPlayer.ChannelSamplePosition;
				
				int rangeInSamples = Math.Abs(_endZoomSamplePosition - _startZoomSamplePosition) + 1;
				
				// TODO: Fix proper scrolling, this works but doesn't make sense when playing and being zoomed
				/*
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
				 */
				// force redraw
				this.Invalidate();
			}
		}

		/// <summary>
		/// Store the information underneath the current mouse position
		/// I.e. Sample Position and Time Position
		/// </summary>
		private void StoreCurrentPointInfo() {
			_currentPointSamplePos = _startZoomSamplePosition + XPositionToSamplePosition(_currentPoint.X, _samplesPerPixel);
			_currentPointTimePos = (double) _currentPointSamplePos / (double) _soundPlayer.SampleRate;
		}
		
		private void StoreZeroCrossingValues(int xPosition1, int xPosition2) {
			int newSamplePos1 = -1;
			int newSamplePos2 = -1;
			int newXpos1 = -1;
			int newXpos2 = -1;
			
			// range to search within is from start zoom until samplePos (searching reverse)
			if (xPosition1 >= 0) {
				ClosestZeroCrossingPosition(true, _soundPlayer, xPosition1, _startZoomSamplePosition, _endZoomSamplePosition, _samplesPerPixel, out newSamplePos1, out newXpos1);
			}
			
			// range to search within is from samplePos until end zoom
			if (xPosition2 >= 0) {
				ClosestZeroCrossingPosition(false, _soundPlayer, xPosition2, _startZoomSamplePosition, _endZoomSamplePosition, _samplesPerPixel, out newSamplePos2, out newXpos2);
			}
			
			if (xPosition1 == -1) {
				// do not update start loop
				_endLoopXPosition = newXpos2;
				EndLoopSamplePosition = newSamplePos2;
			} else if (xPosition2 == -1) {
				// do not update end loop
				if (newXpos1 != -1) {
					_startLoopXPosition = newXpos1;
					StartLoopSamplePosition = newSamplePos1;
				} else {
					// if finding start failed, set to zero
					_startLoopXPosition = 0;
					StartLoopSamplePosition = 0;
				}
			} else if (newXpos1 == -1) {
				// did not find a zero crossing
				_startLoopXPosition = SIDE_MARGIN;
				StartLoopSamplePosition = 0;
			} else if (newXpos2 == -1) {
				// did not find a zero crossing
				
			} else if (newXpos1 < newXpos2) {
				_startLoopXPosition = newXpos1;
				_endLoopXPosition = newXpos2;
				
				StartLoopSamplePosition = newSamplePos1;
				EndLoopSamplePosition = newSamplePos2;
			} else {
				_startLoopXPosition = newXpos2;
				_endLoopXPosition = newXpos1;

				StartLoopSamplePosition = newSamplePos2;
				EndLoopSamplePosition = newSamplePos1;
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
			int rangeInSamples = oldEndZoomSamplePosition - oldStartZoomSamplePosition + 1;
			
			// Scroll the display left/right
			if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
				delta = rangeInSamples / 20;
				
				// If scrolling right (forward in time on the waveform)
				if (e.Delta > 0) {
					delta = MathUtils.LimitInt(delta, 0, channelSampleLength - oldEndZoomSamplePosition - 1);
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
				if (newEndZoomSamplePosition >= channelSampleLength) {
					// subtract 1 since the zoom sample positions are 0-index based
					newEndZoomSamplePosition = channelSampleLength - 1;
				}
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
			// store current point
			_currentPoint = e.Location;
			
			int currentXPosition = _currentPoint.X;
			int mouseDownXPosition = _mouseDownPoint.X;
			int startLoopXPos = _startLoopXPosition;
			int endLoopXPos = _endLoopXPosition;
			int prevStartLoopXPos = _previousStartLoopXPosition;
			int prevEndLoopXPos = _previousEndLoopXPosition;
			
			if (_soundPlayer == null || _soundPlayer.WaveformData == null) return;
			
			if (_isMouseDown) {
				
				if (IsBetween(mouseDownXPosition, prevStartLoopXPos - MOUSE_MOVE_TOLERANCE, prevStartLoopXPos + MOUSE_MOVE_TOLERANCE)) {
					// we are dragging left anchor
					
					if (SnapToZeroCrossing) {
						StoreZeroCrossingValues(currentXPosition, -1);
					} else {
						// test if current left x is bigger than right
						if (currentXPosition > _endLoopXPosition) {
							_startLoopXPosition = ClosestSampleAccurateXPosition(_previousEndLoopXPosition, _samplesPerPixel);
							_endLoopXPosition = ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel);
						} else {
							_startLoopXPosition = ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel);
							_endLoopXPosition = ClosestSampleAccurateXPosition(_previousEndLoopXPosition, _samplesPerPixel);
						}
					}
					
				} else if (IsBetween(mouseDownXPosition, prevEndLoopXPos - MOUSE_MOVE_TOLERANCE, prevEndLoopXPos + MOUSE_MOVE_TOLERANCE)) {
					// we are dragging right anchor
					
					if (SnapToZeroCrossing) {
						StoreZeroCrossingValues(-1, currentXPosition);
					} else {
						// test if current right x is less than left
						if (currentXPosition < _startLoopXPosition) {
							_startLoopXPosition = ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel);
							_endLoopXPosition = ClosestSampleAccurateXPosition(_previousStartLoopXPosition, _samplesPerPixel);
						} else {
							_startLoopXPosition = ClosestSampleAccurateXPosition(_previousStartLoopXPosition, _samplesPerPixel);
							_endLoopXPosition = ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel);
						}
					}
				} else {
					// we have selected a new loop range
					
					if (SnapToZeroCrossing) {
						StoreZeroCrossingValues(mouseDownXPosition, currentXPosition);
					} else {
						_startLoopXPosition = Math.Min(ClosestSampleAccurateXPosition(mouseDownXPosition, _samplesPerPixel), ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel));
						_endLoopXPosition = Math.Max(ClosestSampleAccurateXPosition(mouseDownXPosition, _samplesPerPixel), ClosestSampleAccurateXPosition(currentXPosition, _samplesPerPixel));
					}
				}
				
				// limit start and end to waveform drawing space
				if (_startLoopXPosition < SIDE_MARGIN) _startLoopXPosition = SIDE_MARGIN;
				if (_endLoopXPosition > SIDE_MARGIN + _waveformDrawingWidth) _endLoopXPosition = SIDE_MARGIN + _waveformDrawingWidth;
				
				// store information about current point
				StoreCurrentPointInfo();
				
				// update loop region and redraw
				UpdateLoopRegion();
			} else {
				
				// store information about current point
				StoreCurrentPointInfo();
				
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
			
			switch (_lastButtonUp)
			{
				case System.Windows.Forms.MouseButtons.Left:
					SelectAll();
					
					break;
				case System.Windows.Forms.MouseButtons.Right:

					break;
				case System.Windows.Forms.MouseButtons.Middle:
					
					break;
			}
		}
		
		void CustomWaveViewerMouseUp(object sender, MouseEventArgs e)
		{
			// store last mouse button clicked for checking double clik button in mousedoubleclick event
			_lastButtonUp = e.Button;
			
			if (!_isMouseDown || _soundPlayer == null || _soundPlayer.WaveformData == null)
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
					_soundPlayer.ChannelSamplePosition = curSamplePosition;
				} else {
					_soundPlayer.SelectionSampleBegin = 0;
					_soundPlayer.SelectionSampleEnd = 0;
					_soundPlayer.ChannelSamplePosition = curSamplePosition;
					
					StartLoopSamplePosition = -1;
					EndLoopSamplePosition = -1;
					
					doUpdateLoopRegion = true;
				}
				
			} else {
				
				if (SnapToZeroCrossing) {
					// if we are in snapping mode, we have already stored the start and end loop positions
					// in the mouse move method
					
				} else {
					
					// Update Selection / Loop
					int oldStartLoopSamplePosition = StartLoopSamplePosition;
					int oldEndLoopSamplePosition = EndLoopSamplePosition;

					if (oldStartLoopSamplePosition == -1) oldStartLoopSamplePosition = 0;
					if (oldEndLoopSamplePosition == -1) oldEndLoopSamplePosition = 0;
					
					int newStartLoopSamplePosition = Math.Max(_previousStartZoomSamplePosition + XPositionToSamplePosition(_startLoopXPosition, _samplesPerPixel), 0);
					int newEndLoopSamplePosition = Math.Min(_previousStartZoomSamplePosition + XPositionToSamplePosition(_endLoopXPosition, _samplesPerPixel), _soundPlayer.ChannelSampleLength - 1);
					
					// if start and stop is within zoom window, update both zoom points
					if (newStartLoopSamplePosition > StartZoomSamplePosition &&
					    newEndLoopSamplePosition < EndZoomSamplePosition) {
						
						// Update both if inside zoom window
						StartLoopSamplePosition = newStartLoopSamplePosition;
						EndLoopSamplePosition = newEndLoopSamplePosition;

					} else if (StartZoomSamplePosition > oldStartLoopSamplePosition) {
						// Don't update Start if it's outside of the zoom window
						EndLoopSamplePosition = newEndLoopSamplePosition;
					} else if (EndZoomSamplePosition < oldEndLoopSamplePosition) {
						// Don't update End if it's outside of the zoom window
						StartLoopSamplePosition = newStartLoopSamplePosition;
					} else {
						// Update both if inside zoom window
						StartLoopSamplePosition = newStartLoopSamplePosition;
						EndLoopSamplePosition = newEndLoopSamplePosition;
					}
				}
				
				// Update the soundplayer
				_soundPlayer.SelectionSampleBegin = _startLoopSamplePosition;
				_soundPlayer.SelectionSampleEnd = _endLoopSamplePosition;
				_soundPlayer.ChannelSamplePosition = _startLoopSamplePosition;

				_previousStartLoopXPosition = _startLoopXPosition;
				_previousEndLoopXPosition = _endLoopXPosition;
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
		{ Keys.Left, Keys.Up, Keys.Right, Keys.Down, Keys.Oemcomma, Keys.Home, Keys.OemPeriod, Keys.End, Keys.Decimal };

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
			} else if (e.KeyCode == Keys.Oemcomma || e.KeyCode == Keys.Home || e.KeyCode == Keys.Decimal) {
				_soundPlayer.ChannelSamplePosition = 0;
				
				// keep zoom level and set position to 0
				// last index is different from the range, so do not add 1 to range in samples
				int indexPosition = Math.Abs(_endZoomSamplePosition - _startZoomSamplePosition);
				Zoom(0, indexPosition);
				
			} else if (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.End) {
				_soundPlayer.ChannelSamplePosition = _soundPlayer.ChannelSampleLength - 1;

				// keep zoom level and set position to last range
				int rangeInSamples = Math.Abs(_endZoomSamplePosition - _startZoomSamplePosition) + 1;
				Zoom(_soundPlayer.ChannelSampleLength - rangeInSamples, _soundPlayer.ChannelSampleLength - 1);
			}
		}
		#endregion
		
		#region Private Static Util Methods
		
		/// <summary>
		/// Determine if the mouse location is at one of the loop ends (boundries)
		/// </summary>
		/// <param name="mouseXLocation">mouse x position</param>
		/// <param name="startLoopXPos">start loop X position</param>
		/// <param name="endLoopXPos">end loop X position</param>
		/// <returns></returns>
		private static bool IsLoopBoundry(int mouseXLocation, int startLoopXPos, int endLoopXPos) {
			
			if (IsBetween(mouseXLocation, startLoopXPos - MOUSE_MOVE_TOLERANCE, startLoopXPos + MOUSE_MOVE_TOLERANCE)
			    || IsBetween(mouseXLocation, endLoopXPos - MOUSE_MOVE_TOLERANCE, endLoopXPos + MOUSE_MOVE_TOLERANCE)) {
				return true;
			} else {
				return false;
			}
		}
		
		/// <summary>
		/// Determine if value is between or equal to left and right values
		/// </summary>
		/// <param name="value">value to check</param>
		/// <param name="left">leftmost value</param>
		/// <param name="right">rightmost value</param>
		/// <returns>true if between</returns>
		private static bool IsBetween(int value, int left, int right)
		{
			return value >= left && value <= right;
		}
		
		/// <summary>
		/// Return the x position which is closest to an actual sample
		/// </summary>
		/// <param name="originalXPosition">original x position to correct</param>
		/// <param name="samplesPerPixel">samples per pixel</param>
		/// <returns>a sample accurate x position</returns>
		private static int ClosestSampleAccurateXPosition(int originalXPosition, double samplesPerPixel) {
			return SamplePositionToXPosition(XPositionToSamplePosition(originalXPosition, samplesPerPixel), samplesPerPixel);
		}
		
		/// <summary>
		/// Return the sample and x position which is closest to a zero-crossing sample
		/// </summary>
		/// <param name="isStartLoop">whether this is the start or end loop segment</param>
		/// <param name="soundPlayer">the soundplayer</param>
		/// <param name="originalXPosition">original x position to correct</param>
		/// <param name="startZoomSamplePosition">start zoom sample position (0-index based)</param>
		/// <param name="endZoomSamplePosition">end zoom sample position (0-index based)</param>
		/// <param name="samplesPerPixel">samples per pixel</param>
		/// <param name="newSamplePosition">out new sample position</param>
		/// <param name="newXposition">out new x position</param>
		/// <returns></returns>
		private static bool ClosestZeroCrossingPosition(bool isStartLoop, IWaveformPlayer soundPlayer, int originalXPosition, int startZoomSamplePosition, int endZoomSamplePosition, double samplesPerPixel,
		                                                out int newSamplePosition, out int newXposition) {

			// variables
			int framePosition = -1;
			
			// make sure the zoom start and zoom end is correct
			if (startZoomSamplePosition < 0) {
				startZoomSamplePosition = 0;
			}
			// Ensure that endZoomSamplePosition is 0-index based, e.g. the last index is max length - 1
			if (endZoomSamplePosition >= soundPlayer.ChannelSampleLength || endZoomSamplePosition <= 0) {
				endZoomSamplePosition = soundPlayer.ChannelSampleLength - 1;
			}

			// original current sample position
			int originalSamplePosition = startZoomSamplePosition + XPositionToSamplePosition(originalXPosition, samplesPerPixel);
			
			if (isStartLoop) {
				// range to search within is from start zoom until samplePos
				var data = GetAudioSegment(soundPlayer.WaveformData, soundPlayer.Channels, startZoomSamplePosition, originalSamplePosition);
				
				// start loop - search backwards from the click point
				if (FindZeroCrossing(data, soundPlayer.Channels, true, out framePosition)) {
					newSamplePosition = startZoomSamplePosition + (framePosition / soundPlayer.Channels);
					newXposition = SamplePositionToXPosition((framePosition / soundPlayer.Channels), samplesPerPixel);
				} else {
					newSamplePosition = -1;
					newXposition = -1;
					return false;
				}
			} else {
				// range to search within is from samplePos until end zoom
				var data = GetAudioSegment(soundPlayer.WaveformData, soundPlayer.Channels, originalSamplePosition, endZoomSamplePosition);
				
				// end loop - search forward from the click point
				if (FindZeroCrossing(data, soundPlayer.Channels, false, out framePosition)) {
					newSamplePosition = originalSamplePosition + (framePosition / soundPlayer.Channels);
					newXposition = SamplePositionToXPosition(originalSamplePosition + (framePosition / soundPlayer.Channels) - startZoomSamplePosition, samplesPerPixel);
				} else {
					newSamplePosition = originalSamplePosition;
					newXposition = SamplePositionToXPosition(originalSamplePosition - startZoomSamplePosition, samplesPerPixel);
				}
			}
			return true;
		}

		private static float[] GetAudioSegment(float[] audio, int channels, int start, int end) {

			if (start > end) return null;
			
			// ensure the start zoom index takes the channels into account
			int startIndex = start * channels;

			// add 1 since the zoom sample positions are 0-index based
			int rangeLength = (end - start + 1) * channels;
			
			// limit the range
			if ((startIndex + rangeLength) > audio.Length) {
				rangeLength = audio.Length - startIndex;
			}
			
			var segment = new float[rangeLength];
			Array.Copy(audio, startIndex, segment, 0, rangeLength);
			
			return segment;
		}
		
		/// <summary>
		/// Convert a sample position to a x position
		/// </summary>
		/// <param name="samplePosition">sample position</param>
		/// <param name="samplesPerPixel">samples per pixel</param>
		/// <param name="useMargin">whether to use a margin</param>
		/// <returns>x position</returns>
		private static int SamplePositionToXPosition(int samplePosition, double samplesPerPixel, bool useMargin=true) {
			double pixelPosition = (double) samplePosition / samplesPerPixel;
			int pixelPos = (int) Math.Round(pixelPosition, MidpointRounding.AwayFromZero);
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
				samplePos = samplesPerPixel * (double) xPosition;
			}
			// sample pos cannot be negative
			if (samplePos < 0) samplePos = 0;
			return (int) Math.Round(samplePos, MidpointRounding.AwayFromZero);
		}

		/// <summary>
		/// Convert a sample position to a time in seconds
		/// </summary>
		/// <param name="samplePosition">sample position</param>
		/// <param name="sampleRate">sample rate</param>
		/// <returns>Time in seconds</returns>
		public static double SamplePositionToSeconds(int samplePosition, int sampleRate) {
			return (double) samplePosition / (double) sampleRate;
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

				if (oldEndZoomSamplePosition == channelSampleLength - 1) {
					// we are already at the very end
					return;
				}
				
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
			int rangeInSamples = oldEndZoomSamplePosition - oldStartZoomSamplePosition + 1;
			
			// find delta
			int delta = rangeInSamples / 32;
			if (delta == 0) delta = 1;
			
			// If scrolling right (forward in time on the waveform)
			if (doScrollRight) {

				if (oldEndZoomSamplePosition == channelSampleLength - 1) {
					// we are already at the very end
					return;
				}

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
			if (_soundPlayer.ChannelSampleLength == 0)
				return false;

			double loopStartSamples = _soundPlayer.SelectionSampleBegin;
			double loopEndSamples = _soundPlayer.SelectionSampleEnd;
			
			return (samplePosition >= loopStartSamples && samplePosition < loopEndSamples);
		}
		
		private void UpdateLoopRegion(bool redraw=true)
		{
			if (_startLoopXPosition > 0 && _endLoopXPosition > 0) {
				_loopRegion.Width = _endLoopXPosition - _startLoopXPosition;
				_loopRegion.Height = _waveformDrawingHeight;
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
				_soundPlayer.SelectionSampleBegin = 0;
				_soundPlayer.SelectionSampleEnd = 0;
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
				case "SelectionSampleBegin":
					StartLoopSamplePosition = _soundPlayer.SelectionSampleBegin;
					break;
				case "SelectionSampleEnd":
					EndLoopSamplePosition = _soundPlayer.SelectionSampleEnd;
					break;
				case "WaveformData":
					FitToScreen();
					break;
				case "ChannelSamplePosition":
					UpdateProgressIndicator();
					break;
				case "ChannelSampleLength":
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
		
		/// <summary>
		/// Find Zero Crossing Points
		/// </summary>
		/// <param name="audioData">audio data</param>
		/// <param name="channels">channels</param>
		/// <param name="doReverse">whether to search backwards</param>
		/// <param name="framePosition">out frame position</param>
		/// <returns>whether zero crossing was found</returns>
		/// <remarks>
		/// originally copied from from WaveEdit.cs WavePad Source file
		/// but modified to work with zero crossing both directions
		/// and both mono and stereo files
		/// </remarks>
		public static bool FindZeroCrossing(float[] audioData, int channels, bool doReverse, out int framePosition)
		{
			if (audioData == null) {
				framePosition = -1;
				return false;
			}
			
			int frames = (int) ((double) audioData.Length / (double) channels);
			int start = 0;
			if (doReverse)
			{
				// if searching backwards
				start = frames - 1;
				for (int i = start - 1; i >= 0; i--) // for each frame
				{
					for (int channelCounter = 0; channelCounter < channels; channelCounter++) // for each channel
					{
						// if span crosses zero
						if ((audioData[channels*i+channelCounter] > 0 && audioData[channels*i+channelCounter + 1] <= 0)
						    || (audioData[channels*i+channelCounter] < 0 && audioData[channels*i+channelCounter + 1] >= 0))
						{
							framePosition = channels*i+channelCounter;
							return true;
						}
					}
				}
			}
			else
			{
				// searching forward
				start = 0;
				for (int i = start; i < frames - 1; i++) // for each frame
				{
					for (int channelCounter = 0; channelCounter < channels; channelCounter++) // for each channel
					{
						// if span crosses zero
						if ((audioData[channels*i+channelCounter] > 0 && audioData[channels*i+channelCounter + 1] <= 0)
						    || (audioData[channels*i+channelCounter] < 0 && audioData[channels*i+channelCounter + 1] >= 0))
						{
							framePosition = channels*i+channelCounter;
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