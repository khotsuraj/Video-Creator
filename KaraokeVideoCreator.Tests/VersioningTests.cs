using KaraokeVideoCreator.Domain.Exceptions;
using KaraokeVideoCreator.Infrastructure.Serialization;
using Xunit;

namespace KaraokeVideoCreator.Tests
{
    public class VersioningTests
    {
        private readonly ProjectSerializer _serializer = new ProjectSerializer();

        [Fact]
        public void Deserialize_ValidVersion1_Succeeds()
        {
            string validJson = @"{
                ""format"": ""KaraokeVideoCreatorProject"",
                ""version"": 1,
                ""project"": { ""name"": ""Test"" }
            }";

            var project = _serializer.Deserialize(validJson);
            Assert.NotNull(project);
            Assert.Equal("Test", project.Metadata.Name);
        }

        [Fact]
        public void Deserialize_InvalidFormat_ThrowsInvalidProjectFileException()
        {
            string invalidFormatJson = @"{
                ""format"": ""WrongFormatString"",
                ""version"": 1,
                ""project"": { ""name"": ""Test"" }
            }";

            Assert.Throws<InvalidProjectFileException>(() => _serializer.Deserialize(invalidFormatJson));
        }

        [Fact]
        public void Deserialize_FutureVersion_ThrowsUnsupportedProjectVersionException()
        {
            string futureVersionJson = @"{
                ""format"": ""KaraokeVideoCreatorProject"",
                ""version"": 99,
                ""project"": { ""name"": ""Test"" }
            }";

            var ex = Assert.Throws<UnsupportedProjectVersionException>(() => _serializer.Deserialize(futureVersionJson));
            Assert.Equal(99, ex.ProvidedVersion);
        }

        [Fact]
        public void Deserialize_InvalidVersionZero_ThrowsInvalidProjectFileException()
        {
            string zeroVersionJson = @"{
                ""format"": ""KaraokeVideoCreatorProject"",
                ""version"": 0,
                ""project"": { ""name"": ""Test"" }
            }";

            Assert.Throws<InvalidProjectFileException>(() => _serializer.Deserialize(zeroVersionJson));
        }
    }
}
