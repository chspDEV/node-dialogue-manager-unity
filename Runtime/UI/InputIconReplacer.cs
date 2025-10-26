using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Substitui tags de input por ícones baseados no dispositivo atual.
/// </summary>
public static class InputIconReplacer
{
    private static Dictionary<string, Sprite> keyboardIcons = new Dictionary<string, Sprite>();
    private static Dictionary<string, Sprite> gamepadIcons = new Dictionary<string, Sprite>();

    public static void Initialize()
    {
        // Carrega ícones de Resources
        LoadIcons();
    }

    private static void LoadIcons()
    {
        // Exemplo de carregamento de ícones
        keyboardIcons["Interact"] = Resources.Load<Sprite>("InputIcons/Keyboard/E_Key");
        keyboardIcons["Jump"] = Resources.Load<Sprite>("InputIcons/Keyboard/Space_Key");

        gamepadIcons["Interact"] = Resources.Load<Sprite>("InputIcons/Xbox/A_Button");
        gamepadIcons["Jump"] = Resources.Load<Sprite>("InputIcons/Xbox/A_Button");
    }

    public static Sprite GetIconForAction(string actionName)
    {
        bool isGamepad = Gamepad.current != null;
        var iconDict = isGamepad ? gamepadIcons : keyboardIcons;

        return iconDict.TryGetValue(actionName, out var icon) ? icon : null;
    }

    public static string GetTextForAction(string actionName)
    {
        var action = InputSystem.actions?.FindAction(actionName);
        if (action == null) return actionName;

        var binding = action.GetBindingForControl(action.controls.FirstOrDefault());
        return binding?.ToDisplayString() ?? actionName;
    }
}