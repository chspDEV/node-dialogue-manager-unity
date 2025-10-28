using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

// Add your namespace here if you use one
// namespace ChspDev.DialogueSystem.Editor
// {

/// <summary>
/// Classe base abstrata para todas as visualiza��es de n�s no editor.
/// Gerencia a cria��o de portas e a sincroniza��o com NodeData.
/// </summary>
public abstract class BaseNodeView : Node
{
    // Armazena a refer�ncia aos dados do n� (ScriptableObject)
    protected BaseNodeData nodeData;
    // Listas para manter refer�ncias �s inst�ncias de Port criadas
    protected List<Port> inputPorts = new List<Port>();
    protected List<Port> outputPorts = new List<Port>();

    // Propriedade p�blica para acessar os dados do n�
    public BaseNodeData NodeData => nodeData;

    /// <summary>
    /// Construtor base para todas as visualiza��es de n�.
    /// </summary>
    /// <param name="data">Os dados (ScriptableObject) que este n� representa.</param>
    protected BaseNodeView(BaseNodeData data)
    {
        // Valida��o inicial
        if (data == null)
        {
            Debug.LogError("BaseNodeView created with null NodeData!");
            // Voc� pode querer lan�ar uma exce��o ou lidar com isso de outra forma
            return;
        }

        this.nodeData = data;

        // --- Configura��o Visual Padr�o ---
        this.title = data.GetDisplayTitle(); // Define o t�tulo
        SetPosition(new Rect(data.EditorPosition, Vector2.zero)); // Define a posi��o inicial
        viewDataKey = data.GUID; // Associa a view aos dados pelo GUID (essencial!)
        AddToClassList("dialogue-node"); // Adiciona classe CSS base
        // ------------------------------------

        // --- Cria��o de Elementos Internos ---
        CreatePorts();         // Cria as portas de entrada e sa�da
        CreateNodeContent();   // Chama o m�todo para conte�do customizado (implementado por filhos)
        // -------------------------------------

        // --- Atualiza��o Visual Inicial ---
        RefreshExpandedState(); // Garante que o n� esteja expandido/recolhido corretamente
        RefreshPorts();         // For�a o redesenho das portas
        // ----------------------------------
    }

    /// <summary>
    /// Cria as portas visuais (Input e Output) baseadas na contagem definida no NodeData.
    /// Limpa portas existentes antes de criar novas (importante para UpdateNodeView).
    /// </summary>
    protected virtual void CreatePorts()
    {
        if (nodeData == null) return; // Seguran�a

        // --- Limpeza Visual ---
        // Remove portas *visuais* dos containers antes de limpar as listas de refer�ncia
        inputContainer.Clear();
        outputContainer.Clear();
        // Limpa as listas de refer�ncia internas
        inputPorts.Clear();
        outputPorts.Clear();
        // ----------------------

        // --- Cria Portas de Entrada ---
        int inputCount = nodeData.GetInputPortCount();
        for (int i = 0; i < inputCount; i++)
        {
            // Cria a porta (assume capacidade Multi por padr�o para entradas)
            Port inputPort = CreatePortInstance(Direction.Input, Port.Capacity.Multi, i);
            if (inputPort != null)
            {
                inputPorts.Add(inputPort);      // Adiciona � lista de refer�ncia
                inputContainer.Add(inputPort); // Adiciona ao container visual esquerdo
            }
        }
        // -----------------------------

        // --- Cria Portas de Sa�da ---
        int outputCount = nodeData.GetOutputPortCount();
        for (int i = 0; i < outputCount; i++)
        {
            // Determina a capacidade correta (geralmente Single, exceto talvez para n�s espec�ficos)
            Port.Capacity capacity = GetOutputCapacityForPort(i); // Usa m�todo auxiliar

            Port outputPort = CreatePortInstance(Direction.Output, capacity, i);
            if (outputPort != null)
            {
                outputPorts.Add(outputPort);     // Adiciona � lista de refer�ncia
                outputContainer.Add(outputPort); // Adiciona ao container visual direito
            }
        }
        // ----------------------------
    }

    /// <summary>
    /// Determina a capacidade para uma porta de sa�da espec�fica. Pode ser sobrescrito.
    /// </summary>
    /// <param name="index">O �ndice da porta de sa�da sendo criada.</param>
    /// <returns>A capacidade da porta (Single ou Multi).</returns>
    protected virtual Port.Capacity GetOutputCapacityForPort(int index)
    {
        // Padr�o � Single (uma conex�o por porta de sa�da).
        // OptionNode pode precisar sobrescrever isso se uma op��o puder levar a m�ltiplos caminhos (raro).
        // SpeechNode e RootNode quase sempre usam Single.
        return Port.Capacity.Single;
    }

