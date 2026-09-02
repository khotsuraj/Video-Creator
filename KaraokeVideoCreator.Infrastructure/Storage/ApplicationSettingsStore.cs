using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using KaraokeVideoCreator.Domain.Interfaces;

namespace KaraokeVideoCreator.Infrastructure.Storage
{
    public class ApplicationSettingsStore : IRecentProjectsStore
    {
        private const int MaxRecentProjects = 10;
        private readonly string _settingsFilePath;

        public ApplicationSettingsStore(string? customSettingsFilePath = null)
        {
            if (!string.IsNullOrEmpty(customSettingsFilePath))
            {
                _settingsFilePath = customSettingsFilePath;
            }
            else
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string folder = Path.Combine(appData, "KaraokeVideoCreator");
                Directory.CreateDirectory(folder);
                _settingsFilePath = Path.Combine(folder, "recent_projects.json");
            }
        }

        public IReadOnlyList<RecentProjectItem> GetRecentProjects()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new List<RecentProjectItem>();
            }

            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                var list = JsonSerializer.Deserialize<List<RecentProjectItem>>(json);
                return list?.OrderByDescending(x => x.LastOpened).ToList() ?? new List<RecentProjectItem>();
            }
            catch
            {
                return new List<RecentProjectItem>();
            }
        }

        public void AddRecentProject(string filePath, string name)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var current = GetRecentProjects().ToList();
            current.RemoveAll(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            current.Insert(0, new RecentProjectItem
            {
                FilePath = filePath,
                Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(filePath) : name,
                LastOpened = DateTime.UtcNow
            });

            if (current.Count > MaxRecentProjects)
            {
                current = current.Take(MaxRecentProjects).ToList();
            }

            SaveRecentProjects(current);
        }

        public void RemoveRecentProject(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var current = GetRecentProjects().ToList();
            int removed = current.RemoveAll(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            if (removed > 0)
            {
                SaveRecentProjects(current);
            }
        }

        private void SaveRecentProjects(List<RecentProjectItem> items)
        {
            try
            {
                string directory = Path.GetDirectoryName(_settingsFilePath)!;
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                AtomicFileWriter.WriteAllTextAtomic(_settingsFilePath, json);
            }
            catch
            {
                // Non-critical background settings save exception swallowed cleanly
            }
        }
    }
}
