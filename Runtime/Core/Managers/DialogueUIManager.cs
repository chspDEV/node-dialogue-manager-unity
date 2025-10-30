using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental; // Para animações

/// <summary>
/// Gerencia a exibição da UI de diálogo em runtime usando UI Toolkit.
/// ⚠️ ATUALIZADO: Corrige o bug de input que misturava avanço de fala e seleção de opção.
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

    // Elementos da UI
    private VisualElement root;
    private VisualElement dialoguePanel;
    private Label characterNameLabel;
    private Label dialogueTextLabel;
    private VisualElement characterIconImage;
    private VisualElement optionsContainer;

    // Controladores
    private TypewriterEffect typewriter;
    private DialogueUIController controller; // O seu controlador de input
    private Action onSpeechComplete;

    // Estado
    private bool isOptionsVisible = false; //  Flag para controlar o input
    private float lastScreenWidth;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        InitializeUI();

        typewriter = new TypewriterEffect(dialogueTextLabel, typewriterSpeed);
        controller = new DialogueUIController(this); // Inicializa o controlador

        HideUI(); // Garante que o input esteja desativado no início
    }

    private void InitializeUI()
    {
        root = uiDocument.rootVisualElement;

        dialoguePanel = root.Q<VisualElement>("DialoguePanel");
        characterNameLabel = root.Q<Label>("CharacterName");
        dialogueTextLabel = root.Q<Label>("DialogueText");
        characterIconImage = root.Q<VisualElement>("CharacterIcon");
        optionsContainer = root.Q<VisualElement>("OptionsContainer");

        // Configuração de Picking Mode
        if (root != null)
            root.pickingMode = PickingMode.Position;
        if (dialoguePanel != null)
            dialoguePanel.pickingMode = PickingMode.Position;
        if (optionsContainer != null)
        {
            optionsContainer.pickingMode = PickingMode.Position;
            optionsContainer.focusable = false;
        }

        if (autoScale)
        {
            ApplyResponsiveLayout();
            lastScreenWidth = Screen.width; // Define o valor inicial
        }
    }

    private void ApplyResponsiveLayout()
    {
        if (dialoguePanel == null) return;
        float screenWidth = Screen.width;
        float scale = Mathf.Clamp(screenWidth / 1920f, minWidth / 1920f, maxWidth / 1920f);
        root.style.scale = new Scale(new Vector3(scale, scale, 1f));
    }

    /// <summary>
    /// Exibe uma linha de fala (SpeechNode).
    /// </summary>
    public void DisplaySpeech(SpeechNodeData node, Action onComplete)
    {
        ShowUI(); // Ativa a UI e o input

        isOptionsVisible = false; // ❗ CRÍTICO: Define que estamos em modo "fala"
        onSpeechComplete = onComplete;

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

        optionsContainer.style.display = DisplayStyle.None; // Esconde opções
    }

    /// <summary>
    /// Exibe um conjunto de opções (OptionNode).
    /// </summary>
    public void DisplayOptions(OptionNodeData node, Action<int> onOptionSelected)
    {
        isOptionsVisible = true; // ❗ CRÍTICO: Define que estamos em modo "opções"
        optionsContainer.Clear();
        optionsContainer.style.display = DisplayStyle.Flex;

        var availableOptions = node.GetAvailableOptions();
        Debug.Log($"[DialogueUI] A exibir {availableOptions.Count} opções");

        for (int i = 0; i < availableOptions.Count; i++)
        {
            int optionIndex = node.Options.IndexOf(availableOptions[i]);
            var option = availableOptions[i];

            var button = CreateOptionButton(option.optionText, optionIndex, onOptionSelected);
            optionsContainer.Add(button);
        }

        // Foca no primeiro botão
        optionsContainer.schedule.Execute(() =>
        {
            var firstButton = optionsContainer.Q<Button>();
            if (firstButton != null)
            {
                firstButton.Focus();
                Debug.Log("[DialogueUI] Primeiro botão focado");
            }
        }).ExecuteLater(50);
    }

    /// <summary>
    /// Cria um botão de opção configurado corretamente.
    /// </summary>
    private Button CreateOptionButton(string optionText, int optionIndex, Action<int> onOptionSelected)
    {
        var button = new Button();

        button.pickingMode = PickingMode.Position;
        button.focusable = true; // Permite navegação por teclado/gamepad
        button.tabIndex = optionIndex;
        button.text = TextProcessor.ProcessText(optionText);

        button.AddToClassList("dialogue-option-button");
        button.AddToClassList("button-fade-in");

        button.clicked += () =>
        {
            Debug.Log($"[DialogueUI] Botão clicado: index {optionIndex}");
            PlayUISound(uiClickSoundID);
            button.AddToClassList("button-clicked");

            // Desativa o input para evitar cliques duplos
            controller?.DisableInput();

            onOptionSelected?.Invoke(optionIndex);
        };

        // Handlers de hover (opcional, bom para feedback)
        button.RegisterCallback<MouseEnterEvent>(evt => button.AddToClassList("button-hover"));
        button.RegisterCallback<MouseLeaveEvent>(evt => button.RemoveFromClassList("button-hover"));

        return button;
    }

    /// <summary>
    /// Esconde a UI do diálogo e desativa o input.
    /// </summary>
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
        controller?.DisableInput(); // ❗ CRÍTICO: Desativa o input
        isOptionsVisible = false;
    }

    /// <summary>
    /// Mostra a UI do diálogo e ativa o input.
    /// </summary>
    private void ShowUI()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.style.display = DisplayStyle.Flex;
            dialoguePanel.style.visibility = Visibility.Visible;
            dialoguePanel.style.opacity = 1f;
            dialoguePanel.BringToFront();
        }
        controller?.EnableInput(); // ❗ CRÍTICO: Ativa o input
    }

    private IEnumerator AutoAdvanceCoroutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        // Garante que não avança se o jogador abriu um menu, etc.
        if (this.enabled && onSpeechComplete != null)
        {
            onSpeechComplete.Invoke();
        }
    }

    private void PlayUISound(string soundID)
    {
        if (!string.IsNullOrEmpty(soundID))
        {
            Debug.Log($"[Audio] A tocar som de UI: {soundID}");
            // Integração de áudio aqui
        }
    }

    // --- Animações (Já existentes no seu script) ---
    private void AnimateIn(VisualElement element)
    {
        if (element == null) return;
        element.style.opacity = 0f;
        element.experimental.animation
            .Start(new StyleValues { opacity = 1f }, 300)
            .Ease(Easing.OutCubic);
    }

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

    // --- ⬇️ MÉTODO UPDATE CORRIGIDO ⬇️ ---

    /// <summary>
    /// Substitui a lógica de Input.anyKey por uma verificação controlada
    /// usando o DialogueUIController.
    /// </summary>
    private void Update()
    {
        // 1. Lógica de Input (agora usa o Controller)
        if (controller != null && controller.WasSubmitPressed()) // Verifica "Enter", "Space", etc.
        {
            // Se o typewriter estiver a escrever, completa-o
            if (typewriter != null && typewriter.IsTyping)
            {
                typewriter.CompleteInstantly();
            }
            // Se o typewriter NÃO estiver a escrever E as OPÇÕES NÃO estiverem visíveis...
            else if (!isOptionsVisible)
            {
                // ...então avança o diálogo.
                onSpeechComplete?.Invoke();
            }
            // (Se as opções estiverem visíveis, o WasSubmitPressed não faz nada,
            // pois a seleção é tratada pelo evento .clicked do botão)
        }

        // 2. Lógica de Responsividade (Mantida do seu script original)
        if (autoScale && Screen.width != lastScreenWidth)
        {
            lastScreenWidth = Screen.width;
            ApplyResponsiveLayout();
        }
    }
}