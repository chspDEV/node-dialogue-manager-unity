using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// GraphView principal que exibe e gerencia os nós e conexões do diálogo.
/// Baseado em UI Toolkit GraphView API.
/// </summary>
public class DialogueGraphView : GraphView
{
    private DialogueGraphWindow window;
    private DialogueAsset currentAsset;

    private Dictionary<string, BaseNodeView> nodeViewsCache = new Dictionary<string, BaseNodeView>();

    public DialogueAsset CurrentAsset => currentAsset;

    public DialogueGraphView(DialogueGraphWindow window)
    {
        this.window = window;

        // Configuração da GridBackground
        var gridBackground = new GridBackground();
        Insert(0, gridBackground);
        gridBackground.StretchToParentSize();

        // Adiciona manipuladores
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // Configura menu de contexto
        SetupContextMenu();

        // Configura callback de criação de edges
        graphViewChanged = OnGraphViewChanged;
    }

    /// <summary>
    /// Popula a view com os dados do DialogueAsset.
    /// </summary>
    public void PopulateView(DialogueAsset asset)
    {
        if (asset == null) return;

        currentAsset = asset;

        // Limpa a view atual
        ClearView();

        // Cria visualizações para cada nó
        foreach (var nodeData in asset.Nodes)
        {
            CreateNodeView(nodeData);
        }

        // Cria visualizações para cada conexão
        foreach (var connectionData in asset.Connections)
        {
            CreateEdgeView(connectionData);
        }

        // Restaura posição e zoom (se necessário implementar)
        // UpdateViewTransform(asset.GraphViewPosition, asset.GraphViewScale);
    }

    private void ClearView()
    {
        // Remove todos os elementos do grafo
        graphElements.ForEach(elem => RemoveElement(elem));
        nodeViewsCache.Clear();
    }

    private BaseNodeView CreateNodeView(BaseNodeData nodeData)
    {
        BaseNodeView nodeView = nodeData switch
        {
            RootNodeData root => new RootNodeView(root),
            SpeechNodeData speech => new SpeechNodeView(speech),
            OptionNodeData option => new OptionNodeView(option),
            _ => null
        };

        if (nodeView != null)
        {
            AddElement(nodeView);
            nodeViewsCache[nodeData.GUID] = nodeView;
        }

        return nodeView;
    }

    private void CreateEdgeView(ConnectionData connectionData)
    {
        if (!nodeViewsCache.TryGetValue(connectionData.FromNodeGUID, out var fromNode) ||
            !nodeViewsCache.TryGetValue(connectionData.ToNodeGUID, out var toNode))
        {
            Debug.LogWarning($"Cannot create edge: Node not found");
            return;
        }

        var outputPort = fromNode.GetOutputPort(connectionData.FromPortIndex);
        var inputPort = toNode.GetInputPort(connectionData.ToPortIndex);

        if (outputPort == null || inputPort == null)
        {
            Debug.LogWarning($"Cannot create edge: Port not found");
            return;
        }

        var edge = outputPort.ConnectTo(inputPort);
        edge.userData = connectionData; // Armazena referência aos dados
        AddElement(edge);
    }

    /// <summary>
    /// Callback chamado quando o grafo é modificado (nós movidos, conexões criadas, etc).
    /// </summary>
    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        // Elementos removidos
        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove)
            {
                if (element is BaseNodeView nodeView)
                {
                    RemoveNodeFromAsset(nodeView);
                }
                else if (element is Edge edge)
                {
                    RemoveEdgeFromAsset(edge);
                }
            }
        }

        // Edges criados
        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                CreateConnectionFromEdge(edge);
            }
        }

        // Elementos movidos
        if (change.movedElements != null)
        {
            foreach (var element in change.movedElements)
            {
                if (element is BaseNodeView nodeView)
                {
                    nodeView.NodeData.EditorPosition = nodeView.GetPosition().position;
                    EditorUtility.SetDirty(currentAsset);
                }
            }
        }

        return change;
    }

    private void RemoveNodeFromAsset(BaseNodeView nodeView)
    {
        // Não permite remover o nó raiz
        if (nodeView.NodeData is RootNodeData)
        {
            EditorUtility.DisplayDialog("Error", "Cannot delete the Root node.", "OK");
            return;
        }

        currentAsset.RemoveNode(nodeView.NodeData);
        nodeViewsCache.Remove(nodeView.NodeData.GUID);
        EditorUtility.SetDirty(currentAsset);
    }

    private void RemoveEdgeFromAsset(Edge edge)
    {
        if (edge.userData is ConnectionData connectionData)
        {
            currentAsset.Connections.Remove(connectionData);
            EditorUtility.SetDirty(currentAsset);
        }
    }

    private void CreateConnectionFromEdge(Edge edge)
    {
        var outputNode = edge.output.node as BaseNodeView;
        var inputNode = edge.input.node as BaseNodeView;

        if (outputNode == null || inputNode == null) return;

        var connectionData = new ConnectionData
        {
            FromNodeGUID = outputNode.NodeData.GUID,
            FromPortIndex = outputNode.GetPortIndex(edge.output),
            ToNodeGUID = inputNode.NodeData.GUID,
            ToPortIndex = inputNode.GetPortIndex(edge.input)
        };

        currentAsset.AddConnection(connectionData);
        edge.userData = connectionData;
        EditorUtility.SetDirty(currentAsset);
    }

    /// <summary>
    /// Configura o menu de contexto (botão direito do mouse).
    /// </summary>
    private void SetupContextMenu()
    {
        // Usa callback para menu de contexto
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            var mousePosition = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);

            evt.menu.AppendAction("Create Speech Node", _ => CreateNode<SpeechNodeData>(mousePosition));
            evt.menu.AppendAction("Create Option Node", _ => CreateNode<OptionNodeData>(mousePosition));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete Selection", _ => DeleteSelection());
        }));
    }

    /// <summary>
    /// Cria um novo nó na posição especificada.
    /// </summary>
    private void CreateNode<T>(Vector2 position) where T : BaseNodeData, new()
    {
        var nodeData = NodeFactory.CreateNode<T>();
        nodeData.EditorPosition = position;

        currentAsset.AddNode(nodeData);

        var nodeView = CreateNodeView(nodeData);
        nodeView.SetPosition(new Rect(position, Vector2.zero));

        EditorUtility.SetDirty(currentAsset);
    }

    /// <summary>
    /// Sobrescreve a compatibilidade de portas para permitir conexões.
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node
        ).ToList();
    }

    /// <summary>
    /// Retorna o NodeView associado a um NodeData.
    /// </summary>
    public BaseNodeView GetNodeView(BaseNodeData nodeData)
    {
        return nodeViewsCache.TryGetValue(nodeData.GUID, out var view) ? view : null;
    }
}