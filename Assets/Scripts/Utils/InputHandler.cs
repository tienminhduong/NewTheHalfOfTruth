using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static InputSystem_Actions;

public class InputHandler : MonoBehaviour, IPlayerActions, IUIActions
{
    public static event Action<Vector2> Move;
    public static event Action Interact;

    /// <summary>
    /// bool parameter indicates whether the focus is on UI (true) or Player (false).
    /// </summary>
    public static event Action<bool> OnInputFocusChange;

    public static void FocusUIInput()
    {
        OnInputFocusChange?.Invoke(true);
    }

    public static void FocusPlayerInput()
    {
        OnInputFocusChange?.Invoke(false);
    }


    InputSystem_Actions _inputActions;

    public void EnablePlayerActions()
    {
        if (_inputActions == null)
        {
            _inputActions = new InputSystem_Actions();

            _inputActions.Player.SetCallbacks(this);
            _inputActions.UI.SetCallbacks(this);
        }

        _inputActions.Enable();
        _inputActions.Player.Enable();
        _inputActions.UI.Disable();
    }

    void OnEnable()
    {
        EnablePlayerActions();
        OnInputFocusChange += FocusUI;
    }

    void OnDisable()
    {
        _inputActions.Disable();
        OnInputFocusChange -= FocusUI;
    }

    void FocusUI(bool isUIFocused)
    {
        if (isUIFocused)
        {
            _inputActions.Player.Disable();
            _inputActions.UI.Enable();
        }
        else
        {
            _inputActions.UI.Disable();
            _inputActions.Player.Enable();
        }
    }

    public void OnCancel(InputAction.CallbackContext context) { }
    public void OnClick(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("InputHandler: OnInteract called.");
            Interact?.Invoke();
        }
    }
    public void OnMiddleClick(InputAction.CallbackContext context) { }
    public void OnMove(InputAction.CallbackContext context)
    {
        Move?.Invoke(context.ReadValue<Vector2>());
    }
    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnRightClick(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }
    public void OnSprint(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }

    public void OnPoint(InputAction.CallbackContext context) { }
}