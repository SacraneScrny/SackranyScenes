# SackranyScenes

Additive scene loader with no concept of "system scenes" — no `SystemScene`/`UIScene` or external serializer dependency.

All scenes from Build Settings are registered on startup. At any given moment one "current" scene is loaded; `Load` additively loads the next scene and unloads the previous one (loads first, then unloads — so there is never zero loaded scenes).

```csharp
SceneLoader.Load(GameScenes.MAIN_MENU);
await SceneLoader.LoadAsync(GameScenes.LEVEL_01);
var cur = SceneLoader.CurrentScene;
```

In a build, `SceneConfig.DefaultScene` is loaded on startup (skipped in the editor).

## Codegen

`Sackrany/Scenes/Generate Scene Names` writes to `Assets/_Generated/Scenes/`:
`GameScenes` (scene name constants) + `SackranyScenes.Generated.asmdef`. It is a plain class, not `partial`, so it doesn't interfere with the asmdef.

**Config:** `SceneConfig` (`DefaultScene`).
**Dependencies:** `SackranyConfig`, UniTask. **Editor:** `SackranyScenes.Editor`.
