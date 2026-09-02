namespace KaraokeVideoCreator.Domain.Models
{
    public readonly struct WaveformPoint
    {
        public float MinAmplitude { get; }
        public float MaxAmplitude { get; }

        public WaveformPoint(float minAmplitude, float maxAmplitude)
        {
            MinAmplitude = minAmplitude;
            MaxAmplitude = maxAmplitude;
        }
    }
}
