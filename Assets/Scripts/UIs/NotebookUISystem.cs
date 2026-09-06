using System.Collections.Generic;
using UnityEngine;

public class NotebookUISystem : BaseUIPanelSystem
{
    [SerializeField] private BaseUIButton _notebookOpenButton;


    [SerializeField] private List<string> _evidenceKeysCollected = new();
    [SerializeField] private EvidenceDatabase _evidenceDatabase;

    [SerializeField] private BaseUIButton _leftPageButton;
    [SerializeField] private BaseUIButton _rightPageButton;
    [SerializeField] private BookPage[] pages = new BookPage[2];

    private int _currentPageIndex = 0;

    private SavedDataService _savedDataService;

    protected override void Awake()
    {
        base.Awake();
        ServiceLocator.TryOrWaitForService<SavedDataService>(HandleLoadData);
        SavedDataService.OnEvidenceNoteSaved += HandleEvidenceCollected;
    }

    void OnDestroy()
    {
        ServiceLocator.RemoveServiceCallback<SavedDataService>(HandleLoadData);
        SavedDataService.OnEvidenceNoteSaved -= HandleEvidenceCollected;
    }

    override protected void OnEnable()
    {
        base.OnEnable();
        _notebookOpenButton.AddListener(OpenOrCloseNotebook);
        _leftPageButton.AddListener(NavigateLeftPage);
        _rightPageButton.AddListener(NavigateRightPage);

        ClampPageIndex();
    }

    override protected void OnDisable()
    {
        base.OnDisable();
        _notebookOpenButton.RemoveListener(OpenOrCloseNotebook);
        _leftPageButton.RemoveListener(NavigateLeftPage);
        _rightPageButton.RemoveListener(NavigateRightPage);
    }


    private void HandleLoadData(IService service)
    {
        if (service is not SavedDataService savedDataService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of SavedDataService.");
            return;
        }

        _savedDataService = savedDataService;
        _evidenceKeysCollected.Clear();
        _evidenceKeysCollected.AddRange(_savedDataService.GetEvidenceKeys());
    }

    private void HandleEvidenceCollected(NotebookSavedData notebookData)
    {
        if (!_evidenceKeysCollected.Contains(notebookData.evidenceKey))
        {
            _evidenceKeysCollected.Add(notebookData.evidenceKey);
        }
    }

    private void LoadEvidenceForCurrentPage()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            int evidenceIndex = _currentPageIndex + i;
            if (evidenceIndex < _evidenceKeysCollected.Count)
            {
                string evidenceKey = _evidenceKeysCollected[evidenceIndex];
                if (_evidenceDatabase.TryGetEvidenceData(evidenceKey, out var evidenceData))
                {
                    pages[i].SetEvidenceData(evidenceData);
                    pages[i].SetPageVisibility(true);
                }
                else
                {
                    Debug.LogWarning($"Evidence with key '{evidenceKey}' not found in the database.");
                    pages[i].SetPageVisibility(false);
                }
            }
            else
            {
                pages[i].SetPageVisibility(false);
            }
        }
    }

    private void NavigateLeftPage()
    {
        if (_currentPageIndex > 0)
        {
            _currentPageIndex -= pages.Length;
            LoadEvidenceForCurrentPage();
        }

        ClampPageIndex();
    }

    private void NavigateRightPage()
    {
        if (_currentPageIndex < _evidenceKeysCollected.Count - pages.Length)
        {
            _currentPageIndex += pages.Length;
            LoadEvidenceForCurrentPage();
        }

        ClampPageIndex();
    }

    private void ClampPageIndex()
    {
        if (_currentPageIndex < 0)
            _currentPageIndex = 0;
        else if (_currentPageIndex >= _evidenceKeysCollected.Count)
            _currentPageIndex = Mathf.Max(0, _evidenceKeysCollected.Count - pages.Length);

        if (_currentPageIndex < _evidenceKeysCollected.Count - pages.Length)
            _rightPageButton.SetInteractable(true);
        else
            _rightPageButton.SetInteractable(false);

        if (_currentPageIndex <= 0)
            _leftPageButton.SetInteractable(false);
        else
            _leftPageButton.SetInteractable(true);
    }

    override protected void OpenPanel()
    {
        base.OpenPanel();
        LoadEvidenceForCurrentPage();
    }

    private void OpenOrCloseNotebook()
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
}