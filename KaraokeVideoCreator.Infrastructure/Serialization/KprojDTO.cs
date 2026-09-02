using System;
using System.Text.Json.Serialization;

namespace KaraokeVideoCreator.Infrastructure.Serialization
{
    public class KprojDTO
    {
        [JsonPropertyName("format")]
        public string Format { get; set; } = "KaraokeVideoCreatorProject";

        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("project")]
        public ProjectMetadataDTO Project { get; set; } = new ProjectMetadataDTO();

        [JsonPropertyName("audio")]
        public AudioAssetDTO Audio { get; set; } = new AudioAssetDTO();

        [JsonPropertyName("lyrics")]
        public LyricsDocumentDTO Lyrics { get; set; } = new LyricsDocumentDTO();

        [JsonPropertyName("settings")]
        public ProjectSettingsDTO Settings { get; set; } = new ProjectSettingsDTO();

        [JsonPropertyName("lyricStyle")]
        public LyricStyleDTO LyricStyle { get; set; } = new LyricStyleDTO();
    }

    public class ProjectMetadataDTO
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("artist")]
        public string Artist { get; set; } = string.Empty;

        [JsonPropertyName("album")]
        public string Album { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("modifiedAt")]
        public DateTime ModifiedAt { get; set; }
    }

    public class AudioAssetDTO
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("relativePath")]
        public string RelativePath { get; set; } = string.Empty;

        [JsonPropertyName("durationSeconds")]
        public double DurationSeconds { get; set; }
    }

    public class LyricsDocumentDTO
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("lines")]
        public List<LyricsLineDTO> Lines { get; set; } = new List<LyricsLineDTO>();
    }

    public class LyricsLineDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("words")]
        public List<LyricsWordDTO> Words { get; set; } = new List<LyricsWordDTO>();
    }

    public class LyricsWordDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("startMs")]
        public double? StartMs { get; set; }

        [JsonPropertyName("endMs")]
        public double? EndMs { get; set; }
    }

    public class ProjectSettingsDTO
    {
        [JsonPropertyName("width")]
        public int Width { get; set; } = 1920;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 1080;

        [JsonPropertyName("fps")]
        public int Fps { get; set; } = 30;
    }

    public class LyricStyleDTO
    {
        [JsonPropertyName("fontFamily")]
        public string FontFamily { get; set; } = "Segoe UI";

        [JsonPropertyName("fontSize")]
        public double FontSize { get; set; } = 28.0;

        [JsonPropertyName("fontWeight")]
        public string FontWeight { get; set; } = "Normal";

        [JsonPropertyName("fontStyle")]
        public string FontStyle { get; set; } = "Normal";

        [JsonPropertyName("textColor")]
        public string TextColor { get; set; } = "#FFFFFF";

        [JsonPropertyName("alignment")]
        public string Alignment { get; set; } = "Center";
    }
}
