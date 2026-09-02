using System;

namespace KaraokeVideoCreator.Domain.Models
{
    public class ProjectMetadata
    {
        public string Name { get; set; } = "Untitled Song";
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        public ProjectMetadata Clone()
        {
            return new ProjectMetadata
            {
                Name = Name,
                Artist = Artist,
                Album = Album,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt
            };
        }
    }
}
