using TriInspector;
using UnityEngine;
using UnityEngine.UI;

public class AutoSnapBottomScollView : MonoBehaviour
{
    [SerializeField] private Scrollbar _scrollbar;
    [SerializeField] private float _snapThreshold = 0.01f;
    [ReadOnly] [SerializeField] private bool _isSnapping = true;

    void OnValidate()
    {
        if (_scrollbar == null)
        {
            if (TryGetComponent<Scrollbar>(out var foundScrollbar))
            {
                _scrollbar = foundScrollbar;
            }
            else
            {
                Debug.LogError("Scrollbar reference is missing and no Scrollbar component found on the GameObject.");
            }
        }
    }

    void OnEnable()
    {
        if (_scrollbar != null)
        {
            _scrollbar.onValueChanged.AddListener(HandleValueChanged);
        }
        else
        {
            Debug.LogError("Scrollbar reference is missing. Please assign a Scrollbar in the inspector.");
        }
    }

    void OnDisable()
    {
        if (_scrollbar != null)
        {
            _scrollbar.onValueChanged.RemoveListener(HandleValueChanged);
        }
    }

    void HandleValueChanged(float value)
    {
        if (value < _snapThreshold)
        {
            _scrollbar.value = 0f;
            _isSnapping = true;
        }
    }
}