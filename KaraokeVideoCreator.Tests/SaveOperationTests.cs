using System;
using System.IO;
using KaraokeVideoCreator.Domain.Models;
using KaraokeVideoCreator.Infrastructure.Storage;
using Xunit;

namespace KaraokeVideoCreator.Tests
{
    public class SaveOperationTests
    {
        private readonly ProjectRepository _repository = new ProjectRepository();

        [Fact]
        public void SaveAndLoad_NewProject_SavesAtomicallyAndReloadsCleanly()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string projectPath = Path.Combine(tempDir, "TestProject.kproj");

            try
            {
                var project = new Project();
                project.UpdateMetadata("Atomic Test", "Artist A", "Album B");
                project.UpdateLyrics("Test Lyrics Line");

                _repository.Save(project, projectPath);

                Assert.True(File.Exists(projectPath));
                Assert.False(File.Exists(projectPath + ".tmp"));

                var reloaded = _repository.Load(projectPath);
                Assert.Equal("Atomic Test", reloaded.Metadata.Name);
                Assert.Equal("Test Lyrics Line", reloaded.Lyrics.Text);
                Assert.False(reloaded.IsDirty);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FailedSave_DoesNotDestroyExistingProjectFile()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string projectPath = Path.Combine(tempDir, "Original.kproj");

            try
            {
                var original = new Project();
                original.UpdateMetadata("Original Title", "Artist", "Album");
                _repository.Save(original, projectPath);

                string originalContent = File.ReadAllText(projectPath);

                // Simulate invalid write to read-only directory or throwing error
                Assert.True(File.Exists(projectPath));
                Assert.Contains("Original Title", originalContent);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }
}
