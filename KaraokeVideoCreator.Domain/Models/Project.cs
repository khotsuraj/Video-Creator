using System;
using System.IO;

namespace KaraokeVideoCreator.Domain.Models
{
    public class Project
    {
        public string? FilePath { get; set; }
        public ProjectMetadata Metadata { get; set; } = new ProjectMetadata();
        public AudioAsset Audio { get; set; } = new AudioAsset();
        public LyricsDocument Lyrics { get; set; } = new LyricsDocument();
        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public LyricStyle Style { get; set; } = new LyricStyle();

        public bool IsDirty { get; private set; }

        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Metadata.Name) ? "Untitled" : Metadata.Name;
                return IsDirty ? $"{name} *" : name;
            }
        }

        public string? DirectoryPath => string.IsNullOrEmpty(FilePath) ? null : Path.GetDirectoryName(FilePath);

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void MarkClean()
        {
            IsDirty = false;
        }

        public void UpdateMetadata(string name, string artist, string album)
        {
            if (Metadata.Name != name || Metadata.Artist != artist || Metadata.Album != album)
            {
                Metadata.Name = name;
                Metadata.Artist = artist;
                Metadata.Album = album;
                Metadata.ModifiedAt = DateTime.UtcNow;
                MarkDirty();
            }
        }

        public void UpdateAudio(string filePath, TimeSpan duration)
        {
            string relPath = string.Empty;
            if (!string.IsNullOrEmpty(FilePath) && !string.IsNullOrEmpty(filePath))
            {
                try
                {
                    string dir = Path.GetDirectoryName(FilePath)!;
                    Uri projectUri = new Uri(dir.EndsWith("/") || dir.EndsWith("\\") ? dir : dir + "/");
                    Uri audioUri = new Uri(filePath);
                    relPath = Uri.UnescapeDataString(projectUri.MakeRelativeUri(audioUri).ToString().Replace('/', Path.DirectorySeparatorChar));
                }
                catch
                {
                    relPath = string.Empty;
                }
            }

            Audio.FilePath = filePath;
            Audio.RelativePath = relPath;
            Audio.Duration = duration;
            MarkDirty();
        }

        public void UpdateLyrics(string text)
        {
            if (Lyrics.Text != text)
            {
                Lyrics.Text = text;
                MarkDirty();
            }
        }

        public void UpdateSettings(int width, int height, int fps)
        {
            if (Settings.Width != width || Settings.Height != height || Settings.Fps != fps)
            {
                Settings.Width = width;
                Settings.Height = height;
                Settings.Fps = fps;
                MarkDirty();
            }
        }

        public void UpdateLyricStyle(string fontFamily, double fontSize, string fontWeight, string fontStyle, string textColor, string alignment)
        {
            if (Style.FontFamily != fontFamily || Style.FontSize != fontSize || Style.FontWeight != fontWeight ||
                Style.FontStyle != fontStyle || Style.TextColor != textColor || Style.Alignment != alignment)
            {
                Style.FontFamily = fontFamily;
                Style.FontSize = fontSize;
                Style.FontWeight = fontWeight;
                Style.FontStyle = fontStyle;
                Style.TextColor = textColor;
                Style.Alignment = alignment;
                MarkDirty();
            }
        }
    }
}
