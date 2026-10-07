using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BaseUIButton : MonoBehaviour
{
    private Button _button;

    void OnValidate()
    {
        _button = GetComponent<Button>();
    }

    void Start()
    {
        if (_button == null)
            _button = GetComponent<Button>();
    }

    public void SetInteractable(bool isInteractable)
    {
        if (_button == null)
            return;
        _button.interactable = isInteractable;
    }

    public void AddListener(UnityAction action)
    {
        if (_button == null)
            return;
        Debug.Log($"Adding listener to button: {action.Method.Name}");
        _button.onClick.AddListener(action);
    }

    public void RemoveListener(UnityAction action)
    {
        if (_button == null)
            return;
        _button.onClick.RemoveListener(action);
    }
}