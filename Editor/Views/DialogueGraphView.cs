using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChspDev.DialogueSystem.Editor
{
    /// <summary>
    /// GraphView principal com integração Undo/Redo, criação de nós via SearchWindow,
    /// salvamento de conexões corrigido, e limpeza automática de dados corrompidos.
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        public DialogueAsset dialogueAsset;
        private EditorWindow parentWindow;
        private NodeSearchWindow searchWindowProvider;
        private Dictionary<string, BaseNodeView> nodeViewCache = new Dictionary<string, BaseNodeView>();

        public DialogueGraphView(DialogueAsset asset, EditorWindow window)
        {
            dialogueAsset = asset;
            parentWindow = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            var gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();

            DialogueGraphViewShortcuts.RegisterGraphViewFocus(this);

            graphViewChanged += OnGraphViewChanged;
            SetupNodeCreationRequest();
            InitializeSearchWindow();
            LoadStyles();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            RegisterCallback<DetachFromPanelEvent>(evt => Undo.undoRedoPerformed -= OnUndoRedoPerformed);

            DialogueEditorEvents.OnNodeDataChanged += HandleNodeDataChanged;
            RegisterCallback<DetachFromPanelEvent>(evt => DialogueEditorEvents.OnNodeDataChanged -= HandleNodeDataChanged);

            RegisterCallback<FocusInEvent>(evt =>
            {
                DialogueGraphViewShortcuts.RegisterGraphViewFocus(this);
            });
        }

        public BaseNodeView CreateRootNode(Vector2 position)
        {
            if (dialogueAsset?.RootNode != null)
            {
                EditorUtility.DisplayDialog("Root Node", "Este diálogo já possui um nó raiz. Apenas um root node é permitido por diálogo.", "OK");
                return null;
            }

            return CreateNodeInternal<RootNodeData>("Create Root Node", position, nodeData =>
            {
                // Root node não precisa de inicialização customizada
            });
        }

        private void OnUndoRedoPerformed()
        {
            if (dialogueAsset != null)
            {
                PopulateView();
                EditorUtility.SetDirty(dialogueAsset);
            }
        }

        private void HandleNodeDataChanged(BaseNodeData changedNodeData)
        {
            if (changedNodeData == null || dialogueAsset == null) return;

            if (dialogueAsset.Nodes.Contains(changedNodeData) &&
                nodeViewCache.TryGetValue(changedNodeData.guid, out BaseNodeView nodeView))
            {
                nodeView.UpdateNodeView();
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (dialogueAsset == null) return graphViewChange;

            bool requiresUndoRecordingOnAsset = false;

            // ================== REMOÇÃO DE ELEMENTOS ==================
            if (graphViewChange.elementsToRemove != null)
            {
                requiresUndoRecordingOnAsset = true;
                Undo.SetCurrentGroupName("Remove Graph Elements");
                int group = Undo.GetCurrentGroup();

                foreach (var element in graphViewChange.elementsToRemove.OfType<Edge>())
                {
                    RemoveConnection(element);
                }

                foreach (var element in graphViewChange.elementsToRemove.OfType<BaseNodeView>())
                {
                    RemoveNodeData(element);
                    if (element.NodeData != null) nodeViewCache.Remove(element.NodeData.guid);
                }

                Undo.CollapseUndoOperations(group);
            }

            // ================== CRIAÇÃO DE EDGES ==================
            if (graphViewChange.edgesToCreate != null)
            {
                requiresUndoRecordingOnAsset = true;
                Undo.SetCurrentGroupName("Create Connections");
                int group = Undo.GetCurrentGroup();

                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var outputNode = edge.output?.node as BaseNodeView;
                    var inputNode = edge.input?.node as BaseNodeView;

                    if (outputNode != null && inputNode != null)
                    {
                        SaveConnection(outputNode, inputNode, edge.output, edge.input);
                    }
                }
                Undo.CollapseUndoOperations(group);
            }

            // ================== MOVIMENTO DE ELEMENTOS ==================
            if (graphViewChange.movedElements != null && graphViewChange.movedElements.Count > 0)
            {
                requiresUndoRecordingOnAsset = true;

                foreach (var element in graphViewChange.movedElements.OfType<BaseNodeView>())
                {
                    if (element.NodeData == null) continue;

                    Undo.RecordObject(element.NodeData, "Move Node");
                    element.NodeData.EditorPosition = element.GetPosition().position;
                    EditorUtility.SetDirty(element.NodeData);
                }
            }

            if (requiresUndoRecordingOnAsset && dialogueAsset != null)
            {
                EditorUtility.SetDirty(dialogueAsset);
            }

            return graphViewChange;
        }

        // ==================== CRIAÇÃO DE NÓS (COM UNDO) ====================

        public BaseNodeView CreateSpeechNode(Vector2 position)
        {
            return CreateNodeInternal<SpeechNodeData>("Create Speech Node", position, nodeData =>
            {
                nodeData.CharacterName = "Character";
                nodeData.DialogueText = "Enter dialogue text here...";
            });
        }

        public BaseNodeView CreateOptionNode(Vector2 position)
        {
            return CreateNodeInternal<OptionNodeData>("Create Option Node", position, nodeData =>
            {
                if (nodeData.options == null)
                    nodeData.options = new List<OptionNodeData.Option>();
                else
                    nodeData.options.Clear();

                nodeData.options.Add(new OptionNodeData.Option
                {
                    optionText = "Option 1",
                    conditions = new List<BaseCondition>(),
                    onOptionSelected = new UnityEngine.Events.UnityEvent()
                });
            });
        }

        private BaseNodeView CreateNodeInternal<TNodeData>(string undoName, Vector2 position, Action<TNodeData> initializer = null)
            where TNodeData : BaseNodeData
        {
            if (dialogueAsset == null)
            {
                Debug.LogError("Cannot create node: No DialogueAsset loaded.");
                return null;
            }

            TNodeData nodeData = ScriptableObject.CreateInstance<TNodeData>();
            nodeData.name = typeof(TNodeData).Name;
            nodeData.guid = GUID.Generate().ToString();
            nodeData.EditorPosition = position;

            initializer?.Invoke(nodeData);

            Undo.RegisterCreatedObjectUndo(nodeData, undoName);
            AssetDatabase.AddObjectToAsset(nodeData, dialogueAsset);

            Undo.RecordObject(dialogueAsset, undoName);
            dialogueAsset.Nodes.Add(nodeData);

            EditorUtility.SetDirty(nodeData);
            EditorUtility.SetDirty(dialogueAsset);

            BaseNodeView nodeView = CreateNodeViewVisual(nodeData);

            if (nodeView != null)
            {
                AddElement(nodeView);
                nodeViewCache[nodeData.guid] = nodeView;
            }

            return nodeView;
        }

        // ==================== 🔧 REMOÇÃO DE NÓS MELHORADA ====================

        /// <summary>
        /// 🔧 ATUALIZADO: Remove os dados do nó e TODAS as conexões associadas corretamente
        /// </summary>
        private void RemoveNodeData(BaseNodeView nodeView)
        {
            if (dialogueAsset == null || nodeView?.NodeData == null) return;

            BaseNodeData nodeDataToRemove = nodeView.NodeData;

            Undo.SetCurrentGroupName("Remove Node and Connections");
            int group = Undo.GetCurrentGroup();

            // 🔧 CORREÇÃO: Remove conexões usando FromNodeGUID e ToNodeGUID corretos
            var connectionsToRemove = dialogueAsset.Connections
                .Where(c => c != null && // ✅ Proteção contra null
                           (c.FromNodeGUID == nodeDataToRemove.guid ||
                            c.ToNodeGUID == nodeDataToRemove.guid))
                .ToList();

            if (connectionsToRemove.Count > 0)
            {
                Undo.RecordObject(dialogueAsset, "Remove Node Connections");

                Debug.Log($"[DialogueGraphView] Removing node '{nodeView.title}' and {connectionsToRemove.Count} associated connection(s).");

                foreach (var conn in connectionsToRemove)
                {
                    dialogueAsset.Connections.Remove(conn);

                    
                }

                EditorUtility.SetDirty(dialogueAsset);
            }

            // Remove o nó da lista
            Undo.RecordObject(dialogueAsset, "Remove Node from List");
            bool removed = dialogueAsset.Nodes.Remove(nodeDataToRemove);

            if (removed)
            {
                EditorUtility.SetDirty(dialogueAsset);
            }

            // Destroi o sub-asset do nó
            if (AssetDatabase.IsSubAsset(nodeDataToRemove))
            {
                Undo.DestroyObjectImmediate(nodeDataToRemove);
            }
            else
            {
                Debug.LogWarning($"NodeData '{nodeDataToRemove.name}' was not a sub-asset. Skipping Undo.DestroyObjectImmediate.");
            }

            nodeViewCache.Remove(nodeDataToRemove.guid);

            Undo.CollapseUndoOperations(group);

            // Salva o asset após todas as mudanças
            AssetDatabase.SaveAssets();
        }

        // ==================== GERENCIAMENTO DE CONEXÕES (COM UNDO) ====================

        public void SaveConnection(BaseNodeView outputNode, BaseNodeView inputNode, Port outputPort, Port inputPort)
        {
            if (dialogueAsset == null || outputNode?.NodeData == null || inputNode?.NodeData == null ||
                outputPort == null || inputPort == null)
            {
                Debug.LogError("SaveConnection: Invalid node or port data provided.");
                return;
            }

            int outputPortIndex = outputNode.GetPortIndex(outputPort);
            int inputPortIndex = inputNode.GetPortIndex(inputPort);

            if (outputPortIndex == -1 || inputPortIndex == -1)
            {
                Debug.LogError($"SaveConnection: Invalid port index. Output: {outputPortIndex}, Input: {inputPortIndex}");
                return;
            }

            Undo.SetCurrentGroupName("Create Connection");
            int group = Undo.GetCurrentGroup();

            Undo.RecordObject(dialogueAsset, "Create Connection");

            // Remove conexões existentes da mesma porta de saída se Single
            if (outputPort.capacity == Port.Capacity.Single)
            {
                dialogueAsset.Connections.RemoveAll(c =>
                    c.FromNodeGUID == outputNode.NodeData.GUID &&
                    c.FromPortIndex == outputPortIndex);
            }

            ConnectionData newConnection = new ConnectionData
            {
                FromNodeGUID = outputNode.NodeData.GUID,
                FromPortIndex = outputPortIndex,
                ToNodeGUID = inputNode.NodeData.GUID,
                ToPortIndex = inputPortIndex
            };

            dialogueAsset.Connections.Add(newConnection);

            EditorUtility.SetDirty(dialogueAsset);

            Undo.CollapseUndoOperations(group);

            Debug.Log($"[DialogueGraphView] Connection created: {outputNode.title}[{outputPortIndex}] -> {inputNode.title}[{inputPortIndex}]");
        }

        private void RemoveConnection(Edge edge)
        {
            if (dialogueAsset == null || edge == null) return;

            var outputNode = edge.output?.node as BaseNodeView;
            var inputNode = edge.input?.node as BaseNodeView;
            var outputPort = edge.output;

            if (outputNode?.NodeData == null || inputNode?.NodeData == null || outputPort == null)
            {
                return;
            }

            int outputPortIndex = outputNode.GetPortIndex(outputPort);

            if (outputPortIndex == -1) return;

            var connectionToRemove = dialogueAsset.Connections?.FirstOrDefault(c =>
                c.FromNodeGUID == outputNode.NodeData.guid &&
                c.ToNodeGUID == inputNode.NodeData.guid &&
                c.FromPortIndex == outputPortIndex
            );

            if (connectionToRemove != null)
            {
                Undo.RecordObject(dialogueAsset, "Remove Connection");
                dialogueAsset.Connections.Remove(connectionToRemove);
                EditorUtility.SetDirty(dialogueAsset);
            }
        }

        // ==================== 🧹 LIMPEZA DE CONEXÕES ÓRFÃS ====================

        /// <summary>
        /// 🧹 Limpa conexões órfãs e corrompidas do asset automaticamente
        /// </summary>
        private void CleanOrphanConnections()
        {
            if (dialogueAsset == null) return;

            // Cria hashset com GUIDs válidos dos nós
            var validNodeGuids = dialogueAsset.Nodes
                .Where(n => n != null && !string.IsNullOrEmpty(n.GUID))
                .Select(n => n.GUID)
                .ToHashSet();

            // Encontra conexões corrompidas
            var corruptedConnections = dialogueAsset.Connections
                .Where(c => c == null ||
                           string.IsNullOrEmpty(c.FromNodeGUID) ||
                           string.IsNullOrEmpty(c.ToNodeGUID) ||
                           !validNodeGuids.Contains(c.FromNodeGUID) ||
                           !validNodeGuids.Contains(c.ToNodeGUID))
                .ToList();

            if (corruptedConnections.Count > 0)
            {
                Undo.RecordObject(dialogueAsset, "Clean Orphan Connections");

                Debug.LogWarning($"[DialogueGraphView] Found {corruptedConnections.Count} corrupted connection(s) in '{dialogueAsset.name}'. Auto-cleaning...");

                foreach (var connection in corruptedConnections)
                {
                    dialogueAsset.Connections.Remove(connection);

                    
                }

                EditorUtility.SetDirty(dialogueAsset);
                AssetDatabase.SaveAssets();

                Debug.Log($"[DialogueGraphView] Successfully cleaned {corruptedConnections.Count} corrupted connection(s).");
            }
        }

        // ==================== 🔧 POPULAR VIEW MELHORADO ====================

        /// <summary>
        /// 🔧 ATUALIZADO: PopulateView com validação robusta e limpeza automática
        /// </summary>
        public void PopulateView()
        {
            graphViewChanged -= OnGraphViewChanged;
            DialogueEditorEvents.OnNodeDataChanged -= HandleNodeDataChanged;

            DeleteElements(graphElements.ToList());
            nodeViewCache.Clear();

            if (dialogueAsset == null)
            {
                graphViewChanged += OnGraphViewChanged;
                DialogueEditorEvents.OnNodeDataChanged += HandleNodeDataChanged;
                return;
            }

            // 🧹 LIMPEZA AUTOMÁTICA antes de popular
            CleanOrphanConnections();

            // Recria nós com validação robusta
            if (dialogueAsset.Nodes != null)
            {
                foreach (var nodeData in dialogueAsset.Nodes)
                {
                    // 🔍 Validação 1: Null check
                    if (nodeData == null)
                    {
                        Debug.LogWarning($"[DialogueGraphView] Found null NodeData in asset '{dialogueAsset.name}'. Skipping.");
                        continue;
                    }

                    // 🔍 Validação 2: GUID vazio
                    if (string.IsNullOrEmpty(nodeData.GUID))
                    {
                        Debug.LogWarning($"[DialogueGraphView] Node with empty GUID found in '{dialogueAsset.name}'. Assigning new GUID.");
                        nodeData.guid = System.Guid.NewGuid().ToString();
                        EditorUtility.SetDirty(nodeData);
                        EditorUtility.SetDirty(dialogueAsset);
                    }

                    BaseNodeView nodeView = CreateNodeViewVisual(nodeData);
                    if (nodeView != null)
                    {
                        AddElement(nodeView);
                        nodeViewCache[nodeData.GUID] = nodeView;
                    }
                }
            }

            // Recria conexões com validação completa
            if (dialogueAsset.Connections != null)
            {
                foreach (var connectionData in dialogueAsset.Connections)
                {
                    // 🔍 Validação 1: Null check
                    if (connectionData == null)
                    {
                        Debug.LogWarning($"[DialogueGraphView] Found null ConnectionData in asset '{dialogueAsset.name}'. Skipping.");
                        continue;
                    }

                    // 🔍 Validação 2: GUIDs vazios
                    if (string.IsNullOrEmpty(connectionData.FromNodeGUID) ||
                        string.IsNullOrEmpty(connectionData.ToNodeGUID))
                    {
                        Debug.LogWarning($"[DialogueGraphView] Connection with empty GUIDs found: '{connectionData.FromNodeGUID}' -> '{connectionData.ToNodeGUID}'. Skipping.");
                        continue;
                    }

                    // 🔍 Validação 3: Nós existem no cache?
                    if (!nodeViewCache.TryGetValue(connectionData.FromNodeGUID, out BaseNodeView outputNodeView))
                    {
                        Debug.LogWarning($"[DialogueGraphView] Connection references non-existent source node GUID: {connectionData.FromNodeGUID}. Skipping edge.");
                        continue;
                    }

                    if (!nodeViewCache.TryGetValue(connectionData.ToNodeGUID, out BaseNodeView inputNodeView))
                    {
                        Debug.LogWarning($"[DialogueGraphView] Connection references non-existent target node GUID: {connectionData.ToNodeGUID}. Skipping edge.");
                        continue;
                    }

                    // 🔍 Validação 4: Portas existem?
                    Port outputPort = outputNodeView.GetOutputPort(connectionData.FromPortIndex);
                    Port inputPort = inputNodeView.GetInputPort(connectionData.ToPortIndex);

                    if (outputPort != null && inputPort != null)
                    {
                        var edge = outputPort.ConnectTo(inputPort);
                        edge.userData = connectionData;
                        AddElement(edge);
                    }
                    else
                    {
                        Debug.LogWarning($"[DialogueGraphView] Could not find valid ports for connection: " +
                                       $"'{outputNodeView.title}'[Port:{connectionData.FromPortIndex}] -> " +
                                       $"'{inputNodeView.title}'[Port:{connectionData.ToPortIndex}]. Edge not created.");
                    }
                }
            }

            graphViewChanged += OnGraphViewChanged;
            DialogueEditorEvents.OnNodeDataChanged += HandleNodeDataChanged;

            Debug.Log($"[DialogueGraphView] PopulateView completed. Nodes: {nodeViewCache.Count}, Connections: {edges.ToList().Count}");
        }

        public void PopulateView(DialogueAsset asset)
        {
            dialogueAsset = asset;
            PopulateView();
        }

        private BaseNodeView CreateNodeViewVisual(BaseNodeData nodeData)
        {
            BaseNodeView nodeView = null;

            nodeView = nodeData switch
            {
                RootNodeData rootData => new RootNodeView(rootData),
                SpeechNodeData speechData => new SpeechNodeView(speechData),
                OptionNodeData optionData => new OptionNodeView(optionData),
                _ => null
            };

            if (nodeView != null)
            {
                nodeView.SetPosition(new Rect(nodeData.EditorPosition, Vector2.zero));
                SetupEdgeConnectorListener(nodeView);
            }
            else if (nodeData != null)
            {
                Debug.LogError($"Could not create NodeView: No view class registered for NodeData type '{nodeData.GetType().Name}'.");
            }
            return nodeView;
        }

        // ==================== SEARCH WINDOW ====================

        private void SetupNodeCreationRequest()
        {
            nodeCreationRequest = context =>
            {
                OpenSearchWindow(null, context.screenMousePosition);
            };
        }

        private void InitializeSearchWindow()
        {
            searchWindowProvider = ScriptableObject.CreateInstance<NodeSearchWindow>();
            searchWindowProvider.Initialize(this, parentWindow);
        }

        public void OpenSearchWindow(Port originPort, Vector2 screenPosition)
        {
            if (searchWindowProvider == null) InitializeSearchWindow();

            searchWindowProvider.SetOriginPort(originPort);
            SearchWindowContext context = new SearchWindowContext(screenPosition, 350, 250);
            SearchWindow.Open(context, searchWindowProvider);
        }

        // ==================== DRAG AND DROP ====================

        private void SetupEdgeConnectorListener(BaseNodeView nodeView)
        {
            nodeView.Query<Port>().ForEach(port =>
            {
                var listener = new CustomEdgeConnectorListener(this, port);
                var connector = new EdgeConnector<Edge>(listener);
                port.AddManipulator(connector);
            });
        }

        private class CustomEdgeConnectorListener : IEdgeConnectorListener
        {
            private DialogueGraphView dialogueGraphView;
            private Port originPort;

            public CustomEdgeConnectorListener(DialogueGraphView graphView, Port port)
            {
                this.dialogueGraphView = graphView;
                this.originPort = port;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                if (dialogueGraphView != null && originPort != null && originPort.direction == Direction.Output)
                {
                    Vector2 screenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                    dialogueGraphView.OpenSearchWindow(originPort, screenPosition);
                }
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                // Lógica já está em OnGraphViewChanged
            }
        }

        // ==================== PORT COMPATIBILITY ====================

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node
            ).ToList();
        }

        // ==================== ESTILOS ====================

        private void LoadStyles()
        {
            var styleSheet = Resources.Load<StyleSheet>("USS/DialogueGraphStyles");
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
            else
                Debug.LogWarning("Could not load StyleSheet: Resources/USS/DialogueGraphStyles.uss");

            var nodeStyleSheet = Resources.Load<StyleSheet>("USS/NodeStyles");
            if (nodeStyleSheet != null)
                styleSheets.Add(nodeStyleSheet);
            else
                Debug.LogWarning("Could not load StyleSheet: Resources/USS/NodeStyles.uss");
        }
    }
}