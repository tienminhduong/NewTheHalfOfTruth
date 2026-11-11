using System.Collections.Generic;
using DG.Tweening;
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
    private Tween _notebookVibration;

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
        _notebookVibration?.Kill();
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
        ClampPageIndex();
    }

    private void HandleEvidenceCollected(NotebookSavedData notebookData)
    {
        if (!_evidenceKeysCollected.Contains(notebookData.evidenceKey))
        {
            _evidenceKeysCollected.Add(notebookData.evidenceKey);
            ClampPageIndex();
            VibrateNotebook();
        }
    }

    private void VibrateNotebook()
    {
        if (_notebookOpenButton == null)
            return;

        if (_notebookOpenButton.transform is not RectTransform notebookTransform)
            return;

        _notebookVibration?.Kill();
        _notebookVibration = notebookTransform.DOShakeAnchorPos(
            duration: 0.6f,
            strength: new Vector2(10f, 10f),
            vibrato: 18,
            randomness: 90f,
            snapping: false,
            fadeOut: true);
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
            _rightPageButton.gameObject.SetActive(true);
        else
            _rightPageButton.gameObject.SetActive(false);

        if (_currentPageIndex <= 0)
            _leftPageButton.gameObject.SetActive(false);
        else
            _leftPageButton.gameObject.SetActive(true);
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