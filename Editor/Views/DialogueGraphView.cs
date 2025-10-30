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

            // Atalhos (você tem um DialogueGraphViewShortcutProvider, então esta linha pode ser desnecessária)
            // DialogueGraphViewShortcuts.RegisterGraphViewFocus(this);

            graphViewChanged += OnGraphViewChanged;
            SetupNodeCreationRequest();
            InitializeSearchWindow();
            LoadStyles();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            RegisterCallback<DetachFromPanelEvent>(evt => Undo.undoRedoPerformed -= OnUndoRedoPerformed);

            DialogueEditorEvents.OnNodeDataChanged += HandleNodeDataChanged;
            RegisterCallback<DetachFromPanelEvent>(evt => DialogueEditorEvents.OnNodeDataChanged -= HandleNodeDataChanged);

            // Foco para atalhos
            // RegisterCallback<FocusInEvent>(evt =>
            // {
            //     DialogueGraphViewShortcuts.RegisterGraphViewFocus(this);
            // });
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

        /// <summary>
        /// Cria um nó Raiz (só deve ser chamado se não existir um).
        /// </summary>
        public BaseNodeView CreateRootNode(Vector2 position)
        {
            if (dialogueAsset?.RootNode != null)
            {
                EditorUtility.DisplayDialog("Root Node Exists", "A Root Node already exists for this dialogue. Only one is allowed.", "OK");
                return null; // Não cria se já existir um
            }

            return CreateNodeInternal<RootNodeData>("Create Root Node", position, nodeData =>
            {
                // Posição ajustada para ficar claro
                nodeData.EditorPosition = new Vector2(100, 200);
            });
        }


        /// <summary>
        /// Cria um nó de Fala.
        /// </summary>
        public BaseNodeView CreateSpeechNode(Vector2 position)
        {
            return CreateNodeInternal<SpeechNodeData>("Create Speech Node", position, nodeData =>
            {
                nodeData.CharacterName = "Character";
                nodeData.DialogueText = "Enter dialogue text here...";
            });
        }

        /// <summary>
        /// Cria um nó de Opção.
        /// </summary>
        public BaseNodeView CreateOptionNode(Vector2 position)
        {
            return CreateNodeInternal<OptionNodeData>("Create Option Node", position, nodeData =>
            {
                if (nodeData.options == null)
                    nodeData.options = new List<OptionNodeData.Option>();
                else
                    nodeData.options.Clear();

                // Adiciona uma opção padrão para começar
                nodeData.options.Add(new OptionNodeData.Option
                {
                    optionText = "Option 1",
                    conditions = new List<BaseCondition>(),
                    onOptionSelected = new UnityEngine.Events.UnityEvent()
                });
            });
        }

        /// <summary>
        /// ✨ NOVO: Cria um nó de Branch (If).
        /// </summary>
        public BaseNodeView CreateBranchNode(Vector2 position)
        {
            return CreateNodeInternal<BranchNodeData>("Create Branch Node", position, nodeData =>
            {
                // Inicializa a lista de condições
                if (nodeData.conditions == null)
                    nodeData.conditions = new List<BaseCondition>();
            });
        }


        /// <summary>
        /// Lógica interna genérica para criar qualquer tipo de nó (NodeData + NodeView) com Undo.
        /// </summary>
        private BaseNodeView CreateNodeInternal<TNodeData>(string undoName, Vector2 position, Action<TNodeData> initializer = null)
            where TNodeData : BaseNodeData
        {
            if (dialogueAsset == null)
            {
                Debug.LogError("Cannot create node: No DialogueAsset loaded.");
                return null;
            }

            // 1. Cria a instância do ScriptableObject (NodeData)
            TNodeData nodeData = ScriptableObject.CreateInstance<TNodeData>();
            nodeData.name = typeof(TNodeData).Name; // Nome para visualização no Project
            nodeData.guid = GUID.Generate().ToString(); // Gera GUID único
            nodeData.EditorPosition = position;       // Define posição inicial

            // 2. Aplica inicialização customizada (valores padrão)
            initializer?.Invoke(nodeData);

            // 3. Registra a CRIAÇÃO do objeto para Undo
            Undo.RegisterCreatedObjectUndo(nodeData, undoName);

            // 4. Adiciona como SUB-ASSET ao DialogueAsset principal
            AssetDatabase.AddObjectToAsset(nodeData, dialogueAsset);

            // 5. Registra a ADIÇÃO à lista 'Nodes' do DialogueAsset para Undo
            Undo.RecordObject(dialogueAsset, undoName);
            //if (dialogueAsset.Nodes == null) dialogueAsset.Nodes = new List<BaseNodeData>();
            dialogueAsset.Nodes.Add(nodeData);

            // 6. Marca ambos os assets como "sujos" para salvamento
            EditorUtility.SetDirty(nodeData);
            EditorUtility.SetDirty(dialogueAsset);

            // 7. Cria a VISUALIZAÇÃO (NodeView)
            BaseNodeView nodeView = CreateNodeViewVisual(nodeData);

            // 8. Adiciona a visualização ao GraphView
            if (nodeView != null)
            {
                AddElement(nodeView);
                nodeViewCache[nodeData.guid] = nodeView;
            }

            return nodeView;
        }

        // ==================== REMOÇÃO DE NÓS (COM UNDO) ====================

        /// <summary>
        /// Remove os dados do nó (NodeData) e todas as conexões associadas, com Undo.
        /// </summary>
        private void RemoveNodeData(BaseNodeView nodeView)
        {
            if (dialogueAsset == null || nodeView?.NodeData == null) return;

            // Não permite deletar o RootNode
            if (nodeView.NodeData is RootNodeData)
            {
                EditorUtility.DisplayDialog("Cannot Delete Root Node", "The Root Node (▶ START) cannot be deleted.", "OK");
                return;
            }

            BaseNodeData nodeDataToRemove = nodeView.NodeData;

            Undo.SetCurrentGroupName("Remove Node and Connections");
            int group = Undo.GetCurrentGroup();

            // 1. Remove Conexões Associadas
            var connectionsToRemove = dialogueAsset.Connections
                .Where(c => c != null &&
                           (c.FromNodeGUID == nodeDataToRemove.guid ||
                            c.ToNodeGUID == nodeDataToRemove.guid))
                .ToList();

            if (connectionsToRemove.Count > 0)
            {
                Undo.RecordObject(dialogueAsset, "Remove Node Connections");
                foreach (var conn in connectionsToRemove)
                {
                    dialogueAsset.Connections.Remove(conn);
                }
                EditorUtility.SetDirty(dialogueAsset);
            }

            // 2. Remove o Nó da Lista Principal
            Undo.RecordObject(dialogueAsset, "Remove Node from List");
            bool removed = dialogueAsset.Nodes.Remove(nodeDataToRemove);
            if (removed) EditorUtility.SetDirty(dialogueAsset);

            // 3. Destroi o Sub-Asset (NodeData)
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
            AssetDatabase.SaveAssets(); // Salva as mudanças de sub-asset
        }

        // ==================== GERENCIAMENTO DE CONEXÕES (COM UNDO) ====================

        /// <summary>
        /// Salva uma nova conexão nos dados do DialogueAsset, registrando Undo.
        /// </summary>
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
                // O LogError já ocorreu dentro de GetPortIndex
                return;
            }

            Undo.SetCurrentGroupName("Create Connection");
            int group = Undo.GetCurrentGroup();
            Undo.RecordObject(dialogueAsset, "Create Connection");

            // Remove conexões existentes da MESMA porta de SAÍDA, APENAS se a capacidade for Single
            if (outputPort.capacity == Port.Capacity.Single)
            {
                dialogueAsset.Connections.RemoveAll(c =>
                    c.FromNodeGUID == outputNode.NodeData.guid &&
                    c.FromPortIndex == outputPortIndex);
            }

            // Remove conexões existentes da MESMA porta de ENTRADA, APENAS se a capacidade for Single
            if (inputPort.capacity == Port.Capacity.Single)
            {
                dialogueAsset.Connections.RemoveAll(c =>
                   c.ToNodeGUID == inputNode.NodeData.guid &&
                   c.ToPortIndex == inputPortIndex); // Usa ToPortIndex
            }

            ConnectionData newConnection = new ConnectionData
            {
                FromNodeGUID = outputNode.NodeData.guid,
                FromPortIndex = outputPortIndex,
                ToNodeGUID = inputNode.NodeData.guid,
                ToPortIndex = inputPortIndex // ✨ SALVA O ÍNDICE DA PORTA DE ENTRADA
            };

            //if (dialogueAsset.Connections == null)
                //dialogueAsset.Connections = new List<ConnectionData>();

            dialogueAsset.Connections.Add(newConnection);
            EditorUtility.SetDirty(dialogueAsset);
            Undo.CollapseUndoOperations(group);

            Debug.Log($"[DialogueGraphView] Connection created: {outputNode.title}[{outputPortIndex}] -> {inputNode.title}[{inputPortIndex}]");
        }

        /// <summary>
        /// Remove uma conexão específica dos dados do DialogueAsset, registrando Undo.
        /// </summary>
        private void RemoveConnection(Edge edge)
        {
            if (dialogueAsset == null || edge == null) return;

            var outputNode = edge.output?.node as BaseNodeView;
            var inputNode = edge.input?.node as BaseNodeView;
            var outputPort = edge.output;
            var inputPort = edge.input; // ✨

            if (outputNode?.NodeData == null || inputNode?.NodeData == null || outputPort == null || inputPort == null) // ✨
            {
                return;
            }

            int outputPortIndex = outputNode.GetPortIndex(outputPort);
            int inputPortIndex = inputNode.GetPortIndex(inputPort); // ✨

            if (outputPortIndex == -1 || inputPortIndex == -1) return; // ✨

            // Encontra a ConnectionData correspondente
            var connectionToRemove = dialogueAsset.Connections?.FirstOrDefault(c =>
                c.FromNodeGUID == outputNode.NodeData.guid &&
                c.ToNodeGUID == inputNode.NodeData.guid &&
                c.FromPortIndex == outputPortIndex &&
                c.ToPortIndex == inputPortIndex // ✨
            );

            if (connectionToRemove != null)
            {
                Undo.RecordObject(dialogueAsset, "Remove Connection");
                dialogueAsset.Connections.Remove(connectionToRemove);
                EditorUtility.SetDirty(dialogueAsset);
            }
        }

        // ==================== LIMPEZA DE CONEXÕES ÓRFÃS ====================

        /// <summary>
        /// Limpa conexões órfãs e corrompidas do asset automaticamente
        /// </summary>
        private void CleanOrphanConnections()
        {
            if (dialogueAsset == null) return;

            var validNodeGuids = dialogueAsset.Nodes
                .Where(n => n != null && !string.IsNullOrEmpty(n.GUID))
                .Select(n => n.GUID)
                .ToHashSet();

            // Encontra conexões corrompidas (nulas, GUIDs vazios, ou GUIDs que não existem)
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
                Debug.LogWarning($"[DialogueGraphView] Found {corruptedConnections.Count} corrupted/orphan connection(s) in '{dialogueAsset.name}'. Auto-cleaning...");

                foreach (var connection in corruptedConnections)
                {
                    dialogueAsset.Connections.Remove(connection);
                }

                EditorUtility.SetDirty(dialogueAsset);
                AssetDatabase.SaveAssets(); // Salva a limpeza
            }
        }

        // ==================== POPULAR E CRIAR VIEWS (Visual) ====================

        /// <summary>
        /// Limpa e recria toda a representação visual do grafo a partir do DialogueAsset atual.
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

            // Limpa conexões inválidas ANTES de tentar desenhar
            CleanOrphanConnections();

            // Recria nós com validação
            if (dialogueAsset.Nodes != null)
            {
                // Garante que o RootNode seja desenhado primeiro (opcional, mas bom para layout)
                var rootNodeData = dialogueAsset.RootNode;
                if (rootNodeData != null)
                {
                    DrawNodeAndCache(rootNodeData);
                }

                // Desenha os outros nós
                foreach (var nodeData in dialogueAsset.Nodes.Where(n => n != null && !(n is RootNodeData)))
                {
                    DrawNodeAndCache(nodeData);
                }
            }

            // Recria conexões com validação
            if (dialogueAsset.Connections != null)
            {
                foreach (var connectionData in dialogueAsset.Connections)
                {
                    if (connectionData == null) continue;

                    if (!nodeViewCache.TryGetValue(connectionData.FromNodeGUID, out BaseNodeView outputNodeView) ||
                        !nodeViewCache.TryGetValue(connectionData.ToNodeGUID, out BaseNodeView inputNodeView))
                    {
                        // (Já foi logado pelo CleanOrphanConnections, mas é bom prevenir)
                        continue;
                    }

                    // Encontra as Ports corretas
                    Port outputPort = outputNodeView.GetOutputPort(connectionData.FromPortIndex);
                    Port inputPort = inputNodeView.GetInputPort(connectionData.ToPortIndex); // ✨ USA ToPortIndex

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
        }

        // Helper para popular a view, evitando duplicação
        private void DrawNodeAndCache(BaseNodeData nodeData)
        {
            if (string.IsNullOrEmpty(nodeData.GUID))
            {
                Debug.LogWarning($"[DialogueGraphView] Node '{nodeData.name}' has empty GUID. Assigning new one.");
                nodeData.guid = System.Guid.NewGuid().ToString();
                EditorUtility.SetDirty(nodeData);
            }

            if (nodeViewCache.ContainsKey(nodeData.GUID)) return; // Já foi processado

            BaseNodeView nodeView = CreateNodeViewVisual(nodeData);
            if (nodeView != null)
            {
                AddElement(nodeView);
                nodeViewCache[nodeData.GUID] = nodeView;
            }
        }

        public void PopulateView(DialogueAsset asset)
        {
            dialogueAsset = asset;
            PopulateView();
        }

        /// <summary>
        /// Cria a instância da classe de visualização (NodeView) correta para um NodeData.
        /// </summary>
        private BaseNodeView CreateNodeViewVisual(BaseNodeData nodeData)
        {
            BaseNodeView nodeView = null;

            // Usa switch expression para determinar qual View instanciar
            nodeView = nodeData switch
            {
                RootNodeData rootData => new RootNodeView(rootData),
                SpeechNodeData speechData => new SpeechNodeView(speechData),
                OptionNodeData optionData => new OptionNodeView(optionData),
                BranchNodeData branchData => new BranchNodeView(branchData), // ✨ ADICIONADO BRANCH VIEW
                _ => null
            };

            if (nodeView != null)
            {
                nodeView.SetPosition(new Rect(nodeData.EditorPosition, Vector2.zero));
                SetupEdgeConnectorListener(nodeView); // Configura drag-and-drop
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
                // A lógica já está em OnGraphViewChanged
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