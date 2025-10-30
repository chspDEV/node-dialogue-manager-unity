using System;
using System.Collections.Generic; // Necessário para Dictionary
using UnityEngine;
using System.Linq; // Necessário para .OfType

/// <summary>
/// Singleton que gerencia o fluxo de conversas em runtime.
/// API principal para iniciar diálogos e interagir com o sistema.
/// ⚠️ ATUALIZADO: Agora armazena "Blackboards de runtime" para que
/// as variáveis persistam entre execuções do mesmo diálogo.
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

    private DialogueAsset currentDialogue;
    private BaseNodeData currentNode; // Controlado pelo DialogueRunner
    private DialogueProcessor processor;
    private IAudioIntegration audioIntegration;
    private float previousTimeScale;
    private bool isConversationActive;


    // --- ⬇️ CORREÇÃO DE ARQUITETURA AQUI ⬇️ ---

    /// <summary>
    /// Armazena as cópias de runtime dos Blackboards.
    /// A Chave (Key) é o DialogueAsset (o ficheiro de dados).
    /// O Valor (Value) é a cópia de runtime (activeBlackboard).
    /// </summary>
    private Dictionary<DialogueAsset, BlackboardData> runtimeBlackboards = new Dictionary<DialogueAsset, BlackboardData>();

    /// <summary>
    /// O Blackboard que está a ser usado na conversa ATUAL.
    /// </summary>
    private BlackboardData activeBlackboard;

    // --- ⬆️ FIM DA CORREÇÃO ⬆️ ---


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
        audioIntegration = FindAudioIntegration();

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<DialogueUIManager>();
        }
    }

    /// <summary>
    /// Prepara o ConversationManager para um novo diálogo.
    /// </summary>
    public void StartConversation(DialogueAsset dialogue)
    {
        if (dialogue == null)
        {
            Debug.LogError("Cannot start conversation: DialogueAsset is null");
            return;
        }

        Debug.Log($"[DEBUG] ConversationManager: StartConversation() chamado para '{dialogue.name}'.");

        if (isConversationActive)
        {
            Debug.LogWarning("A conversation is already active. Ending previous conversation.");
            EndConversation();
        }

        currentDialogue = dialogue;
        currentNode = dialogue.RootNode; // Define o ponto de partida

        if (currentNode == null)
        {
            Debug.LogError("DialogueAsset has no Root Node!");
            return;
        }

        // --- ⬇️ CORREÇÃO DE ARQUITETURA AQUI ⬇️ ---

        // 1. Tenta encontrar um blackboard de runtime já existente para este asset
        if (runtimeBlackboards.TryGetValue(dialogue, out BlackboardData existingBlackboard))
        {
            // 2. Se encontrou, usa-o. (As alterações persistiram!)
            activeBlackboard = existingBlackboard;
            Debug.Log($"[DEBUG] ConversationManager: Blackboard de runtime encontrado para '{dialogue.name}'.");
        }
        else
        {
            // 3. Se NÃO encontrou (primeira vez a executar), cria uma nova cópia
            activeBlackboard = new BlackboardData();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(dialogue.Blackboard), activeBlackboard);

            // 4. Armazena a nova cópia no Dicionário
            runtimeBlackboards[dialogue] = activeBlackboard;
            Debug.Log($"[DEBUG] ConversationManager: Blackboard de runtime NÃO encontrado. Criada nova cópia para '{dialogue.name}'.");
        }

        object defaultValue = activeBlackboard?.GetVariable("TESTE01");
        Debug.Log($"[DEBUG] ConversationManager: Valor de 'TESTE01' no início da conversa: '{defaultValue}'");

        // --- ⬆️ FIM DA CORREÇÃO ⬆️ ---

        isConversationActive = true;

        if (pauseGameDuringDialogue)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        OnConversationStarted?.Invoke(dialogue);
    }

    /// <summary>
    /// Encerra a conversa atual.
    /// </summary>
    public void EndConversation()
    {
        if (!isConversationActive) return;

        Debug.Log($"[DEBUG] ConversationManager: EndConversation() chamado.");

        if (currentNode != null)
        {
            currentNode.OnNodeExit();
        }

        OnConversationEnded?.Invoke(currentDialogue);

        if (pauseGameDuringDialogue)
        {
            Time.timeScale = previousTimeScale;
        }

        currentDialogue = null;
        currentNode = null;

        // --- ⬇️ CORREÇÃO DE ARQUITETURA AQUI ⬇️ ---
        activeBlackboard = null; // Limpa a *referência* ativa, mas NÃO remove do Dicionário
                                 // --- ⬆️ FIM DA CORREÇÃO ⬆️ ---

        isConversationActive = false;
    }

    /// <summary>
    /// Define uma variável no blackboard DE RUNTIME.
    /// </summary>
    public void SetVariable(string name, object value)
    {
        if (activeBlackboard == null)
        {
            Debug.LogError($"[DEBUG] ConversationManager: FALHA ao definir variável! activeBlackboard é NULO.");
            return;
        }

        object oldValue = activeBlackboard.GetVariable(name);
        Debug.Log($"[DEBUG] ConversationManager: SetVariable() - Nome: '{name}', Valor Antigo: '{oldValue}', Novo Valor: '{value}'");

        activeBlackboard.SetVariable(name, value);
    }

    /// <summary>
    /// Obtém uma variável do blackboard DE RUNTIME.
    /// </summary>
    public object GetVariable(string name)
    {
        if (activeBlackboard == null)
        {
            // Tenta obter do diálogo atual se o activeBlackboard for nulo (segurança)
            if (currentDialogue != null)
            {
                Debug.LogWarning("[DEBUG] ConversationManager: activeBlackboard era nulo, mas foi recuperado do currentDialogue.");
                StartConversation(currentDialogue); // Tenta recuperar o estado
            }

            if (activeBlackboard == null)
            {
                Debug.LogError($"[DEBUG] ConversationManager: FALHA ao obter variável! activeBlackboard está NULO.");
                return null;
            }
        }

        object value = activeBlackboard.GetVariable(name);
        return value;
    }

    /// <summary>
    /// Obtém uma variável tipada do blackboard DE RUNTIME.
    /// </summary>
    public T GetVariable<T>(string name)
    {
        if (activeBlackboard == null)
        {
            Debug.LogError($"[DEBUG] ConversationManager: FALHA ao obter variável<T>! activeBlackboard é NULO.");
            return default;
        }
        return activeBlackboard.GetVariable<T>(name);
    }

    /// <summary>
    /// Limpa o estado de runtime de TODOS os diálogos.
    /// (Chame isto ao carregar um novo nível ou voltar ao menu principal).
    /// </summary>
    public void ClearRuntimeState()
    {
        Debug.Log("[DEBUG] ConversationManager: Limpando todo o estado de runtime do Blackboard.");
        runtimeBlackboards.Clear();
    }

    private IAudioIntegration FindAudioIntegration()
    {
        var signalType = System.Type.GetType("Signal, Assembly-CSharp");
        if (signalType != null)
        {
            return new SignalAudioIntegration();
        }
        return new DefaultAudioIntegration();
    }
}