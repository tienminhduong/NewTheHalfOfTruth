using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private SceneService _sceneService;
    private bool _endSceneTransitionScheduled;
    private bool _hasMetJohny;

    protected override void Awake()
    {
        base.Awake();
        _nameButtonContainer ??= transform.Find("ContentPanel/Scroll View/Viewport/Content");
        _selectionPrompt ??= transform.Find("ContentPanel/Lựa chọn")?.GetComponentInChildren<TextMeshProUGUI>();
        _winCondOpenButton?.gameObject.SetActive(false);
        ServiceLocator.TryOrWaitForService<SavedDataService>(HandleLoadData);
        ServiceLocator.TryOrWaitForService<SceneService>(HandleSceneService);
        SavedDataService.OnNPCInteracted += HandleNPCInteracted;
    }

    void OnDestroy()
    {
        ServiceLocator.RemoveServiceCallback<SavedDataService>(HandleLoadData);
        ServiceLocator.RemoveServiceCallback<WinCondService>(HandleWinCondService);
        ServiceLocator.RemoveServiceCallback<SceneService>(HandleSceneService);
        SavedDataService.OnNPCInteracted -= HandleNPCInteracted;

        if (_winCondService != null)
        {
            _winCondService.OnWinConditionMet -= HandleWinConditionMet;
            _winCondService.OnWinConditionFailed -= HandleWinConditionFailed;
        }
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
        _hasMetJohny = _savedDataService.HasInteractedWithNPC("Johny");
        ServiceLocator.TryOrWaitForService<WinCondService>(HandleWinCondService);
    }

    private void HandleNPCInteracted(string npcName)
    {
        if (npcName != "Johny")
            return;

        if (_hasMetJohny)
            _winCondOpenButton?.gameObject.SetActive(true);

        _hasMetJohny = true;
    }

    private void HandleWinCondService(IService winCondService)
    {
        if (winCondService is not WinCondService service)
        {
            Debug.LogError("WinCondService is not available.");
            return;
        }
        _winCondService = service;
        _winCondService.OnWinConditionMet += HandleWinConditionMet;
        _winCondService.OnWinConditionFailed += HandleWinConditionFailed;
    }

    private void HandleSceneService(IService sceneService)
    {
        if (sceneService is not SceneService service)
        {
            Debug.LogError("SceneService is not available.");
            return;
        }

        _sceneService = service;
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
        {
            _winCondService.SetSelectedNPCName(selectionName);
            UpdateSelectionPrompt();
            LoadNameButtons();
            return;
        }

        _winCondService.SetSelectedEvidenceName(selectionName);
    }

    private void HandleWinConditionMet()
    {
        ClosePanel();
        ShowResultPopup("Bạn đã giải được vụ án!");
        TransitionToEndSceneAfterDelay().Forget();
    }

    private void HandleWinConditionFailed()
    {
        ClosePanel();
        ShowResultPopup("Đó có vẻ không phải là hung thủ và bằng chứng đúng. Hãy thử lại!");
    }

    private void ShowResultPopup(string message)
    {
        var popup = new GameObject("WinConditionResultPopup", typeof(RectTransform), typeof(Image));
        popup.transform.SetParent(transform, false);

        var popupRect = (RectTransform)popup.transform;
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(600f, 180f);

        var background = popup.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.85f);

        var textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(popup.transform, false);
        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 20f);
        textRect.offsetMax = new Vector2(-20f, -20f);

        var resultText = textObject.GetComponent<TextMeshProUGUI>();
        resultText.text = message;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.fontSize = 48f;
        resultText.color = Color.white;

        Destroy(popup, 2f);
    }

    private async UniTaskVoid TransitionToEndSceneAfterDelay()
    {
        if (_endSceneTransitionScheduled)
            return;

        _endSceneTransitionScheduled = true;

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: this.GetCancellationTokenOnDestroy());

            if (_sceneService == null)
            {
                Debug.LogError("SceneService is not available. Cannot transition to EndScene.");
                return;
            }

            _sceneService.TransitionToScene("Assets/Scenes/EndScene.unity");
        }
        catch (OperationCanceledException)
        {
        }
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
