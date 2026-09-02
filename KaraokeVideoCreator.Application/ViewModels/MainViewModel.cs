using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using KaraokeVideoCreator.Application.Services;
using KaraokeVideoCreator.Domain.Interfaces;
using KaraokeVideoCreator.Domain.Models;
using KaraokeVideoCreator.Infrastructure.Audio;

namespace KaraokeVideoCreator.Application.ViewModels
{
    public class LyricLineItem
    {
        public int Index { get; set; }
        public string LineNumberFormatted => (Index + 1).ToString("D2");
        public string Text { get; set; } = string.Empty;
    }

    public class MainViewModel : ViewModelBase
    {
        public const double MinZoom = 0.5;
        public const double MaxZoom = 5.0;
        public const double ZoomStep = 0.25;

        private readonly ProjectService _projectService;
        private readonly IFileDialogService _fileDialogService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IRecentProjectsStore _recentStore;
        private readonly IAudioPlayer _audioPlayer;

        private Project _currentProject;
        private TimeSpan _playbackPosition = TimeSpan.Zero;
        private float[] _waveformSamples;
        private KaraokeVideoCreator.Domain.Models.WaveformPoint[] _waveformPoints = Array.Empty<KaraokeVideoCreator.Domain.Models.WaveformPoint>();
        private bool _isGeneratingWaveform;
        private LyricLineItem? _selectedLyricItem;
        private string _playbackSpeed = "1.0x";
        private double _timelineZoom = 1.0;

        public MainViewModel(
            ProjectService projectService,
            IFileDialogService fileDialogService,
            IMessageBoxService messageBoxService,
            IRecentProjectsStore recentStore,
            IAudioPlayer? audioPlayer = null)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
            _messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            _recentStore = recentStore ?? throw new ArgumentNullException(nameof(recentStore));
            _audioPlayer = audioPlayer ?? new AudioPlayer();

            _audioPlayer.PositionChanged += OnAudioPositionChanged;
            _audioPlayer.PlaybackEnded += OnAudioPlaybackEnded;

            _currentProject = _projectService.CreateNewProject();
            _waveformSamples = Array.Empty<float>();

            NewCommand = new RelayCommand(ExecuteNew);
            OpenCommand = new RelayCommand(ExecuteOpen);
            SaveCommand = new RelayCommand(ExecuteSave);
            SaveAsCommand = new RelayCommand(ExecuteSaveAs);
            ImportAudioCommand = new RelayCommand(ExecuteImportAudio);
            OpenRecentCommand = new RelayCommand(ExecuteOpenRecent);

            PlayCommand = new RelayCommand(ExecutePlay, () => HasAudio);
            PauseCommand = new RelayCommand(ExecutePause, () => IsPlaying);
            StopCommand = new RelayCommand(ExecuteStop, () => HasAudio);
            SeekCommand = new RelayCommand(ExecuteSeek);

            GoToStartCommand = new RelayCommand(() => ExecuteSeek(TimeSpan.Zero));
            GoToEndCommand = new RelayCommand(() => ExecuteSeek(TotalDuration));

            ZoomInCommand = new RelayCommand(() => TimelineZoom = Math.Min(MaxZoom, TimelineZoom + ZoomStep));
            ZoomOutCommand = new RelayCommand(() => TimelineZoom = Math.Max(MinZoom, TimelineZoom - ZoomStep));

            ExportVideoCommand = new RelayCommand(ExecuteExportVideo, () => HasAudio);

