﻿using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;

using CommonUtils.Audio;

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
			openFileDialog.Filter = "Audio Files(*.wav;*.mp3)|*.wav;*.mp3|All files (*.*)|*.*";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				string fileName = openFileDialog.FileName;
				OpenFile(fileName);
			}
		}
		
		void OpenFile(string fileName) {
			_soundPlayer.OpenFile(fileName);
			lblFilename.Text = Path.GetFileName(fileName);
			lblBitdepth.Text = String.Format("{0} Bit", _soundPlayer.BitsPerSample);
			lblChannels.Text = String.Format("{0} Ch.", _soundPlayer.Channels);
			lblSamplerate.Text = String.Format("{0} Hz", _soundPlayer.SampleRate);
			
			string durationTime = TimeSpan.FromSeconds(_soundPlayer.ChannelLength).ToString(@"hh\:mm\:ss\.fff");
			int durationSamples = _soundPlayer.ChannelSampleLength;
			lblDuration.Text = String.Format("{0} [{1}]", durationTime, durationSamples);
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
			
			_soundPlayer.ChannelPosition = 0;
			_soundPlayer.SelectionBegin = TimeSpan.FromMilliseconds(0);
			_soundPlayer.SelectionEnd = TimeSpan.FromMilliseconds(0);
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
				case "ChannelPosition":
					string channelPos = TimeSpan.FromSeconds(_soundPlayer.ChannelPosition).ToString(@"hh\:mm\:ss\.fff");
					ChangeChannelPosition(channelPos);
					break;
				case "IsPlaying":
					break;
				case "ChannelLength":
					break;
				case "SelectionBegin":
					break;
				case "SelectionEnd":
					double selBegin = _soundPlayer.SelectionBegin.TotalSeconds;
					double selEnd = _soundPlayer.SelectionEnd.TotalSeconds;
					string selectionBegin = TimeSpan.FromSeconds(selBegin).ToString(@"hh\:mm\:ss\.fff");
					string selectionEnd = TimeSpan.FromSeconds(selEnd).ToString(@"hh\:mm\:ss\.fff");
					string selectionDuration = TimeSpan.FromSeconds(selEnd-selBegin).ToString(@"hh\:mm\:ss\.fff");
					ChangeSelection(string.Format("{0} - {1} ({2})", selectionBegin, selectionEnd, selectionDuration));
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
					// 1:2 = thumb: total scrollbar width divided by 64
					
					int startPos = customWaveViewer1.StartZoomSamplePosition;
					int endPos = customWaveViewer1.EndZoomSamplePosition;
					int rangeInSamples = Math.Abs(endPos - startPos);
					int channelSampleLength = _soundPlayer.ChannelSampleLength;
					
					// if ratio is 1 the large change is the same as maximum, i.e. the thumb is maximum
					double ratio = (channelSampleLength / rangeInSamples);
					
					hScrollBar.Minimum = 0;
					hScrollBar.Maximum = channelSampleLength;
					hScrollBar.LargeChange = (int) (hScrollBar.Maximum / ratio);
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
			} else {
				int newStartPos = e.NewValue;
				customWaveViewer1.ScrollTime(false, newStartPos);
			}
		}
		#endregion
		
	}
}