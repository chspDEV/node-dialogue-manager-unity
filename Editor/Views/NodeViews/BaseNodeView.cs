using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

// Add your namespace here if you use one
// namespace ChspDev.DialogueSystem.Editor
// {

/// <summary>
/// Classe base abstrata para todas as visualizações de nós no editor.
/// Gerencia a criação de portas e a sincronização com NodeData.
/// </summary>
public abstract class BaseNodeView : Node
{
    // Armazena a referência aos dados do nó (ScriptableObject)
    protected BaseNodeData nodeData;
    // Listas para manter referências às instâncias de Port criadas
    protected List<Port> inputPorts = new List<Port>();
    protected List<Port> outputPorts = new List<Port>();

    // Propriedade pública para acessar os dados do nó
    public BaseNodeData NodeData => nodeData;

    /// <summary>
    /// Construtor base para todas as visualizações de nó.
    /// </summary>
    /// <param name="data">Os dados (ScriptableObject) que este nó representa.</param>
    protected BaseNodeView(BaseNodeData data)
    {
        // Validação inicial
        if (data == null)
        {
            Debug.LogError("BaseNodeView created with null NodeData!");
            // Você pode querer lançar uma exceção ou lidar com isso de outra forma
            return;
        }

        this.nodeData = data;

        // --- Configuração Visual Padrão ---
        this.title = data.GetDisplayTitle(); // Define o título
        SetPosition(new Rect(data.EditorPosition, Vector2.zero)); // Define a posição inicial
        viewDataKey = data.GUID; // Associa a view aos dados pelo GUID (essencial!)
        AddToClassList("dialogue-node"); // Adiciona classe CSS base
        // ------------------------------------

        // --- Criação de Elementos Internos ---
        CreatePorts();         // Cria as portas de entrada e saída
        CreateNodeContent();   // Chama o método para conteúdo customizado (implementado por filhos)
        // -------------------------------------

        // --- Atualização Visual Inicial ---
        RefreshExpandedState(); // Garante que o nó esteja expandido/recolhido corretamente
        RefreshPorts();         // Força o redesenho das portas
        // ----------------------------------
    }

    /// <summary>
    /// Cria as portas visuais (Input e Output) baseadas na contagem definida no NodeData.
    /// Limpa portas existentes antes de criar novas (importante para UpdateNodeView).
    /// </summary>
    protected virtual void CreatePorts()
    {
        if (nodeData == null) return; // Segurança

        // --- Limpeza Visual ---
        // Remove portas *visuais* dos containers antes de limpar as listas de referência
        inputContainer.Clear();
        outputContainer.Clear();
        // Limpa as listas de referência internas
        inputPorts.Clear();
        outputPorts.Clear();
        // ----------------------

        // --- Cria Portas de Entrada ---
        int inputCount = nodeData.GetInputPortCount();
        for (int i = 0; i < inputCount; i++)
        {
            // Cria a porta (assume capacidade Multi por padrão para entradas)
            Port inputPort = CreatePortInstance(Direction.Input, Port.Capacity.Multi, i);
            if (inputPort != null)
            {
                inputPorts.Add(inputPort);      // Adiciona à lista de referência
                inputContainer.Add(inputPort); // Adiciona ao container visual esquerdo
            }
        }
        // -----------------------------

        // --- Cria Portas de Saída ---
        int outputCount = nodeData.GetOutputPortCount();
        for (int i = 0; i < outputCount; i++)
        {
            // Determina a capacidade correta (geralmente Single, exceto talvez para nós específicos)
            Port.Capacity capacity = GetOutputCapacityForPort(i); // Usa método auxiliar

            Port outputPort = CreatePortInstance(Direction.Output, capacity, i);
            if (outputPort != null)
            {
                outputPorts.Add(outputPort);     // Adiciona à lista de referência
                outputContainer.Add(outputPort); // Adiciona ao container visual direito
            }
        }
        // ----------------------------
    }

    /// <summary>
    /// Determina a capacidade para uma porta de saída específica. Pode ser sobrescrito.
    /// </summary>
    /// <param name="index">O índice da porta de saída sendo criada.</param>
    /// <returns>A capacidade da porta (Single ou Multi).</returns>
    protected virtual Port.Capacity GetOutputCapacityForPort(int index)
    {
        // Padrão é Single (uma conexão por porta de saída).
        // OptionNode pode precisar sobrescrever isso se uma opção puder levar a múltiplos caminhos (raro).
        // SpeechNode e RootNode quase sempre usam Single.
        return Port.Capacity.Single;
    }

