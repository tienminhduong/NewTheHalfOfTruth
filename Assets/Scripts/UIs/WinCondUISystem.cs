using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WinCondUISystem : BaseUIPanelSystem
{
    [SerializeField] private BaseUIButton _winCondOpenButton;
    [SerializeField] private SelectNameButton _nameButtonPrefab;

    [SerializeField] private string _selectNPCStr;
    [SerializeField] private string _selectEvidenceStr;
    [SerializeField] private NPCDatabase _npcDatabase;
    [SerializeField] private EvidenceDatabase _evidenceDatabase;
    [SerializeField] private Transform _nameButtonContainer;
    [SerializeField] private TextMeshProUGUI _selectionPrompt;

    private readonly List<SelectNameButton> _nameButtons = new();
    private WinCondService _winCondService;
    private SavedDataService _savedDataService;

    protected override void Awake()
    {
        base.Awake();
        _nameButtonContainer ??= transform.Find("ContentPanel/Scroll View/Viewport/Content");
        _selectionPrompt ??= transform.Find("ContentPanel/Lựa chọn")?.GetComponentInChildren<TextMeshProUGUI>();
        ServiceLocator.TryOrWaitForService<SavedDataService>(HandleLoadData);
    }

    void OnDestroy()
    {
        ServiceLocator.RemoveServiceCallback<SavedDataService>(HandleLoadData);
        ServiceLocator.RemoveServiceCallback<WinCondService>(HandleWinCondService);
    }

    override protected void OnEnable()
    {
        base.OnEnable();
        _winCondOpenButton.AddListener(OpenOrCloseWinCondUI);
    }

    override protected void OnDisable()
    {
        base.OnDisable();
        _winCondOpenButton.RemoveListener(OpenOrCloseWinCondUI);
    }

    private void OpenOrCloseWinCondUI()
    {
        if (IsPanelOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    private void HandleLoadData(IService savedDataService)
    {
        if (savedDataService is not SavedDataService service)
        {
            Debug.LogError("SavedDataService is not available.");
            return;
        }
        _savedDataService = service;
        ServiceLocator.TryOrWaitForService<WinCondService>(HandleWinCondService);
    }

    private void HandleWinCondService(IService winCondService)
    {
        if (winCondService is not WinCondService service)
        {
            Debug.LogError("WinCondService is not available.");
            return;
        }
        _winCondService = service;
    }

    protected override void OpenPanel()
    {
        base.OpenPanel();
        UpdateSelectionPrompt();
        LoadNameButtons();
    }

    private void UpdateSelectionPrompt()
    {
        if (_selectionPrompt == null || _winCondService == null)
            return;

        _selectionPrompt.text = _winCondService.CurrentSelectingState == 0
            ? _selectNPCStr
            : _selectEvidenceStr;
    }

    private void LoadNameButtons()
    {
        if (_savedDataService == null || _winCondService == null || _nameButtonContainer == null)
            return;

        var index = 0;
        if (_winCondService.CurrentSelectingState == 0)
        {
            foreach (var npcName in _savedDataService.GetInteractedNPCNames())
            {
                if (!_npcDatabase.TryGetNPCData(npcName, out var npcData))
                {
                    Debug.LogWarning($"NPC with name '{npcName}' not found in the database.");
                    continue;
                }

                var button = GetNameButton(index++);
                button.SetSelectionData(npcName, npcData.npcName, npcData.npcSprite);
                button.SetSelectionAction(() => HandleSelection(button.NameToSelect));
            }
        }
        else
        {
            foreach (var evidenceKey in _savedDataService.GetEvidenceKeys())
            {
                if (!_evidenceDatabase.TryGetEvidenceData(evidenceKey, out var evidenceData))
                {
                    Debug.LogWarning($"Evidence with key '{evidenceKey}' not found in the database.");
                    continue;
                }

                var button = GetNameButton(index++);
                button.SetSelectionData(evidenceKey, evidenceData.name, evidenceData.evidenceSprite);
                button.SetSelectionAction(() => HandleSelection(button.NameToSelect));
            }
        }

        for (; index < _nameButtons.Count; index++)
            _nameButtons[index].gameObject.SetActive(false);
    }

    private void HandleSelection(string selectionName)
    {
        if (_winCondService.CurrentSelectingState == 0)
            _winCondService.SetSelectedNPCName(selectionName);
        else
            _winCondService.SetSelectedEvidenceName(selectionName);

        UpdateSelectionPrompt();
        LoadNameButtons();
    }

    private SelectNameButton GetNameButton(int index)
    {
        SelectNameButton button;
        if (index < _nameButtons.Count)
        {
            button = _nameButtons[index];
            button.gameObject.SetActive(true);
        }
        else
        {
            button = Instantiate(_nameButtonPrefab, _nameButtonContainer);
            _nameButtons.Add(button);
        }

        return button;
    }
}
