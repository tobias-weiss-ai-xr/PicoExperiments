using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The Input Manager class for Convai, allowing you to control inputs in your project through this class.
/// It supports both the New Input System and Old Input System.
/// </summary>
[DefaultExecutionOrder(-105)]
public class ConvaiInputManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance providing easy access to the ConvaiInputManager from other scripts.
    /// </summary>
    public static ConvaiInputManager Instance { get; private set; }

    /// <summary>
    /// Input Action for player movement.
    /// </summary>
    [Header("Player Related")]
    public InputAction PlayerMovementKeyAction;

    /// <summary>
    /// Input Action for player jumping.
    /// </summary>
    public InputAction PlayerJumpKeyAction;

    /// <summary>
    /// Input Action for player running.
    /// </summary>
    public InputAction PlayerRunKeyAction;

    /// <summary>
    /// Input Action for locking the cursor.
    /// </summary>
    [Header("General")]
    public InputAction CursorLockKeyAction;

    /// <summary>
    /// Input Action for sending text.
    /// </summary>
    public InputAction TextSendKeyAction;

    /// <summary>
    /// Input Action for talk functionality.
    /// </summary>
    public InputAction TalkKeyAction;

    /// <summary>
    /// Action to open the Settings Panel.
    /// </summary>
    public InputAction SettingsKeyAction;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        // Ensure only one instance of ConvaiInputManager exists
        if (Instance != null)
        {
            Debug.LogError("There's more than one ConvaiInputManager! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Enable input actions when the object is enabled.
    /// </summary>
    private void OnEnable()
    {
        PlayerMovementKeyAction.Enable();
        PlayerJumpKeyAction.Enable();
        PlayerRunKeyAction.Enable();
        CursorLockKeyAction.Enable();
        TextSendKeyAction.Enable();
        TalkKeyAction.Enable();
        SettingsKeyAction.Enable();
    }

    /// <summary>
    /// Checks if the left mouse button was pressed.
    /// </summary>
    public bool WasMouseLeftButtonPressed()
    {
        // Check if the left mouse button was pressed this frame
#if ENABLE_INPUT_SYSTEM
        return Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    /// <summary>
    /// Gets the current mouse position.
    /// </summary>
    public Vector2 GetMousePosition()
    {
        // Get the current mouse position
#if ENABLE_INPUT_SYSTEM
        return Mouse.current.position.ReadValue();
#else
        return Input.mousePosition;
#endif
    }

    /// <summary>
    /// Gets the vertical movement of the mouse.
    /// </summary>
    public float GetMouseYAxis()
    {
        // Get the vertical movement of the mouse
#if ENABLE_INPUT_SYSTEM
        return Mouse.current.delta.y.ReadValue();
#else
        return Input.GetAxis("Mouse Y");
#endif
    }

    /// <summary>
    /// Gets the horizontal movement of the mouse.
    /// </summary>
    public float GetMouseXAxis()
    {
        // Get the horizontal movement of the mouse
#if ENABLE_INPUT_SYSTEM
        return Mouse.current.delta.x.ReadValue();
#else
        return Input.GetAxis("Mouse X");
#endif
    }

    // General input methods
    /// <summary>
    /// Checks if the cursor lock key was pressed.
    /// </summary>
    public bool WasCursorLockKeyPressed()
    {
        // Check if the cursor lock key was pressed this frame
#if ENABLE_INPUT_SYSTEM
        return CursorLockKeyAction.WasPressedThisFrame();
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    /// <summary>
    /// Checks if the text send key was pressed.
    /// </summary>
    public bool WasTextSendKeyPressed()
    {
        // Check if the text send key was pressed this frame
#if ENABLE_INPUT_SYSTEM
        return TextSendKeyAction.WasPressedThisFrame();
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    /// <summary>
    /// Checks if the talk key was pressed.
    /// </summary>
    public bool WasTalkKeyPressed()
    {
        // Check if the talk key was pressed this frame
#if ENABLE_INPUT_SYSTEM
        return TalkKeyAction.WasPressedThisFrame();
#else
        return Input.GetKeyDown(KeyCode.T);
#endif
    }

    /// <summary>
    /// Checks if the talk key is being held down.
    /// </summary>
    public bool IsTalkKeyHeld()
    {
        // Check if the talk key is being held down
#if ENABLE_INPUT_SYSTEM
        return TalkKeyAction.IsPressed();
#else
        return Input.GetKey(KeyCode.T);
#endif
    }

    /// <summary>
    /// Retrieves the InputAction associated with the talk key.
    /// </summary>
    /// <returns>The InputAction for handling talk-related input.</returns>
    public InputAction GetTalkKeyAction() => TalkKeyAction;

    /// <summary>
    /// Checks if the talk key was released.
    /// </summary>
    public bool WasTalkKeyReleased()
    {
        // Check if the talk key was released this frame
#if ENABLE_INPUT_SYSTEM
        return TalkKeyAction.WasReleasedThisFrame();
#else
        return Input.GetKeyUp(KeyCode.T);
#endif
    }

    /// <summary>
    /// Checks if the Settings key was pressed.
    /// </summary>
    public bool WasSettingsKeyPressed()
    {
        // Check if the Settings key was pressed this frame
#if ENABLE_INPUT_SYSTEM
        return SettingsKeyAction.WasPressedThisFrame();
#else
        return Input.GetKeyDown(KeyCode.F10);
#endif
    }

    // Player related input methods
    /// <summary>
    /// Checks if the jump key was pressed.
    /// </summary>
    public bool WasJumpKeyPressed()
    {
        // Check if the jump key was pressed this frame
#if ENABLE_INPUT_SYSTEM
        return PlayerJumpKeyAction.WasPressedThisFrame();
#else
        return Input.GetButton("Jump");
#endif
    }

    /// <summary>
    /// Checks if the run key is being held down.
    /// </summary>
    public bool IsRunKeyHeld()
    {
        // Check if the run key is being held down
#if ENABLE_INPUT_SYSTEM
        return PlayerRunKeyAction.IsPressed();
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    /// <summary>
    /// Gets the player's movement input vector.
    /// </summary>
    public Vector2 GetPlayerMoveVector()
    {
        // Get the player's movement input vector
#if ENABLE_INPUT_SYSTEM
        return PlayerMovementKeyAction.ReadValue<Vector2>();
#else
        Vector2 inputMoveDir = new Vector2(0, 0);
        // Manual input for player movement
        if (Input.GetKey(KeyCode.W))
        {
            inputMoveDir.y += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            inputMoveDir.y -= 1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            inputMoveDir.x -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            inputMoveDir.x += 1f;
        }

        return inputMoveDir;
#endif
    }
}