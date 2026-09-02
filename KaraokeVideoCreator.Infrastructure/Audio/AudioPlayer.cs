using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using KaraokeVideoCreator.Domain.Interfaces;
using KaraokeVideoCreator.Infrastructure.Logging;

namespace KaraokeVideoCreator.Infrastructure.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
        private readonly MediaPlayer _mediaPlayer;
        private readonly DispatcherTimer _timer;
        private bool _isPlaying;

        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler? PlaybackEnded;

        public AudioPlayer()
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.MediaEnded += OnMediaEnded;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            _timer.Tick += OnTimerTick;
        }

        public bool IsPlaying => _isPlaying;

        public TimeSpan Position
        {
            get => _mediaPlayer.Position;
            set
            {
                if (_mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    TimeSpan clamped = value;
                    if (clamped < TimeSpan.Zero) clamped = TimeSpan.Zero;
                    if (clamped > Duration) clamped = Duration;

                    _mediaPlayer.Position = clamped;
                    PositionChanged?.Invoke(this, _mediaPlayer.Position);
                }
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (_mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    return _mediaPlayer.NaturalDuration.TimeSpan;
                }
                return TimeSpan.Zero;
            }
        }

        public double Volume
        {
            get => _mediaPlayer.Volume;
            set => _mediaPlayer.Volume = Math.Clamp(value, 0.0, 1.0);
        }

        public void Open(string filePath)
        {
            Stop();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                AppLogger.LogWarning($"Audio file does not exist: {filePath}");
                return;
            }

            try
            {
                AppLogger.LogInfo($"Loading audio player media: {filePath}");
                _mediaPlayer.Open(new Uri(filePath, UriKind.Absolute));
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to open media player for: {filePath}", ex);
            }
        }

        public void Play()
        {
            if (IsPlaying) return;

            try
            {
                if (_mediaPlayer.NaturalDuration.HasTimeSpan && _mediaPlayer.Position >= _mediaPlayer.NaturalDuration.TimeSpan)
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                }

                _mediaPlayer.Play();
                _isPlaying = true;
                _timer.Start();
                AppLogger.LogInfo("Audio playback started.");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error starting playback.", ex);
            }
        }

        public void Pause()
        {
            if (!IsPlaying) return;

            try
            {
                _mediaPlayer.Pause();
                _isPlaying = false;
                _timer.Stop();
                AppLogger.LogInfo("Audio playback paused.");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error pausing playback.", ex);
            }
        }

        public void Stop()
        {
            try
            {
                _mediaPlayer.Stop();
                _isPlaying = false;
                _timer.Stop();
                _mediaPlayer.Position = TimeSpan.Zero;
                PositionChanged?.Invoke(this, TimeSpan.Zero);
                AppLogger.LogInfo("Audio playback stopped.");
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error stopping playback.", ex);
            }
        }

        public void Close()
        {
            Stop();
            _mediaPlayer.Close();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_isPlaying && _mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                PositionChanged?.Invoke(this, _mediaPlayer.Position);
            }
        }

        private void OnMediaEnded(object? sender, EventArgs e)
        {
            _isPlaying = false;
            _timer.Stop();
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
            AppLogger.LogInfo("Audio playback reached end.");
        }
    }
}
