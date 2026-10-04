# Agent instructions

Read this file at the beginning of every task in this repository. Use [README.md](README.md) for the broader architecture reference and inspect the relevant source, scene, prefab, or ScriptableObject before making assumptions.

## Project context

- This is a Unity `6000.3.19f1` 2D investigation/adventure game.
- The release target is the Web platform, with publication on itch.io.
- Runtime code is C# under `Assets/Scripts/`.
- The project uses URP, Addressables, the Input System, UniTask, Cinemachine, Timeline, TextMesh Pro, and Tri Inspector.
- Player progression includes chat history, NPC interactions, evidence, notebook notes, doors, and custom save data.

## Scope and file boundaries

- Prefer precise, surgical changes limited to the requested behavior.
- Do not edit `Library/`, `Temp/`, `Build/`, `Logs/`, or `UserSettings/`; these are generated/local data.
- Treat `*.csproj`, `*.sln`, and `*.slnx` as Unity-generated unless the task explicitly requires changing them.
- Do not manually rewrite serialized scene, prefab, ScriptableObject, Addressables, or ProjectSettings data without inspecting the affected asset and understanding its references.
- Keep documentation changes synchronized with behavior when a public workflow or integration changes.
- For Web/itch.io changes, verify browser compatibility, WebGL/Web platform restrictions, HTTPS/CORS requirements for external services, local persistence behavior, and Addressables loading in a browser build.

## Code conventions

- Follow the existing namespace, naming, access modifier, formatting, and serialization patterns in the surrounding file.
- Preserve type safety; use explicit types and guards rather than broad casts or silent fallbacks.
- Prefer existing services, helpers, events, and base classes over duplicating logic.
- Keep comments limited to non-obvious decisions.
- Surface errors through the repository's existing logging or user-notification patterns. Do not swallow exceptions or return success-shaped fallback values.
- Preserve existing behavior unless the request explicitly changes it, and fix tightly coupled regressions caused by the change.

## Services and lifecycle

- Services implement `IService` and are initialized through the existing service bootstrap/dependency flow.
- Use `ServiceLocator` for service resolution and `TryOrWaitForService` when startup ordering can vary.
- Remove service callbacks and event subscriptions in `OnDisable`/`OnDestroy` as appropriate.
- Respect the distinction between `MonoBehaviourService<T>` and `LogicOnlyService<T>`.
- Do not introduce new singleton or automatic-object creation behavior without checking the existing lifecycle and scene setup.

## Gameplay, UI, and input

- Keep persistence in save/history services and presentation in UI systems.
- Use the existing event-driven flow for chat, save, evidence, and interaction state.
- Preserve input focus rules: UI panels use UI input while open and restore player input when closed.
- Check active panel, chat, and cancellation state before updating UI asynchronously.
- When streaming dialogue, update the UI through the existing token callback and keep final-response handling consistent with the accumulated text.

## Addressables and serialized references

- Use `AssetReference` or existing Addressable runtime keys for loadable scenes and assets.
- When moving or adding Addressable content, verify groups and Inspector references in Unity.
- Avoid changing stable identifiers used by save data, teleport points, evidence, NPCs, doors, or scene transitions.
- Changes to `Assets/InputSystem_Actions.inputactions` can affect the generated wrapper and all input subscribers; validate dependent code.

## LLM and HTTP streaming

- `LLMService` calls the external RAG/LLM endpoint and sends JSON request fields `NpcName`, `NpcSystem`, and `Question`.
- The streaming endpoint returns SSE events such as `meta`, `token`, and `done`; token payloads currently use `{"text":"..."}`.
- Preserve chunk-safe parsing: network chunks can split at arbitrary byte or line boundaries.
- Keep `weakPointHit` handling separate from token accumulation and ensure token callbacks remain incremental.
- Do not log secrets or sensitive user content unnecessarily. Follow the existing `FileLogService` behavior when changing diagnostics.

## Validation

- Use the smallest relevant validation command after each coherent implementation.
- For C# changes, compile the generated solution when practical and report warnings or errors.
- For Unity-facing changes, inspect serialized references and note when editor/runtime testing is still required.
- Review `git diff` before finishing and ensure unrelated worktree changes are untouched.
- Do not commit changes unless explicitly requested.
