# §14 Audio

| Decision | Value | Rationale |
|---|---|---|
| **Audio system access** | Game Service (`IAudioManager`) | Centralized, globally accessible, testable via fake. |
| **Asset loading** | Centralized in `LoadContent` | Screens use logical names, not file paths. |
| **Music playback API** | `IMusicManager` interface | Wraps the framework's static media player for Song-based music. Screens depend on the interface, not the concrete class. |
| **Loop timing** | Manual delay via `Update()` | Does NOT use the framework's built-in repeat flag. Instead, `Update()` detects when a song ends and counts down a configurable delay before replaying. Enables variable pause-between-loops per song. |
| **Delta-time delay** | `gameTime.ElapsedGameTime.TotalSeconds` | Loop delay countdown never assumes a fixed frame rate. |
| **Idempotent play** | Same-song calls are no-ops | Calling `PlaySong` with the name of the already-playing song does nothing, preventing accidental restarts. |
| **Music ownership** | Screen-owned (not a global service yet) | `MusicManager` is currently owned by the screen that needs it. Can be promoted to `Game.Services` later if multiple screens require music control. |
| **Pause volume dimming** | `PauseScreen` sets volume to 20% on enter, restores to 100% on exit | Keeps music audible during pause without full silence. Volume levels are named constants in `PauseScreen`. |
| **Content format** | MP3 via Content Pipeline | MP3 files processed by `Mp3Importer` + `SongProcessor`, accessed as `Song` objects via `content.Load<Song>()`. |

## Audio Classes

| Class | Description |
|---|---|
| `IMusicManager` | Music playback contract: `LoadContent`, `PlaySong` with configurable loop delay, `StopSong`, `Update`, `SetVolume`, `IsPlaying`. |
| `MusicManager` | Concrete implementation wrapping the framework's static media player for `Song` playback. Manages delayed looping via delta-time countdown in `Update()`. Explicitly disables the framework's built-in repeat flag. |

## IMusicManager API

| Member | Type | Description |
|---|---|---|
| `IsPlaying` | `bool` | Whether music is currently playing. |
| `LoadContent(content)` | `void` | Loads all music assets from the content pipeline. |
| `PlaySong(songName, loopDelaySeconds)` | `void` | Plays a song by registered name. Idempotent if the same song is already playing. `loopDelaySeconds`: 0 = immediate loop, positive = pause before replay, negative = no loop. |
| `StopSong()` | `void` | Stops the currently playing song and cancels any pending loop. |
| `Update(gameTime)` | `void` | Must be called every frame. Detects song completion and counts down the loop delay before replaying. |
| `SetVolume(volume)` | `void` | Sets music playback volume (0.0 to 1.0), clamped. |

*(Add entries as audio patterns are established — SFX triggering, ambient sound, volume hierarchy, etc.)*
