using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Input_Reader")]

public class InputReader : ScriptableObject, UserInput.IPlayerActions
{
    private UserInput user_input;

    private void OnEnable()
    {
        if (user_input == null)
        {
            user_input = new UserInput();
            user_input.Player.SetCallbacks(this);
            SetPlayer();
        }
    }

    public void SetPlayer()
    {
        user_input.Player.Enable();
    }

    public event Action<Vector2> MoveEvent;
    public event Action<Vector2> LookEvent;

    public event Action ClickEvent;
    public event Action ClickReleasedEvent;

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            ClickEvent?.Invoke();
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            ClickReleasedEvent?.Invoke();
        }
    }
}