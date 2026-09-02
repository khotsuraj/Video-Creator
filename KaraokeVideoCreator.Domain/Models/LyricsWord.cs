using System;

namespace KaraokeVideoCreator.Domain.Models
{
    public class LyricsWord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Order { get; set; }
        public string Text { get; set; } = string.Empty;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public bool IsSynced => StartTime.HasValue && EndTime.HasValue;

        public TimeSpan? Duration => (StartTime.HasValue && EndTime.HasValue) 
            ? EndTime.Value - StartTime.Value 
            : null;

        public LyricsWord Clone()
        {
            return new LyricsWord
            {
                Id = Id,
                Order = Order,
                Text = Text,
                StartTime = StartTime,
                EndTime = EndTime
            };
        }
    }
}
