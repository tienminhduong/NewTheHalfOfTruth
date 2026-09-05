using UnityEngine;
using UnityEngine.UI;

public class BaseUIPanelSystem : MonoBehaviour
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private GameObject _contentPanel;

    private void OnEnable()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnDisable()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(ClosePanel);
    }

    protected virtual void ClosePanel()
    {
        if (_contentPanel == null)
            return;

        InputHandler.FocusPlayerInput();
        _contentPanel.SetActive(false);
    }

    protected virtual void OpenPanel()
    {
        if (_contentPanel == null)
            return;

        InputHandler.FocusUIInput();
        _contentPanel.SetActive(true);
    }
}