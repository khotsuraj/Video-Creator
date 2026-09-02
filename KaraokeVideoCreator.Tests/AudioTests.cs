using System;
using System.IO;
using KaraokeVideoCreator.Domain.Models;
using KaraokeVideoCreator.Infrastructure.Audio;
using Xunit;

namespace KaraokeVideoCreator.Tests
{
    public class AudioTests
    {
        [Fact]
        public void AudioAsset_NonExistentFile_IdentifiesMissingMediaWithoutCrashing()
        {
            var audio = new AudioAsset
            {
                FilePath = @"C:\NonExistentDirectory\MissingAudio.mp3",
                Duration = TimeSpan.FromMinutes(3)
            };

            Assert.True(audio.HasAudio);
            Assert.True(audio.IsMissing());
            Assert.Equal("MissingAudio.mp3", audio.FileName);
        }

        [Fact]
        public void AudioMetadataReader_NonExistentFile_ReturnsFailureResultGracefully()
        {
            var reader = new AudioMetadataReader();
            var result = reader.ReadAudioMetadata(@"C:\InvalidFolder\NoAudioFile.wav");

            Assert.False(result.Success);
            Assert.Equal(TimeSpan.Zero, result.Duration);
            Assert.NotEmpty(result.ErrorMessage);
        }
    }
}
