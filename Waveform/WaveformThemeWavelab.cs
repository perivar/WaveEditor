namespace aybe.Waveform
{
	public sealed class WaveformThemeWavelab : IWaveformTheme
	{
		public WaveformThemeWavelab()
		{
			Color6dBLevel = unchecked((int)0xFFB0B0B0);
			ColorBackground = unchecked((int)0xFFCCCFD5);
			ColorDCLevel = unchecked((int)0xFF808080);
			ColorEndIndicator = unchecked((int)0xFF808080);
			ColorEnvelope = unchecked((int)0xFF00003E);
			ColorForm = unchecked((int)0xFFAAD2FF);
			ColorSeparationLine = unchecked((int)0xFF808080);
			Draw6dBLevel = true;
			DrawBackground = true;
			DrawDCLevel = true;
			DrawEndIndicator = true;
			DrawEnvelope = true;
			DrawForm = true;
			DrawSeparationLine = true;
		}

		public int Color6dBLevel { get; set; }
		public int ColorBackground { get; set; }
		public int ColorDCLevel { get; set; }
		public int ColorEndIndicator { get; set; }
		public int ColorEnvelope { get; set; }
		public int ColorForm { get; set; }
		public int ColorSeparationLine { get; set; }

		public bool Draw6dBLevel { get; set; }
		public bool DrawBackground { get; set; }
		public bool DrawDCLevel { get; set; }
		public bool DrawEndIndicator { get; set; }
		public bool DrawEnvelope { get; set; }
		public bool DrawForm { get; set; }
		public bool DrawSeparationLine { get; set; }
	}
}