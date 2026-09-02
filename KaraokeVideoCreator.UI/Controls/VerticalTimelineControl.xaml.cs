using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using KaraokeVideoCreator.Application.ViewModels;

namespace KaraokeVideoCreator.UI.Controls
{
    public partial class VerticalTimelineControl : UserControl
    {
        private bool _isDraggingPlayhead;
        private double _timelineHeight = 1600;
        private double _canvasWidth = 320;
        private double _centerX => _canvasWidth / 2.0;

        public VerticalTimelineControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is MainViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                RenderWaveformAndTicks();
                UpdatePlayheadPosition();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.PlaybackPosition))
            {
                UpdatePlayheadPosition();
            }
            else if (e.PropertyName == nameof(MainViewModel.WaveformSamples) || 
                     e.PropertyName == nameof(MainViewModel.TimelineZoom) || 
                     e.PropertyName == nameof(MainViewModel.HasAudio) || 
                     e.PropertyName == nameof(MainViewModel.TotalDuration))
            {
                RenderWaveformAndTicks();
                UpdatePlayheadPosition();
            }
        }

        private void RenderWaveformAndTicks()
        {
            if (ViewModel != null)
            {
                _timelineHeight = 1600.0 * ViewModel.TimelineZoom;
                TimelineCanvas.Height = _timelineHeight;
                CenterAxisLine.Y2 = _timelineHeight;
            }

            TimelineCanvas.Children.Clear();
            TimelineCanvas.Children.Add(CenterAxisLine);
            TimelineCanvas.Children.Add(PlayheadContainer);

            if (ViewModel == null) return;

            float[] samples = ViewModel.WaveformSamples;
            if (samples == null || samples.Length == 0) return;

            double totalSeconds = Math.Max(1, ViewModel.TotalDuration.TotalSeconds);
            int sampleCount = samples.Length;
            double stepY = _timelineHeight / sampleCount;

            // 1. Draw Mirrored Vertical Waveform Amplitude Bars
            for (int i = 0; i < sampleCount; i++)
            {
                double y = i * stepY;
                float amp = samples[i];
                double halfWidth = amp * 100.0; // max width 200px

                Line bar = new Line
                {
                    X1 = _centerX - halfWidth,
                    Y1 = y,
                    X2 = _centerX + halfWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)), // Indigo/Blue primary waveform
                    StrokeThickness = 3
                };

                TimelineCanvas.Children.Add(bar);
            }

            // 2. Draw Time Scale Ticks and Millisecond Labels (Top -> Bottom)
            double tickIntervalSeconds = totalSeconds > 300 ? 30.0 : (totalSeconds > 120 ? 15.0 : 10.0);
            for (double sec = 0; sec <= totalSeconds; sec += tickIntervalSeconds)
            {
                double fraction = sec / totalSeconds;
                double y = fraction * _timelineHeight;

                Line tickLine = new Line
                {
                    X1 = 40,
                    Y1 = y,
                    X2 = 280,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(217, 221, 227)),
                    StrokeThickness = 1
                };
                TimelineCanvas.Children.Add(tickLine);

                TimeSpan labelTime = TimeSpan.FromSeconds(sec);
                TextBlock labelText = new TextBlock
                {
                    Text = labelTime.ToString(@"mm\:ss\.fff"),
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                Canvas.SetLeft(labelText, 6);
                Canvas.SetTop(labelText, Math.Max(0, y - 7));
                TimelineCanvas.Children.Add(labelText);
            }
        }

        private void UpdatePlayheadPosition()
        {
            if (ViewModel == null) return;

            double totalMs = Math.Max(1.0, ViewModel.TotalDuration.TotalMilliseconds);
            double currentMs = Math.Clamp(ViewModel.PlaybackPosition.TotalMilliseconds, 0.0, totalMs);
            double fraction = currentMs / totalMs;

            double y = fraction * _timelineHeight;

            Canvas.SetTop(PlayheadContainer, y - 12);
            PlayheadTimeText.Text = ViewModel.PlaybackPosition.ToString(@"mm\:ss\.fff");

            // Auto-Scroll to keep Playhead centered in viewport during playback
            if (ViewModel.IsPlaying && !_isDraggingPlayhead)
            {
                double viewportHeight = TimelineScrollViewer.ViewportHeight;
                double targetOffset = Math.Max(0, y - (viewportHeight / 2));
                TimelineScrollViewer.ScrollToVerticalOffset(targetOffset);
            }
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingPlayhead = true;
            TimelineCanvas.CaptureMouse();
            SeekToMouseY(e.GetPosition(TimelineCanvas).Y);
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingPlayhead)
            {
                SeekToMouseY(e.GetPosition(TimelineCanvas).Y);
            }
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingPlayhead)
            {
                _isDraggingPlayhead = false;
                TimelineCanvas.ReleaseMouseCapture();
            }
        }

        private void SeekToMouseY(double mouseY)
        {
            if (ViewModel == null) return;

            double clampedY = Math.Clamp(mouseY, 0.0, _timelineHeight);
            double fraction = clampedY / _timelineHeight;

            if (ViewModel.SeekCommand.CanExecute(fraction))
            {
                ViewModel.SeekCommand.Execute(fraction);
            }
        }
    }
}
