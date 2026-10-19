using UnityEngine;
using UnityEngine.UI;

public class BaseUIPanelSystem : MonoBehaviour
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private GameObject _contentPanel;


    protected virtual bool ShowPanelOnStart => false;
    public bool IsPanelOpen => _contentPanel != null && _contentPanel.activeSelf;

    protected virtual void Awake()
    {
        if (!ShowPanelOnStart)
        {
            _closeButton?.gameObject.SetActive(false);
            _contentPanel.SetActive(false);
        }
    }

    protected virtual void OnEnable()
    {
        InputHandler.Cancel += HandleCancel;

        if (_closeButton != null)
            _closeButton.onClick.AddListener(ClosePanel);
    }

    protected virtual void OnDisable()
    {
        InputHandler.Cancel -= HandleCancel;

        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(ClosePanel);
    }

    private void HandleCancel()
    {
        if (IsPanelOpen)
            ClosePanel();
    }

    protected virtual void ClosePanel()
    {
        if (_contentPanel == null)
            return;

        InputHandler.FocusPlayerInput();
        _contentPanel.SetActive(false);
        _closeButton?.gameObject.SetActive(false);

        Debug.Log("Panel closed. Player input focus restored.");
    }

    protected virtual void OpenPanel()
    {
        if (_contentPanel == null)
            return;

        InputHandler.FocusUIInput();
        _contentPanel.SetActive(true);
        _closeButton?.gameObject.SetActive(true);
    }
}