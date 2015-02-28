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
		Rectangle _selectRegion = new Rectangle();
		int _startSelectXPosition = -1;
		int _endSelectXPosition = -1;
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
				_samplesPerPixel = value;
				NotifyPropertyChanged("SamplesPerPixel");
				NotifyPropertyChanged("ZoomRatioString");
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
		
		public int WaveformDrawingWidth {
			get {
				return _waveformDrawingWidth;
			}
			set {
				_waveformDrawingWidth = value;
			}
		}

		public int WaveformDrawingHeight {
			get {
				return _waveformDrawingHeight;
			}
			set {
				_waveformDrawingHeight = value;
			}
		}

		public int StartZoomSamplePosition {
			get {
				return _startZoomSamplePosition;
			}
			set {
				_startZoomSamplePosition = value;
				NotifyPropertyChanged("StartZoomSamplePosition");
			}
		}

		public int EndZoomSamplePosition {
			get {
				return _endZoomSamplePosition;
			}
			set {
				_endZoomSamplePosition = value;
				NotifyPropertyChanged("EndZoomSamplePosition");
			}
		}

		public int PreviousStartZoomSamplePosition {
			get {
				return _previousStartZoomSamplePosition;
			}
			set {
				_previousStartZoomSamplePosition = value;
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
				var destination = new Rectangle(_startSelectXPosition, TOP_MARGIN - 1, _selectRegion.Width, _selectRegion.Height - 2 * TOP_MARGIN);
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
			_startSelectXPosition = SIDE_MARGIN;
			_endSelectXPosition = SIDE_MARGIN + _waveformDrawingWidth;
			
			_soundPlayer.SelectionBegin = TimeSpan.FromSeconds(0);
			_soundPlayer.SelectionEnd = TimeSpan.FromSeconds(_soundPlayer.ChannelLength);
			_soundPlayer.ChannelPosition = 0;
			
			UpdateSelectRegion();
		}
		
		public void ScrollRight() {
			ScrollTime(true);
		}
		
		public void ScrollLeft() {
			ScrollTime(false);
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
		
		#region Zoom Methods
		public void ZoomHorizontal(int i)
		{
			if (_soundPlayer != null && _soundPlayer.ChannelSampleLength > 1)
			{
				// Update zoom
				if (i > 0)
				{
					// don't exceed 32:1 (2^5)
					const float min = (float)1/32;
					if (SamplesPerPixel > min)
					{
						SamplesPerPixel /= 2;
					}
				}
				else if (i < 0)
				{
					// don't exceed 1:65536 (2^16)
					if (SamplesPerPixel < 65536)
					{
						SamplesPerPixel *= 2;
					}
				}
				else if (i == 0)
				{
					SamplesPerPixel = 1;
				}
				
				int startZoomSamplePos = 0;
				int endZoomSamplePos = 0;
				if (_startZoomSamplePosition > 0) {
					// TODO: Fix this
					endZoomSamplePos = (int) (WaveformDrawingWidth*SamplesPerPixel);
				} else {
					endZoomSamplePos = (int) (WaveformDrawingWidth*SamplesPerPixel);
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
				if (endZoomSamplePos > _soundPlayer.ChannelSampleLength) {
					endZoomSamplePos = _soundPlayer.ChannelSampleLength;
				}
				
				StartZoomSamplePosition = startZoomSamplePos;
				EndZoomSamplePosition = endZoomSamplePos;
				PreviousStartZoomSamplePosition = startZoomSamplePos;
				SamplesPerPixel = (float) (endZoomSamplePos - startZoomSamplePos) / (float) _waveformDrawingWidth;

				// remove select region after zooming
				//ClearSelectRegion();
				
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
			ClearSelectRegion();
			
			// reset amplitude
			this._amplitude = 1;
			
			UpdateWaveform();
		}
		#endregion
		
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
				
				// force redraw
				this.Invalidate();
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
			this.ResumeLayout(false);
		}
		#endregion
		
		#region MouseAndKeyEvents
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

			if (_soundPlayer.WaveformData == null) return;
			
			if (_isMouseDown) {
				if (Math.Abs(_currentPoint.X - _mouseDownPoint.X) > MOUSE_MOVE_TOLERANCE) {
					_startSelectXPosition = Math.Min(_mouseDownPoint.X, _currentPoint.X);
					_endSelectXPosition = Math.Max(_mouseDownPoint.X, _currentPoint.X);
					
					// limit start and end to waveform drawing space
					if (_startSelectXPosition < SIDE_MARGIN) _startSelectXPosition = SIDE_MARGIN;
					if (_endSelectXPosition > SIDE_MARGIN + _waveformDrawingWidth) _endSelectXPosition = SIDE_MARGIN + _waveformDrawingWidth;
				} else {
					ClearSelectRegion();
				}
				
				UpdateSelectRegion();
			}
		}
		
		void CustomWaveViewerMouseUp(object sender, MouseEventArgs e)
		{
			if (!_isMouseDown || _soundPlayer.WaveformData == null)
				return;
			
			_isMouseDown = false;

			if (_isZooming) {
				_startZoomSamplePosition = Math.Max((int)(_previousStartZoomSamplePosition + _samplesPerPixel * _startSelectXPosition), 0);
				_endZoomSamplePosition = Math.Min((int)(_previousStartZoomSamplePosition + _samplesPerPixel * _endSelectXPosition), _soundPlayer.ChannelSampleLength);
				
				// only allow zooming if samples are more than 10
				int samplesSelected = _endZoomSamplePosition - _startZoomSamplePosition;
				if (samplesSelected > 10) {
					// Zoom
					Zoom(_startZoomSamplePosition, _endZoomSamplePosition);
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
					
					_startLoopSamplePosition = -1;
					_endLoopSamplePosition = -1;
					
					doUpdateLoopRegion = true;
				}
			} else {
				_startLoopSamplePosition = Math.Max((int)(_previousStartZoomSamplePosition + _samplesPerPixel * (_startSelectXPosition-SIDE_MARGIN)), 0);
				_endLoopSamplePosition = Math.Min((int)(_previousStartZoomSamplePosition + _samplesPerPixel * (_endSelectXPosition-SIDE_MARGIN)), _soundPlayer.ChannelSampleLength);

				_soundPlayer.SelectionBegin = TimeSpan.FromSeconds(SamplePositionToSeconds(_startLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength));
				_soundPlayer.SelectionEnd = TimeSpan.FromSeconds(SamplePositionToSeconds(_endLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength));
				_soundPlayer.ChannelPosition = SamplePositionToSeconds(_startLoopSamplePosition, _soundPlayer.ChannelSampleLength, _soundPlayer.ChannelLength);
			}
			
			if (doUpdateLoopRegion) {
				_startSelectXPosition = 0;
				_endSelectXPosition = 0;
				UpdateSelectRegion();
			}
		}
		
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
				ScrollRight();
			} else if (e.KeyCode == Keys.Left) {
				ScrollLeft();
			} else if (e.KeyCode == Keys.Oemcomma || e.KeyCode == Keys.Home) {
				_soundPlayer.ChannelPosition = 0;
				FitToScreen();
			} else if (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.End) {
				_soundPlayer.ChannelPosition = _soundPlayer.ChannelLength;
				FitToScreen();
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
		
		#region Private Methods
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
		
		public void ScrollTime(bool doScrollRight, int channelSampleLength, int oldStartZoomSamplePosition, int oldEndZoomSamplePosition) {
			int newStartZoomSamplePosition = -1;
			int newEndZoomSamplePosition = -1;
			
			// calculate the range
			int rangeInSamples = oldEndZoomSamplePosition - oldStartZoomSamplePosition;
			int delta = rangeInSamples / 20;
			
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
			
			ScrollTime(doScrollRight, channelSampleLength, oldStartZoomSamplePosition, oldEndZoomSamplePosition);
		}
		
		private void UpdateSelectRegion()
		{
			_selectRegion.Width = _endSelectXPosition - _startSelectXPosition;
			_selectRegion.Height = this.Height;
			
			// force redraw
			this.Invalidate();
		}
		
		private void ClearSelectRegion() {
			_startSelectXPosition = -1;
			_endSelectXPosition = -1;
			_selectRegion.Width = 0;
			_selectRegion.Height = 0;

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
					_startLoopSamplePosition = SecondsToSamplePosition(_soundPlayer.SelectionBegin.TotalSeconds, _soundPlayer.ChannelLength, _soundPlayer.ChannelSampleLength);
					break;
				case "SelectionEnd":
					_endLoopSamplePosition = SecondsToSamplePosition(_soundPlayer.SelectionEnd.TotalSeconds, _soundPlayer.ChannelLength, _soundPlayer.ChannelSampleLength);
					break;
				case "WaveformData":
					FitToScreen();
					break;
				case "ChannelPosition":
					UpdateProgressIndicator();
					break;
				case "ChannelLength":
					_startLoopSamplePosition = -1;
					_endLoopSamplePosition = -1;
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