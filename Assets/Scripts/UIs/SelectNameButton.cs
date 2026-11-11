using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
public class SelectNameButton : BaseUIButton
{
    private string _nameToSelect;
    private UnityAction _selectionAction;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _sprite;
    public string NameToSelect => _nameToSelect;

    public void SetNpcData(string name, Sprite sprite)
    {
        _nameToSelect = name;
        _nameText.text = name;
        _sprite.sprite = sprite;
    }

    public void SetSelectionData(string selectionName, string displayName, Sprite sprite)
    {
        _nameToSelect = selectionName;
        _nameText.text = displayName;
        _sprite.sprite = sprite;
    }

    public void SetSelectionAction(UnityAction action)
    {
        if (_selectionAction != null)
            RemoveListener(_selectionAction);

        _selectionAction = action;
        if (_selectionAction != null)
            AddListener(_selectionAction);
    }
}