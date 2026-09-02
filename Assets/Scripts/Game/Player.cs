using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{

    [SerializeField] private List<CD.AnimClipReference> _animRefs;
    private Animator _animator;
    private HumanAnimSystem _animSystem;

    [SerializeField] private float _moveSpeed = 5f;
    private Vector2 _direction;

    void OnValidate()
    {
        _animator = GetComponent<Animator>();
    }

    void Start()
    {
        _animSystem = new HumanAnimSystem(_animator, _animRefs);
    }

    void OnEnable()
    {
        InputHandler.Move += OnMove;
    }

    void OnDisable()
    {
        InputHandler.Move -= OnMove;
    }

    void Update()
    {
        transform.Translate(_moveSpeed * Time.deltaTime * _direction);
    }

    private void OnMove(Vector2 moveInput)
    {
        if (!_animSystem.IsInitialized)
            return;

        if (Mathf.Abs(moveInput.x) == Mathf.Abs(moveInput.y))
        {
            if (_direction.x != 0)
                moveInput.x = 0;
            else
                moveInput.y = 0;
        }
        else
        {
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                moveInput.y = 0;
            else
                moveInput.x = 0;
        }

        _direction = moveInput.normalized;
        _animSystem.PlayAnimation(moveInput);
    }

    void OnDestroy()
    {
        _animSystem?.Destroy();
    }
}