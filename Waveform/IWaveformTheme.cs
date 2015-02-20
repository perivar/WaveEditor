namespace aybe.Waveform
{
    public interface IWaveformTheme
    {
        int Color6dBLevel { get; set; }
        int ColorBackground { get; set; }
        int ColorDCLevel { get; set; }
        int ColorEndIndicator { get; set; }
        int ColorEnvelope { get; set; }
        int ColorForm { get; set; }
        int ColorSeparationLine { get; set; }
        bool Draw6dBLevel { get; set; }
        bool DrawBackground { get; set; }
        bool DrawDCLevel { get; set; }
        bool DrawEndIndicator { get; set; }
        bool DrawEnvelope { get; set; }
        bool DrawForm { get; set; }
        bool DrawSeparationLine { get; set; }
    }
}