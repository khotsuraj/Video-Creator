using System;
using System.IO;
using KaraokeVideoCreator.Domain.Models;
using KaraokeVideoCreator.Infrastructure.Serialization;
using Xunit;

namespace KaraokeVideoCreator.Tests
{
    public class SerializationTests
    {
        private readonly ProjectSerializer _serializer = new ProjectSerializer();

        [Fact]
        public void SerializeAndDeserialize_PreservesAllValues()
        {
            var original = new Project
            {
                Metadata = new ProjectMetadata
                {
                    Name = "Complex Title",
                    Artist = "Complex Artist",
                    Album = "Complex Album"
                },
                Audio = new AudioAsset
                {
                    FilePath = @"C:\Music\song.mp3",
                    RelativePath = @"song.mp3",
                    Duration = TimeSpan.FromSeconds(214.5)
                },
                Lyrics = new LyricsDocument
                {
                    Text = "First line\nSecond line"
                },
                Settings = new ProjectSettings
                {
                    Width = 3840,
                    Height = 2160,
                    Fps = 60
                }
            };

            string json = _serializer.Serialize(original);
            Project restored = _serializer.Deserialize(json);

            Assert.Equal(original.Metadata.Name, restored.Metadata.Name);
            Assert.Equal(original.Metadata.Artist, restored.Metadata.Artist);
            Assert.Equal(original.Metadata.Album, restored.Metadata.Album);
            Assert.Equal(original.Audio.FilePath, restored.Audio.FilePath);
            Assert.Equal(original.Audio.Duration.TotalSeconds, restored.Audio.Duration.TotalSeconds);
            Assert.Equal(original.Lyrics.Text, restored.Lyrics.Text);
            Assert.Equal(original.Settings.Width, restored.Settings.Width);
            Assert.Equal(original.Settings.Height, restored.Settings.Height);
            Assert.Equal(original.Settings.Fps, restored.Settings.Fps);
            Assert.False(restored.IsDirty);
        }

        [Theory]
        [InlineData("🎤 Karaoke Song - 🎵 日本語歌詞 & Special #$@! Characters!")]
        [InlineData("")]
        [InlineData("Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nVery long lyrics content...")]
        public void SerializeAndDeserialize_HandlesEdgeCaseLyrics(string lyricsText)
        {
            var project = new Project();
            project.UpdateLyrics(lyricsText);

            string json = _serializer.Serialize(project);
            Project restored = _serializer.Deserialize(json);

            Assert.Equal(lyricsText, restored.Lyrics.Text);
        }
    }
}
