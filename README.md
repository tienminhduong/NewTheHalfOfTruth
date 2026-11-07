# NewTheHalfOfTruth

## Summary

NewTheHalfOfTruth is a 2D investigation and adventure game built with Unity. The player explores connected scenes, talks with NPCs, collects evidence, writes notebook notes, unlocks doors, and progresses through scripted events and cutscenes.

NPC conversations are sent to an external LLM/RAG service through `LLMService`. The game also stores player progression locally, including interacted NPCs, evidence notes, unlocked doors, and custom save data.

The project targets the Web platform and is intended to be published on itch.io. Web builds must therefore be tested in a browser and should preserve browser-compatible networking, local persistence, input, and Addressables behavior.

## Main Technology

| Area | Technology |
| --- | --- |
| Game engine | Unity `6000.3.19f1` |
| Language | C# |
| Rendering | Universal Render Pipeline (URP) |
| 2D gameplay | Unity 2D Animation, Sprites, Tilemap, Sprite Shape, Aseprite and PSD importers |
| Input | Unity Input System with `InputSystem_Actions` |
| Asset loading | Unity Addressables and `AssetReference` |
| Async operations | Cysharp UniTask |
| Camera | Cinemachine |
| Cutscenes | Unity Timeline and Signal assets |
| UI | Unity UI and TextMesh Pro |
| Inspector tooling | Tri Inspector |
| NPC dialogue | HTTP requests to an external RAG/LLM API |
| Deployment target | Web platform, published on itch.io |

The external dialogue endpoint currently used by `LLMService` is:

```text
https://rag-jkoh.onrender.com/api/query/ask
```

This endpoint is hard-coded in the Unity client. The backend is not included in this repository.

## Repository Structure

```text
NewTheHalfOfTruth/
├── Assets/
│   ├── Scenes/                 Unity scenes
│   ├── Scripts/
│   │   ├── Animations/         Character animation systems
│   │   ├── Game/               Player, interactables, doors, evidence and NPCs
│   │   ├── Interfaces/         Shared contracts such as IHuman
│   │   ├── ScriptableObjects/   Runtime data databases
│   │   ├── Services/           Scene, interaction, chat, LLM and save services
│   │   ├── UIs/                Menus, dialogue, chat and notebook panels
│   │   └── Utils/              Bootstrap, input, service locator and helpers
│   ├── GameAssets/
│   │   ├── Animations/         Animation assets
│   │   ├── Fonts/              Font assets
│   │   ├── Prefabs/            Reusable GameObjects
│   │   ├── Shaders/            Project shaders
│   │   ├── Signal/             Timeline signal assets
│   │   ├── SOs/                ScriptableObject assets
│   │   ├── Sprites/            2D art
│   │   └── TilePalette/        Tilemap palettes
│   ├── AddressableAssetsData/  Addressables configuration and generated data
│   ├── InputSystem_Actions.*   Input Action asset and generated C# wrapper
│   ├── Resources/              Resources loaded by Unity's Resources system
│   ├── Settings/               Rendering and project settings assets
│   ├── TextMesh Pro/           TextMesh Pro assets and settings
│   └── Timeline/               Timeline assets
├── Packages/
│   ├── manifest.json           Direct package dependencies
│   └── packages-lock.json      Resolved package versions and dependencies
├── ProjectSettings/            Unity editor and build configuration
├── Assembly-CSharp*.csproj     Generated C# project files
├── NewTheHalfOfTruth.slnx      Solution file
├── Build/                      Local build output
├── Library/, Temp/, Logs/      Unity-generated working data
└── README.md
```

Unity-generated folders such as `Library`, `Temp`, and most local `Logs` files should not be edited manually. They are recreated by Unity and are not part of the application architecture.

## Definitions and Core Concepts

### Service registration

Services implement `IService`, which exposes `Awake()` and `OnDestroy()`. `ServiceLocator` stores registered services by type and allows scene objects to either retrieve a service immediately or register a callback until that service becomes available.

There are two main service base types:

- `MonoBehaviourService<T>` registers a scene-backed `MonoBehaviour` service.
- `LogicOnlyService<T>` registers a non-`MonoBehaviour` service created by `DependencyInitializer`.

Service callbacks must be removed when an object is disabled or destroyed. This prevents callbacks from pointing at objects that no longer exist.

### Singleton behavior

`Singleton<T>` provides a static instance for selected `MonoBehaviour` classes. It can locate an existing object or create one automatically. Scene setup should still provide the intended instance because automatic creation can hide missing scene references.