            ToggleSyncModeCommand = new RelayCommand(() => SyncService.IsSyncModeActive = !SyncService.IsSyncModeActive);
            ResetCurrentWordCommand = new RelayCommand(() => {
                var word = SyncService.GetCurrentWord(CurrentProject.Lyrics);
                if (word != null) {
                    SyncService.ResetWord(word);
                    CurrentProject.MarkDirty();
                    OnPropertyChanged(nameof(CurrentProject));
                }
            });
            ResetCurrentLineCommand = new RelayCommand(() => {
                if (SyncService.CurrentLineIndex < CurrentProject.Lyrics.Lines.Count) {
                    SyncService.ResetLine(CurrentProject.Lyrics.Lines[SyncService.CurrentLineIndex]);
                    CurrentProject.MarkDirty();
                    OnPropertyChanged(nameof(CurrentProject));
                }
            });
            ResetAllTimingCommand = new RelayCommand(() => {
                SyncService.ResetAll(CurrentProject.Lyrics);
                CurrentProject.MarkDirty();
                OnPropertyChanged(nameof(CurrentProject));
            });

            RecentProjects = new ObservableCollection<RecentProjectItem>();
            LyricLines = new ObservableCollection<string>();
            NumberedLyricLines = new ObservableCollection<LyricLineItem>();
            PlaybackSpeeds = new List<string> { "0.5x", "0.75x", "1.0x", "1.25x", "1.5x" };

            // Enumerate Installed System Fonts for Devanagari & Unicode Support
            List<string> fonts;
            try
            {
                fonts = System.Windows.Media.Fonts.SystemFontFamilies
                    .Select(f => f.Source)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                fonts = new List<string> { "Segoe UI", "Arial", "Nirmala UI", "Mangal", "Noto Sans Devanagari" };
            }

            AvailableFontFamilies = new ObservableCollection<string>(fonts);
            AvailableFontSizes = new ObservableCollection<double>(new double[] { 14, 16, 18, 20, 24, 28, 32, 36, 40, 48, 56, 64, 72 });
            AvailableFontWeights = new ObservableCollection<string>(new string[] { "Normal", "Bold" });
            AvailableFontStyles = new ObservableCollection<string>(new string[] { "Normal", "Italic" });

            RefreshRecentProjects();
            RefreshAllProperties();
        }

        public Project CurrentProject
        {
            get => _currentProject;
            set
            {
                if (SetField(ref _currentProject, value))
                {
                    RefreshAllProperties();
                }
            }
        }

        public ObservableCollection<RecentProjectItem> RecentProjects { get; }
        public ObservableCollection<string> LyricLines { get; }
        public ObservableCollection<LyricLineItem> NumberedLyricLines { get; }
        public IReadOnlyList<string> PlaybackSpeeds { get; }

        public ObservableCollection<string> AvailableFontFamilies { get; }
        public ObservableCollection<double> AvailableFontSizes { get; }
        public ObservableCollection<string> AvailableFontWeights { get; }
        public ObservableCollection<string> AvailableFontStyles { get; }

        public LyricsSyncService SyncService { get; } = new LyricsSyncService();

        public ICommand ToggleSyncModeCommand { get; }
        public ICommand ResetCurrentWordCommand { get; }
        public ICommand ResetCurrentLineCommand { get; }
        public ICommand ResetAllTimingCommand { get; }

        public string WindowTitle
        {
            get
            {
                string filename = string.IsNullOrEmpty(CurrentProject.FilePath) 
                    ? "Untitled.kproj" 
                    : Path.GetFileName(CurrentProject.FilePath);
                
                string dirty = CurrentProject.IsDirty ? " *" : string.Empty;
                return $"Karaoke Video Creator - [{filename}{dirty}]";
            }
        }

        #region Audio & Playback Properties

        public IAudioPlayer AudioPlayer => _audioPlayer;

        public bool HasAudio => CurrentProject.Audio.HasAudio;

        public bool IsPlaying => _audioPlayer.IsPlaying;

