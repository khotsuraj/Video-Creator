using System;
using System.Text.Json;
using KaraokeVideoCreator.Domain.Exceptions;
using KaraokeVideoCreator.Domain.Models;

namespace KaraokeVideoCreator.Infrastructure.Serialization
{
    public class ProjectSerializer
    {
        public const string CurrentFormat = "KaraokeVideoCreatorProject";
        public const int CurrentVersion = 1;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public string Serialize(Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var dto = new KprojDTO
            {
                Format = CurrentFormat,
                Version = CurrentVersion,
                Project = new ProjectMetadataDTO
                {
                    Name = project.Metadata.Name,
                    Artist = project.Metadata.Artist,
                    Album = project.Metadata.Album,
                    CreatedAt = project.Metadata.CreatedAt,
                    ModifiedAt = project.Metadata.ModifiedAt
                },
                Audio = new AudioAssetDTO
                {
                    Path = project.Audio.FilePath,
                    RelativePath = project.Audio.RelativePath,
                    DurationSeconds = project.Audio.Duration.TotalSeconds
                },
                Lyrics = new LyricsDocumentDTO
                {
                    Text = project.Lyrics.Text,
                    Lines = project.Lyrics.Lines.Select(l => new LyricsLineDTO
                    {
                        Id = l.Id,
                        Order = l.Order,
                        Text = l.Text,
                        Words = l.Words.Select(w => new LyricsWordDTO
                        {
                            Id = w.Id,
                            Order = w.Order,
                            Text = w.Text,
                            StartMs = w.StartTime?.TotalMilliseconds,
                            EndMs = w.EndTime?.TotalMilliseconds
                        }).ToList()
                    }).ToList()
                },
                Settings = new ProjectSettingsDTO
                {
                    Width = project.Settings.Width,
                    Height = project.Settings.Height,
                    Fps = project.Settings.Fps
                },
                LyricStyle = new LyricStyleDTO
                {
                    FontFamily = project.Style.FontFamily,
                    FontSize = project.Style.FontSize,
                    FontWeight = project.Style.FontWeight,
                    FontStyle = project.Style.FontStyle,
                    TextColor = project.Style.TextColor,
                    Alignment = project.Style.Alignment
                }
            };

            return JsonSerializer.Serialize(dto, _jsonOptions);
        }

        public Project Deserialize(string jsonContent, string? projectFilePath = null)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new InvalidProjectFileException("Project file is empty.");
            }

            KprojDTO? dto;
            try
            {
                dto = JsonSerializer.Deserialize<KprojDTO>(jsonContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidProjectFileException("Failed to parse project file JSON.", ex);
            }

            if (dto == null)
            {
                throw new InvalidProjectFileException("Deserialized project file object is null.");
            }

            if (string.IsNullOrEmpty(dto.Format) || !dto.Format.Equals(CurrentFormat, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidProjectFileException($"Invalid project format. Expected '{CurrentFormat}' but found '{dto.Format}'.");
            }

            if (dto.Version <= 0)
            {
                throw new InvalidProjectFileException($"Invalid format version '{dto.Version}'.");
            }

            if (dto.Version > CurrentVersion)
            {
                throw new UnsupportedProjectVersionException(dto.Version, CurrentVersion);
            }

            var project = new Project
            {
                FilePath = projectFilePath,
                Metadata = new ProjectMetadata
                {
                    Name = dto.Project?.Name ?? "Untitled",
                    Artist = dto.Project?.Artist ?? string.Empty,
                    Album = dto.Project?.Album ?? string.Empty,
                    CreatedAt = (dto.Project == null || dto.Project.CreatedAt == default) ? DateTime.UtcNow : dto.Project.CreatedAt,
                    ModifiedAt = (dto.Project == null || dto.Project.ModifiedAt == default) ? DateTime.UtcNow : dto.Project.ModifiedAt
                },
                Audio = new AudioAsset
                {
                    FilePath = dto.Audio?.Path ?? string.Empty,
                    RelativePath = dto.Audio?.RelativePath ?? string.Empty,
                    Duration = TimeSpan.FromSeconds(dto.Audio?.DurationSeconds ?? 0)
                },
                Lyrics = (dto.Lyrics?.Lines != null && dto.Lyrics.Lines.Count > 0)
                    ? new LyricsDocument
                    {
                        Text = dto.Lyrics.Text ?? string.Empty,
                        Lines = dto.Lyrics.Lines.Select(l => new LyricsLine
                        {
                            Id = string.IsNullOrEmpty(l.Id) ? Guid.NewGuid().ToString() : l.Id,
                            Order = l.Order,
                            Text = l.Text ?? string.Empty,
                            Words = (l.Words ?? new List<LyricsWordDTO>()).Select(w => new LyricsWord
                            {
                                Id = string.IsNullOrEmpty(w.Id) ? Guid.NewGuid().ToString() : w.Id,
                                Order = w.Order,
                                Text = w.Text ?? string.Empty,
                                StartTime = w.StartMs.HasValue ? TimeSpan.FromMilliseconds(w.StartMs.Value) : null,
                                EndTime = w.EndMs.HasValue ? TimeSpan.FromMilliseconds(w.EndMs.Value) : null
                            }).ToList()
                        }).ToList()
                    }
                    : new LyricsDocument { Text = dto.Lyrics?.Text ?? string.Empty },
                Settings = new ProjectSettings
                {
                    Width = dto.Settings?.Width ?? 1920,
                    Height = dto.Settings?.Height ?? 1080,
                    Fps = dto.Settings?.Fps ?? 30
                },
                Style = new LyricStyle
                {
                    FontFamily = dto.LyricStyle?.FontFamily ?? "Segoe UI",
                    FontSize = dto.LyricStyle?.FontSize ?? 28.0,
                    FontWeight = dto.LyricStyle?.FontWeight ?? "Normal",
                    FontStyle = dto.LyricStyle?.FontStyle ?? "Normal",
                    TextColor = dto.LyricStyle?.TextColor ?? "#FFFFFF",
                    Alignment = dto.LyricStyle?.Alignment ?? "Center"
                }
            };

            project.MarkClean();
            return project;
        }
    }
}