    /// <summary>
    /// Cria e configura uma inst�ncia de Port com seu �ndice armazenado em userData.
    /// </summary>
    /// <param name="direction">Dire��o da porta (Input/Output).</param>
    /// <param name="capacity">Capacidade da porta (Single/Multi).</param>
    /// <param name="index">O �ndice num�rico desta porta (0, 1, 2...).</param>
    /// <returns>A inst�ncia Port criada ou null em caso de erro.</returns>
    protected Port CreatePortInstance(Direction direction, Port.Capacity capacity, int index)
    {
        // O tipo de porta (typeof(bool)) � gen�rico, usado para compatibilidade b�sica.
        var port = InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(bool));
        if (port == null)
        {
            Debug.LogError($"[{this.title}] Failed to instantiate port (Dir: {direction}, Idx: {index})");
            return null;
        }


        port.portName = GetPortName(direction, index); // Atribui nome (geralmente vazio)

        // --- Ponto Cr�tico: Armazenar o �ndice ---
        // Garante que o �ndice *inteiro* seja armazenado no userData.
        port.userData = index;
        // ------------------------------------------

        // Log opcional para depura��o profunda (descomente se o erro persistir)
        // Debug.Log($"[{this.title}] Created Port: Name='{port.portName}', Dir='{direction}', Idx='{index}', UserData='{port.userData}', UserDataType='{port.userData?.GetType()}'");

        return port;
    }

    /// <summary>
    /// Retorna o nome (label) a ser exibido ao lado da porta. Padr�o � vazio.
    /// Pode ser sobrescrito (ex: OptionNodeView para mostrar texto da op��o).
    /// </summary>
    protected virtual string GetPortName(Direction direction, int index)
    {
        return string.Empty; // Mant�m limpo por padr�o
    }

    /// <summary>
    /// M�todo abstrato para classes filhas adicionarem UIElements customizados
    /// ao container principal do n� (mainContainer).
    /// </summary>
    protected abstract void CreateNodeContent();

    /// <summary>
    /// Retorna a inst�ncia da porta de SA�DA no �ndice especificado.
    /// Retorna null se o �ndice for inv�lido.
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
    /// Retorna a inst�ncia da porta de ENTRADA no �ndice especificado.
    /// Retorna null se o �ndice for inv�lido.
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
    /// Recupera o �ndice num�rico armazenado no userData de uma porta.
    /// Retorna -1 se a porta for nula ou o userData n�o for um inteiro.
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
            // Falha - Log detalhado para diagn�stico
            Debug.LogError($"[{this.title ?? "Unknown Node"}] GetPortIndex FAILED: Port '{port.portName}' (Direction: {port.direction}) userData is NOT an int. UserData Value: '{port.userData}', UserData Type: {port.userData?.GetType()}. This port might have been created incorrectly or its userData was overwritten.");
            return -1; // Indica falha
        }
    }

    /// <summary>
    /// Atualiza a apar�ncia visual do n� (t�tulo, portas, conte�do) para refletir
    /// mudan�as nos dados (NodeData). Chamado pelo GraphView via DialogueEditorEvents.
    /// </summary>
    public virtual void UpdateNodeView()
    {
        // Atualiza T�tulo
        if (nodeData != null)
        {
            title = nodeData.GetDisplayTitle();
        }

        // Recria Portas (isso limpa visualmente e recria baseado na contagem atual)
        CreatePorts();

        // Limpa e Recria Conte�do Customizado (se necess�rio)
        // Implemente l�gica espec�fica nas classes filhas se o conte�do precisar mudar
        // Ex: Limpar mainContainer e chamar CreateNodeContent() novamente.
        // mainContainer.Clear();
        // CreateNodeContent();


        // For�a redesenho
        RefreshPorts();         // Atualiza visual das portas
        RefreshExpandedState(); // Recalcula tamanho/layout
    }

    /// <summary>
    /// Callback padr�o quando o n� � selecionado no GraphView.
    /// Foca o Inspector da Unity nos dados (ScriptableObject) deste n�.
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
            UnityEditor.Selection.activeObject = null; // Limpa sele��o se n�o for SO
        }
    }
}

// } // Fecha namespace se aplic�vel