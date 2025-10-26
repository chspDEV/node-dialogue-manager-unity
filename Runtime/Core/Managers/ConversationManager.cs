using System;
using UnityEngine;

/// <summary>
/// Singleton que gerencia o fluxo de conversas em runtime.
/// API principal para iniciar diálogos e interagir com o sistema.
/// </summary>
public class ConversationManager : MonoBehaviour
{
    private static ConversationManager instance;
    public static ConversationManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[ConversationManager]");
                instance = go.AddComponent<ConversationManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Eventos globais
    public static event Action<DialogueAsset> OnConversationStarted;
    public static event Action<DialogueAsset> OnConversationEnded;
    public static event Action<SpeechNodeData> OnNodeDisplayed;
    public static event Action<OptionNodeData> OnOptionsDisplayed;

    [SerializeField] private DialogueUIManager uiManager;
    [SerializeField] private bool pauseGameDuringDialogue = true;
    [SerializeField] private bool useUnscaledTime = true;

    private DialogueAsset currentDialogue;
    private BaseNodeData currentNode;
    private DialogueProcessor processor;
    private IAudioIntegration audioIntegration;
    private float previousTimeScale;
    private bool isConversationActive;

    public DialogueAsset CurrentDialogue => currentDialogue;
    public bool IsConversationActive => isConversationActive;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        processor = new DialogueProcessor();

        // Tenta encontrar integração com Signal Audio Manager
        audioIntegration = FindAudioIntegration();

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<DialogueUIManager>();
        }
    }

    /// <summary>
    /// Inicia uma conversa a partir de um DialogueAsset.
    /// </summary>
    public void StartConversation(DialogueAsset dialogue)
    {
        if (dialogue == null)
        {
            Debug.LogError("Cannot start conversation: DialogueAsset is null");
            return;
        }

        if (isConversationActive)
        {
            Debug.LogWarning("A conversation is already active. Ending previous conversation.");
            EndConversation();
        }

        currentDialogue = dialogue;
        currentNode = dialogue.RootNode;

        if (currentNode == null)
        {
            Debug.LogError("DialogueAsset has no Root Node!");
            return;
        }

        isConversationActive = true;

        if (pauseGameDuringDialogue)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        OnConversationStarted?.Invoke(dialogue);

        // Move para o primeiro nó conectado ao Root
        AdvanceToNextNode();
    }

    /// <summary>
    /// Avança para o próximo nó na conversa.
    /// </summary>
    public void AdvanceToNextNode(int portIndex = 0)
    {
        if (currentNode != null)
        {
            currentNode.OnNodeExit();
        }

        var nextNode = processor.GetNextNode(currentDialogue, currentNode, portIndex);

        if (nextNode == null)
        {
            EndConversation();
            return;
        }

        currentNode = nextNode;
        currentNode.OnNodeEnter();

        if (currentNode is SpeechNodeData speechNode)
        {
            DisplaySpeechNode(speechNode);
        }
        else if (currentNode is OptionNodeData optionNode)
        {
            DisplayOptionNode(optionNode);
        }
    }

    private void DisplaySpeechNode(SpeechNodeData node)
    {
        OnNodeDisplayed?.Invoke(node);

        // Reproduz áudio via integração
        if (!string.IsNullOrEmpty(node.AudioSignalID))
        {
            audioIntegration?.PlayDialogueAudio(node.AudioSignalID);
        }

        // Delega exibição para o UI Manager
        uiManager?.DisplaySpeech(node, () => AdvanceToNextNode());
    }

    private void DisplayOptionNode(OptionNodeData node)
    {
        var availableOptions = node.GetAvailableOptions();

        if (availableOptions.Count == 0)
        {
            Debug.LogWarning("OptionNode has no available options. Ending conversation.");
            EndConversation();
            return;
        }

        OnOptionsDisplayed?.Invoke(node);

        uiManager?.DisplayOptions(node, (optionIndex) =>
        {
            node.Options[optionIndex].onOptionSelected?.Invoke();
            AdvanceToNextNode(optionIndex);
        });
    }

    /// <summary>
    /// Encerra a conversa atual.
    /// </summary>
    public void EndConversation()
    {
        if (!isConversationActive) return;

        if (currentNode != null)
        {
            currentNode.OnNodeExit();
        }

        OnConversationEnded?.Invoke(currentDialogue);

        if (pauseGameDuringDialogue)
        {
            Time.timeScale = previousTimeScale;
        }

        uiManager?.HideUI();

        currentDialogue = null;
        currentNode = null;
        isConversationActive = false;
    }

    /// <summary>
    /// Define uma variável no blackboard da conversa atual.
    /// </summary>
    public void SetVariable(string name, object value)
    {
        currentDialogue?.Blackboard.SetVariable(name, value);
    }

    /// <summary>
    /// Obtém uma variável do blackboard da conversa atual.
    /// </summary>
    public object GetVariable(string name)
    {
        return currentDialogue?.Blackboard.GetVariable(name);
    }

    public T GetVariable<T>(string name)
    {
        return currentDialogue != null ? currentDialogue.Blackboard.GetVariable<T>(name) : default;
    }

    private IAudioIntegration FindAudioIntegration()
    {
        // Tenta encontrar Signal Audio Manager via reflection
        var signalType = System.Type.GetType("Signal, Assembly-CSharp");
        if (signalType != null)
        {
            return new SignalAudioIntegration();
        }

        // Fallback para sistema padrão
        return new DefaultAudioIntegration();
    }
}