using System;
using System.IO;
using KaraokeVideoCreator.Domain.Interfaces;
using KaraokeVideoCreator.Domain.Models;
using KaraokeVideoCreator.Infrastructure.Serialization;
using KaraokeVideoCreator.Infrastructure.Storage;

namespace KaraokeVideoCreator.Infrastructure.Storage
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ProjectSerializer _serializer = new ProjectSerializer();

        public Project Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Project file not found: '{filePath}'", filePath);
            }

            string content = File.ReadAllText(filePath);
            Project project = _serializer.Deserialize(content, filePath);
            return project;
        }

        public void Save(Project project, string filePath)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            project.FilePath = filePath;
            project.Metadata.ModifiedAt = DateTime.UtcNow;

            string json = _serializer.Serialize(project);
            AtomicFileWriter.WriteAllTextAtomic(filePath, json);

            project.MarkClean();
        }
    }
}