### Interactables

`Interactable` is the base class for objects that can be selected and used by the player. It registers itself with `InteractableService` and receives access to `SavedDataService`.

Current interactable types include:

- `Evidence`
- `NPC`
- `Item`

### Input focus

`InputHandler` connects the generated `InputSystem_Actions` wrapper to gameplay systems. It publishes movement and interaction events and switches between player and UI action maps.

- `InputHandler.FocusPlayerInput()` enables player actions.
- `InputHandler.FocusUIInput()` enables UI actions.

UI panels and scene transitions must preserve this focus behavior so that gameplay input does not remain active while the player is navigating a menu or dialogue panel.

### Addressables

Scenes, animation clips, and cutscene assets are referenced with Addressables. Runtime code should use `AssetReference` or an Addressable runtime key rather than assuming that a scene is available through the normal build index.

### ScriptableObject data

`EvidenceDatabase` and `NPCDatabase` hold serialized content used by gameplay objects. These assets are assigned through the Unity Inspector and are part of the content model even though their data is not stored directly in C# source.

### Scene load data

`CD.LoadSceneData` carries scene transition information:

- The Addressable scene to load
- The direction the player should face after loading
- The identifier of the teleport/start point where the player should appear

### Event-driven state

Save and chat systems publish events when state changes. UI systems subscribe to these events instead of directly owning the underlying save data. This keeps persistence and presentation separate.

## Project Flow

### 1. Startup

1. `Bootstrapper.LoadBootstrapper()` runs before the first scene loads.
2. It loads scene index `0` using `SceneManager.LoadSceneAsync`.
3. `ProjectSettings/EditorBuildSettings.asset` currently places `Assets/Scenes/Bootstrapper.unity` at that index.
4. `DependencyInitializer` creates the logic-only services:
   - `SavedDataService`
   - `ChatHistoryService`
   - `LLMService`
5. Scene-backed services register themselves with `ServiceLocator` when their GameObjects awaken.

### 2. Player input and interaction

```text
InputSystem_Actions
        |
        v
InputHandler
   |         |
   v         v
Player    InteractableService
```

`InputHandler` publishes movement to `Player`, which moves the character and updates `HumanAnimSystem`. It publishes interaction input to `InteractableService`, which finds the closest active interactable inside the configured range.

When an interaction is selected, the player and target face each other before `Interactable.OnInteract()` is called.

### 3. NPC dialogue

```text
Player interaction
        |
        v
ChatNpc
        |
        v
ChatHistoryService / ChatUISystem
        |
        v
LLMService
        |
        v
External RAG/LLM API
        |
        v
Dialogue or chat UI response
```

`ChatNpc` obtains its name, system prompt, and initial dialogue from `NPCDatabase`. `LLMService` sends the NPC name, NPC system, and player question as JSON. The response contains the answer and a `weakPointHit` flag. A weak-point response raises `LLMService.OnWeakPointHit` for game-specific reactions.

### 4. Evidence, doors and save data

Interactables notify `SavedDataService` when the player collects evidence, writes a notebook note, talks to an NPC, or changes a door state. `SavedDataService` serializes player progression to Unity `PlayerPrefs` and publishes events for UI updates.

`ConditionDoor` extends door behavior with progression checks. Evidence and door identifiers therefore act as save-data keys and should remain stable.

### 5. Scene transitions

```text
MainMenu / SceneTransis / Door
              |
              v
        SceneService
              |
              v
Fade to black -> Addressables scene load -> fade in
              |
              v
Player start point and facing direction restored
```

`SceneService` controls the transition overlay, temporarily switches input focus to UI, loads the Addressable scene, and restores player input afterward. It can also map a scene trigger address to a cutscene asset before loading.

Configured cutscene mappings are one-time transitions. `SceneService` stores a PlayerPrefs key derived from the configured scene trigger address immediately before loading the cutscene. Later transitions using the same trigger address skip the cutscene and load the normal target scene directly.

When the player clicks Start Game in the main menu, `FileLogService` checks PlayerPrefs for `FileLogService.SessionKey`. It reuses the existing key or creates and saves a new GUID when no key exists. After `Assets/Scenes/EndScene.unity` finishes loading, `SceneService` uploads `Logs/game.log` to the configured Google Form. The upload uses `entry.2070173541` for the session key and `entry.207076898` for the log text. The same automatic upload is requested when the application quits and when Unity Editor Play Mode is stopped, but a shared guard allows only one automatic request per session. Browser shutdown can interrupt requests on Web/itch.io builds; the endgame upload is the reliable path. The local file is retained if an upload fails.
Each successful LLM response also submits a separate Google Form response using the same entries, with the session key and the NPC name, question, and response in the log field. This upload runs asynchronously and does not block the chat UI; upload failures are logged while the successful in-game response remains available.

