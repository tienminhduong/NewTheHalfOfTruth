# Project Guidelines

Read this file at the beginning of every task. Use [README.md](README.md) for
the architecture overview, and inspect the relevant C# source plus any affected
scene, prefab, or ScriptableObject before making assumptions.

## Project

- This is a Unity `6000.3.19f1` 2D investigation/adventure game.
- Runtime code is C# under `Assets/Scripts/`.
- The target is Web/itch.io; preserve browser-compatible networking, local
  persistence, input, and Addressables behavior.
- Main systems include URP, Addressables, Input System, UniTask, Cinemachine,
  Timeline, TextMesh Pro, and Tri Inspector.
- Player progression includes chat history, NPC interactions, evidence,
  notebook notes, doors, and custom save data.

## Scope and repository safety

- Make precise, surgical changes and preserve existing behavior unless the
  request explicitly changes it.
- Do not edit generated or local folders: `Library/`, `Temp/`, `Build/`,
  `Logs/`, or `UserSettings/`.
- Treat `*.csproj`, `*.sln`, and `*.slnx` as Unity-generated; do not edit them
  unless the task explicitly requires it.
- Preserve Unity `.meta` files and stable identifiers used by save data,
  Addressables, scenes, prefabs, NPCs, doors, evidence, and teleport points.
- Do not manually rewrite serialized scene, prefab, ScriptableObject,
  Addressables, or ProjectSettings data without inspecting its references.
- Do not add secrets or log sensitive user content unnecessarily.
- Do not commit changes unless explicitly requested.

## Code and lifecycle conventions

- Follow nearby namespaces, naming, access modifiers, formatting, and
  serialization patterns. Prefer existing services, helpers, events, and base
  classes over duplicating logic.
- Keep persistence in save/history services and presentation in UI systems.
- Services implement `IService` and use the existing bootstrap/dependency flow.
  Resolve services through `ServiceLocator`; use `TryOrWaitForService` when
  startup order can vary.
- Register and unregister callbacks and static event subscriptions
  symmetrically in `OnEnable`/`OnDisable` or `Awake`/`OnDestroy`.
- Respect the distinction between `MonoBehaviourService<T>` and
  `LogicOnlyService<T>`. Do not introduce new singleton or automatic-object
  creation behavior without checking scene setup.
- Preserve type safety and surface errors through the repository's existing
  logging or notification patterns. Do not swallow exceptions or use silent
  success fallbacks.

## Gameplay, UI, and assets

- Preserve the player/UI input contract: panels use UI input while open and
  restore player input when closed. Use `BaseUIPanelSystem` behavior for new
  panels where appropriate.
- Keep asynchronous UI updates guarded by active-panel, chat, and cancellation
  state. Streaming dialogue must update through the existing token callback and
  handle the final accumulated response consistently.
- Use `AssetReference` or existing Addressable runtime keys for loadable assets.
  Confirm Addressable groups, Inspector references, and browser scene loading
  after moving or adding content. Release manually acquired Addressables handles.
- Keep `Bootstrapper.unity` at build index `0` unless the bootstrap strategy is
  changed at the same time.
- Treat evidence keys, door names, NPC lookup names, teleport identifiers,
  PlayerPrefs keys, and save-field names as compatibility-sensitive contracts.
- After changing serialized fields, open affected scenes and prefabs in Unity
  and verify their references.

## LLM and logging

- `LLMService` sends `NpcName`, `NpcSystem`, and `Question` to the external
  RAG/LLM endpoint and parses SSE events such as `meta`, `token`, and `done`.
- Preserve chunk-safe SSE parsing: network chunks may split at arbitrary byte
  or line boundaries. Keep `weakPointHit` handling separate from token
  accumulation.
- If the endpoint or response schema changes, update `LLMService`, response
  models, and consuming UI code together. Network failures must not leave chat
  UI or input locked.
- `MainMenu` reuses `FileLogService.SessionKey` from PlayerPrefs or creates a
  GUID for a new game. Endgame log upload is the reliable Web path; browser
  shutdown can interrupt quit/Editor-stop uploads, and failed uploads retain
  the local log.

## Validation

- Use the smallest relevant validation command after each coherent change.
- For C# changes, compile the generated solution when practical and report
  warnings or errors. Unity compilation alone does not validate serialized
  references.
- For Unity-facing changes, test from `Assets/Scenes/Bootstrapper.unity` with
  player movement, interaction targeting, NPC chat, evidence persistence, and
  at least one Addressable scene transition.
- For Web changes, verify behavior in a browser and consider HTTPS/CORS,
  browser teardown, local persistence, and Addressables loading.
- Review `git diff` and `git status` before finishing; leave unrelated worktree
  changes untouched.

See [README.md](README.md) for detailed architecture, configuration, and
manual verification notes. See [HTTP-Stream-Guide.md](HTTP-Stream-Guide.md)
for the LLM streaming contract.
