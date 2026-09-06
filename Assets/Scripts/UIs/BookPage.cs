using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookPage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI evidenceNameText;
    [SerializeField] private TextMeshProUGUI evidenceDescriptionText;
    [SerializeField] private Image image;
    [SerializeField] private TMP_InputField playerNoteInputField;
    [SerializeField] private TextMeshProUGUI guideText;
    private string _evidenceKey;

    private SavedDataService _savedDataService;

    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        ServiceLocator.TryOrWaitForService<SavedDataService>(SetupSavedDataService);
        if (playerNoteInputField != null)
            playerNoteInputField.onEndEdit.AddListener(HandleEndEditNote);
    }

    void OnDisable()
    {
        ServiceLocator.RemoveServiceCallback<SavedDataService>(SetupSavedDataService);
        if (playerNoteInputField != null)
            playerNoteInputField.onEndEdit.RemoveListener(HandleEndEditNote);
    }

    public void SetPageVisibility(bool isVisible)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
        }
    }

    public void SetEvidenceData(EvidenceData evidenceData)
    {
        _evidenceKey = evidenceData.key;
        evidenceNameText.text = evidenceData.name;
        evidenceDescriptionText.text = evidenceData.description;
        image.sprite = evidenceData.evidenceSprite;
        playerNoteInputField.text = evidenceData.playerNote;
        guideText.text = evidenceData.playerGuide;
    }

    private void HandleEndEditNote(string note)
    {
        if (_savedDataService != null)
        {
            _savedDataService.SaveNotebookData(_evidenceKey, note);
        }
        else
        {
            Debug.LogWarning("SavedDataService is not available. Note will not be saved.");
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
    }

}
