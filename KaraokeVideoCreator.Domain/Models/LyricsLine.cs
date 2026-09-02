using System;
using System.Collections.Generic;
using System.Linq;

namespace KaraokeVideoCreator.Domain.Models
{
    public class LyricsLine
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Order { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<LyricsWord> Words { get; set; } = new List<LyricsWord>();

        public TimeSpan? StartTime => Words.FirstOrDefault(w => w.StartTime.HasValue)?.StartTime;
        public TimeSpan? EndTime => Words.LastOrDefault(w => w.EndTime.HasValue)?.EndTime;

        public bool IsSynced => Words.Count > 0 && Words.All(w => w.IsSynced);
        public bool IsPartiallySynced => Words.Any(w => w.IsSynced);

        public LyricsLine Clone()
        {
            return new LyricsLine
            {
                Id = Id,
                Order = Order,
                Text = Text,
                Words = Words.Select(w => w.Clone()).ToList()
            };
        }

        public static LyricsLine FromText(string text, int order)
        {
            var line = new LyricsLine
            {
                Order = order,
                Text = text
            };

            var rawWords = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rawWords.Length; i++)
            {
                line.Words.Add(new LyricsWord
                {
                    Order = i,
                    Text = rawWords[i]
                });
            }

            return line;
        }
    }
}
