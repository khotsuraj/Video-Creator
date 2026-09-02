using System;
using System.IO;
using KaraokeVideoCreator.Domain.Models;
using KaraokeVideoCreator.Infrastructure.Storage;
using Xunit;

namespace KaraokeVideoCreator.Tests
{
    public class RegressionTests
    {
        [Fact]
        public void Phase1_RegressionTest_SaveCloseReopenFidelity()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"Regression_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            string projectFilePath = Path.Combine(tempDir, "MySong.kproj");

            string expectedLyrics = "I found a love for me\nDarling just dive right in\nAnd follow my lead";

            try
            {
                // 1. Create project with exact spec values
                var originalProject = new Project();
                originalProject.UpdateMetadata("My Song", "Test Artist", "Test Album");
                originalProject.UpdateAudio("test.mp3", TimeSpan.FromMinutes(4).Add(TimeSpan.FromSeconds(23)));
                originalProject.UpdateLyrics(expectedLyrics);
                originalProject.UpdateSettings(1920, 1080, 30);

                var repository = new ProjectRepository();

                // 2. Save project
                repository.Save(originalProject, projectFilePath);

                // 3. Simulate closing application session by letting objects dispose/scope end
                Assert.True(File.Exists(projectFilePath));

                // 4. Open project again in fresh repository/deserializer context
                var restoredProject = repository.Load(projectFilePath);

                // 5. Verify every single property is identical
                Assert.Equal("My Song", restoredProject.Metadata.Name);
                Assert.Equal("Test Artist", restoredProject.Metadata.Artist);
                Assert.Equal("Test Album", restoredProject.Metadata.Album);
                Assert.Equal("test.mp3", restoredProject.Audio.FilePath);
                Assert.Equal(expectedLyrics, restoredProject.Lyrics.Text);
                Assert.Equal(1920, restoredProject.Settings.Width);
                Assert.Equal(1080, restoredProject.Settings.Height);
                Assert.Equal(30, restoredProject.Settings.Fps);
                Assert.False(restoredProject.IsDirty);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
