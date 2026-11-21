# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity `6000.3.19f1` 2D detective/investigation game (URP), targeting **WebGL on itch.io**. The player explores scenes, talks to NPCs whose replies come from an external LLM/RAG backend, collects evidence into a notebook, and finally accuses a culprit. Gameplay content and design docs are partly in Vietnamese.

Key packages: Addressables, Input System, UniTask (all async code uses `UniTask`, not `Task`/coroutines), Cinemachine, Timeline, TextMesh Pro, Tri Inspector. All runtime scripts live in the default `Assembly-CSharp` (no asmdefs); editor tools are in `Assets/Editor/`.

Reference docs in the repo root:
- `HTTP-Stream-Guide.md` – LLM SSE streaming contract and history compaction.
- `PROJECT-DATA-MODEL.md` – save-data model (PlayerPrefs), authored ScriptableObject data, key invariants.
- `RainsweptScene-Investigation-Design.md` and `RainsweptScene-Characters/*.md` – story, evidence and NPC knowledge design.
- `.devtool/features/` – feature/bug notes (`done/` holds finished ones).

## Build, run, verify

- There is no CLI build or automated test suite. Work is verified in the Unity Editor: open `Assets/Scenes/Bootstrapper.unity` and press Play.
- Compile-check C# from the shell (uses Unity-generated csproj files, requires `Library/` to exist):
  ```
  dotnet build Assembly-CSharp.csproj
  dotnet build Assembly-CSharp-Editor.csproj
  ```
  A clean compile does **not** validate serialized Inspector references in scenes/prefabs/SOs.
- Editor helpers: `Tools/Developer Tools`, `Tools/Developer/Scene Switcher`, `Assets/Export/To JSON` (ScriptableObject → JSON).
- Web builds must be tested in a browser (HTTPS/CORS, PlayerPrefs/IndexedDB persistence, Addressables loading, browser teardown).

## Architecture

### Bootstrap and services
- In the Editor, `Bootstrapper` (`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`) forces build index `0` = `Bootstrapper.unity` to load first, whichever scene you hit Play in. Keep `Bootstrapper.unity` as the only/first build scene; every other scene is loaded via **Addressables** (`AssetReference`), not build index.
- `ServiceLocator` is a static type→`IService` registry. Two service kinds:
  - `LogicOnlyService<T>` – plain C# objects constructed in `DependencyInitializer.Awake()` (`FileLogService`, `SavedDataService`, `ChatHistoryService`, `LLMService`, `WinCondService`). Adding one means adding it to that list.
  - `MonoBehaviourService<T>` – scene components that self-register (`SceneService`, `InteractableService`, `CutsceneService`, …).
- Startup order varies, so consumers use `ServiceLocator.TryOrWaitForService<T>(callback)` and must call `RemoveServiceCallback<T>` in `OnDisable`/`OnDestroy`. Don't cache scene services across scene destruction.
- `Singleton<T>` can auto-create instances; prefer explicit scene setup.

### Input and UI focus
- `InputHandler` wraps the generated `InputSystem_Actions` (do not hand-edit `InputSystem_Actions.cs`; edit the `.inputactions` asset) and exposes static events plus `FocusPlayerInput()` / `FocusUIInput()`.
- `BaseUIPanelSystem` switches to UI input on open and restores player input on close. New panels should derive from it; any error path (e.g. a failed LLM call) must not leave input locked.

### Interaction → dialogue → LLM
- `Interactable` subclasses (`Evidence`, `ChatNpc` and its NPC subclasses, `Door`/`ConditionDoor`, …) register with `InteractableService`, which picks the closest target in range.
- `ChatNpc` looks up its prompt/initial dialogue in `NPCDatabase` **by GameObject name**.
- `LLMService.CallLLMAsync` posts `{npcName, npcSystem, question, summary, history, stage}` to the hard-coded Render endpoint (`.../api/query/ask-stream`) and parses an SSE stream (`meta`, `token`, `done` events). Chunks can split anywhere — keep parsing chunk-safe. `weakPointHit` raises `LLMService.OnWeakPointHit` and is tracked separately from token text. After N lines, history is compacted via `.../api/query/compact-history` into `ChatHistory.summary`. Schema changes require updating `LLMService`, its request/response structs, and the chat UI together.
- `ChatHistoryService` holds per-NPC history and the active chat NPC.

### Persistence and events
- `SavedDataService` (notebook/evidence notes, interacted NPCs, door states, custom key/value) and `ChatHistoryService` serialize JSON into `PlayerPrefs` via `BaseSaveLoadService`. They raise static events (`OnEvidenceNoteSaved`, `OnNPCInteracted`, `OnDoorUnlockedSaved`, …) that UI systems subscribe to — keep persistence in services, presentation in UI.
- Evidence keys, door names, NPC lookup names, teleport-point IDs, PlayerPrefs keys and save field names are save-compatibility contracts; renaming them breaks existing saves and serialized references.
- `WinCondService` handles the final accusation (selected NPC + evidence).

### Scene transitions and logging
- `SceneService` fades out, switches input to UI, loads the Addressable scene, then restores input. `CD.LoadSceneData` carries target scene, facing direction and teleport-point ID consumed by `Player`. Cutscene mappings in `SceneService` play once (a PlayerPrefs flag is set per trigger address).
- `FileLogService` writes `Logs/game.log` under a session GUID stored in PlayerPrefs (`MainMenu` reuses or creates it). On reaching `EndScene` the log is uploaded to a Google Form; each LLM response is also posted there asynchronously. Quit-time uploads are unreliable in browsers by design.

## Working rules
- Don't edit `Library/`, `Temp/`, `Build/`, `Logs/`, `UserSettings/`, or the Unity-generated `*.csproj`/`*.slnx`.
- Preserve `.meta` files (GUIDs back Addressables, scenes, prefabs, SOs). Inspect references before editing serialized `.unity`/`.prefab`/`.asset` YAML by hand.
- Subscribe/unsubscribe static events and service callbacks symmetrically (`OnEnable`/`OnDisable` or `Awake`/`OnDestroy`).
- Release Addressables handles you acquire manually.
- Don't commit unless asked.