    /// <summary>
    /// Cria e configura uma instância de Port com seu índice armazenado em userData.
    /// </summary>
    /// <param name="direction">Direção da porta (Input/Output).</param>
    /// <param name="capacity">Capacidade da porta (Single/Multi).</param>
    /// <param name="index">O índice numérico desta porta (0, 1, 2...).</param>
    /// <returns>A instância Port criada ou null em caso de erro.</returns>
    protected Port CreatePortInstance(Direction direction, Port.Capacity capacity, int index)
    {
        // O tipo de porta (typeof(bool)) é genérico, usado para compatibilidade básica.
        var port = InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(bool));
        if (port == null)
        {
            Debug.LogError($"[{this.title}] Failed to instantiate port (Dir: {direction}, Idx: {index})");
            return null;
        }


        port.portName = GetPortName(direction, index); // Atribui nome (geralmente vazio)

        // --- Ponto Crítico: Armazenar o Índice ---
        // Garante que o índice *inteiro* seja armazenado no userData.
        port.userData = index;
        // ------------------------------------------

        // Log opcional para depuração profunda (descomente se o erro persistir)
        // Debug.Log($"[{this.title}] Created Port: Name='{port.portName}', Dir='{direction}', Idx='{index}', UserData='{port.userData}', UserDataType='{port.userData?.GetType()}'");

        return port;
    }

    /// <summary>
    /// Retorna o nome (label) a ser exibido ao lado da porta. Padrão é vazio.
    /// Pode ser sobrescrito (ex: OptionNodeView para mostrar texto da opção).
    /// </summary>
    protected virtual string GetPortName(Direction direction, int index)
    {
        return string.Empty; // Mantém limpo por padrão
    }

    /// <summary>
    /// Método abstrato para classes filhas adicionarem UIElements customizados
    /// ao container principal do nó (mainContainer).
    /// </summary>
    protected abstract void CreateNodeContent();

    /// <summary>
    /// Retorna a instância da porta de SAÍDA no índice especificado.
    /// Retorna null se o índice for inválido.
    /// </summary>
    public Port GetOutputPort(int index)
    {
        if (index >= 0 && index < outputPorts.Count)
        {
            return outputPorts[index];
        }
        // Log opcional: Debug.LogWarning($"[{this.title}] GetOutputPort: Index {index} out of range (Count: {outputPorts.Count})");
        return null;
    }

    /// <summary>
    /// Retorna a instância da porta de ENTRADA no índice especificado.
    /// Retorna null se o índice for inválido.
    /// </summary>
    public Port GetInputPort(int index)
    {
        if (index >= 0 && index < inputPorts.Count)
        {
            return inputPorts[index];
        }
        // Log opcional: Debug.LogWarning($"[{this.title}] GetInputPort: Index {index} out of range (Count: {inputPorts.Count})");
        return null;
    }

    /// <summary>
    /// Recupera o índice numérico armazenado no userData de uma porta.
    /// Retorna -1 se a porta for nula ou o userData não for um inteiro.
    /// </summary>
    public int GetPortIndex(Port port)
    {
        if (port == null)
        {
            Debug.LogError($"[{this.title ?? "Unknown Node"}] GetPortIndex called with a null port!");
            return -1;
        }

        // Tenta converter userData para int
        if (port.userData is int index)
        {
            // Sucesso!
            return index;
        }
        else
        {
            // Falha - Log detalhado para diagnóstico
            Debug.LogError($"[{this.title ?? "Unknown Node"}] GetPortIndex FAILED: Port '{port.portName}' (Direction: {port.direction}) userData is NOT an int. UserData Value: '{port.userData}', UserData Type: {port.userData?.GetType()}. This port might have been created incorrectly or its userData was overwritten.");
            return -1; // Indica falha
        }
    }

    /// <summary>
    /// Atualiza a aparência visual do nó (título, portas, conteúdo) para refletir
    /// mudanças nos dados (NodeData). Chamado pelo GraphView via DialogueEditorEvents.
    /// </summary>
    public virtual void UpdateNodeView()
    {
        // Atualiza Título
        if (nodeData != null)
        {
            title = nodeData.GetDisplayTitle();
        }

        // Recria Portas (isso limpa visualmente e recria baseado na contagem atual)
        CreatePorts();

        // Limpa e Recria Conteúdo Customizado (se necessário)
        // Implemente lógica específica nas classes filhas se o conteúdo precisar mudar
        // Ex: Limpar mainContainer e chamar CreateNodeContent() novamente.
        // mainContainer.Clear();
        // CreateNodeContent();


        // Força redesenho
        RefreshPorts();         // Atualiza visual das portas
        RefreshExpandedState(); // Recalcula tamanho/layout
    }

    /// <summary>
    /// Callback padrão quando o nó é selecionado no GraphView.
    /// Foca o Inspector da Unity nos dados (ScriptableObject) deste nó.
    /// </summary>
    public override void OnSelected()
    {
        base.OnSelected();
        // Seleciona o ScriptableObject associado no Inspector
        if (nodeData is ScriptableObject so)
        {
            UnityEditor.Selection.activeObject = so;
        }
        else
        {
            UnityEditor.Selection.activeObject = null; // Limpa seleção se não for SO
        }
    }
}

// } // Fecha namespace se aplicável