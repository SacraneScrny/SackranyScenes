# SackranyScenes

Чистый аддитивный загрузчик сцен. **Без концепции «системных сцен»** — никаких
`SystemScene`/`UIScene` и зависимости от внешнего сериализатора.

Все сцены из Build Settings регистрируются на старте. В каждый момент загружена одна
«текущая» сцена; `Load` аддитивно подгружает новую и выгружает предыдущую (сначала
грузит, потом выгружает — чтобы никогда не остаться без сцены).

```csharp
SceneLoader.Load(GameScenes.MAIN_MENU);
await SceneLoader.LoadAsync(GameScenes.LEVEL_01);
var cur = SceneLoader.CurrentScene;
```

В билде на старте грузится `SceneConfig.DefaultScene` (в редакторе — нет).

## Кодген

`Sackrany/Scenes/Generate Scene Names` пишет в `Assets/_Generated/Scenes/`:
`GameScenes` (константы имён сцен) + `SackranyScenes.Generated.asmdef`. Это обычный
класс, а не `partial`, поэтому asmdef не ломается.

**Конфиг:** `SceneConfig` (`DefaultScene`).
**Зависимости:** `SackranyConfig`, UniTask. **Editor:** генератор `SackranyScenes.Editor`.
