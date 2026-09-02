using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using KaraokeVideoCreator.Application.ViewModels;

namespace KaraokeVideoCreator.UI.Controls
{
    public partial class HorizontalTimelineControl : UserControl
    {
        private bool _isDraggingPlayhead;
        private double _timelineWidth = 2400.0;

        public HorizontalTimelineControl()
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
                RenderTimelineContent();
                UpdatePlayheadPosition();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.PlaybackPosition))
            {
                UpdatePlayheadPosition();
            }
            else if (e.PropertyName == nameof(MainViewModel.WaveformPoints) || 
                     e.PropertyName == nameof(MainViewModel.WaveformSamples) || 
                     e.PropertyName == nameof(MainViewModel.TimelineZoom) || 
                     e.PropertyName == nameof(MainViewModel.LyricsText) || 
                     e.PropertyName == nameof(MainViewModel.HasAudio) || 
                     e.PropertyName == nameof(MainViewModel.TotalDuration))
            {
                RenderTimelineContent();
                UpdatePlayheadPosition();
            }
        }

        private void RenderTimelineContent()
        {
            if (ViewModel != null)
            {
                _timelineWidth = 2400.0 * ViewModel.TimelineZoom;
                TimelineCanvas.Width = _timelineWidth;
            }

            TimelineCanvas.Children.Clear();

            // Re-add static track separators
            Line sep1 = new Line { X1 = 0, Y1 = 28, X2 = _timelineWidth, Y2 = 28, Stroke = (Brush)FindResource("CardBorderBrush"), StrokeThickness = 1 };
            Line sep2 = new Line { X1 = 0, Y1 = 84, X2 = _timelineWidth, Y2 = 84, Stroke = (Brush)FindResource("CardBorderBrush"), StrokeThickness = 1 };
            TimelineCanvas.Children.Add(sep1);
            TimelineCanvas.Children.Add(sep2);

            TimelineCanvas.Children.Add(PlayheadContainer);

            if (ViewModel == null) return;

            double totalSeconds = Math.Max(1.0, ViewModel.TotalDuration.TotalSeconds);
            double totalMs = totalSeconds * 1000.0;

            // 1. Draw Time Scale Ruler Ticks and Labels (Top Track)
            double tickIntervalSeconds = totalSeconds > 300 ? 30.0 : (totalSeconds > 120 ? 15.0 : 10.0);
            for (double sec = 0; sec <= totalSeconds; sec += tickIntervalSeconds)
            {
                double fraction = sec / totalSeconds;
                double x = fraction * _timelineWidth;

                Line tickLine = new Line
                {
                    X1 = x,
                    Y1 = 18,
                    X2 = x,
                    Y2 = 28,
                    Stroke = (Brush)FindResource("CardBorderBrush"),
                    StrokeThickness = 1
                };
                TimelineCanvas.Children.Add(tickLine);

                TimeSpan labelTime = TimeSpan.FromSeconds(sec);
                TextBlock labelText = new TextBlock
                {
                    Text = labelTime.ToString(@"mm\:ss\.fff"),
                    FontSize = 9,
                    FontFamily = (FontFamily)FindResource("AppFontFamily"),
                    Foreground = (Brush)FindResource("TextSecondaryBrush")
                };
                Canvas.SetLeft(labelText, Math.Max(2, x - 20));
                Canvas.SetTop(labelText, 4);
                TimelineCanvas.Children.Add(labelText);
            }

            // 2. Draw Real PCM Audio Waveform (Middle Track Y=28 to Y=84, Center Y=56)
            var points = ViewModel.WaveformPoints;
            if (points != null && points.Length > 0)
            {
                int count = points.Length;
                double stepX = _timelineWidth / count;
                Brush barBrush = (Brush)FindResource("WaveformBarBrush");

                for (int i = 0; i < count; i++)
                {
                    double x = i * stepX;
                    var pt = points[i];

                    double yMin = 56.0 - (pt.MinAmplitude * 26.0);
                    double yMax = 56.0 - (pt.MaxAmplitude * 26.0);

                    if (Math.Abs(yMin - yMax) < 1.5)
                    {
                        yMax = 55.25;
                        yMin = 56.75;
                    }

                    Line bar = new Line
                    {
                        X1 = x,
                        Y1 = yMax,
                        X2 = x,
                        Y2 = yMin,
                        Stroke = barBrush,
                        StrokeThickness = Math.Max(1.0, stepX * 0.8)
                    };
                    TimelineCanvas.Children.Add(bar);
                }
            }

            // 3. Draw Word Timing Blocks (Bottom Track Y=84 to Y=140)
            if (ViewModel.CurrentProject?.Lyrics?.Lines != null)
            {
                TimeSpan currentPos = ViewModel.PlaybackPosition;
                foreach (var line in ViewModel.CurrentProject.Lyrics.Lines)
                {
                    foreach (var word in line.Words)
                    {
                        if (!word.StartTime.HasValue) continue;

                        double startX = (word.StartTime.Value.TotalMilliseconds / totalMs) * _timelineWidth;
                        double endX = word.EndTime.HasValue 
                            ? (word.EndTime.Value.TotalMilliseconds / totalMs) * _timelineWidth
                            : startX + 60.0;

                        double blockWidth = Math.Max(24.0, endX - startX);

                        bool isActive = word.StartTime.HasValue && word.EndTime.HasValue &&
                                        currentPos >= word.StartTime.Value && currentPos < word.EndTime.Value;

                        Border wordBlock = new Border
                        {
                            Background = isActive 
                                ? new SolidColorBrush(Color.FromRgb(254, 240, 138))
                                : word.IsSynced 
                                    ? new SolidColorBrush(Color.FromRgb(219, 234, 254))
                                    : new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                            BorderBrush = isActive
                                ? new SolidColorBrush(Color.FromRgb(234, 179, 8))
                                : word.IsSynced
                                    ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
                                    : new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                            BorderThickness = new Thickness(1.5),
                            CornerRadius = new CornerRadius(3),
                            Padding = new Thickness(4, 2, 4, 2),
                            Width = blockWidth,
                            Height = 36,
                            ToolTip = $"{word.Text} ({(word.StartTime.HasValue ? word.StartTime.Value.ToString(@"mm\:ss\.fff") : "")} - {(word.EndTime.HasValue ? word.EndTime.Value.ToString(@"mm\:ss\.fff") : "")})"
                        };

                        TextBlock txt = new TextBlock
                        {
                            Text = word.Text,
                            FontSize = 11,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        };
                        wordBlock.Child = txt;

                        Canvas.SetLeft(wordBlock, startX);
                        Canvas.SetTop(wordBlock, 94);
                        TimelineCanvas.Children.Add(wordBlock);
                    }
                }
            }
        }

        private void UpdatePlayheadPosition()
        {
            if (ViewModel == null) return;

            double totalMs = Math.Max(1.0, ViewModel.TotalDuration.TotalMilliseconds);
            double currentMs = Math.Clamp(ViewModel.PlaybackPosition.TotalMilliseconds, 0.0, totalMs);
            double fraction = currentMs / totalMs;

            double x = fraction * _timelineWidth;

            Canvas.SetLeft(PlayheadContainer, x - 12);
            PlayheadTimeText.Text = ViewModel.PlaybackPosition.ToString(@"mm\:ss\.fff");

            // Auto-Scroll to keep Playhead centered horizontally in viewport during playback
            if (ViewModel.IsPlaying && !_isDraggingPlayhead)
            {
                double viewportWidth = TimelineScrollViewer.ViewportWidth;
                double targetOffset = Math.Max(0, x - (viewportWidth / 2));
                TimelineScrollViewer.ScrollToHorizontalOffset(targetOffset);
            }
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingPlayhead = true;
            TimelineCanvas.CaptureMouse();
            SeekToMouseX(e.GetPosition(TimelineCanvas).X);
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingPlayhead)
            {
                SeekToMouseX(e.GetPosition(TimelineCanvas).X);
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

        private void SeekToMouseX(double mouseX)
        {
            if (ViewModel == null) return;

            double clampedX = Math.Clamp(mouseX, 0.0, _timelineWidth);
            double fraction = clampedX / _timelineWidth;

            if (ViewModel.SeekCommand.CanExecute(fraction))
            {
                ViewModel.SeekCommand.Execute(fraction);
            }
        }

        private void OnTimelinePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel == null || !ViewModel.HasAudio) return;

            e.Handled = true;

            Point mousePosCanvas = e.GetPosition(TimelineCanvas);
            double currentCanvasWidth = TimelineCanvas.Width;
            double mouseRatio = currentCanvasWidth > 0 ? Math.Clamp(mousePosCanvas.X / currentCanvasWidth, 0.0, 1.0) : 0.5;

            double oldZoom = ViewModel.TimelineZoom;
            double newZoom = e.Delta > 0
                ? Math.Min(MainViewModel.MaxZoom, oldZoom + MainViewModel.ZoomStep)
                : Math.Max(MainViewModel.MinZoom, oldZoom - MainViewModel.ZoomStep);

            if (Math.Abs(newZoom - oldZoom) < 0.001) return;

            ViewModel.TimelineZoom = newZoom;

            double newCanvasWidth = 2400.0 * newZoom;
            double newMouseX = mouseRatio * newCanvasWidth;
            Point mousePosScrollViewer = e.GetPosition(TimelineScrollViewer);
            double targetScrollOffset = Math.Max(0, newMouseX - mousePosScrollViewer.X);

            TimelineScrollViewer.ScrollToHorizontalOffset(targetScrollOffset);
        }
    }
}
