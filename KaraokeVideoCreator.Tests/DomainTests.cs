using System;
using KaraokeVideoCreator.Domain.Models;
using Xunit;

namespace KaraokeVideoCreator.Tests
{
    public class DomainTests
    {
        [Fact]
        public void CreateEmptyProject_HasDefaultSettingsAndCleanState()
        {
            var project = new Project();
            Assert.Equal("Untitled Song", project.Metadata.Name);
            Assert.Equal(1920, project.Settings.Width);
            Assert.Equal(1080, project.Settings.Height);
            Assert.Equal(30, project.Settings.Fps);
            Assert.False(project.IsDirty);
        }

        [Fact]
        public void CreateProjectWithMetadata_PropertiesAssignedCorrectly()
        {
            var project = new Project
            {
                Metadata = new ProjectMetadata
                {
                    Name = "Test Song",
                    Artist = "Test Artist",
                    Album = "Test Album"
                }
            };

            Assert.Equal("Test Song", project.Metadata.Name);
            Assert.Equal("Test Artist", project.Metadata.Artist);
            Assert.Equal("Test Album", project.Metadata.Album);
        }

        [Fact]
        public void ModifyProject_SetsDirtyState()
        {
            var project = new Project();
            project.MarkClean();
            Assert.False(project.IsDirty);

            project.UpdateMetadata("New Title", "New Artist", "New Album");
            Assert.True(project.IsDirty);

            project.MarkClean();
            Assert.False(project.IsDirty);

            project.UpdateLyrics("Some lyrics text");
            Assert.True(project.IsDirty);

            project.MarkClean();
            project.UpdateSettings(1280, 720, 60);
            Assert.True(project.IsDirty);
        }

        [Fact]
        public void LyricsDocument_LineCount_CalculatesAccurately()
        {
            var lyrics = new LyricsDocument { Text = "Line 1\nLine 2\r\nLine 3" };
            Assert.Equal(3, lyrics.LineCount);

            var emptyLyrics = new LyricsDocument { Text = "" };
            Assert.Equal(0, emptyLyrics.LineCount);
        }
    }
}
