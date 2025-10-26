using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Gerencia a exibição da UI de diálogo em runtime usando UI Toolkit.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class DialogueUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float typewriterSpeed = 50f; // caracteres por segundo

    [Header("Audio")]
    [SerializeField] private string uiClickSoundID = "UI_Click";

    private VisualElement root;
    private VisualElement dialoguePanel;
    private Label characterNameLabel;
    private Label dialogueTextLabel;
    private VisualElement characterIconImage;
    private VisualElement optionsContainer;

    private TypewriterEffect typewriter;
    private DialogueUIController controller;
    private Action onSpeechComplete;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        InitializeUI();

        typewriter = new TypewriterEffect(dialogueTextLabel, typewriterSpeed);
        controller = new DialogueUIController(this);

        HideUI();
    }

    private void InitializeUI()
    {
        root = uiDocument.rootVisualElement;

        dialoguePanel = root.Q<VisualElement>("DialoguePanel");
        characterNameLabel = root.Q<Label>("CharacterName");
        dialogueTextLabel = root.Q<Label>("DialogueText");
        characterIconImage = root.Q<VisualElement>("CharacterIcon");
        optionsContainer = root.Q<VisualElement>("OptionsContainer");
    }

    public void DisplaySpeech(SpeechNodeData node, Action onComplete)
    {
        ShowUI();

        onSpeechComplete = onComplete;

        characterNameLabel.text = node.CharacterName;

        if (node.CharacterIcon != null)
        {
            characterIconImage.style.backgroundImage = new StyleBackground(node.CharacterIcon);
            characterIconImage.style.display = DisplayStyle.Flex;
        }
        else
        {
            characterIconImage.style.display = DisplayStyle.None;
        }

        string processedText = TextProcessor.ProcessText(node.DialogueText);

        typewriter.StartTyping(processedText, () =>
        {
            // Typewriter completado
            if (node.DisplayDuration > 0)
            {
                StartCoroutine(AutoAdvanceCoroutine(node.DisplayDuration));
            }
        });

        optionsContainer.style.display = DisplayStyle.None;
    }

    public void DisplayOptions(OptionNodeData node, Action<int> onOptionSelected)
    {
        optionsContainer.Clear();
        optionsContainer.style.display = DisplayStyle.Flex;

        var availableOptions = node.GetAvailableOptions();

        for (int i = 0; i < availableOptions.Count; i++)
        {
            int optionIndex = node.Options.IndexOf(availableOptions[i]);
            var option = availableOptions[i];

            var button = new Button(() =>
            {
                PlayUISound(uiClickSoundID);
                onOptionSelected?.Invoke(optionIndex);
            })
            {
                text = TextProcessor.ProcessText(option.optionText)
            };

            button.AddToClassList("dialogue-option-button");
            optionsContainer.Add(button);
        }

        // Foca no primeiro botão
        var firstButton = optionsContainer.Q<Button>();
        firstButton?.Focus();

    }

    public void HideUI()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.style.display = DisplayStyle.None;
        }

        typewriter?.Stop();
    }

    private void ShowUI()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.style.display = DisplayStyle.Flex;
        }
    }

    private IEnumerator AutoAdvanceCoroutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        onSpeechComplete?.Invoke();
    }

    private void PlayUISound(string soundID)
    {
        // Usa canal de áudio unscaled dedicado para UI
        // Integração com sistema de áudio será feita através da interface
        if (!string.IsNullOrEmpty(soundID))
        {
            // Signal.PlaySFX(soundID) seria chamado aqui via interface
        }
    }

    private void Update()
    {
        // Input para avançar diálogo (New Input System)
        if (Input.anyKey)
        {
            if (typewriter.IsTyping)
            {
                typewriter.CompleteInstantly();
            }
            else
            {
                onSpeechComplete?.Invoke();
            }
        }
    }
}