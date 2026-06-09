# SackranyScenes

Additive scene loader for Unity built on UniTask. There is no concept of
"system scenes" (no `SystemScene`/`UIScene`) and no external serializer dependency.

At any moment exactly one scene is the **current** scene. Switching to another
scene loads the next one additively and unloads the previous one — and it loads
*before* it unloads, so there is never zero loaded scenes. Switch requests are
**queued**, so rapid calls run one after another in order instead of being
dropped.

Optionally, each switch can play a **transition** (a prefab with a two-stage
animation: hide the current scene → swap → reveal the next scene).

## Dependencies

- [UniTask](https://github.com/Cysharp/UniTask)
- `SackranyConfig` (for the build-time default scene)

Assemblies: `SackranyScenes` (runtime), `SackranyScenes.Editor` (codegen),
`SackranyScenes.Generated` (the generated name constants).

## Quick start

All scenes from Build Settings are registered automatically on startup. Switch
between them by name:

```csharp
using SackranyScenes;

// Fire-and-forget
SceneLoader.Load(GameScenes.MAIN_MENU);

// Awaitable — completes when this specific switch is done
await SceneLoader.LoadAsync(GameScenes.LEVEL_01);

// The currently loaded scene
string current = SceneLoader.CurrentScene;
```

`GameScenes` is generated from Build Settings (see [Codegen](#codegen)). You can
also pass a raw scene name string; unknown names are skipped with a warning.

## Queue

A switch already in progress is never interrupted and never dropped. Calls are
queued and processed in order:

```csharp
SceneLoader.Load(GameScenes.CUTSCENE);   // runs first
SceneLoader.Load(GameScenes.LEVEL_01);   // runs after the cutscene switch finishes
```

Each `LoadAsync`/`ReloadAsync` returns a task that completes when *that* request
finishes, so awaiting it is always accurate. `SceneLoader.IsTransitioning` is
true while the queue is draining.

## Reload

Reloads the current scene. A fresh instance is loaded before the old one is
unloaded, so the scene stays continuously present (and Unity never has to unload
the last remaining scene).

```csharp
SceneLoader.Reload();                       // fire-and-forget
await SceneLoader.ReloadAsync();            // awaitable
await SceneLoader.ReloadAsync(GameTransitions.FADE); // with a transition
```

## Transitions

A transition is a prefab whose root has a component deriving from
`SceneTransition`. You write the animations; the loader only awaits the two
stages:

1. `PlayOut` — hide / cover the current scene.
2. *(scenes are swapped)*
3. `PlayIn` — reveal the next scene.

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using SackranyScenes;
using UnityEngine;

public class FadeTransition : SceneTransition
{
    [SerializeField] CanvasGroup _group;

    public override async UniTask PlayOut(CancellationToken cancellationToken = default)
    {
        // ...your fade-to-black animation here...
        await FadeTo(1f, cancellationToken);
    }

    public override async UniTask PlayIn(CancellationToken cancellationToken = default)
    {
        // ...your fade-from-black animation here...
        await FadeTo(0f, cancellationToken);
    }

    async UniTask FadeTo(float target, CancellationToken ct)
    {
        float start = _group.alpha;
        for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
        {
            _group.alpha = Mathf.Lerp(start, target, t);
            await UniTask.Yield(ct);
        }
        _group.alpha = target;
    }
}
```

The instance is spawned with `DontDestroyOnLoad` so it survives the swap, and is
destroyed after `PlayIn`. Add a `Canvas` with a high sort order to the prefab so
it covers the scenes.

### Registering transitions

Transition prefabs can live anywhere (including inside this package). They are
centralized in a single `SceneTransitionLibrary` ScriptableObject:

1. `Sackrany/Scenes/Create Transition Library` — creates the one library asset in
   a `Resources` folder inside the package.
2. Add an entry per transition: a **Name** and the **Prefab**.
3. Optionally set **Default Transition** to a name — it becomes the current
   transition on startup.

### Selecting a transition

```csharp
// Per call
SceneLoader.Load(GameScenes.LEVEL_01, GameTransitions.FADE);
SceneLoader.Load(GameScenes.LEVEL_01, null);   // explicitly no transition

// Or set the current one and omit it afterwards
SceneLoader.SetTransition(GameTransitions.FADE);
SceneLoader.Load(GameScenes.LEVEL_01);         // uses the current transition
```

`GameTransitions` constants are generated from the library. **null is always
valid and means "no transition".** If the project has no library or no
transitions at all, everything resolves to null — no errors.

## Codegen

`Sackrany/Scenes/Generate Scene Names` writes to `Assets/_Generated/Scenes/`:

- `GameScenes` — scene-name constants from Build Settings.
- `GameTransitions` — transition-name constants from the library.
- `SackranyScenes.Generated.asmdef` — created once.

Both are plain (non-`partial`) classes. Names are sanitized into valid C#
identifiers (non-identifier characters become `_`, a leading digit is prefixed);
colliding names are skipped with a warning. Generation also runs automatically on
every build (`IPreprocessBuildWithReport`).

## Startup default scene

`SceneConfig` (a `SackranyConfig` config) exposes `DefaultScene`. In a build, it
is loaded on startup. In the editor this is skipped, so play mode uses whatever
scene is open.

```csharp
// SceneConfig.DefaultScene — name of the scene to load first in a build.
```
