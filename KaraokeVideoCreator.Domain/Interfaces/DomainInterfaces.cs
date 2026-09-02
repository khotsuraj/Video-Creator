using System;
using System.Collections.Generic;
using KaraokeVideoCreator.Domain.Models;

namespace KaraokeVideoCreator.Domain.Interfaces
{
    public interface IProjectRepository
    {
        Project Load(string filePath);
        void Save(Project project, string filePath);
    }

    public struct AudioMetadataResult
    {
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; }
    }

    public interface IAudioMetadataReader
    {
        AudioMetadataResult ReadAudioMetadata(string filePath);
    }

    public class RecentProjectItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; } = DateTime.UtcNow;
    }

    public interface IRecentProjectsStore
    {
        IReadOnlyList<RecentProjectItem> GetRecentProjects();
        void AddRecentProject(string filePath, string name);
        void RemoveRecentProject(string filePath);
    }
}
