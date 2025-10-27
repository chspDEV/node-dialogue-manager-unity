using UnityEngine;
using System;

/// <summary>
/// Orquestra a execução de um DialogueAsset em runtime.
/// Funciona como o "cérebro" do sistema, lendo os dados
/// e comandando o DialogueUIManager.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private DialogueUIManager uiManager;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueAsset dialogueToRun;

    private DialogueAsset currentAsset;
    private BaseNodeData currentNode;

    private void Start()
    {
        // Para teste rápido, inicia o diálogo ao começar a cena.
        // Em um jogo real, você chamaria StartDialogue() a partir de um NPC, trigger, etc.
        if (dialogueToRun != null)
        {
            StartDialogue(dialogueToRun);
        }
    }

    /// <summary>
    /// Inicia a execução de um gráfico de diálogo.
    /// </summary>
    public void StartDialogue(DialogueAsset asset)
    {
        if (asset == null || uiManager == null)
        {
            Debug.LogError("Dialogue Runner não está configurado corretamente!");
            return;
        }

        currentAsset = asset;
        currentNode = currentAsset.GetRootNode(); // Precisamos criar este método

        if (currentNode == null)
        {
            Debug.LogError("Diálogo não tem um Root Node!");
            return;
        }

        ProcessNode(currentNode);
    }

    /// <summary>
    /// Processa o nó atual com base em seu tipo.
    /// </summary>
    private void ProcessNode(BaseNodeData node)
    {
        if (node == null)
        {
            EndDialogue();
            return;
        }

        currentNode = node;

        // Dispara eventos do nó (OnNodeEnter, Actions, etc.)
        node.OnNodeEnter();

        // Usa um switch expression para lidar com cada tipo de nó
        switch (node)
        {
            case RootNodeData root:
                // O RootNode apenas aponta para o próximo nó.
                AdvanceToNextNode(root);
                break;

            case SpeechNodeData speech:
                // Pede ao UIManager para mostrar a fala.
                // O UIManager chamará o callback "() => ..." quando o jogador avançar.
                uiManager.DisplaySpeech(speech, () => AdvanceToNextNode(speech));
                break;

            case OptionNodeData option:
                // Pede ao UIManager para mostrar as opções.
                // O UIManager chamará o callback "(index) => ..." com a escolha do jogador.
                uiManager.DisplayOptions(option, (choiceIndex) => AdvanceToNextNode(option, choiceIndex));
                break;

            default:
                // Se for um nó desconhecido ou um nó final sem saída
                EndDialogue();
                break;
        }
    }

    /// <summary>
    /// Encontra o próximo nó no asset e o processa.
    /// </summary>
    /// <param name="fromNode">O nó de onde estamos saindo.</param>
    /// <param name="portIndex">O índice da porta de saída (0 para Speech, 0..N para Option).</param>
    private void AdvanceToNextNode(BaseNodeData fromNode, int portIndex = 0)
    {
        if (currentAsset == null) return;

        // Pede ao asset para encontrar o próximo nó
        BaseNodeData nextNode = currentAsset.GetNextNode(fromNode, portIndex);
        ProcessNode(nextNode);
    }

    /// <summary>
    /// Termina o diálogo e limpa a UI.
    /// </summary>
    private void EndDialogue()
    {
        uiManager.HideUI();
        currentNode = null;
        currentAsset = null;
        Debug.Log("Diálogo terminado.");
    }
}