# Project Data Model

This diagram describes the model currently implemented in the Unity runtime.
It separates content authored in ScriptableObjects, runtime services, and data
serialized to `PlayerPrefs`.

## Persistence model

```mermaid
classDiagram
    class PlayerPrefs {
        PlayerSavedData PlayerSavedData
        ChatHistorySaved ChatHistories
    }

    class PlayerSavedData {
        List~NotebookSavedData~ notebookData
        List~InteractedNPCSavedData~ interactedNPCs
        List~UnlockedDoorSavedData~ unlockedDoors
        List~CustomSavedData~ customData
    }

    class NotebookSavedData {
        string evidenceKey
        string playerNote
    }

    class InteractedNPCSavedData {
        string name
    }

    class UnlockedDoorSavedData {
        string doorName
        DoorUnlockedState unlockedState
    }

    class CustomSavedData {
        string key
        string value
    }

    class DoorUnlockedState {
        <<enumeration>>
        Locked
        FirstTimeOpened
        Opened
    }

    class ChatHistorySaved {
        List~ChatHistory~ chatHistories
    }

    class ChatHistory {
        string key
        string summary
        List~ChatItem~ chatItems
    }

    class ChatItem {
        string personKey
        string message
    }

    PlayerPrefs --> PlayerSavedData : JSON key PlayerSavedData
    PlayerPrefs --> ChatHistorySaved : JSON key ChatHistories
    PlayerSavedData "1" *-- "0..*" NotebookSavedData
    PlayerSavedData "1" *-- "0..*" InteractedNPCSavedData
    PlayerSavedData "1" *-- "0..*" UnlockedDoorSavedData
    PlayerSavedData "1" *-- "0..*" CustomSavedData
    UnlockedDoorSavedData --> DoorUnlockedState
    ChatHistorySaved "1" *-- "0..*" ChatHistory
    ChatHistory "1" *-- "0..*" ChatItem
```

## Runtime and authored-content relationships

```mermaid
flowchart LR
    subgraph Content["Authored content"]
        EvidenceDB["EvidenceDatabase\nEvidenceData[]"]
        NPCDB["NPCDatabase\nNPCData[]"]
        EvidenceData["EvidenceData\nkey, description, guide,\nsprite, playerNote"]
        NPCData["NPCData\nnpcName, npcSystem,\nstartup dialogue, description, guide"]
        SceneData["CD.LoadSceneData\nAssetReference scene\nFaceDir\nStartPointKey"]
    end

    subgraph Runtime["Runtime objects and services"]
        Dependency["DependencyInitializer"]
        Saved["SavedDataService"]
        Chat["ChatHistoryService"]
        LLM["LLMService"]
        Evidence["Evidence"]
        NPC["ChatNpc"]
        Door["ConditionDoor"]
        Notebook["NotebookUISystem / BookPage"]
        Scene["SceneService"]
    end

    subgraph Storage["PlayerPrefs JSON"]
        PlayerSaved["PlayerSavedData"]
        ChatSaved["ChatHistorySaved"]
    end

    EvidenceDB --> EvidenceData
    NPCDB --> NPCData
    Dependency --> Saved
    Dependency --> Chat
    Dependency --> LLM

    Evidence -->|EvidenceKey = GameObject.name| EvidenceDB
    NPC -->|NpcName = GameObject.name| NPCDB
    Door -->|required evidenceKey / NPC name| Saved
    Door -->|stores doorName + state| PlayerSaved
    Evidence -->|SaveNotebookData| Saved
    NPC -->|SaveInteractedNPC| Saved
    Notebook -->|reads and edits notes| Saved
    Saved -->|serializes| PlayerSaved
    Chat -->|serializes| ChatSaved
    NPC -->|sets active NPC| Chat
    LLM -->|reads/pushes history| Chat
    LLM -->|sends NPC data + question + history| External["External RAG/LLM API"]
    Chat -->|chat events| ChatUI["Chat UI"]
    Saved -->|evidence/NPC/door events| UI["Notebook and door state"]
    Door -->|CD.LoadSceneData| Scene
    SceneData --> Door
    Scene -->|Addressables scene load| Addressable["Addressable Scene"]
```

## Key invariants

- `EvidenceData.key`, `NPCData.npcName`, `ConditionDoor.DoorName`, and
  `ChatHistory.key` are string identity contracts. Runtime objects normally
  derive these identities from their GameObject names.
- Evidence notes are the progression signal for collected evidence. Saving an
  empty note still creates a `NotebookSavedData` record and makes the evidence
  count as collected.
- A `ConditionDoor` unlocks when every required evidence key has a saved note
  and every required NPC name has been recorded as interacted with. Its state
  progresses from `Locked` to `FirstTimeOpened` to `Opened`.
- `EvidenceDatabase` mirrors saved player notes back into its in-memory
  `EvidenceData.playerNote` values through `OnEvidenceNoteSaved`; the
  ScriptableObject remains the authored content source.
- Chat history is keyed by NPC name and stores both a compact `summary` and
  individual `ChatItem` messages. `LLMService` uses both when building a
  request.
- `CD.LoadSceneData` is transient scene-transition data. It is not part of
  `PlayerSavedData`; `SceneService` holds it as `CurrentLoadSceneData` while
  loading an Addressable scene.

## Source map

- [SavedDataService.cs](Assets/Scripts/Services/SaveLoad/SavedDataService.cs)
- [ChatHistoryService.cs](Assets/Scripts/Services/ChatServices/ChatHistoryService.cs)
- [EvidenceDatabase.cs](Assets/Scripts/ScriptableObjects/EvidenceDatabase.cs)
- [NPCDatabase.cs](Assets/Scripts/ScriptableObjects/NPCDatabase.cs)
- [Evidence.cs](Assets/Scripts/Game/Evidence.cs)
- [ChatNpc.cs](Assets/Scripts/Game/Npcs/ChatNpc.cs)
- [ConditionDoor.cs](Assets/Scripts/Game/ConditionDoor.cs)
- [CommonData.cs](Assets/Scripts/Utils/CommonData.cs)
- [SceneService.cs](Assets/Scripts/Services/SceneService.cs)
