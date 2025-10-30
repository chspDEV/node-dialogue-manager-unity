using ChspDev.DialogueSystem.Editor;
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 🎮 Orquestra a execução de um DialogueAsset em runtime.
/// Funciona como o "cérebro" do sistema, lendo os dados
/// e comandando o DialogueUIManager.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    [Header("🔌 Dependências Principais")]
    [Tooltip("A referência para o DialogueUIManager na cena que exibirá o diálogo.")]
    [SerializeField] private DialogueUIManager uiManager;

    [Header("📚 Dados do Diálogo")]
    [Tooltip("O asset de diálogo padrão para rodar se StartAssignedDialogue() for chamado.")]
    [SerializeField] private DialogueAsset dialogueToRun;

    // --- Estado Interno ---
    private DialogueAsset currentAsset;
    private BaseNodeData currentNode;
    private ConversationManager conversationManager; // Para gerenciar o Blackboard

    /// <summary>
    /// Awake é usado para inicialização de componentes.
    /// </summary>
    private void Awake()
    {
        conversationManager = ConversationManager.Instance;

        TextProcessor.Initialize(new RuntimeVariableProvider(conversationManager));

        if (uiManager == null)
        {
            Debug.LogWarning($"[DialogueRunner] DialogueUIManager não foi atribuído em '{gameObject.name}'. Tentando encontrar na cena...", this);
            uiManager = FindObjectOfType<DialogueUIManager>();
            if (uiManager == null)
            {
                Debug.LogError($"[DialogueRunner] Nenhum DialogueUIManager encontrado na cena! O diálogo não pode funcionar.", this);
            }
        }
    }

    /// <summary>
    /// Inicia o diálogo padrão atribuído no Inspector.
    /// </summary>
    public void StartAssignedDialogue()
    {
        if (dialogueToRun == null)
        {
            Debug.LogError($"[DialogueRunner] Nenhum 'Dialogue To Run' atribuído no Inspector de '{gameObject.name}'.", this);
            return;
        }
        StartDialogue(dialogueToRun);
    }

    /// <summary>
    /// Inicia a execução de um gráfico de diálogo específico.
    /// </summary>
    public void StartDialogue(DialogueAsset asset)
    {
        if (asset == null)
        {
            Debug.LogError("[DialogueRunner] Tentativa de iniciar diálogo com um Asset nulo.", this);
            return;
        }

        if (uiManager == null)
        {
            Debug.LogError("[DialogueRunner] Não pode iniciar diálogo: DialogueUIManager é nulo.", this);
            return;
        }

        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] DialogueRunner: StartDialogue() chamado com o asset '{asset.name}'.", asset);
        // -------------------------

        currentAsset = asset;
        currentNode = currentAsset.GetRootNode();

        if (currentNode == null)
        {
            Debug.LogError($"[DialogueRunner] Diálogo '{asset.name}' não possui um Root Node! Não é possível iniciar.", asset);
            return;
        }

        // Notifica o ConversationManager (que gerencia o Blackboard)
        conversationManager?.StartConversation(asset);

        // Inicia o processo
        ProcessNode(currentNode);
    }

    /// <summary>
    /// Processa o nó atual (Root, Speech, Option, Branch) e decide o que fazer.
    /// </summary>
    private void ProcessNode(BaseNodeData node)
    {
        if (node == null)
        {
            EndDialogue(); // Se o nó for nulo, o diálogo termina
            return;
        }

        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] DialogueRunner: ProcessNode() - Processando nó: '{node.GetDisplayTitle()}' (Tipo: {node.GetType().Name})", node);
        // -------------------------

        currentNode = node;

        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] DialogueRunner: ProcessNode() - Chamando node.OnNodeEnter() para '{node.GetDisplayTitle()}'...");
        // -------------------------

        // Executa todas as "Actions" definidas no nó
        node.OnNodeEnter();

        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] DialogueRunner: ProcessNode() - node.OnNodeEnter() concluído. Avaliando tipo de nó...");
        // -------------------------

        // Determina o tipo de nó e age de acordo
        switch (node)
        {
            case RootNodeData root:
                Debug.Log("[DEBUG] DialogueRunner: Nó é RootNode. Avançando...");
                AdvanceToNextNode(root, 0);
                break;

            case SpeechNodeData speech:
                Debug.Log("[DEBUG] DialogueRunner: Nó é SpeechNode. Chamando UIManager.DisplaySpeech().");
                uiManager.DisplaySpeech(speech, () => AdvanceToNextNode(speech, 0));
                break;

            case OptionNodeData option:
                Debug.Log("[DEBUG] DialogueRunner: Nó é OptionNode. Chamando UIManager.DisplayOptions().");
                uiManager.DisplayOptions(option, (choiceIndex) => AdvanceToNextNode(option, choiceIndex));
                break;

            case BranchNodeData branch:
                bool result = branch.EvaluateConditions();
                int portIndex = result ? 0 : 1;
                Debug.Log($"[DEBUG] DialogueRunner: Nó é BranchNode. Resultado da avaliação: {result}. Avançando para porta {portIndex}");
                AdvanceToNextNode(branch, portIndex);
                break;

            default:
                Debug.LogWarning($"[DialogueRunner] Nó '{node.name}' é de tipo desconhecido ou é um nó final. Terminando diálogo.");
                EndDialogue();
                break;
        }
    }

    /// <summary>
    /// Encontra o próximo nó conectado à porta de saída especificada e o processa.
    /// </summary>
    private void AdvanceToNextNode(BaseNodeData fromNode, int portIndex = 0)
    {
        if (currentAsset == null) return;

        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] DialogueRunner: AdvanceToNextNode() - Procurando próximo nó a partir de '{fromNode.GetDisplayTitle()}' (Porta: {portIndex})");
        // -------------------------

        BaseNodeData nextNode = currentAsset.GetNextNode(fromNode, portIndex);

        if (nextNode == null)
        {
            // --- ⬇️ LOG DE DEBUG ⬇️ ---
            Debug.Log($"[DEBUG] DialogueRunner: Fim do fluxo. Nó '{fromNode.name}' não tem conexão na porta {portIndex}. Próximo nó é NULO.");
            // -------------------------
        }

        ProcessNode(nextNode); // Processa o próximo nó (ou null, que encerra o diálogo)
    }

    /// <summary>
    /// Termina o diálogo atual e limpa a UI.
    /// </summary>
    private void EndDialogue()
    {
        if (uiManager != null)
        {
            uiManager.HideUI();
        }

        conversationManager?.EndConversation();

        currentNode = null;
        currentAsset = null;

        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log("[DEBUG] DialogueRunner: Diálogo terminado. Estado limpo.");
        // -------------------------
    }


    /// <summary>
    /// Classe interna simples que atua como ponte entre o
    /// TextProcessor (estático) e o ConversationManager (instância).
    /// </summary>
    private class RuntimeVariableProvider : IVariableProvider
    {
        private ConversationManager cm;

        public RuntimeVariableProvider(ConversationManager manager)
        {
            this.cm = manager;
        }

        public bool TryGetVariable(string variableName, out string value)
        {
            if (cm != null)
            {
                object variableValue = cm.GetVariable(variableName);
                if (variableValue != null)
                {
                    value = variableValue.ToString();
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}