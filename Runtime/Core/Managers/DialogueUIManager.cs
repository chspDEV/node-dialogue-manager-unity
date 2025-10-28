using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

/// <summary>
/// Gerencia a exibição da UI de diálogo em runtime usando UI Toolkit.
/// Sistema completamente responsivo e moderno.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class DialogueUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float typewriterSpeed = 50f; // caracteres por segundo

    [Header("Audio")]
    [SerializeField] private string uiClickSoundID = "UI_Click";

    [Header("Responsiveness")]
    [SerializeField] private bool autoScale = true;
    [SerializeField] private float minWidth = 320f;
    [SerializeField] private float maxWidth = 1920f;

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

       // HideUI();
    }

    private void InitializeUI()
    {
        root = uiDocument.rootVisualElement;

        dialoguePanel = root.Q<VisualElement>("DialoguePanel");
        characterNameLabel = root.Q<Label>("CharacterName");
        dialogueTextLabel = root.Q<Label>("DialogueText");
        characterIconImage = root.Q<VisualElement>("CharacterIcon");
        optionsContainer = root.Q<VisualElement>("OptionsContainer");

        // 🔧 CONFIGURAÇÃO DE PICKING MODE (não pode ser feito no USS)
        if (root != null)
            root.pickingMode = PickingMode.Position;

        if (dialoguePanel != null)
            dialoguePanel.pickingMode = PickingMode.Position;

        if (optionsContainer != null)
        {
            optionsContainer.pickingMode = PickingMode.Position;
            optionsContainer.focusable = false;
        }

        // 🎨 Aplicar responsividade
        if (autoScale)
        {
            ApplyResponsiveLayout();
        }
    }

    private void ApplyResponsiveLayout()
    {
        if (dialoguePanel == null) return;

        // Escala baseada na largura da tela
        float screenWidth = Screen.width;
        float scale = Mathf.Clamp(screenWidth / 1920f, minWidth / 1920f, maxWidth / 1920f);

        // Aplica escala proporcional
        root.style.scale = new Scale(new Vector3(scale, scale, 1f));
    }

    public void DisplaySpeech(SpeechNodeData node, Action onComplete)
    {
        ShowUI();

        onSpeechComplete = onComplete;

        // 🎨 Animação de entrada (opcional)
        AnimateIn(dialoguePanel);

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

        Debug.Log($"[DialogueUI] Displaying {availableOptions.Count} options");

        for (int i = 0; i < availableOptions.Count; i++)
        {
            int optionIndex = node.Options.IndexOf(availableOptions[i]);
            var option = availableOptions[i];

            // 🔧 CORREÇÃO CRÍTICA: Criar botão com configuração completa
            var button = CreateOptionButton(option.optionText, optionIndex, onOptionSelected);

            optionsContainer.Add(button);

            Debug.Log($"[DialogueUI] Added button {i}: '{option.optionText}' (index {optionIndex})");
        }

        // 🎯 Foca no primeiro botão após frame render
        optionsContainer.schedule.Execute(() =>
        {
            var firstButton = optionsContainer.Q<Button>();
            if (firstButton != null)
            {
                firstButton.Focus();
                Debug.Log("[DialogueUI] First button focused");
            }
        }).ExecuteLater(50); // Delay pequeno para garantir render
    }

    /// <summary>
    /// 🔧 MÉTODO CORRIGIDO: Cria botão de opção com todas as configurações necessárias
    /// </summary>
    private Button CreateOptionButton(string optionText, int optionIndex, Action<int> onOptionSelected)
    {
        var button = new Button();

        // 🎯 CONFIGURAÇÕES CRÍTICAS DE INTERATIVIDADE
        button.pickingMode = PickingMode.Position;
        button.focusable = true;
        button.tabIndex = optionIndex;

        // 🎨 Texto processado
        button.text = TextProcessor.ProcessText(optionText);

        // 🎨 Classes de estilo
        button.AddToClassList("dialogue-option-button");
        button.AddToClassList("button-fade-in"); // Animação

        // 🎵 Callback de clique
        button.clicked += () =>
        {
            Debug.Log($"[DialogueUI] Button clicked: index {optionIndex}");
            PlayUISound(uiClickSoundID);

            // Feedback visual
            button.AddToClassList("button-clicked");

            onOptionSelected?.Invoke(optionIndex);
        };

        // 🎨 Hover effect via código (fallback se USS falhar)
        button.RegisterCallback<MouseEnterEvent>(evt =>
        {
            button.AddToClassList("button-hover");
        });

        button.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            button.RemoveFromClassList("button-hover");
        });

        return button;
    }

    public void HideUI()
    {
        if (dialoguePanel != null)
        {
            AnimateOut(dialoguePanel, () =>
            {
                dialoguePanel.style.display = DisplayStyle.None;
            });
        }

        typewriter?.Stop();
    }

    private void ShowUI()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.style.display = DisplayStyle.Flex;
            dialoguePanel.style.visibility = Visibility.Visible;
            dialoguePanel.style.opacity = 1f;

            // 🔧 Força o painel para frente
            dialoguePanel.BringToFront();

            Debug.Log("[DialogueUI] UI shown and brought to front");
        }
    }

    private IEnumerator AutoAdvanceCoroutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        onSpeechComplete?.Invoke();
    }

    private void PlayUISound(string soundID)
    {
        if (!string.IsNullOrEmpty(soundID))
        {
            // Signal.PlaySFX(soundID) seria chamado aqui via interface
            Debug.Log($"[Audio] Playing UI sound: {soundID}");
        }
    }

    /// <summary>
    /// 🎨 Animação suave de entrada
    /// </summary>
    private void AnimateIn(VisualElement element)
    {
        if (element == null) return;

        element.style.opacity = 0f;
        element.experimental.animation
            .Start(new StyleValues { opacity = 1f }, 300)
            .Ease(Easing.OutCubic);
    }

    /// <summary>
    /// 🎨 Animação suave de saída
    /// </summary>
    private void AnimateOut(VisualElement element, Action onComplete)
    {
        if (element == null)
        {
            onComplete?.Invoke();
            return;
        }

        element.experimental.animation
            .Start(new StyleValues { opacity = 0f }, 200)
            .Ease(Easing.InCubic)
            .OnCompleted(onComplete);
    }

    private void Update()
    {
        // Input para avançar diálogo
        if (Input.anyKeyDown)
        {
            if (typewriter != null && typewriter.IsTyping)
            {
                typewriter.CompleteInstantly();
            }
            else if (optionsContainer.style.display == DisplayStyle.None)
            {
                // Só avança se não houver opções visíveis
                onSpeechComplete?.Invoke();
            }
        }

        // 🎨 Atualiza responsividade em tempo real (opcional)
        if (autoScale && Screen.width != lastScreenWidth)
        {
            lastScreenWidth = Screen.width;
            ApplyResponsiveLayout();
        }
    }

    private float lastScreenWidth;
}