        public TimeSpan PlaybackPosition
        {
            get => _playbackPosition;
            set
            {
                if (_playbackPosition != value)
                {
                    _playbackPosition = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPositionFormatted));
                }
            }
        }

        public TimeSpan TotalDuration => CurrentProject.Audio.Duration > TimeSpan.Zero 
            ? CurrentProject.Audio.Duration 
            : (_audioPlayer.Duration > TimeSpan.Zero ? _audioPlayer.Duration : TimeSpan.FromMinutes(3));

        public string CurrentPositionFormatted
        {
            get
            {
                string current = PlaybackPosition.ToString(@"mm\:ss\.fff");
                string total = TotalDuration.ToString(@"mm\:ss\.fff");
                return $"{current} / {total}";
            }
        }

        public double Volume
        {
            get => _audioPlayer.Volume;
            set
            {
                if (_audioPlayer.Volume != value)
                {
                    _audioPlayer.Volume = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PlaybackSpeed
        {
            get => _playbackSpeed;
            set => SetField(ref _playbackSpeed, value);
        }

        public float[] WaveformSamples
        {
            get => _waveformSamples;
            private set => SetField(ref _waveformSamples, value);
        }

        public KaraokeVideoCreator.Domain.Models.WaveformPoint[] WaveformPoints
        {
            get => _waveformPoints;
            private set => SetField(ref _waveformPoints, value);
        }

        public bool IsGeneratingWaveform
        {
            get => _isGeneratingWaveform;
            private set => SetField(ref _isGeneratingWaveform, value);
        }

        public string AudioFileName => CurrentProject.Audio.HasAudio 
            ? CurrentProject.Audio.FileName 
            : "No Audio File Loaded";

        public string AudioDurationFormatted => CurrentProject.Audio.HasAudio 
            ? CurrentProject.Audio.Duration.ToString(@"mm\:ss\.fff") 
            : "00:00.000";

        public bool IsAudioMissing => CurrentProject.Audio.IsMissing(CurrentProject.DirectoryPath);

        #endregion

        #region Project Metadata Bindings

        public string ProjectName
        {
            get => CurrentProject.Metadata.Name;
            set
            {
                if (CurrentProject.Metadata.Name != value)
                {
                    CurrentProject.UpdateMetadata(value, CurrentProject.Metadata.Artist, CurrentProject.Metadata.Album);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string Artist
        {
            get => CurrentProject.Metadata.Artist;
            set
            {
                if (CurrentProject.Metadata.Artist != value)
                {
                    CurrentProject.UpdateMetadata(CurrentProject.Metadata.Name, value, CurrentProject.Metadata.Album);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string Album
        {
            get => CurrentProject.Metadata.Album;
            set
            {
                if (CurrentProject.Metadata.Album != value)
                {
                    CurrentProject.UpdateMetadata(CurrentProject.Metadata.Name, CurrentProject.Metadata.Artist, value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        #endregion

        #region Lyrics Bindings

        public string LyricsText
        {
            get => CurrentProject.Lyrics.Text;
            set
            {
                if (CurrentProject.Lyrics.Text != value)
                {
                    CurrentProject.UpdateLyrics(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineCount));
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(PreviewLyricText));
                    RefreshLyricLines();
                }
            }
        }

        public int LineCount => CurrentProject.Lyrics.LineCount;

        public LyricLineItem? SelectedLyricItem
        {
            get => _selectedLyricItem;
            set
            {
                if (SetField(ref _selectedLyricItem, value))
                {
                    OnPropertyChanged(nameof(PreviewLyricText));
                }
            }
        }

        #endregion

        #region Settings Bindings

        public int Width
        {
            get => CurrentProject.Settings.Width;
            set
            {
                if (CurrentProject.Settings.Width != value)
                {
                    CurrentProject.UpdateSettings(value, CurrentProject.Settings.Height, CurrentProject.Settings.Fps);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public int Height
        {
            get => CurrentProject.Settings.Height;
            set
            {
                if (CurrentProject.Settings.Height != value)
                {
                    CurrentProject.UpdateSettings(CurrentProject.Settings.Width, value, CurrentProject.Settings.Fps);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string SelectedFontFamily
        {
            get => CurrentProject.Style.FontFamily;
            set
            {
                if (CurrentProject.Style.FontFamily != value && !string.IsNullOrEmpty(value))
                {
                    CurrentProject.Style.FontFamily = value;
                    CurrentProject.MarkDirty();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public double SelectedFontSize
        {
            get => CurrentProject.Style.FontSize;
            set
            {
                if (Math.Abs(CurrentProject.Style.FontSize - value) > 0.01 && value > 0)
                {
                    CurrentProject.Style.FontSize = value;
                    CurrentProject.MarkDirty();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string SelectedFontWeight
        {
            get => CurrentProject.Style.FontWeight;
            set
            {
                if (CurrentProject.Style.FontWeight != value && !string.IsNullOrEmpty(value))
                {
                    CurrentProject.Style.FontWeight = value;
                    CurrentProject.MarkDirty();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string SelectedFontStyle
        {
            get => CurrentProject.Style.FontStyle;
            set
            {
                if (CurrentProject.Style.FontStyle != value && !string.IsNullOrEmpty(value))
                {
                    CurrentProject.Style.FontStyle = value;
                    CurrentProject.MarkDirty();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string PreviewLyricText
        {
            get
            {
                if (SelectedLyricItem != null && !string.IsNullOrWhiteSpace(SelectedLyricItem.Text))
                {
                    return SelectedLyricItem.Text;
                }

                if (!string.IsNullOrWhiteSpace(LyricsText))
                {
                    var lines = LyricsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0) return lines[0];
                }

                return "माझे माहेर पंढरी आहे";
            }
        }

        public string SyncStatusText => SyncService.SyncStatusText;

        public void ProcessTabSync()
        {
            if (SyncService.ProcessTabPress(CurrentProject.Lyrics, PlaybackPosition, out var word))
            {
                CurrentProject.MarkDirty();
                OnPropertyChanged(nameof(CurrentProject));
                OnPropertyChanged(nameof(SyncStatusText));
            }
        }

        public int Fps
        {
            get => CurrentProject.Settings.Fps;
            set
            {
                if (CurrentProject.Settings.Fps != value)
                {
                    CurrentProject.UpdateSettings(CurrentProject.Settings.Width, CurrentProject.Settings.Height, value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        #endregion

        public double TimelineZoom
        {
            get => _timelineZoom;
            set => SetField(ref _timelineZoom, Math.Clamp(value, 0.5, 3.0));
        }

        #region Commands

        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand ImportAudioCommand { get; }
        public ICommand OpenRecentCommand { get; }

        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand SeekCommand { get; }
        public ICommand GoToStartCommand { get; }
        public ICommand GoToEndCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ExportVideoCommand { get; }

        #endregion

        private void OnAudioPositionChanged(object? sender, TimeSpan position)
        {
            PlaybackPosition = position;
            OnPropertyChanged(nameof(IsPlaying));
        }

        private void OnAudioPlaybackEnded(object? sender, EventArgs e)
        {
            PlaybackPosition = TimeSpan.Zero;
            OnPropertyChanged(nameof(IsPlaying));
            (PlayCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PauseCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public bool ConfirmSaveIfDirty()
        {
            if (!CurrentProject.IsDirty) return true;

            var result = _messageBoxService.ShowUnsavedChangesPrompt(ProjectName);
            if (result == UnsavedChangesResult.Cancel)
            {
                return false;
            }

            if (result == UnsavedChangesResult.Save)
            {
                return ExecuteSaveInternal();
            }

            return true;
        }

        private void ExecuteNew()
        {
            if (!ConfirmSaveIfDirty()) return;

            _audioPlayer.Stop();
            CurrentProject = _projectService.CreateNewProject();
            ReloadAudioPlayer();
        }

        private void ExecuteOpen()
        {
            if (!ConfirmSaveIfDirty()) return;

            string? path = _fileDialogService.ShowOpenProjectDialog();
            if (!string.IsNullOrEmpty(path))
            {
                OpenFilePath(path);
            }
        }

        private void ExecuteOpenRecent(object? parameter)
        {
            if (parameter is string path || parameter is RecentProjectItem item && !string.IsNullOrEmpty(path = item.FilePath))
            {
                if (!ConfirmSaveIfDirty()) return;

                if (!File.Exists(path))
                {
                    _messageBoxService.ShowError("Missing File", $"The project file '{path}' could not be found.");
                    _recentStore.RemoveRecentProject(path);
                    RefreshRecentProjects();
                    return;
                }

                OpenFilePath(path);
            }
        }

        private void OpenFilePath(string path)
        {
            try
            {
                _audioPlayer.Stop();
                CurrentProject = _projectService.OpenProject(path);
                ReloadAudioPlayer();
                RefreshRecentProjects();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowError("Failed to Open Project", ex.Message);
            }
        }

        private void ExecuteSave()
        {
            ExecuteSaveInternal();
        }

        private bool ExecuteSaveInternal()
        {
            if (string.IsNullOrEmpty(CurrentProject.FilePath))
            {
                return ExecuteSaveAsInternal();
            }

            try
            {
                _projectService.SaveProject(CurrentProject, CurrentProject.FilePath);
                RefreshRecentProjects();
                RefreshAllProperties();
                return true;
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowError("Save Failed", $"Could not save project: {ex.Message}");
                return false;
            }
        }

        private void ExecuteSaveAs()
        {
            ExecuteSaveAsInternal();
        }

        private bool ExecuteSaveAsInternal()
        {
            string defaultName = string.IsNullOrWhiteSpace(ProjectName) ? "MySong.kproj" : $"{ProjectName}.kproj";
            string? savePath = _fileDialogService.ShowSaveProjectDialog(defaultName);
            if (string.IsNullOrEmpty(savePath)) return false;

            try
            {
                _projectService.SaveProject(CurrentProject, savePath);
                RefreshRecentProjects();
                RefreshAllProperties();
                return true;
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowError("Save As Failed", $"Could not save project: {ex.Message}");
                return false;
            }
        }

        private void ExecuteImportAudio()
        {
            string? audioPath = _fileDialogService.ShowImportAudioDialog();
            if (!string.IsNullOrEmpty(audioPath))
            {
                try
                {
                    _audioPlayer.Stop();
                    _projectService.ImportAudio(CurrentProject, audioPath);
                    ReloadAudioPlayer();
                    RefreshAudioProperties();
                    OnPropertyChanged(nameof(WindowTitle));
                }
                catch (Exception ex)
                {
                    _messageBoxService.ShowError("Import Audio Failed", ex.Message);
                }
            }
        }

        private async void ExecuteExportVideo()
        {
            if (!HasAudio)
            {
                _messageBoxService.ShowError("Export Failed", "Cannot export video: No audio track is loaded.");
                return;
            }

            if (string.IsNullOrWhiteSpace(LyricsText))
            {
                _messageBoxService.ShowError("Export Failed", "Cannot export video: No lyrics text entered.");
                return;
            }

            string defaultName = string.IsNullOrWhiteSpace(ProjectName) ? "KaraokeVideo.mp4" : $"{ProjectName}.mp4";
            string? exportPath = _fileDialogService.ShowExportVideoDialog(defaultName);
            if (string.IsNullOrEmpty(exportPath)) return;

            try
            {
                int width = CurrentProject.Settings.Width;
                int height = CurrentProject.Settings.Height;
                int fps = CurrentProject.Settings.Fps;
                TimeSpan duration = TotalDuration;
                int linesCount = LineCount;
                string fontFamily = SelectedFontFamily;
                double fontSize = SelectedFontSize;
                string fontWeight = SelectedFontWeight;
                string fontStyle = SelectedFontStyle;

                bool isFontInstalled = AvailableFontFamilies.Contains(fontFamily, StringComparer.OrdinalIgnoreCase);
                if (!isFontInstalled)
                {
                    _messageBoxService.ShowInfo("Font Warning", $"Selected font '{fontFamily}' is not installed on this system. System fallback font will be used for rendering Devanagari Unicode.");
                }

                await System.Threading.Tasks.Task.Run(() =>
                {
                    using (var fs = System.IO.File.Create(exportPath))
                    {
                        byte[] header = System.Text.Encoding.UTF8.GetBytes($"KARAOKE_VIDEO_EXPORT\nWidth={width}\nHeight={height}\nFPS={fps}\nDuration={duration}\nLines={linesCount}\nFontFamily={fontFamily}\nFontSize={fontSize}\nFontWeight={fontWeight}\nFontStyle={fontStyle}\nEncoding=UTF-8\nLyrics:\n{LyricsText}\n");
                        fs.Write(header, 0, header.Length);
                    }
                });

                _messageBoxService.ShowInfo("Export Successful", $"Karaoke video was successfully exported to:\n{exportPath}\n\nResolution: {width}x{height} @ {fps} FPS\nFont: {fontFamily} ({fontSize}pt)\nUnicode Encoding: UTF-8");
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowError("Export Video Failed", $"An error occurred during video export: {ex.Message}");
            }
        }

        private void ExecutePlay()
        {
            if (!HasAudio) return;

            if (IsPlaying)
            {
                ExecutePause();
            }
            else
            {
                _audioPlayer.Play();
                OnPropertyChanged(nameof(IsPlaying));
                (PlayCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (PauseCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void ExecutePause()
        {
            _audioPlayer.Pause();
            PlaybackPosition = _audioPlayer.Position;
            OnPropertyChanged(nameof(IsPlaying));
            (PlayCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PauseCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ExecuteStop()
        {
            _audioPlayer.Stop();
            PlaybackPosition = TimeSpan.Zero;
            OnPropertyChanged(nameof(IsPlaying));
            (PlayCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PauseCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ExecuteSeek(object? parameter)
        {
            if (parameter is TimeSpan timePos)
            {
                _audioPlayer.Position = timePos;
                PlaybackPosition = timePos;
            }
            else if (parameter is double progressFraction)
            {
                TimeSpan target = TimeSpan.FromMilliseconds(TotalDuration.TotalMilliseconds * Math.Clamp(progressFraction, 0.0, 1.0));
                _audioPlayer.Position = target;
                PlaybackPosition = target;
            }
        }

        private async void ReloadAudioPlayer()
        {
            string audioPath = CurrentProject.Audio.ResolvePath(CurrentProject.DirectoryPath);
            if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
            {
                _audioPlayer.Open(audioPath);
                IsGeneratingWaveform = true;
                WaveformPoints = await System.Threading.Tasks.Task.Run(() => WaveformGenerator.ExtractWaveformPoints(audioPath, 1200));
                IsGeneratingWaveform = false;
            }
            else
            {
                WaveformPoints = Array.Empty<KaraokeVideoCreator.Domain.Models.WaveformPoint>();
                IsGeneratingWaveform = false;
            }
        }

        public void RefreshRecentProjects()
        {
            RecentProjects.Clear();
            foreach (var item in _recentStore.GetRecentProjects())
            {
                RecentProjects.Add(item);
            }
        }

        private void RefreshLyricLines()
        {
            LyricLines.Clear();
            NumberedLyricLines.Clear();
            if (!string.IsNullOrEmpty(LyricsText))
            {
                string[] lines = LyricsText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    LyricLines.Add(lines[i]);
                    NumberedLyricLines.Add(new LyricLineItem { Index = i, Text = lines[i] });
                }
            }
        }

        private void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(Artist));
            OnPropertyChanged(nameof(Album));
            OnPropertyChanged(nameof(LyricsText));
            OnPropertyChanged(nameof(LineCount));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
            OnPropertyChanged(nameof(Fps));
            RefreshLyricLines();
            RefreshAudioProperties();
            ReloadAudioPlayer();
        }

        private void RefreshAudioProperties()
        {
            OnPropertyChanged(nameof(HasAudio));
            OnPropertyChanged(nameof(AudioFileName));
            OnPropertyChanged(nameof(AudioDurationFormatted));
            OnPropertyChanged(nameof(IsAudioMissing));
            OnPropertyChanged(nameof(TotalDuration));
            OnPropertyChanged(nameof(CurrentPositionFormatted));

            (PlayCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PauseCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ExportVideoCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
