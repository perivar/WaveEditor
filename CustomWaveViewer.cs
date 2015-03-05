using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using CommonUtils.FFT; // Audio Analyzer
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
		
		float _samplesPerPixel = 128;
		
		// Mouse variables
		const int MOUSE_MOVE_TOLERANCE = 3;
		bool _isMouseDown = false;
		bool _isZooming = false;
		Point _mouseDownPoint;
		Point _currentPoint;

		// setup image attributes for color negation
		ImageAttributes _imageAttributes = new ImageAttributes();
		#endregion

		#region Properties
		public float SamplesPerPixel {
			get {
				return _samplesPerPixel;
			}
			set {
				float oldValue = _samplesPerPixel;
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
						double xLocation = SIDE_MARGIN + ((_progressSample - _startZoomSamplePosition) / _samplesPerPixel) - 1;
						gNewBitmap.DrawLine(markerPen, (float) xLocation, TOP_MARGIN - 1, (float) xLocation, _waveformDrawingHeight + TOP_MARGIN - 1);
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
				float samplesPerPixel = _samplesPerPixel;
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

				// TODO: update loop region after zooming instead of clearing it
				ClearLoopRegion();
				
				// and update the waveform
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
				this._offlineBitmap = AudioAnalyzer.DrawWaveform(_soundPlayer.WaveformData, new Size(this.Width, this.Height), _amplitude, _startZoomSamplePosition, _endZoomSamplePosition, -1, -1, -1, _soundPlayer.SampleRate, _soundPlayer.Channels);

				// force redraw
				this.Invalidate();
			}
		}

		private void UpdateProgressIndicator()
		{
			if (_soundPlayer != null && _soundPlayer.ChannelSampleLength != 0)
			{
				_progressSample = SecondsToSamplePosition(_soundPlayer.ChannelPosition, _soundPlayer.ChannelLength, _soundPlayer.ChannelSampleLength);
				
				if (_progressSample < _startZoomSamplePosition) {
					int rangeInSamples = Math.Abs(_endZoomSamplePosition - _startZoomSamplePosition);
					Zoom(0, _progressSample + rangeInSamples);
				} else if (_progressSample > _endZoomSamplePosition) {
					int rangeInSamples = Math.Abs(_endZoomSamplePosition - _startZoomSamplePosition);
					Zoom(_progressSample - rangeInSamples/2, _progressSample + rangeInSamples/2);
				} else {
					// force redraw
					this.Invalidate();
				}
				
				//int rangeInSamples = Math.Abs(_endZoomSamplePosition - _startZoomSamplePosition);
				//Zoom(_progressSample - rangeInSamples/2, _progressSample + rangeInSamples/2);
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
				if (Between(mouseDownXPosition, prevStartXPos - MOUSE_MOVE_TOLERANCE, prevStartXPos + MOUSE_MOVE_TOLERANCE)) {
					// dragging left anchor
					// test if current left x is bigger than right
					if (currentXPosition > _endLoopXPosition) {
						_startLoopXPosition = _previousEndLoopXPosition;
						_endLoopXPosition = currentXPosition;
					} else {
						_startLoopXPosition = currentXPosition;
						_endLoopXPosition = _previousEndLoopXPosition;
					}
				} else if (Between(mouseDownXPosition, prevEndXPos - MOUSE_MOVE_TOLERANCE, prevEndXPos + MOUSE_MOVE_TOLERANCE)) {
					// dragging right anchor
					// test if current right x is less than left
					if (currentXPosition < _startLoopXPosition) {
						_startLoopXPosition = currentXPosition;
						_endLoopXPosition = _previousStartLoopXPosition;
					} else {
						_startLoopXPosition = _previousStartLoopXPosition;
						_endLoopXPosition = currentXPosition;
					}
				} else {
					_startLoopXPosition = Math.Min(mouseDownXPosition, currentXPosition);
					_endLoopXPosition = Math.Max(mouseDownXPosition, currentXPosition);
				}
				
				// limit start and end to waveform drawing space
				if (_startLoopXPosition < SIDE_MARGIN) _startLoopXPosition = SIDE_MARGIN;
				if (_endLoopXPosition > SIDE_MARGIN + _waveformDrawingWidth) _endLoopXPosition = SIDE_MARGIN + _waveformDrawingWidth;
				
				UpdateLoopRegion();
			} else {
				// show sample pos and other info
				int currentPointSamplePos = (int)(_startZoomSamplePosition + _samplesPerPixel * (_currentPoint.X - SIDE_MARGIN));
				double currentPointTimePos = SamplePositionToSeconds(currentPointSamplePos, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength);
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
		
		static bool Between(int value, int left, int right)
		{
			return value > left && value < right;
		}
		
		static bool IsLoopBoundry(int mouseXLocation, int startLoopXPos, int endLoopXPos) {
			
			if (Between(mouseXLocation, startLoopXPos - MOUSE_MOVE_TOLERANCE, startLoopXPos + MOUSE_MOVE_TOLERANCE)
			    || Between(mouseXLocation, endLoopXPos - MOUSE_MOVE_TOLERANCE, endLoopXPos + MOUSE_MOVE_TOLERANCE)) {
				return true;
			} else {
				return false;
			}
		}
		
		void CustomWaveViewerMouseUp(object sender, MouseEventArgs e)
		{
			if (!_isMouseDown || _soundPlayer.WaveformData == null)
				return;
			
			_isMouseDown = false;

			if (_isZooming) {
				int startZoomSamplePos = Math.Max((int)(_previousStartZoomSamplePosition + _samplesPerPixel * (_startLoopXPosition - SIDE_MARGIN)), 0);
				int endZoomSamplePos = Math.Min((int)(_previousStartZoomSamplePosition + _samplesPerPixel * (_endLoopXPosition - SIDE_MARGIN)), _soundPlayer.ChannelSampleLength);
				
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
				int curSamplePosition = (int)(_previousStartZoomSamplePosition + _samplesPerPixel * (_mouseDownPoint.X - SIDE_MARGIN));

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

				StartLoopSamplePosition = Math.Max((int)(_previousStartZoomSamplePosition + _samplesPerPixel * (_startLoopXPosition - SIDE_MARGIN)), 0);
				EndLoopSamplePosition = Math.Min((int)(_previousStartZoomSamplePosition + _samplesPerPixel * (_endLoopXPosition - SIDE_MARGIN)), _soundPlayer.ChannelSampleLength);

				_soundPlayer.SelectionBegin = TimeSpan.FromSeconds(SamplePositionToSeconds(_startLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength));
				_soundPlayer.SelectionEnd = TimeSpan.FromSeconds(SamplePositionToSeconds(_endLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength));
				_soundPlayer.ChannelPosition = SamplePositionToSeconds(_startLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength);
			}
			
			if (doUpdateLoopRegion) {
				_startLoopXPosition = 0;
				_endLoopXPosition = 0;
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
		/// Convert a sample position to a time in seconds
		/// </summary>
		/// <param name="samplePosition">sample position</param>
		/// <param name="totalSamples">total number of samples</param>
		/// <param name="totalDurationSeconds">total duration in seconds</param>
		/// <returns>Time in seconds</returns>
		private static double SamplePositionToSeconds(int samplePosition, int totalSamples, double totalDurationSeconds) {
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
		private static int SecondsToSamplePosition(double channelPositionSeconds, double totalDurationSeconds, int totalSamples) {
			double progressPercent = channelPositionSeconds / totalDurationSeconds;
			int position = (int) (totalSamples * progressPercent);
			return Math.Min(totalSamples, Math.Max(0, position));
		}
		#endregion
		
		#region Scroll methods
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
		
		private void ScrollTime(bool doScrollRight) {

			if (_soundPlayer.WaveformData == null) return;

			// read in the current zoom sample positions and sample length
			int oldStartZoomSamplePosition = _startZoomSamplePosition;
			int oldEndZoomSamplePosition = _endZoomSamplePosition;
			int channelSampleLength = _soundPlayer.ChannelSampleLength;
			
			int newStartZoomSamplePosition = -1;
			int newEndZoomSamplePosition = -1;
			
			// calculate the range
			int rangeInSamples = oldEndZoomSamplePosition - oldStartZoomSamplePosition;
			int delta = rangeInSamples / 32;
			
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
		
		private void UpdateLoopRegion()
		{
			_loopRegion.Width = _endLoopXPosition - _startLoopXPosition;
			_loopRegion.Height = this.Height;
			
			// force redraw
			this.Invalidate();
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