Scene load data is consumed by `Player`, which uses the configured start-point identifier to position itself and applies the configured facing direction.

### 6. UI focus

`BaseUIPanelSystem` disables player input when a panel opens and restores player input when the panel closes. `ChatUISystem`, `DialogueUISystem`, `NotebookUISystem`, and menu components build on this behavior.

## Main Classes by Module

### Utils

| Class | Responsibility |
| --- | --- |
| `Bootstrapper` | Loads the bootstrap scene before normal scene startup and can request an initial Addressable scene. |
| `DependencyInitializer` | Creates and initializes logic-only services. |
| `ServiceLocator` | Registers services and resolves immediate or deferred service dependencies. |
| `Singleton<T>` | Provides singleton lifecycle support for selected `MonoBehaviour` classes. |
| `InputHandler` | Bridges the Input System to gameplay/UI events and switches input maps. |
| `ObjectIdentifier` | Resolves scene objects by stable identifiers. |
| `PersistentObject` | Supports objects that persist across scene loads. |
| `CommonData` | Defines shared scene, animation and cutscene data structures. |

### Services

| Class | Responsibility |
| --- | --- |
| `SceneService` | Fades between scenes, loads Addressable scenes, and carries scene load data. |
| `InteractableService` | Tracks interactables, chooses the closest target, and handles interaction input. |
| `LLMService` | Sends NPC questions to the external RAG/LLM API and reports responses. |
| `ChatHistoryService` | Stores the active NPC conversation and chat history. |
| `SavedDataService` | Loads and saves notebook, NPC, door and custom progression data. |
| `CutsceneService` | Service shell reserved for cutscene-related behavior; current scene transition cutscene mapping is implemented in `SceneService`. |
| `MonoBehaviourService<T>` | Base for scene-backed registered services. |
| `LogicOnlyService<T>` | Base for services created without a scene `GameObject`. |

### Game

| Class | Responsibility |
| --- | --- |
| `Player` | Handles movement, directional animation, interaction registration and scene spawn placement. |
| `Interactable` | Base contract and service registration for interactable objects. |
| `Evidence` | Interactable evidence object used by the investigation and notebook systems. |
| `Door` | Scene transition and door state behavior. |
| `ConditionDoor` | Door behavior controlled by saved progression conditions. |
| `SceneTransis` | Inspector-driven scene transition trigger. |
| `TeleportPoint` | Identifiable destination used when entering a scene. |

### NPCs

| Class | Responsibility |
| --- | --- |
| `ChatNpc` | Base conversational NPC; loads NPC data and starts chat interaction. |
| `Johny` | NPC-specific behavior for spawn imagery and ending/cutscene references. |
| `Brad` | NPC-specific behavior derived from `ChatNpc`. |
| `WalkingNpc` | NPC movement behavior. |
| `AppearCondNpc` | NPC appearance behavior controlled by conditions. |

### UI

| Class | Responsibility |
| --- | --- |
| `BaseUIPanelSystem` | Common open/close behavior and player/UI input focus switching. |
| `ChatUISystem` | Chat history and player question interface. |
| `DialogueUISystem` | Dialogue presentation and response handling. |
| `NotebookUISystem` | Notebook and saved evidence-note interface. |
| `MainMenu` | Main menu actions and scene entry. |
| `ChatBubble` | Individual dialogue/chat visual element. |
| `BookPage` | Notebook page display and saved note interaction. |
| `TypingEffectTMP` | TextMesh Pro typing presentation effect. |

### Animations

| Class | Responsibility |
| --- | --- |
| `BaseAnimSystem` | Loads and manages Addressable animation clips. |
| `HumanAnimSystem` | Selects idle/walk animations using direction and human state. |

### ScriptableObjects

| Class | Responsibility |
| --- | --- |
| `EvidenceDatabase` | Stores evidence definitions and connects evidence data to save behavior. |
| `NPCDatabase` | Stores NPC system prompts and startup dialogue. |

### Interfaces and shared data

