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
    /// e salvamento de conexões corrigido.
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        public DialogueAsset dialogueAsset;
        private EditorWindow parentWindow;
        private NodeSearchWindow searchWindowProvider; // Cache para Search Window

        // Cache para mapear GUIDs de NodeData para NodeViews (otimiza PopulateView)
        private Dictionary<string, BaseNodeView> nodeViewCache = new Dictionary<string, BaseNodeView>();

        public DialogueGraphView(DialogueAsset asset, EditorWindow window)
        {
            dialogueAsset = asset;
            parentWindow = window;

            // --- Configuração Visual Padrão ---
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            var gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();
            // ------------------------------------

            // --- Callbacks e Estilos ---
            graphViewChanged += OnGraphViewChanged;
            SetupNodeCreationRequest(); // Usa Search Window para clique direito
            InitializeSearchWindow();   // Prepara a Search Window
            LoadStyles();              // Carrega USS
            // ---------------------------

            // --- Registro de Callbacks para Desfazer/Refazer ---
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            // Garante desregistro quando a view for removida
            RegisterCallback<DetachFromPanelEvent>(evt => Undo.undoRedoPerformed -= OnUndoRedoPerformed);
            // ----------------------------------------------------

            // --- Registro para Atualizações do Inspector ---
            DialogueEditorEvents.OnNodeDataChanged += HandleNodeDataChanged;
            RegisterCallback<DetachFromPanelEvent>(evt => DialogueEditorEvents.OnNodeDataChanged -= HandleNodeDataChanged);
            // -----------------------------------------------
        }

        /// <summary>
        /// Recarrega a view quando Undo/Redo é executado pelo usuário.
        /// </summary>
        private void OnUndoRedoPerformed()
        {
            // A maneira mais segura de sincronizar após Undo/Redo é recarregar tudo
            if (dialogueAsset != null)
            {
                PopulateView();
                // Força o Inspector a redesenhar (caso o Undo tenha afetado o nó selecionado)
                EditorUtility.SetDirty(dialogueAsset); // Marca o asset para garantir atualização
                // Se um nó específico estava selecionado, pode ser necessário re-selecioná-lo programaticamente
                // Selection.activeObject = ...;
            }
        }

        /// <summary>
        /// Ouve mudanças nos dados (via Inspector) e atualiza a view do nó correspondente.
        /// </summary>
        private void HandleNodeDataChanged(BaseNodeData changedNodeData)
        {
            if (changedNodeData == null || dialogueAsset == null) return;

            // Verifica se o nó pertence a este asset e se a view existe no cache
            if (dialogueAsset.Nodes.Contains(changedNodeData) && nodeViewCache.TryGetValue(changedNodeData.guid, out BaseNodeView nodeView))
            {
                // Manda a view específica se atualizar
                nodeView.UpdateNodeView();
            }
        }


        /// <summary>
        /// Callback principal que intercepta mudanças no grafo (criação, remoção, movimento)
        /// e as registra com o sistema Undo da Unity.
        /// </summary>
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (dialogueAsset == null) return graphViewChange; // Não faz nada se não houver asset carregado

            bool requiresUndoRecordingOnAsset = false; // Flag para marcar o asset principal

            // ================== REMOÇÃO DE ELEMENTOS ==================
            if (graphViewChange.elementsToRemove != null)
            {
                requiresUndoRecordingOnAsset = true;
                Undo.SetCurrentGroupName("Remove Graph Elements");
                int group = Undo.GetCurrentGroup();

                // Processa Edges (Conexões) PRIMEIRO
                foreach (var element in graphViewChange.elementsToRemove.OfType<Edge>())
                {
                    // A remoção da conexão dos dados é registrada dentro de RemoveConnection
                    RemoveConnection(element);
                }

                // Processa Nós DEPOIS
                foreach (var element in graphViewChange.elementsToRemove.OfType<BaseNodeView>())
                {
                    // A remoção do nó e suas conexões dos dados é registrada dentro de RemoveNodeData
                    RemoveNodeData(element);
                    // Remove do cache visual
                    if (element.NodeData != null) nodeViewCache.Remove(element.NodeData.guid);
                }

                Undo.CollapseUndoOperations(group); // Agrupa as operações de remoção
            }

            // ================== CRIAÇÃO DE EDGES ==================
            if (graphViewChange.edgesToCreate != null)
            {
                requiresUndoRecordingOnAsset = true;
                Undo.SetCurrentGroupName("Create Connections");
                int group = Undo.GetCurrentGroup();

                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var outputNode = edge.output?.node as BaseNodeView; // Null check
                    var inputNode = edge.input?.node as BaseNodeView;   // Null check

                    // Garante que ambas as pontas são nós válidos do nosso sistema
                    if (outputNode != null && inputNode != null)
                    {
                        // Registra a adição da conexão aos dados dentro de SaveConnection
                        SaveConnection(outputNode, inputNode, edge.output, edge.input);
                    }
                }
                Undo.CollapseUndoOperations(group);
            }

            // ================== MOVIMENTO DE ELEMENTOS ==================
            if (graphViewChange.movedElements != null && graphViewChange.movedElements.Count > 0)
            {
                requiresUndoRecordingOnAsset = true; // Movimento também suja o asset principal

                foreach (var element in graphViewChange.movedElements.OfType<BaseNodeView>())
                {
                    if (element.NodeData == null) continue; // Segurança

                    // Registra o ScriptableObject específico do nó ANTES de mudar a posição
                    Undo.RecordObject(element.NodeData, "Move Node");

                    // Atualiza a posição nos dados
                    element.NodeData.EditorPosition = element.GetPosition().position;

                    // Marca o ScriptableObject do nó como sujo (importante se for sub-asset)
                    EditorUtility.SetDirty(element.NodeData);
                }
            }

            // Marca o asset principal como sujo se qualquer operação ocorreu
            if (requiresUndoRecordingOnAsset && dialogueAsset != null)
            {
                EditorUtility.SetDirty(dialogueAsset);
                // Considerar chamar AssetDatabase.SaveAssets() aqui se quiser salvamento *real* automático,
                // mas SetDirty() é geralmente preferível para permitir ao usuário salvar manualmente (Ctrl+S).
            }

            return graphViewChange; // Retorna a mudança para o GraphView processar visualmente
        }


        // ==================== CRIAÇÃO DE NÓS (COM UNDO) ====================

        /// <summary>
        /// Cria um nó de fala, registrando Undo e adicionando ao asset.
        /// </summary>
        public BaseNodeView CreateSpeechNode(Vector2 position)
        {
            // Chama o método genérico com o tipo e inicializador
            return CreateNodeInternal<SpeechNodeData>("Create Speech Node", position, nodeData =>
            {
                // Define valores padrão específicos para SpeechNodeData
                nodeData.CharacterName = "Character";
                nodeData.DialogueText = "Enter dialogue text here...";
                // Inicializa UnityEvents se não forem inicializados por padrão
                // nodeData.OnNodeActivated = new UnityEngine.Events.UnityEvent();
                // nodeData.OnNodeCompleted = new UnityEngine.Events.UnityEvent();
            });
        }

        /// <summary>
        /// Cria um nó de opção, registrando Undo e adicionando ao asset.
        /// </summary>
        public BaseNodeView CreateOptionNode(Vector2 position)
        {
            // Chama o método genérico com o tipo e inicializador
            return CreateNodeInternal<OptionNodeData>("Create Option Node", position, nodeData =>
            {
                // Garante que a lista exista e adiciona uma opção padrão
                if (nodeData.options == null)
                    nodeData.options = new List<OptionNodeData.Option>();
                else
                    nodeData.options.Clear(); // Limpa caso o SO esteja sendo reutilizado?

                nodeData.options.Add(new OptionNodeData.Option
                {
                    optionText = "Option 1",
                    // Garante que sub-listas também sejam inicializadas
                    conditions = new List<BaseCondition>(),
                    onOptionSelected = new UnityEngine.Events.UnityEvent()
                });
            });
        }

        /// <summary>
        /// Lógica interna genérica para criar qualquer tipo de nó (NodeData + NodeView) com Undo.
        /// </summary>
        private BaseNodeView CreateNodeInternal<TNodeData>(string undoName, Vector2 position, Action<TNodeData> initializer = null)
            where TNodeData : BaseNodeData // Garante que TNodeData herda de BaseNodeData
        {
            if (dialogueAsset == null)
            {
                Debug.LogError("Cannot create node: No DialogueAsset loaded.");
                return null;
            }


            // 1. Cria a instância do ScriptableObject (NodeData)
            TNodeData nodeData = ScriptableObject.CreateInstance<TNodeData>();
            nodeData.name = typeof(TNodeData).Name; // Nome útil no Project view se não for sub-asset
            nodeData.guid = GUID.Generate().ToString(); // Gera GUID único
            nodeData.EditorPosition = position;       // Define posição inicial

            // 2. Aplica inicialização customizada (valores padrão)
            initializer?.Invoke(nodeData);

            // 3. Registra a CRIAÇÃO do objeto para Undo
            Undo.RegisterCreatedObjectUndo(nodeData, undoName);

            // 4. Adiciona o NodeData como SUB-ASSET do DialogueAsset principal
            //    Isso garante que ele seja salvo junto com o asset principal.
            AssetDatabase.AddObjectToAsset(nodeData, dialogueAsset);

            // 5. Registra a ADIÇÃO à lista 'Nodes' do DialogueAsset para Undo
            Undo.RecordObject(dialogueAsset, undoName);
            // Garante que a lista exista antes de adicionar
            //if (dialogueAsset.Nodes == null) dialogueAsset.Nodes = new List<BaseNodeData>();
            dialogueAsset.Nodes.Add(nodeData);

            // 6. Marca ambos os assets como "sujos" para salvamento
            EditorUtility.SetDirty(nodeData);      // Marca o sub-asset
            EditorUtility.SetDirty(dialogueAsset); // Marca o asset principal

            // 7. Cria a VISUALIZAÇÃO (NodeView) para o NodeData
            BaseNodeView nodeView = CreateNodeViewVisual(nodeData);

            // 8. Adiciona a visualização ao GraphView (operação visual, não precisa de Undo)
            if (nodeView != null)
            {
                AddElement(nodeView);
                // Adiciona ao cache para acesso rápido
                nodeViewCache[nodeData.guid] = nodeView;
            }

            return nodeView; // Retorna a view criada
        }


        // ==================== REMOÇÃO DE NÓS (COM UNDO) ====================

        /// <summary>
        /// Remove os dados do nó (NodeData) do asset, registrando Undo apropriadamente.
        /// Isso inclui remover o nó da lista, remover conexões associadas e destruir o sub-asset.
        /// </summary>
        private void RemoveNodeData(BaseNodeView nodeView)
        {
            if (dialogueAsset == null || nodeView?.NodeData == null) return;

            BaseNodeData nodeDataToRemove = nodeView.NodeData;

            // --- Agrupa todas as operações de remoção sob um único nome Undo ---
            Undo.SetCurrentGroupName("Remove Node and Connections");
            int group = Undo.GetCurrentGroup();

            // 1. Remove Conexões Associadas (Registra Undo na lista de conexões)
            var connectionsToRemove = dialogueAsset.Connections
                .Where(c => c.OutputNodeGuid == nodeDataToRemove.guid || c.InputNodeGuid == nodeDataToRemove.guid)
                .ToList(); // Copia para evitar problemas de modificação durante iteração

            if (connectionsToRemove.Count > 0)
            {
                Undo.RecordObject(dialogueAsset, "Remove Node Connections"); // Registra o asset ANTES de mudar a lista
                foreach (var conn in connectionsToRemove)
                {
                    dialogueAsset.Connections.Remove(conn);
                }
                EditorUtility.SetDirty(dialogueAsset); // Marca sujo após modificar lista
            }

            // 2. Remove o Nó da Lista Principal (Registra Undo na lista de nós)
            Undo.RecordObject(dialogueAsset, "Remove Node from List"); // Registra ANTES
            bool removed = dialogueAsset.Nodes.Remove(nodeDataToRemove);
            if (removed) EditorUtility.SetDirty(dialogueAsset); // Marca sujo se removeu

            // 3. Destroi o Sub-Asset (NodeData) (Registra a destruição para Undo)
            //    Isso remove o ScriptableObject do arquivo .asset principal.
            if (AssetDatabase.IsSubAsset(nodeDataToRemove)) // Segurança extra
            {
                Undo.DestroyObjectImmediate(nodeDataToRemove);
            }
            else
            {
                Debug.LogWarning($"NodeData '{nodeDataToRemove.name}' was not a sub-asset of '{dialogueAsset.name}'. It might not be properly removed by Undo.");
                // Considerar DestroyImmediate sem Undo se não for sub-asset?
            }


            // Remove do cache visual (não precisa de Undo)
            nodeViewCache.Remove(nodeDataToRemove.guid);

            Undo.CollapseUndoOperations(group); // Finaliza o grupo Undo
                                                // -------------------------------------------------------------------

            // Marca o asset principal como sujo (redundante se já marcado, mas garante)
            if (removed || connectionsToRemove.Count > 0)
                EditorUtility.SetDirty(dialogueAsset);
        }

        // ==================== GERENCIAMENTO DE CONEXÕES (COM UNDO) ====================

        /// <summary>
        /// Salva uma nova conexão nos dados do DialogueAsset, registrando Undo.
        /// Remove conexões antigas da mesma porta de SAÍDA se a capacidade for Single.
        /// </summary>
        public void SaveConnection(BaseNodeView outputNode, BaseNodeView inputNode, Port outputPort, Port inputPort)
        {
            // Validações Essenciais
            if (dialogueAsset == null || outputNode?.NodeData == null || inputNode?.NodeData == null || outputPort == null || inputPort == null)
            {
                Debug.LogError("SaveConnection: Invalid node or port data provided.");
                return;
            }

            // Obtém índices usando o método seguro que retorna -1 em caso de erro
            int outputPortIndex = outputNode.GetPortIndex(outputPort);
            int inputPortIndex = inputNode.GetPortIndex(inputPort); // Pode não ser necessário salvar, mas obtemos para validação

            if (outputPortIndex == -1 || inputPortIndex == -1)
            {
                // O LogError já ocorreu dentro de GetPortIndex se houve falha.
                return; // Não prossegue se os índices forem inválidos
            }

            // --- Lógica de Salvamento com Undo ---
            Undo.SetCurrentGroupName("Create Connection");
            int group = Undo.GetCurrentGroup();

            Undo.RecordObject(dialogueAsset, "Create Connection"); // Registra o asset ANTES das modificações

            // Remove conexões existentes da MESMA porta de SAÍDA, APENAS se a capacidade for Single
            if (outputPort.capacity == Port.Capacity.Single)
            {
                dialogueAsset.Connections.RemoveAll(c =>
                    c.OutputNodeGuid == outputNode.NodeData.guid &&
                    c.OutputPortIndex == outputPortIndex);
            }

            // Cria o objeto ConnectionData
            ConnectionData newConnection = new ConnectionData
            {
                // Usamos os GUIDs dos NodeData associados
                OutputNodeGuid = outputNode.NodeData.guid,
                OutputPortIndex = outputPortIndex,
                InputNodeGuid = inputNode.NodeData.guid,
                // InputPortIndex = inputPortIndex // Descomente se sua lógica precisar do índice de entrada
            };

            // Garante que a lista de conexões exista
            //if (dialogueAsset.Connections == null)
                //dialogueAsset.Connections = new List<ConnectionData>();

            // Adiciona a nova conexão (verificação de duplicata opcional, mas geralmente não necessária com a remoção acima)
            dialogueAsset.Connections.Add(newConnection);

            EditorUtility.SetDirty(dialogueAsset); // Marca o asset como sujo

            Undo.CollapseUndoOperations(group); // Finaliza o grupo
            // ------------------------------------
        }

        public void DeleteSelectedNodes()
        {
            var selectedNodes = new List<BaseNodeView>(selection.Cast<BaseNodeView>());

            if (selectedNodes.Count == 0)
                return;

            Undo.RecordObject(m_DialogueAsset, "Delete Multiple Nodes");

            foreach (var nodeView in selectedNodes)
            {
                RemoveNodeData(nodeView.BaseNodeData.GUID);
            }

            Undo.FlushUndoRecordObjects();
        }

        public void DuplicateSelectedNodes()
        {
            var selectedNodes = new List<BaseNodeView>(selection.Cast<BaseNodeView>());
            if (selectedNodes.Count == 0) return;

            // Implementado na próxima secção (Copy/Paste)
        }


        /// <summary>
        /// Remove uma conexão específica dos dados do DialogueAsset, registrando Undo.
        /// </summary>
        private void RemoveConnection(Edge edge)
        {
            if (dialogueAsset == null || edge == null) return;

            // Encontra os NodeViews e Ports associados à Edge
            var outputNode = edge.output?.node as BaseNodeView;
            var inputNode = edge.input?.node as BaseNodeView;
            var outputPort = edge.output;
            var inputPort = edge.input; // Pode ser necessário se ConnectionData salvar InputPortIndex

            if (outputNode?.NodeData == null || inputNode?.NodeData == null || outputPort == null)
            {
                // Não foi possível encontrar os dados associados, talvez já removidos?
                // Debug.LogWarning("RemoveConnection: Could not find associated node data or ports for the edge.");
                return;
            }

            int outputPortIndex = outputNode.GetPortIndex(outputPort);
            // int inputPortIndex = inputNode.GetPortIndex(inputPort); // Se necessário

            if (outputPortIndex == -1) return; // Índice inválido

            // Encontra a ConnectionData correspondente na lista do asset
            var connectionToRemove = dialogueAsset.Connections?.FirstOrDefault(c =>
                c.OutputNodeGuid == outputNode.NodeData.guid &&
                c.InputNodeGuid == inputNode.NodeData.guid && // Compara também o destino
                c.OutputPortIndex == outputPortIndex
                // && c.InputPortIndex == inputPortIndex // Adicione se salvou o índice de entrada
                );

            if (connectionToRemove != null)
            {
                // Registra o asset ANTES de remover da lista
                Undo.RecordObject(dialogueAsset, "Remove Connection");
                dialogueAsset.Connections.Remove(connectionToRemove);
                EditorUtility.SetDirty(dialogueAsset); // Marca sujo
            }
            else
            {
                // A conexão visual existe, mas os dados não? Pode acontecer em cenários de erro.
                // Debug.LogWarning("RemoveConnection: Could not find matching connection data for the removed edge.");
            }
        }


        // ==================== POPULAR E CRIAR VIEWS (Visual) ====================

        /// <summary>
        /// Limpa e recria toda a representação visual do grafo a partir do DialogueAsset atual.
        /// Chamado ao carregar um asset ou após Undo/Redo.
        /// </summary>
        public void PopulateView()
        {
            // Desregistra temporariamente para evitar NREs e loops durante a limpeza/recriação
            graphViewChanged -= OnGraphViewChanged;
            DialogueEditorEvents.OnNodeDataChanged -= HandleNodeDataChanged; // Desregistra evento do inspector também


            DeleteElements(graphElements.ToList()); // Remove todos os nós e edges visuais
            nodeViewCache.Clear(); // Limpa o cache de views

            // Se não há asset carregado, termina aqui
            if (dialogueAsset == null)
            {
                // Re-registra os callbacks para futuras interações
                graphViewChanged += OnGraphViewChanged;
                DialogueEditorEvents.OnNodeDataChanged += HandleNodeDataChanged;
                return;
            }


            // Recria as visualizações dos nós
            if (dialogueAsset.Nodes != null)
            {
                foreach (var nodeData in dialogueAsset.Nodes)
                {
                    if (nodeData == null)
                    {
                        Debug.LogWarning($"Found null NodeData in asset '{dialogueAsset.name}'. Skipping.");
                        continue;
                    }

                    BaseNodeView nodeView = CreateNodeViewVisual(nodeData); // Cria a view (sem Undo)
                    if (nodeView != null)
                    {
                        AddElement(nodeView); // Adiciona ao grafo
                        nodeViewCache[nodeData.guid] = nodeView; // Adiciona ao cache
                    }
                }
            }

            // Recria as visualizações das conexões (edges)
            if (dialogueAsset.Connections != null)
            {
                foreach (var connectionData in dialogueAsset.Connections)
                {
                    if (connectionData == null)
                    {
                        Debug.LogWarning($"Found null ConnectionData in asset '{dialogueAsset.name}'. Skipping.");
                        continue;
                    }

                    // Encontra as NodeViews de origem e destino usando o cache
                    if (!nodeViewCache.TryGetValue(connectionData.OutputNodeGuid, out BaseNodeView outputNodeView) ||
                        !nodeViewCache.TryGetValue(connectionData.InputNodeGuid, out BaseNodeView inputNodeView))
                    {
                        Debug.LogWarning($"Connection references non-existent node GUID: {connectionData.OutputNodeGuid} -> {connectionData.InputNodeGuid}. Skipping edge.");
                        continue; // Pula esta conexão se um dos nós não foi encontrado
                    }

                    // Encontra as Ports corretas nas views encontradas
                    Port outputPort = outputNodeView.GetOutputPort(connectionData.OutputPortIndex);
                    // Assume conexão com a primeira porta de entrada (índice 0)
                    // TODO: Ler InputPortIndex de ConnectionData se você o salvou
                    Port inputPort = inputNodeView.GetInputPort(0);

                    if (outputPort != null && inputPort != null)
                    {
                        // Cria a Edge visual e a adiciona
                        var edge = outputPort.ConnectTo(inputPort);
                        edge.userData = connectionData; // Armazena dados na edge (opcional)
                        AddElement(edge);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find valid ports for connection: Node '{outputNodeView.title}'[Port:{connectionData.OutputPortIndex}] -> Node '{inputNodeView.title}'[Port:0]. Edge not created.");
                    }
                }
            }

            // Re-registra os callbacks após a população estar completa
            graphViewChanged += OnGraphViewChanged;
            DialogueEditorEvents.OnNodeDataChanged += HandleNodeDataChanged;
        }

        /// <summary>
        /// Carrega um novo DialogueAsset na view (wrapper para PopulateView).
        /// </summary>
        public void PopulateView(DialogueAsset asset)
        {
            dialogueAsset = asset; // Define o novo asset
            PopulateView();       // Chama a lógica principal de população
        }

        /// <summary>
        /// Cria a instância da classe de visualização (NodeView) correta para um NodeData.
        /// Configura a posição e o listener de conexão. NÃO registra Undo.
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
                // Adicione outros tipos aqui:
                // BranchNodeData branchData => new BranchNodeView(branchData),
                _ => null // Caso para tipos não mapeados
            };

            if (nodeView != null)
            {
                // Define a posição visual baseada nos dados
                nodeView.SetPosition(new Rect(nodeData.EditorPosition, Vector2.zero));
                // Configura o listener para permitir arrastar conexões para o vazio
                SetupEdgeConnectorListener(nodeView);
            }
            else if (nodeData != null) // Só loga erro se nodeData não for null
            {
                Debug.LogError($"Could not create NodeView: No view class registered for NodeData type '{nodeData.GetType().Name}'.");
            }
            return nodeView;
        }


        // ==================== SEARCH WINDOW (COMMAND PALETTE) ====================

        /// <summary>
        /// Configura o GraphView para abrir a SearchWindow ao clicar com botão direito no fundo.
        /// </summary>
        private void SetupNodeCreationRequest()
        {
            // O callback nodeCreationRequest é chamado quando o usuário clica com botão direito
            // em uma área vazia e seleciona a opção padrão de criar nó (se habilitada),
            // ou quando o menu de contexto padrão é invocado no fundo.
            nodeCreationRequest = context =>
            {
                // Abre nossa SearchWindow customizada na posição do mouse
                OpenSearchWindow(null, context.screenMousePosition);
            };
        }

        /// <summary>
        /// Inicializa a instância do provedor da SearchWindow.
        /// </summary>
        private void InitializeSearchWindow()
        {
            searchWindowProvider = ScriptableObject.CreateInstance<NodeSearchWindow>();
            searchWindowProvider.Initialize(this, parentWindow); // Passa referências
        }

        /// <summary>
        /// Abre a janela de busca (Command Palette) na posição especificada.
        /// </summary>
        /// <param name="originPort">A porta de onde o arraste começou (null se não começou de uma porta).</param>
        /// <param name="screenPosition">A posição na tela onde abrir a janela.</param>
        public void OpenSearchWindow(Port originPort, Vector2 screenPosition)
        {
            if (searchWindowProvider == null) InitializeSearchWindow(); // Garante que foi inicializado

            // Informa à SearchWindow qual porta originou a chamada (se houver)
            searchWindowProvider.SetOriginPort(originPort);

            // Define um tamanho padrão para a janela
            SearchWindowContext context = new SearchWindowContext(screenPosition, 350, 250);

            // Abre a janela de busca
            SearchWindow.Open(context, searchWindowProvider);
        }

        // ==================== DRAG AND DROP (Criar nó ao soltar edge) ====================

        /// <summary>
        /// Adiciona um listener customizado a todas as portas de um NodeView
        /// para interceptar o evento de soltar uma conexão no vazio.
        /// </summary>
        private void SetupEdgeConnectorListener(BaseNodeView nodeView)
        {
            // Itera sobre todas as portas (entrada e saída) do nó
            nodeView.Query<Port>().ForEach(port =>
            {
                // Cria uma instância do nosso listener customizado, passando o GraphView e a porta
                var listener = new CustomEdgeConnectorListener(this, port);
                // Cria um EdgeConnector que usa nosso listener
                var connector = new EdgeConnector<Edge>(listener);
                // Adiciona o conector como um manipulador da porta
                port.AddManipulator(connector); // Usa AddManipulator
            });
        }

        /// <summary>
        /// Listener customizado que implementa IEdgeConnectorListener para
        /// abrir a SearchWindow quando uma conexão é solta fora de uma porta válida.
        /// </summary>
        private class CustomEdgeConnectorListener : IEdgeConnectorListener
        {
            private DialogueGraphView dialogueGraphView;
            private Port originPort; // Porta de onde o arraste começou

            public CustomEdgeConnectorListener(DialogueGraphView graphView, Port port)
            {
                this.dialogueGraphView = graphView;
                this.originPort = port;
            }

            // Chamado quando a edge é solta em uma área vazia do grafo
            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                // Verifica se o arraste começou de uma porta válida e se é uma porta de SAÍDA
                if (dialogueGraphView != null && originPort != null && originPort.direction == Direction.Output)
                {
                    // Obtém a posição atual do mouse na tela
                    // Usar Event.current.mousePosition pode ser mais confiável dentro do callback
                    Vector2 screenPosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

                    // Chama o método do GraphView para abrir a SearchWindow, passando a porta de origem
                    dialogueGraphView.OpenSearchWindow(originPort, screenPosition);
                }
                // Se soltar de uma porta de ENTRADA, não faz nada (geralmente não se cria nós assim)
            }

            // Chamado quando a conexão é bem-sucedida (solta em outra porta compatível)
            public void OnDrop(GraphView graphView, Edge edge)
            {
                // A lógica de salvar a conexão já está no callback OnGraphViewChanged,
                // que é disparado automaticamente após uma conexão bem-sucedida.
                // Não precisamos fazer nada aqui.
            }
        }


        // ==================== PORT COMPATIBILITY ====================

        /// <summary>
        /// Determina quais portas são compatíveis para conexão com uma porta inicial.
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            // Filtra todas as portas no grafo
            return ports.ToList().Where(endPort =>
                // Regra 1: Direção oposta (Input conecta com Output e vice-versa)
                endPort.direction != startPort.direction &&
                // Regra 2: Não conectar um nó a ele mesmo
                endPort.node != startPort.node
            // Regra 3 (Opcional): Mesmo tipo de dado (se usar tipos diferentes de bool)
            // && endPort.portType == startPort.portType
            ).ToList();
        }

        // ==================== ESTILOS ====================
        /// <summary>
        /// Carrega os arquivos USS para estilizar o GraphView e os nós.
        /// </summary>
        private void LoadStyles()
        {
            // Carrega o stylesheet principal do grafo
            var styleSheet = Resources.Load<StyleSheet>("USS/DialogueGraphStyles");
            if (styleSheet != null)
                styleSheets.Add(styleSheet);
            else
                Debug.LogWarning("Could not load StyleSheet: Resources/USS/DialogueGraphStyles.uss");

            // Carrega o stylesheet específico dos nós
            var nodeStyleSheet = Resources.Load<StyleSheet>("USS/NodeStyles");
            if (nodeStyleSheet != null)
                styleSheets.Add(nodeStyleSheet);
            else
                Debug.LogWarning("Could not load StyleSheet: Resources/USS/NodeStyles.uss");
        }
    }
} // Fim do namespace