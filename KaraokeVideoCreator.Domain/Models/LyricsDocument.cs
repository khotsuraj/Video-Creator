using System;
using System.Collections.Generic;
using System.Linq;

namespace KaraokeVideoCreator.Domain.Models
{
    public class LyricsDocument
    {
        private string _text = string.Empty;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    SyncLinesWithText();
                }
            }
        }

        public List<LyricsLine> Lines { get; set; } = new List<LyricsLine>();

        public int LineCount => Lines.Count;

        public void SyncLinesWithText()
        {
            var rawLines = string.IsNullOrEmpty(_text)
                ? Array.Empty<string>()
                : _text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var newLines = new List<LyricsLine>();
            for (int i = 0; i < rawLines.Length; i++)
            {
                string rawText = rawLines[i];
                LyricsLine line;
                if (i < Lines.Count && Lines[i].Text == rawText)
                {
                    line = Lines[i];
                    line.Order = i;
                }
                else
                {
                    line = LyricsLine.FromText(rawText, i);
                }
                newLines.Add(line);
            }
            Lines = newLines;
        }

        public LyricsDocument Clone()
        {
            return new LyricsDocument
            {
                Text = _text,
                Lines = Lines.Select(l => l.Clone()).ToList()
            };
        }
    }
}