| Type | Responsibility |
| --- | --- |
| `IHuman` | Shared contract for objects that can face a direction. |
| `IService` | Lifecycle contract used by `ServiceLocator`. |
| `CD.LoadSceneData` | Scene target, player facing direction and start-point data. |
| `CD.AnimClipReference` | Strongly typed Addressable animation reference. |
| `CD.CutsceneData` | Mapping between a scene trigger address and a cutscene asset. |

## Important Configuration

### Unity version

The required editor version is recorded in `ProjectSettings/ProjectVersion.txt`:

```text
6000.3.19f1
```

Use the matching Unity editor when possible. Opening the project in another editor version may reserialize scenes, assets, or project settings.

### Build Settings

`ProjectSettings/EditorBuildSettings.asset` currently contains only:

```text
Assets/Scenes/Bootstrapper.unity
```

This is important because `Bootstrapper` loads build index `0` before the rest of the game starts. Gameplay scenes are expected to be loaded through Addressables and do not need to be listed as normal build scenes for this flow.

### Packages

- `Packages/manifest.json` declares direct dependencies.
- `Packages/packages-lock.json` records resolved package versions.
- Important packages include Addressables, Input System, UniTask, Cinemachine, Timeline, URP, Unity 2D packages, UI, TextMesh Pro support, and Tri Inspector.

Update both package files through Unity's Package Manager when changing dependencies. Avoid hand-editing resolved package information unless there is a specific reason.

### Input Actions

`Assets/InputSystem_Actions.inputactions` is the source asset for the generated `Assets/InputSystem_Actions.cs` wrapper. Changes to action names, action maps, or bindings can affect `InputHandler` and every class subscribing to its static events.

### Addressables

Addressable scenes, animation clips, and cutscene assets depend on serialized GUIDs and runtime keys. When adding or moving a loadable asset:

1. Confirm it remains Addressable.
2. Reassign Inspector references if Unity reports a broken reference.
3. Rebuild Addressables content when required.
4. Test the transition from the bootstrap scene.

The exact Addressables group layout should be checked in the Unity editor rather than inferred from code.

### Serialized scene and asset references

Many dependencies are assigned in scenes, prefabs, or ScriptableObjects rather than constructed in code. Important Inspector references include:

- `DependencyInitializer` databases and save settings
- `SceneService` transition image and cutscene mappings
- `InteractableService` interaction indicator canvas
- `Player` animation references and sprite renderer
- NPC database and animation references
- Door, teleport-point, and scene transition references
- UI panel objects and buttons

A successful C# compile does not guarantee that these serialized references are valid.

## Important Points When Changing Code

- Preserve Unity `.meta` files. They contain GUIDs used by scenes, prefabs, Addressables and ScriptableObjects.
- Keep `Bootstrapper.unity` at build index `0` unless the bootstrap loading strategy is changed at the same time.
- Register and unregister service callbacks symmetrically in `OnEnable`/`OnDisable` or `Awake`/`OnDestroy`.
- Unsubscribe from static input and save events when objects are disabled or destroyed.
- Preserve the player/UI input focus contract. A new UI panel should use `BaseUIPanelSystem` behavior or explicitly restore player input.
- Keep NPC GameObject names aligned with `NPCDatabase` lookup keys.
- Treat evidence keys, door names, teleport-point identifiers, and save-field names as stable data contracts.
- Keep Addressable references valid when moving or renaming scenes, animation clips, or cutscene assets.
- Release Addressables handles when manually loading Addressable assets.
- Handle LLM and network failures without leaving the chat UI or input system in a locked state.
- If the LLM endpoint or response schema changes, update `LLMService`, `LlmApiResponse`, and the consuming UI code together.
- Avoid bypassing `ServiceLocator` lifecycle assumptions by caching scene services across scene destruction.
- Consider backward compatibility before changing `PlayerPrefs` save data. Existing users may already have serialized data from the previous schema.
- Do not manually edit `Library`, `Temp`, or other generated Unity output folders.
- After changing serialized fields, open the affected scenes and prefabs in Unity to confirm that references were not lost.

## Development and Verification

1. Install Unity `6000.3.19f1`.
2. Open the repository root as a Unity project.
3. Allow Unity to resolve packages from `Packages/manifest.json`.
4. Check Addressables configuration and confirm required groups/assets are available.
5. Open `Assets/Scenes/Bootstrapper.unity`.
6. Run the project from the bootstrap scene.
7. Verify player movement, interaction targeting, an NPC chat flow, evidence persistence, and at least one Addressable scene transition.

The repository does not currently document an automated gameplay test suite. Runtime verification in the Unity editor remains important because many dependencies are stored in serialized scene and asset data.
