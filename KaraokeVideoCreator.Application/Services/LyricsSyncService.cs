using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KaraokeVideoCreator.Domain.Models;

namespace KaraokeVideoCreator.Application.Services
{
    public enum SyncStepState
    {
        NotStarted,
        WordStarted,
        Completed
    }

    public class LyricsSyncService : INotifyPropertyChanged
    {
        private bool _isSyncModeActive;
        private int _currentLineIndex;
        private int _currentWordIndex;
        private SyncStepState _state = SyncStepState.NotStarted;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsSyncModeActive
        {
            get => _isSyncModeActive;
            set
            {
                if (_isSyncModeActive != value)
                {
                    _isSyncModeActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SyncStatusText));
                }
            }
        }

        public int CurrentLineIndex
        {
            get => _currentLineIndex;
            set
            {
                if (_currentLineIndex != value)
                {
                    _currentLineIndex = value;
                    _currentWordIndex = 0;
                    _state = SyncStepState.NotStarted;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SyncStatusText));
                }
            }
        }

        public int CurrentWordIndex
        {
            get => _currentWordIndex;
            set
            {
                if (_currentWordIndex != value)
                {
                    _currentWordIndex = value;
                    _state = SyncStepState.NotStarted;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SyncStatusText));
                }
            }
        }

        public SyncStepState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SyncStatusText));
                }
            }
        }

        public string SyncStatusText
        {
            get
            {
                if (!IsSyncModeActive) return "SYNC MODE: OFF";
                if (State == SyncStepState.Completed) return "SYNC COMPLETE";
                return $"SYNC MODE: ON [Line {CurrentLineIndex + 1}, Word {CurrentWordIndex + 1}]";
            }
        }

        public LyricsWord? GetCurrentWord(LyricsDocument document)
        {
            if (document == null || CurrentLineIndex < 0 || CurrentLineIndex >= document.Lines.Count)
                return null;

            var line = document.Lines[CurrentLineIndex];
            if (CurrentWordIndex < 0 || CurrentWordIndex >= line.Words.Count)
                return null;

            return line.Words[CurrentWordIndex];
        }

        public bool ProcessTabPress(LyricsDocument document, TimeSpan currentAudioPosition, out LyricsWord? updatedWord)
        {
            updatedWord = null;
            if (document == null || document.Lines.Count == 0) return false;

            if (CurrentLineIndex >= document.Lines.Count)
            {
                State = SyncStepState.Completed;
                return false;
            }

            var line = document.Lines[CurrentLineIndex];
            if (line.Words.Count == 0)
            {
                AdvanceToNextLine(document);
                return false;
            }

            if (CurrentWordIndex >= line.Words.Count)
            {
                if (!AdvanceToNextLine(document))
                {
                    State = SyncStepState.Completed;
                    return false;
                }
                line = document.Lines[CurrentLineIndex];
            }

            var word = line.Words[CurrentWordIndex];
            updatedWord = word;

            if (State == SyncStepState.NotStarted)
            {
                word.StartTime = currentAudioPosition;
                State = SyncStepState.WordStarted;
                OnPropertyChanged(nameof(SyncStatusText));
                return true;
            }
            else if (State == SyncStepState.WordStarted)
            {
                word.EndTime = currentAudioPosition;

                if (CurrentWordIndex + 1 < line.Words.Count)
                {
                    CurrentWordIndex++;
                    var nextWord = line.Words[CurrentWordIndex];
                    nextWord.StartTime = currentAudioPosition;
                    State = SyncStepState.WordStarted;
                }
                else
                {
                    if (!AdvanceToNextLine(document))
                    {
                        State = SyncStepState.Completed;
                    }
                    else
                    {
                        var nextLine = document.Lines[CurrentLineIndex];
                        if (nextLine.Words.Count > 0)
                        {
                            nextLine.Words[0].StartTime = currentAudioPosition;
                            State = SyncStepState.WordStarted;
                        }
                    }
                }
                OnPropertyChanged(nameof(SyncStatusText));
                return true;
            }

            return false;
        }

        private bool AdvanceToNextLine(LyricsDocument document)
        {
            if (CurrentLineIndex + 1 < document.Lines.Count)
            {
                CurrentLineIndex++;
                CurrentWordIndex = 0;
                State = SyncStepState.NotStarted;
                return true;
            }
            State = SyncStepState.Completed;
            return false;
        }

        public void StepPrevious(LyricsDocument document)
        {
            if (document == null || document.Lines.Count == 0) return;

            if (CurrentWordIndex > 0)
            {
                CurrentWordIndex--;
            }
            else if (CurrentLineIndex > 0)
            {
                CurrentLineIndex--;
                var prevLine = document.Lines[CurrentLineIndex];
                CurrentWordIndex = Math.Max(0, prevLine.Words.Count - 1);
            }
            State = SyncStepState.NotStarted;
        }

        public void ResetWord(LyricsWord word)
        {
            if (word == null) return;
            word.StartTime = null;
            word.EndTime = null;
            State = SyncStepState.NotStarted;
            OnPropertyChanged(nameof(SyncStatusText));
        }

        public void ResetLine(LyricsLine line)
        {
            if (line == null) return;
            foreach (var w in line.Words)
            {
                w.StartTime = null;
                w.EndTime = null;
            }
            State = SyncStepState.NotStarted;
            OnPropertyChanged(nameof(SyncStatusText));
        }

        public void ResetAll(LyricsDocument document)
        {
            if (document == null) return;
            foreach (var line in document.Lines)
            {
                foreach (var w in line.Words)
                {
                    w.StartTime = null;
                    w.EndTime = null;
                }
            }
            CurrentLineIndex = 0;
            CurrentWordIndex = 0;
            State = SyncStepState.NotStarted;
            OnPropertyChanged(nameof(SyncStatusText));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
