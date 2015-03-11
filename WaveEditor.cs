using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;

using System.Xml.Linq;
using CommonUtils.Audio;
using CommonUtils.FFT; // AudioAnalyzer and TimeLineUnit
using  CommonUtils.GUI; // Custom Wave Viewer

namespace WaveEditor
{
	/// <summary>
	/// Wave Editor
	/// </summary>
	public partial class WaveEditor : Form
	{
		#region Private constants
		private const int SliderSmallChange = 1;
		private const int SliderLargeChange = 32;
		#endregion

		IWaveformPlayer _soundPlayer;
		
		private TimelineUnit _timelineUnit = TimelineUnit.Time;
		
		#region Constructors
		public WaveEditor()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			InitializeScrollbar();
		}

		#endregion
		
		/// <summary>
		/// Register a sound player from which the waveform timeline
		/// can get the necessary playback data.
		/// </summary>
		/// <param name="soundPlayer">A sound player that provides waveform data through the IWaveformPlayer interface methods.</param>
		public void RegisterSoundPlayer(IWaveformPlayer soundPlayer)
		{
			this._soundPlayer = soundPlayer;
			soundPlayer.PropertyChanged += soundPlayer_PropertyChanged;
			
			customWaveViewer1.RegisterSoundPlayer(soundPlayer);
			customWaveViewer1.PropertyChanged += CustomWaveViewer_PropertyChanged;

			//customSpectrumAnalyzer1.RegisterSoundPlayer(soundPlayer);
		}
		
		#region Play and Open file methods
		public void OpenFileAndRedraw(string fileName) {
			if (File.Exists(fileName)) {
				OpenFile(fileName);
				customWaveViewer1.FitToScreen(); // Force redraw
			}
		}

