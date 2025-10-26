using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador adicional para lógica de UI e navegação com New Input System.
/// Compatible com com.unity.inputsystem
/// </summary>
public class DialogueUIController
{
    private DialogueUIManager manager;
    private InputActionAsset inputActions;
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    public DialogueUIController(DialogueUIManager manager)
    {
        this.manager = manager;
        InitializeInput();
    }

    private void InitializeInput()
    {
        // Cria um InputActionMap para o Dialogue System
        var actionMap = new InputActionMap("DialogueUI");

        // Ação de navegação (setas, D-pad, stick analógico)
        navigateAction = actionMap.AddAction("Navigate", InputActionType.Value, "<Gamepad>/leftStick");
        navigateAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        navigateAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        navigateAction.AddBinding("<Gamepad>/dpad");

        // Ação de submit (Space, Enter, A do gamepad)
        submitAction = actionMap.AddAction("Submit", InputActionType.Button);
        submitAction.AddBinding("<Keyboard>/enter");
        submitAction.AddBinding("<Keyboard>/space");
        submitAction.AddBinding("<Gamepad>/buttonSouth"); // A/Cross

        // Ação de cancelar (ESC, B do gamepad)
        cancelAction = actionMap.AddAction("Cancel", InputActionType.Button);
        cancelAction.AddBinding("<Keyboard>/escape");
        cancelAction.AddBinding("<Gamepad>/buttonEast"); // B/Circle

        // Habilita as ações
        actionMap.Enable();
    }

    /// <summary>
    /// Habilita o input do diálogo
    /// </summary>
    public void EnableInput()
    {
        navigateAction?.Enable();
        submitAction?.Enable();
        cancelAction?.Enable();
    }

    /// <summary>
    /// Desabilita o input do diálogo
    /// </summary>
    public void DisableInput()
    {
        navigateAction?.Disable();
        submitAction?.Disable();
        cancelAction?.Disable();
    }

    /// <summary>
    /// Limpa recursos ao destruir
    /// </summary>
    public void Dispose()
    {
        navigateAction?.Disable();
        submitAction?.Disable();
        cancelAction?.Disable();

        navigateAction?.Dispose();
        submitAction?.Dispose();
        cancelAction?.Dispose();
    }

    /// <summary>
    /// Retorna se o botão de submit foi pressionado neste frame
    /// </summary>
    public bool WasSubmitPressed()
    {
        return submitAction?.WasPressedThisFrame() ?? false;
    }

    /// <summary>
    /// Retorna se o botão de cancelar foi pressionado neste frame
    /// </summary>
    public bool WasCancelPressed()
    {
        return cancelAction?.WasPressedThisFrame() ?? false;
    }

    /// <summary>
    /// Retorna o valor da navegação (Vector2)
    /// </summary>
    public Vector2 GetNavigationValue()
    {
        return navigateAction?.ReadValue<Vector2>() ?? Vector2.zero;
    }
}