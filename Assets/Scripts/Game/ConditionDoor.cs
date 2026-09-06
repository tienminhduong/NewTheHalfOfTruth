using System.Collections.Generic;
using DG.Tweening;
using TriInspector;
using UnityEngine;

public class ConditionDoor : Door
{
    [SerializeField] private List<string> _evidenceKeysRequired;
    [SerializeField] private List<string> _npcNamesRequired;
    [SerializeField] private GameObject _lockedDoorVisual;
    [SerializeField] private GameObject _firstTimeOpenedDoorVisual;

    [HideIf(nameof(_firstTimeOpenedDoorVisual), null)]
    [SerializeField]
    private IndicatorMoveDirection _indicatorMoveDirection;

    private HashSet<string> _npcNamesRequiredSet;
    private HashSet<string> _evidenceKeysRequiredSet;

    private DoorUnlockedState _doorUnlockedState = DoorUnlockedState.Locked;
    private SavedDataService _savedDataService;
    public string DoorName => gameObject.name;

    void Awake()
    {
        _npcNamesRequiredSet ??= new HashSet<string>();
        _evidenceKeysRequiredSet ??= new HashSet<string>();
        foreach (var npcName in _npcNamesRequired)
        {
            _npcNamesRequiredSet.Add(npcName);
        }

        foreach (var evidenceKey in _evidenceKeysRequired)
        {
            _evidenceKeysRequiredSet.Add(evidenceKey);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        ServiceLocator.TryOrWaitForService<SavedDataService>(SetupSavedDataService);
        SavedDataService.OnEvidenceNoteSaved += HandleEvidenceSaved;
        SavedDataService.OnNPCInteractedSaved += HandleNPCInteracted;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        ServiceLocator.RemoveServiceCallback<SavedDataService>(SetupSavedDataService);
        SavedDataService.OnEvidenceNoteSaved -= HandleEvidenceSaved;
        SavedDataService.OnNPCInteractedSaved -= HandleNPCInteracted;
    }

    protected override void HandleDoorInteract()
    {
        if (_doorUnlockedState == DoorUnlockedState.Locked)
            return;

        if (_doorUnlockedState == DoorUnlockedState.FirstTimeOpened)
            SetDoorUnlockedState(DoorUnlockedState.Opened);

        base.HandleDoorInteract();
    }

    private void HandleEvidenceSaved(NotebookSavedData savedData)
    {
        _evidenceKeysRequiredSet.Remove(savedData.evidenceKey);
        CheckUnlocked();
    }

    private void HandleNPCInteracted(InteractedNPCSavedData savedData)
    {
        _npcNamesRequiredSet.Remove(savedData.name);
        CheckUnlocked();
    }

    private void CheckUnlocked()
    {
        if (_doorUnlockedState != DoorUnlockedState.Locked)
            return;
        if (_evidenceKeysRequiredSet.Count == 0 && _npcNamesRequiredSet.Count == 0)
        {
            SetDoorUnlockedState(DoorUnlockedState.FirstTimeOpened);
        }
    }

    private void SetupSavedDataService(IService service)
    {
        if (service is not SavedDataService savedDataService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of SavedDataService.");
            return;
        }

        _savedDataService = savedDataService;
        SetDoorUnlockedState(_savedDataService.GetDoorUnlockedState(DoorName) ?? DoorUnlockedState.Locked);
        if (_doorUnlockedState != DoorUnlockedState.Locked)
            return;

        foreach (var evidenceKey in _evidenceKeysRequired)
        {
            if (_savedDataService.GetPlayerNoteForEvidence(evidenceKey) != null)
                _evidenceKeysRequiredSet.Remove(evidenceKey);
        }

        foreach (var npcName in _npcNamesRequired)
        {
            if (_savedDataService.HasInteractedWithNPC(npcName))
                _npcNamesRequiredSet.Remove(npcName);
        }

        CheckUnlocked();
    }

    private void SetDoorUnlockedState(DoorUnlockedState unlockedState)
    {
        _doorUnlockedState = unlockedState;
        switch (_doorUnlockedState)
        {
            case DoorUnlockedState.Locked:
                if (_lockedDoorVisual != null)
                    _lockedDoorVisual.SetActive(true);
                if (_firstTimeOpenedDoorVisual != null)
                    _firstTimeOpenedDoorVisual.SetActive(false);
                break;
            case DoorUnlockedState.FirstTimeOpened:
                if (_lockedDoorVisual != null)
                    _lockedDoorVisual.SetActive(false);
                if (_firstTimeOpenedDoorVisual != null)
                {
                    _firstTimeOpenedDoorVisual.SetActive(true);
                    _firstTimeOpenedDoorVisual.transform.DOBlendableLocalMoveBy(
                        _indicatorMoveDirection == IndicatorMoveDirection.Horizontal
                        ? new Vector3(0.5f, 0, 0)
                        : new Vector3(0, 0.5f, 0), 1f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(_firstTimeOpenedDoorVisual, LinkBehaviour.KillOnDisable);
                }
                _savedDataService.SaveUnlockedDoor(DoorName, _doorUnlockedState);
                break;
            case DoorUnlockedState.Opened:
                if (_lockedDoorVisual != null)
                    _lockedDoorVisual.SetActive(false);
                if (_firstTimeOpenedDoorVisual != null)
                    _firstTimeOpenedDoorVisual.SetActive(false);
                _savedDataService.SaveUnlockedDoor(DoorName, _doorUnlockedState);
                break;
        }
    }

    public enum IndicatorMoveDirection
    {
        Horizontal,
        Vertical
    }
}