		void OpenFileDialog()
		{
			openFileDialog.Filter = "All supported Audio Files|*.wav;*.ogg;*.mp1;*.m1a;*.mp2;*.m2a;*.mpa;*.mus;*.mp3;*.mpg;*.mpeg;*.mp3pro;*.aif;*.aiff;*.bwf;*.wma;*.wmv;*.aac;*.adts;*.mp4;*.m4a;*.m4b;*.mod;*.mdz;*.mo3;*.s3m;*.s3z;*.xm;*.xmz;*.it;*.itz;*.umx;*.mtm;*.flac;*.fla;*.oga;*.ogg;*.aac;*.m4a;*.m4b;*.mp4;*.mpc;*.mp+;*.mpp;*.ac3;*.wma;*.ape;*.mac|WAVE Audio|*.wav|Ogg Vorbis|*.ogg|MPEG Layer 1|*.mp1;*.m1a|MPEG Layer 2|*.mp2;*.m2a;*.mpa;*.mus|MPEG Layer 3|*.mp3;*.mpg;*.mpeg;*.mp3pro|Audio IFF|*.aif;*.aiff|Broadcast Wave|*.bwf|Windows Media Audio|*.wma;*.wmv|Advanced Audio Codec|*.aac;*.adts|MPEG 4 Audio|*.mp4;*.m4a;*.m4b|MOD Music|*.mod;*.mdz|MO3 Music|*.mo3|S3M Music|*.s3m;*.s3z|XM Music|*.xm;*.xmz|IT Music|*.it;*.itz;*.umx|MTM Music|*.mtm|Free Lossless Audio Codec|*.flac;*.fla|Free Lossless Audio Codec (Ogg)|*.oga;*.ogg|Advanced Audio Coding|*.aac|Advanced Audio Coding MPEG-4|*.m4a;*.m4b;*.mp4|Musepack|*.mpc;*.mp+;*.mpp|Dolby Digital AC-3|*.ac3|Windows Media Audio|*.wma|Monkey's Audio|*.ape;*.mac";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				string fileName = openFileDialog.FileName;
				OpenFile(fileName);
			}
		}
		
		string SaveFileDialog()
		{
			saveFileDialog1.InitialDirectory = _soundPlayer.FilePath;
			saveFileDialog1.Title = "Save Audio File";
			saveFileDialog1.CheckPathExists = true;
			saveFileDialog1.DefaultExt = "wav";
			saveFileDialog1.Filter = "Wav files (*.wav)|*.wav|All files (*.*)|*.*";
			saveFileDialog1.FilterIndex = 2;
			saveFileDialog1.RestoreDirectory = true;
			
			if (saveFileDialog1.ShowDialog() == DialogResult.OK)
			{
				_soundPlayer.SaveFile(saveFileDialog1.FileName);
				return saveFileDialog1.FileName;
			} else {
				return null;
			}
		}
		
		void OpenFile(string fileName) {
			_soundPlayer.OpenFile(fileName);
			lblFilename.Text = Path.GetFileName(fileName);
			lblBitdepth.Text = String.Format("{0} Bit", _soundPlayer.BitsPerSample);
			lblChannels.Text = String.Format("{0} Ch.", _soundPlayer.Channels);
			lblSamplerate.Text = String.Format("{0} Hz", _soundPlayer.SampleRate);
			
			UpdateDuration();
		}
		
		void TogglePlay()
		{
			// Toggle Play
			if (_soundPlayer.IsPlaying) {
				if (_soundPlayer.CanPause) {
					_soundPlayer.Pause();
				}
			} else {
				if (_soundPlayer.CanPlay) {
					_soundPlayer.Play();
				}
			}
		}
		
		void Stop()
		{
			if (_soundPlayer.CanStop)
				_soundPlayer.Stop();
			
			_soundPlayer.ChannelSamplePosition = 0;
			_soundPlayer.SelectionSampleBegin = 0;
			_soundPlayer.SelectionSampleEnd = 0;
		}
		#endregion
		
		#region Key and Mouse handlers
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			
			// Space toggles play
			if (e.KeyCode == Keys.Space) {
				TogglePlay();
				
			} else if (((Control.ModifierKeys & Keys.Control) == Keys.Control)
			           && e.KeyCode == Keys.A)
			{
				customWaveViewer1.SelectAll();
			}
		}
		#endregion
		
		#region Label Clicks (Zoom)
		void LblZoomInClick(object sender, EventArgs e)
		{
			customWaveViewer1.ZoomHorizontal(+1);
		}
		void LblZoomOutClick(object sender, EventArgs e)
		{
			customWaveViewer1.ZoomHorizontal(-1);
		}
		void LblZoomSelectionClick(object sender, EventArgs e)
		{
			customWaveViewer1.ZoomSelection();
		}
		void LblZoomInAmplitudeClick(object sender, EventArgs e)
		{
			customWaveViewer1.ZoomInAmplitude();
		}
		void LblZoomOutAmplitudeClick(object sender, EventArgs e)
		{
			customWaveViewer1.ZoomOutAmplitude();
		}
		void LblIncreaseSelectionClick(object sender, EventArgs e)
		{
			customWaveViewer1.IncreaseSelection();
		}
		void LblDecreaseSelectionClick(object sender, EventArgs e)
		{
			customWaveViewer1.DecreaseSelection();
		}
		#endregion

		#region Change Labels Methods
		private void ChangeChannelPosition(string channelPos) {
			if(this.InvokeRequired)
			{
				this.Invoke(new Action(() => ChangeChannelPosition(channelPos)));
			}
			else
			{
				lblPlayPosition.Text = channelPos;
			}
		}

		private void ChangeSelection(string selection) {
			if(this.InvokeRequired)
			{
				this.Invoke(new Action(() => ChangeSelection(selection)));
			}
			else
			{
				lblSelection.Text = selection;
			}
		}
		
		private void ChangeZoomRatio(string zoomRatio) {
			if(this.InvokeRequired)
			{
				this.Invoke(new Action(() => ChangeZoomRatio(zoomRatio)));
			}
			else
			{
				lblZoomRatio.Text = zoomRatio;
			}
		}
		
		private void ChangeHScrollbarPosition(int position) {
			if(this.InvokeRequired)
			{
				this.Invoke(new Action(() => ChangeHScrollbarPosition(position)));
			}
			else
			{
				if (position < hScrollBar.Maximum) {
					hScrollBar.Value = position;
				}
			}
		}
		
		#endregion
		
		#region PropertyChanged
		void soundPlayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "ChannelSamplePosition":
					UpdateChannelPosition();
					break;
				case "IsPlaying":
					break;
				case "ChannelSampleLength":
					break;
				case "SelectionSampleBegin":
					UpdateSelection();
					break;
				case "SelectionSampleEnd":
					UpdateSelection();
					break;
				case "WaveformData":
					//hScrollBar.Maximum = (int) (soundEngine.ChannelSampleLength - 1);
					break;
			}
		}
		
		void CustomWaveViewer_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
				case "StartZoomSamplePosition":
					ChangeHScrollbarPosition(customWaveViewer1.StartZoomSamplePosition);
					break;
				case "ZoomRatioString":
					ChangeZoomRatio(customWaveViewer1.ZoomRatioString);
					UpdateScrollbar();
					break;
			}
		}
		#endregion
		
		#region Scrollbar
		private void InitializeScrollbar() {
			// Min Usually set to zero or one
			// Max Set this to the number of rows in the file minus the number of rows displayed. If you want to scroll past the last row, then set it larger.
			// Value Where the slider is located.
			// LargeChange Amount Value is changed when the user clicks above or below the slider, or presses PgUp or PgDn keys.
			// SmallChange Amount Value is changed when the user clicks an arrow or presses the up and down arrow keys.
			
			hScrollBar.SmallChange = SliderSmallChange;
			hScrollBar.LargeChange = SliderLargeChange;
			hScrollBar.Value = 0;
			hScrollBar.Minimum = 0;
			hScrollBar.Maximum= 0;
		}
		
		private void UpdateScrollbar() {

			if(this.InvokeRequired)
			{
				this.Invoke(new Action(UpdateScrollbar));
			}
			else
			{
				if (customWaveViewer1.EndZoomSamplePosition > 0) {
					// 1:64 = thumb: total scrollbar width divided by 2
					// 1:32 = thumb: total scrollbar width divided by 4
					// 1:16 = thumb: total scrollbar width divided by 8
					// 1:8 = thumb: total scrollbar width divided by 16
					// 1:4 = thumb: total scrollbar width divided by 32
					// 1:2 = thumb: total scrollbar width divided by 64
					
					int startPos = customWaveViewer1.StartZoomSamplePosition;
					int endPos = customWaveViewer1.EndZoomSamplePosition;
					int rangeInSamples = Math.Abs(endPos - startPos) + 1;
					int channelSampleLength = _soundPlayer.ChannelSampleLength;
					
					// if ratio is 1 the large change is the same as maximum, i.e. the thumb is maximum
					double ratio = (double) channelSampleLength / (double) rangeInSamples;
					
					// a scroll bar can only go up to it's maximum minus the size of the scrollbar's slider.
					// And the size of the slider appears to be equal to (LargeChange - 1).

					// The value of a scroll bar cannot reach its maximum value through user interaction at run time.
					// The maximum value that can be reached through user interaction is equal to
					// 1 plus the Maximum property value minus the LargeChange property value.
					// If necessary, you can set the Maximum property to the size of the object -1 to account for the term of 1.
					
					hScrollBar.Minimum = 0;
					hScrollBar.Maximum = channelSampleLength;
					hScrollBar.LargeChange = (int) ( (double) channelSampleLength / ratio );
					hScrollBar.SmallChange = (int) (hScrollBar.LargeChange / SliderLargeChange);
					hScrollBar.Value = startPos;
				}
			}
		}
		
		void HScrollBarScroll(object sender, ScrollEventArgs e)
		{
			if (e.NewValue > e.OldValue) {
				int newStartPos = e.NewValue;
				customWaveViewer1.ScrollTime(true, newStartPos);
			} else if (e.NewValue == e.OldValue) {
				// no change in value, means do nothing
			} else {
				int newStartPos = e.NewValue;
				customWaveViewer1.ScrollTime(false, newStartPos);
			}
		}
		#endregion
		
		#region LabelTimeMode Clicks
		void LblSelectionClick(object sender, EventArgs e)
		{
			ToggleTimeMode();
		}
		void LblDurationClick(object sender, EventArgs e)
		{
			ToggleTimeMode();
		}
		void LblPlayPositionClick(object sender, EventArgs e)
		{
			ToggleTimeMode();
		}
		#endregion
		
		void ToggleTimeMode() {
			
			if (_timelineUnit == TimelineUnit.Samples) {
				_timelineUnit = TimelineUnit.Seconds;

				samplesToolStripMenuItem.Checked = false;
				secondsToolStripMenuItem.Checked = true;
				timeFormatToolStripMenuItem.Checked = false;
				
			} else if (_timelineUnit == TimelineUnit.Seconds) {
				_timelineUnit = TimelineUnit.Time;

				samplesToolStripMenuItem.Checked = false;
				secondsToolStripMenuItem.Checked = false;
				timeFormatToolStripMenuItem.Checked = true;
			} else { // if TimelineUnit.Time
				_timelineUnit = TimelineUnit.Samples;

				samplesToolStripMenuItem.Checked = true;
				secondsToolStripMenuItem.Checked = false;
				timeFormatToolStripMenuItem.Checked = false;
			}
			customWaveViewer1.TimelineUnit = _timelineUnit;
			
			UpdateSelection();
			UpdateChannelPosition();
			UpdateDuration();
		}
		
		void UpdateSelection() {
			string selectionLabel = "";
			switch (_timelineUnit) {
				case TimelineUnit.Samples:
					int selectionSampleBegin = _soundPlayer.SelectionSampleBegin;
					int selectionSampleEnd = _soundPlayer.SelectionSampleEnd;
					int selectionSampleDuration = selectionSampleEnd-selectionSampleBegin + 1;
					selectionLabel = string.Format("{0} - {1} ({2})", selectionSampleBegin, selectionSampleEnd, selectionSampleDuration);
					break;
				case TimelineUnit.Time:
					double selTimeBegin = CustomWaveViewer.SamplePositionToSeconds(_soundPlayer.SelectionSampleBegin, _soundPlayer.SampleRate);
					double selTimeEnd = CustomWaveViewer.SamplePositionToSeconds(_soundPlayer.SelectionSampleEnd, _soundPlayer.SampleRate);
					string selectionTimeBegin = TimeSpan.FromSeconds(selTimeBegin).ToString(@"hh\:mm\:ss\.fff");
					string selectionTimeEnd = TimeSpan.FromSeconds(selTimeEnd).ToString(@"hh\:mm\:ss\.fff");
					string selectionTimeDuration = TimeSpan.FromSeconds(selTimeEnd-selTimeBegin).ToString(@"hh\:mm\:ss\.fff");
					selectionLabel = string.Format("{0} - {1} ({2})", selectionTimeBegin, selectionTimeEnd, selectionTimeDuration);
					break;
				case TimelineUnit.Seconds:
					double selSecondsBegin = CustomWaveViewer.SamplePositionToSeconds(_soundPlayer.SelectionSampleBegin, _soundPlayer.SampleRate);
					double selSecondsEnd = CustomWaveViewer.SamplePositionToSeconds(_soundPlayer.SelectionSampleEnd, _soundPlayer.SampleRate);
					double selSecondsDuration = selSecondsEnd-selSecondsBegin;
					selectionLabel = string.Format("{0:0.000} - {1:0.000} ({2:0.000})", selSecondsBegin, selSecondsEnd, selSecondsDuration);
					break;
			}
			ChangeSelection(selectionLabel);
		}
		
		void UpdateChannelPosition() {
			string channelPosLabel = "";
			switch (_timelineUnit) {
				case TimelineUnit.Samples:
					int channelSamplePos = _soundPlayer.ChannelSamplePosition;
					channelPosLabel = string.Format("{0}", channelSamplePos);
					break;
				case TimelineUnit.Time:
					channelPosLabel = TimeSpan.FromSeconds(CustomWaveViewer.SamplePositionToSeconds(_soundPlayer.ChannelSamplePosition, _soundPlayer.SampleRate)).ToString(@"hh\:mm\:ss\.fff");
					break;
				case TimelineUnit.Seconds:
					channelPosLabel = string.Format("{0:0.000}", CustomWaveViewer.SamplePositionToSeconds(_soundPlayer.ChannelSamplePosition, _soundPlayer.SampleRate));
					break;
			}

			ChangeChannelPosition(channelPosLabel);
		}
		
		void UpdateDuration() {
			string durationLabel = "";
			switch (_timelineUnit) {
				case TimelineUnit.Samples:
					durationLabel = String.Format("{0}", _soundPlayer.ChannelSampleLength);
					break;
				case TimelineUnit.Time:
					durationLabel = TimeSpan.FromSeconds(CustomWaveViewer.SamplePositionToSeconds(_soundPlayer.ChannelSampleLength, _soundPlayer.SampleRate)).ToString(@"hh\:mm\:ss\.fff");
					break;
				case TimelineUnit.Seconds:
					durationLabel = String.Format("{0:0.000}", CustomWaveViewer.SamplePositionToSeconds(_soundPlayer.ChannelSampleLength, _soundPlayer.SampleRate));
					break;
			}

			lblDuration.Text = durationLabel;
		}
		
		void NewToolStripMenuItemClick(object sender, EventArgs e)
		{
			MessageBox.Show("Not Implemented");
		}
		void OpenToolStripMenuItemClick(object sender, EventArgs e)
		{
			OpenFileDialog();
		}
		void SaveToolStripMenuItemClick(object sender, EventArgs e)
		{
			string newPath = _soundPlayer.FilePath + ".new.wav";
			_soundPlayer.SaveFile(newPath);
			OpenFile(newPath);
		}
		void SaveAsToolStripMenuItemClick(object sender, EventArgs e)
		{
			string newPath = SaveFileDialog();
			if (newPath != null) {
				OpenFile(newPath);
			}
		}
		void ExitToolStripMenuItemClick(object sender, EventArgs e)
		{
			this.Close();
		}
		void SelectionGridLinesToolStripMenuItemClick(object sender, EventArgs e)
		{
			MessageBox.Show("Not Implemented");
		}
		void SamplesToolStripMenuItemClick(object sender, EventArgs e)
		{
			samplesToolStripMenuItem.Checked = true;
			secondsToolStripMenuItem.Checked = false;
			timeFormatToolStripMenuItem.Checked = false;

			_timelineUnit = TimelineUnit.Samples;
			customWaveViewer1.TimelineUnit = _timelineUnit;
			
			UpdateSelection();
			UpdateChannelPosition();
			UpdateDuration();
		}
		void SecondsToolStripMenuItemClick(object sender, EventArgs e)
		{
			samplesToolStripMenuItem.Checked = false;
			secondsToolStripMenuItem.Checked = true;
			timeFormatToolStripMenuItem.Checked = false;

			_timelineUnit = TimelineUnit.Seconds;
			customWaveViewer1.TimelineUnit = _timelineUnit;
			
			UpdateSelection();
			UpdateChannelPosition();
			UpdateDuration();
		}
		void TimeFormatToolStripMenuItemClick(object sender, EventArgs e)
		{
			samplesToolStripMenuItem.Checked = false;
			secondsToolStripMenuItem.Checked = false;
			timeFormatToolStripMenuItem.Checked = true;

			_timelineUnit = TimelineUnit.Time;
			customWaveViewer1.TimelineUnit = _timelineUnit;
			
			UpdateSelection();
			UpdateChannelPosition();
			UpdateDuration();
		}
		void SnapToZeroCrossingToolStripMenuItemClick(object sender, EventArgs e)
		{
			snapToZeroCrossingToolStripMenuItem.Checked = !snapToZeroCrossingToolStripMenuItem.Checked;
			customWaveViewer1.SnapToZeroCrossing = snapToZeroCrossingToolStripMenuItem.Checked;
		}
	}
}