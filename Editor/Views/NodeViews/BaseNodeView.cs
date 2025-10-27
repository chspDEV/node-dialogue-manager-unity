using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Classe base abstrata para todas as visualizações de nós no editor.
/// </summary>
public abstract class BaseNodeView : Node
{
    protected BaseNodeData nodeData;
    protected List<Port> inputPorts = new List<Port>();
    protected List<Port> outputPorts = new List<Port>();

    public BaseNodeData NodeData => nodeData;

    protected BaseNodeView(BaseNodeData data)
    {
        nodeData = data;

        // Define posição
        SetPosition(new Rect(data.EditorPosition, Vector2.zero));

        // Define título
        title = data.GetDisplayTitle();

        // ID para seleção no Inspector
        viewDataKey = data.GUID;

        // Adiciona classe CSS
        AddToClassList("dialogue-node");

        // Cria portas
        CreatePorts();

        // Cria conteúdo customizado
        CreateNodeContent();

        // Atualiza visual
        RefreshExpandedState();
        RefreshPorts();
    }

    protected virtual void CreatePorts()
    {
        // Cria portas de entrada
        for (int i = 0; i < nodeData.GetInputPortCount(); i++)
        {
            var inputPort = CreatePort(Direction.Input, Port.Capacity.Multi, i);
            inputPorts.Add(inputPort);
            inputContainer.Add(inputPort);
        }

        // Cria portas de saída
        for (int i = 0; i < nodeData.GetOutputPortCount(); i++)
        {
            var outputPort = CreatePort(Direction.Output, Port.Capacity.Multi, i);
            outputPorts.Add(outputPort);
            outputContainer.Add(outputPort);
        }
    }

    protected Port CreatePort(Direction direction, Port.Capacity capacity, int index)
    {
        var port = InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(bool));
        port.portName = GetPortName(direction, index);
        port.userData = index; // Armazena o índice da porta
        return port;
    }

    protected virtual string GetPortName(Direction direction, int index)
    {
        return string.Empty; //sem nome In e Out 
    }

    protected abstract void CreateNodeContent();

    /// <summary>
    /// Retorna a porta de saída no índice especificado.
    /// </summary>
    public Port GetOutputPort(int index)
    {
        return index >= 0 && index < outputPorts.Count ? outputPorts[index] : null;
    }

    /// <summary>
    /// Retorna a porta de entrada no índice especificado.
    /// </summary>
    public Port GetInputPort(int index)
    {
        return index >= 0 && index < inputPorts.Count ? inputPorts[index] : null;
    }

    /// <summary>
    /// Retorna o índice de uma porta.
    /// </summary>
    public int GetPortIndex(Port port)
    {
        return port.userData is int index ? index : -1;
    }

    /// <summary>
    /// Atualiza o visual do nó quando os dados mudam.
    /// </summary>
    public virtual void UpdateNodeView()
    {
        // 1. Atualiza o título
        title = nodeData.GetDisplayTitle();

        // 2. Limpa as portas de entrada antigas
        foreach (var port in inputPorts)
        {
            inputContainer.Remove(port);
        }
        inputPorts.Clear();

        // 3. Limpa as portas de saída antigas
        foreach (var port in outputPorts)
        {
            outputContainer.Remove(port);
        }
        outputPorts.Clear();

        // 4. Recria as portas com os dados atualizados
        CreatePorts();

        // 5. Atualiza o layout visual do nó
        RefreshPorts();
        RefreshExpandedState();
    }

    /// <summary>
    /// Callback quando o nó é selecionado.
    /// </summary>
    public override void OnSelected()
    {
        base.OnSelected();

        // Mostra o nó no Inspector
        UnityEditor.Selection.activeObject = nodeData as ScriptableObject;
    }
}