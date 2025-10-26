using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Classe base abstrata para todas as visualiza��es de n�s no editor.
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

        // Define posi��o
        SetPosition(new Rect(data.EditorPosition, Vector2.zero));

        // Define t�tulo
        title = data.GetDisplayTitle();

        // ID para sele��o no Inspector
        viewDataKey = data.GUID;

        // Adiciona classe CSS
        AddToClassList("dialogue-node");

        // Cria portas
        CreatePorts();

        // Cria conte�do customizado
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

        // Cria portas de sa�da
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
        port.userData = index; // Armazena o �ndice da porta
        return port;
    }

    protected virtual string GetPortName(Direction direction, int index)
    {
        return direction == Direction.Input ? "In" : "Out";
    }

    protected abstract void CreateNodeContent();

    /// <summary>
    /// Retorna a porta de sa�da no �ndice especificado.
    /// </summary>
    public Port GetOutputPort(int index)
    {
        return index >= 0 && index < outputPorts.Count ? outputPorts[index] : null;
    }

    /// <summary>
    /// Retorna a porta de entrada no �ndice especificado.
    /// </summary>
    public Port GetInputPort(int index)
    {
        return index >= 0 && index < inputPorts.Count ? inputPorts[index] : null;
    }

    /// <summary>
    /// Retorna o �ndice de uma porta.
    /// </summary>
    public int GetPortIndex(Port port)
    {
        return port.userData is int index ? index : -1;
    }

    /// <summary>
    /// Atualiza o visual do n� quando os dados mudam.
    /// </summary>
    public virtual void UpdateNodeView()
    {
        title = nodeData.GetDisplayTitle();
    }

    /// <summary>
    /// Callback quando o n� � selecionado.
    /// </summary>
    public override void OnSelected()
    {
        base.OnSelected();

        // Mostra o n� no Inspector
        UnityEditor.Selection.activeObject = nodeData as ScriptableObject;
    }
}