using System;

namespace KaraokeVideoCreator.Domain.Interfaces
{
    public interface IAudioPlayer
    {
        event EventHandler<TimeSpan>? PositionChanged;
        event EventHandler? PlaybackEnded;

        bool IsPlaying { get; }
        TimeSpan Position { get; set; }
        TimeSpan Duration { get; }
        double Volume { get; set; } // 0.0 to 1.0

        void Open(string filePath);
        void Play();
        void Pause();
        void Stop();
        void Close();
    }
}
