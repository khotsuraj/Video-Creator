using System;
using System.IO;
using KaraokeVideoCreator.Domain.Interfaces;
using KaraokeVideoCreator.Domain.Models;
using KaraokeVideoCreator.Infrastructure.Logging;

namespace KaraokeVideoCreator.Application.Services
{
    public class ProjectService
    {
        private readonly IProjectRepository _repository;
        private readonly IAudioMetadataReader _audioReader;
        private readonly IRecentProjectsStore _recentStore;

        public ProjectService(IProjectRepository repository, IAudioMetadataReader audioReader, IRecentProjectsStore recentStore)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _audioReader = audioReader ?? throw new ArgumentNullException(nameof(audioReader));
            _recentStore = recentStore ?? throw new ArgumentNullException(nameof(recentStore));
        }

        public Project CreateNewProject()
        {
            AppLogger.LogInfo("Creating new project.");
            var project = new Project
            {
                Metadata = new ProjectMetadata
                {
                    Name = "My Song",
                    Artist = string.Empty,
                    Album = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                },
                Settings = new ProjectSettings
                {
                    Width = 1920,
                    Height = 1080,
                    Fps = 30
                }
            };

            project.MarkClean();
            return project;
        }

        public Project OpenProject(string filePath)
        {
            AppLogger.LogInfo($"Opening project file: {filePath}");
            try
            {
                var project = _repository.Load(filePath);
                _recentStore.AddRecentProject(filePath, project.Metadata.Name);
                AppLogger.LogInfo($"Project '{project.Metadata.Name}' opened successfully.");
                return project;
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to open project file: {filePath}", ex);
                throw;
            }
        }

        public void SaveProject(Project project, string filePath)
        {
            AppLogger.LogInfo($"Saving project to: {filePath}");
            try
            {
                _repository.Save(project, filePath);
                _recentStore.AddRecentProject(filePath, project.Metadata.Name);
                AppLogger.LogInfo("Project saved successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Failed to save project file: {filePath}", ex);
                throw;
            }
        }

        public void ImportAudio(Project project, string audioFilePath)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            AppLogger.LogInfo($"Importing audio file: {audioFilePath}");

            var metadataResult = _audioReader.ReadAudioMetadata(audioFilePath);
            project.UpdateAudio(audioFilePath, metadataResult.Duration);
        }
    }
}
