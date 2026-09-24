using TMPro;
using TriInspector;
using UnityEngine;
using UnityEngine.UI;

public class ChatBubble : MonoBehaviour
{
    [Header("You setting")]
    [SerializeField] private Sprite _youSprite;
    [SerializeField] private Color _youColor = Color.white;
    [Header("Not you setting")]
    [SerializeField] private Sprite _notYouSprite;
    [SerializeField] private Color _notYouColor = Color.white;
    [Header("Common setting")]
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Image _spriteRenderer;
    [SerializeField] private HorizontalLayoutGroup _layoutGroup;

    public void SetText(string text)
    {
        if (_text != null)
        {
            _text.text = text;
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI component is not assigned in the inspector.");
        }
    }

    public void SetTextWithRole(string text, bool isYou)
    {
        SetText(text);
        if (isYou)
        {
            if (_youSprite != null)
            {
                _spriteRenderer.sprite = _youSprite;
                _spriteRenderer.color = _youColor;
                _text.alignment = TextAlignmentOptions.TopRight;
                _layoutGroup.childAlignment = TextAnchor.UpperRight;
            }
            else
            {
                Debug.LogWarning("You Sprite is not assigned in the inspector.");
            }
        }
        else
        {
            if (_notYouSprite != null)
            {
                _spriteRenderer.sprite = _notYouSprite;
                _spriteRenderer.color = _notYouColor;
                _text.alignment = TextAlignmentOptions.TopLeft;
                _layoutGroup.childAlignment = TextAnchor.UpperLeft;
            }
            else
            {
                Debug.LogWarning("Not You Sprite is not assigned in the inspector.");
            }
        }
    }

    void OnValidate()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
    }

    [Button("Set you")]
    private void SetYou()
    {
        SetTextWithRole("This is a test message from you.", true);
    }

    [Button("Set not you")]
    private void SetNotYou()
    {
        SetTextWithRole("This is a test message from not you.", false);
    }

    [Button("Set lorem")]
    private void SetLorem()
    {
        string loremText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
        SetText(loremText);
    }

    public void ForceUpdateLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_text.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_spriteRenderer.rectTransform);
    }
}
