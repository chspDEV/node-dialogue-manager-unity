using System;

/// <summary>
/// Integração com Unity Localization Package.
/// </summary>
public class LocalizationIntegration
{
    private static string currentLanguage = "en";

    public static string CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            currentLanguage = value;
            OnLanguageChanged?.Invoke(value);
        }
    }

    public static event Action<string> OnLanguageChanged;

    public static string GetLocalizedText(string key, string fallbackText)
    {
        // Integração com Unity Localization
        // var localizedString = LocalizationSettings.StringDatabase.GetLocalizedString("DialogueTable", key);

        // Fallback simples
        return fallbackText;
    }

    public static string GetLocalizedDialogueText(DialogueAsset asset, SpeechNodeData node)
    {
        if (string.IsNullOrEmpty(asset.LocalizationTableReference))
        {
            return node.DialogueText;
        }

        string key = $"{asset.AssetGUID}_{node.GUID}_text";
        return GetLocalizedText(key, node.DialogueText);
    }

    public static string GetLocalizedCharacterName(DialogueAsset asset, SpeechNodeData node)
    {
        if (string.IsNullOrEmpty(asset.LocalizationTableReference))
        {
            return node.CharacterName;
        }

        string key = $"{asset.AssetGUID}_{node.GUID}_name";
        return GetLocalizedText(key, node.CharacterName);
    }
}