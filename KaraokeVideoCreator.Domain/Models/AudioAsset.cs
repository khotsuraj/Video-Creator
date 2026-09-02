using System;
using System.IO;

namespace KaraokeVideoCreator.Domain.Models
{
    public class AudioAsset
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string FileName => string.IsNullOrEmpty(FilePath) ? string.Empty : Path.GetFileName(FilePath);
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public bool HasAudio => !string.IsNullOrWhiteSpace(FilePath);

        /// <summary>
        /// Dynamically resolves effective path and checks existence without throwing.
        /// </summary>
        public bool IsMissing(string? projectDirectoryPath = null)
        {
            if (!HasAudio) return false;
            string resolvedPath = ResolvePath(projectDirectoryPath);
            return !File.Exists(resolvedPath);
        }

        public string ResolvePath(string? projectDirectoryPath = null)
        {
            if (!string.IsNullOrEmpty(RelativePath) && !string.IsNullOrEmpty(projectDirectoryPath))
            {
                string relativeToProject = Path.GetFullPath(Path.Combine(projectDirectoryPath, RelativePath));
                if (File.Exists(relativeToProject))
                {
                    return relativeToProject;
                }
            }

            if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                return FilePath;
            }

            return FilePath;
        }

        public AudioAsset Clone()
        {
            return new AudioAsset
            {
                FilePath = FilePath,
                RelativePath = RelativePath,
                Duration = Duration
            };
        }
    }
}
