# Karaoke Video Creator - Developer Documentation (Phase 1)

## 1. Architecture Overview

Karaoke Video Creator is built using a clean, layered architecture separating domain logic, application orchestration, infrastructure services, and UI presentation:

```
KaraokeVideoCreator/
├── KaraokeVideoCreator.Domain/          # Core Domain Aggregate & Interfaces
├── KaraokeVideoCreator.Infrastructure/  # Serialization, File IO, Audio Reader, Settings
├── KaraokeVideoCreator.Application/     # Services, MVVM ViewModels, Commands
├── KaraokeVideoCreator.UI/              # WPF Desktop UI Views & Dialog Services
└── KaraokeVideoCreator.Tests/           # xUnit Unit & Integration Test Suite
```

## 2. Project Domain Model

The central aggregate root is `Project`:
- `ProjectMetadata`: `Name`, `Artist`, `Album`, `CreatedAt`, `ModifiedAt`.
- `AudioAsset`: `FilePath`, `RelativePath`, `Duration`, `FileName`, `HasAudio`, `IsMissing()`.
- `LyricsDocument`: `Text`, `LineCount` (Extensible for timing & word tokens in Phase 2+).
- `ProjectSettings`: `Width` (1920), `Height` (1080), `Fps` (30).
- `IsDirty`: Tracks unsaved changes across metadata, lyrics, settings, and audio modifications.

## 3. `.kproj` Format (v1)

Projects are serialized to JSON with `.kproj` file extension. The format is explicitly versioned and decoupled from domain models via Data Transfer Objects (`KprojDTO`):

```json
{
  "format": "KaraokeVideoCreatorProject",
  "version": 1,
  "project": {
    "name": "My Song",
    "artist": "Test Artist",
    "album": "Test Album",
    "createdAt": "2026-09-01T18:00:00Z",
    "modifiedAt": "2026-09-01T18:05:00Z"
  },
  "audio": {
    "path": "C:/Projects/MySong/audio/perfect.mp3",
    "relativePath": "audio/perfect.mp3",
    "durationSeconds": 263.0
  },
  "lyrics": {
    "text": "I found a love for me\nDarling just dive right in\nAnd follow my lead"
  },
  "settings": {
    "width": 1920,
    "height": 1080,
    "fps": 30
  }
}
```

## 4. Save/Load Flow & Atomic Operations

- **Atomic Save Strategy**: Saves write to `project.kproj.tmp`. Upon successful write and serialization, `File.Replace` safely overwrites `project.kproj`. If write fails, original `.kproj` remains completely untouched.
- **Load Strategy**: Validates format identity (`KaraokeVideoCreatorProject`), version compatibility (v1), JSON structure integrity, resolves co-located audio paths, and instantiates clean `Project` aggregate without throwing raw system unhandled exceptions.

## 5. Versioning & Migration Strategy

- Deserializer validates `Format` and `Version`.
- If `version > 1`, throws `UnsupportedProjectVersionException` so older app instances cannot corrupt newer schema formats.
- Prepares extension boundary in `Infrastructure/Serialization/Migrations/` for schema transformation pipelines when Phase 2+ introduces breaking changes.

## 6. Media Path Strategy

- Stores both `path` (absolute) and `relativePath` (calculated relative to `.kproj` directory).
- `AudioAsset.IsMissing()` checks resolution dynamically. If audio is co-located or moved within project directory, relative path resolves first.
- Missing media displays a clear UI alert banner without crashing the application.

## 7. Testing Strategy

The xUnit test suite (`KaraokeVideoCreator.Tests`) verifies:
- Domain model dirty state management.
- `.kproj` JSON serialization / deserialization (Unicode, special chars, long strings).
- Format and versioning exception handling.
- Audio asset resolution and missing media handling.
- Atomic file save operations.
- Recent projects manager (MRU tracking, limit 10).
- End-to-end regression test (verifies save -> close -> reopen state fidelity).

## 8. Extending for Phase 2

In Phase 2+, `LyricsDocument` can be extended with `IReadOnlyList<LyricLine>` containing `LyricWord` timestamp objects without breaking Phase 1 raw `Text` serialization:
- Phase 1 schema reads `lyrics.text`.
- Phase 2 schema can add `lyrics.lines` with timestamp array while keeping `text` as fallback.
