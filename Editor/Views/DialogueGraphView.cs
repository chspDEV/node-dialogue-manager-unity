using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChspDev.DialogueSystem.Editor
{
    public class DialogueGraphView : GraphView
    {
        public DialogueAsset dialogueAsset;
        private EditorWindow parentWindow;

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

            graphViewChanged += OnGraphViewChanged;
            nodeCreationRequest = OnNodeCreationRequest;

            var styleSheet = Resources.Load<StyleSheet>("NodeStyles");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
        }

        // ==================== AUTO-SAVE ====================

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            bool needsSave = false;

            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var element in graphViewChange.elementsToRemove)
                {
                    if (element is BaseNodeView nodeView)
                    {
                        RemoveNodeData(nodeView);
                        needsSave = true;
                    }
                    else if (element is Edge edge)
                    {
                        RemoveConnection(edge);
                        needsSave = true;
                    }
                }
            }

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var outputNode = edge.output.node as BaseNodeView;
                    var inputNode = edge.input.node as BaseNodeView;

                    if (outputNode != null && inputNode != null)
                    {
                        SaveConnection(outputNode, inputNode, edge.output, edge.input);
                        needsSave = true;
                    }
                }
            }

            if (graphViewChange.movedElements != null && graphViewChange.movedElements.Count > 0)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                    if (element is BaseNodeView nodeView)
                    {
                        nodeView.NodeData.EditorPosition = nodeView.GetPosition().position;
                        EditorUtility.SetDirty(nodeView.NodeData);
                    }
                }
                needsSave = true;
            }

            if (needsSave)
            {
                MarkAssetDirty();
            }

            return graphViewChange;
        }

        private void MarkAssetDirty()
        {
            if (dialogueAsset != null)
            {
                EditorUtility.SetDirty(dialogueAsset);

                foreach (var nodeData in dialogueAsset.Nodes)
                {
                    if (nodeData != null)
                    {
                        EditorUtility.SetDirty(nodeData);
                    }
                }

                AssetDatabase.SaveAssets();
            }
        }

        // ==================== NODE SEARCH WINDOW ====================

        private void OnNodeCreationRequest(NodeCreationContext context)
        {
            OpenSearchWindow(null, context.screenMousePosition);
        }

        public void OpenSearchWindow(Port originPort, Vector2 screenPosition)
        {
            var searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            searchWindow.Initialize(this, parentWindow, originPort);
            SearchWindow.Open(new SearchWindowContext(screenPosition), searchWindow);
        }

        // ==================== EDGE DRAG HANDLER ====================

        public class CustomEdgeConnectorListener : IEdgeConnectorListener
        {
            private GraphView graphView;
            private DialogueGraphView dialogueGraphView;

            public CustomEdgeConnectorListener(GraphView graphView)
            {
                this.graphView = graphView;
                this.dialogueGraphView = graphView as DialogueGraphView;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                if (dialogueGraphView != null && edge.output != null)
                {
                    Vector2 screenPosition = GUIUtility.GUIToScreenPoint(position);
                    dialogueGraphView.OpenSearchWindow(edge.output, screenPosition);
                }
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                // Conexão bem-sucedida
            }
        }

        // ==================== CREATE NODE METHODS ====================

        public BaseNodeView CreateSpeechNode(Vector2 position)
        {
            var nodeData = ScriptableObject.CreateInstance<SpeechNodeData>();
            nodeData.guid = GUID.Generate().ToString();
            nodeData.EditorPosition = position;
            nodeData.CharacterName = "Character";
            nodeData.DialogueText = "Enter dialogue here...";

            AssetDatabase.AddObjectToAsset(nodeData, dialogueAsset);
            dialogueAsset.Nodes.Add(nodeData);

            var nodeView = new SpeechNodeView(nodeData);
            nodeView.SetPosition(new Rect(position, Vector2.zero));

            SetupNodePorts(nodeView);

            AddElement(nodeView);
            MarkAssetDirty();

            return nodeView;
        }

        public BaseNodeView CreateOptionNode(Vector2 position)
        {
            var nodeData = ScriptableObject.CreateInstance<OptionNodeData>();
            nodeData.guid = GUID.Generate().ToString();
            nodeData.EditorPosition = position;

            nodeData.options = new List<OptionNodeData.Option>
            {
                new OptionNodeData.Option { optionText = "Option 1" },
                new OptionNodeData.Option { optionText = "Option 2" }
            };

            AssetDatabase.AddObjectToAsset(nodeData, dialogueAsset);
            dialogueAsset.Nodes.Add(nodeData);

            var nodeView = new OptionNodeView(nodeData);
            nodeView.SetPosition(new Rect(position, Vector2.zero));

            SetupNodePorts(nodeView);

            AddElement(nodeView);
            MarkAssetDirty();

            return nodeView;
        }

        // ==================== PORT SETUP ====================

        private void SetupNodePorts(BaseNodeView nodeView)
        {
            var ports = nodeView.Query<Port>().ToList();
            foreach (var port in ports)
            {
                var edgeConnector = new EdgeConnector<Edge>(new CustomEdgeConnectorListener(this));
                port.AddManipulator(edgeConnector);
            }
        }

        // ==================== CONNECTION MANAGEMENT ====================

        public void SaveConnection(BaseNodeView outputNode, BaseNodeView inputNode, Port outputPort, Port inputPort)
        {
            int outputPortIndex = outputNode.outputContainer.IndexOf(outputPort);

            ConnectionData connection = new ConnectionData
            {
                OutputNodeGuid = outputNode.NodeData.guid,
                InputNodeGuid = inputNode.NodeData.guid,
                OutputPortIndex = outputPortIndex
            };

            dialogueAsset.Connections.RemoveAll(c =>
                c.OutputNodeGuid == connection.OutputNodeGuid &&
                c.OutputPortIndex == connection.OutputPortIndex);

            dialogueAsset.Connections.Add(connection);
            MarkAssetDirty();
        }

        private void RemoveConnection(Edge edge)
        {
            var outputNode = edge.output?.node as BaseNodeView;
            var inputNode = edge.input?.node as BaseNodeView;

            if (outputNode == null || inputNode == null) return;

            int outputPortIndex = outputNode.outputContainer.IndexOf(edge.output);

            dialogueAsset.Connections.RemoveAll(c =>
                c.OutputNodeGuid == outputNode.NodeData.guid &&
                c.InputNodeGuid == inputNode.NodeData.guid &&
                c.OutputPortIndex == outputPortIndex);
        }

        private void RemoveNodeData(BaseNodeView nodeView)
        {
            dialogueAsset.Nodes.Remove(nodeView.NodeData);

            dialogueAsset.Connections.RemoveAll(c =>
                c.OutputNodeGuid == nodeView.NodeData.guid ||
                c.InputNodeGuid == nodeView.NodeData.guid);

            AssetDatabase.RemoveObjectFromAsset(nodeView.NodeData);
            UnityEngine.Object.DestroyImmediate(nodeView.NodeData, true);
        }

        // ==================== POPULATE VIEW ====================

        /// <summary>
        /// Popula a view com os dados do DialogueAsset atual
        /// </summary>
        public void PopulateView()
        {
            if (dialogueAsset == null)
            {
                Debug.LogWarning("Cannot populate view: dialogueAsset is null");
                return;
            }

            // Limpa o grafo
            graphViewChanged -= OnGraphViewChanged;

            DeleteElements(graphElements.ToList());

            graphViewChanged += OnGraphViewChanged;

            // Cria os nós
            foreach (var nodeData in dialogueAsset.Nodes)
            {
                CreateNodeView(nodeData);
            }

            // Cria as conexões
            foreach (var connection in dialogueAsset.Connections)
            {
                var outputNode = nodes.ToList().Find(n => (n as BaseNodeView)?.NodeData.guid == connection.OutputNodeGuid) as BaseNodeView;
                var inputNode = nodes.ToList().Find(n => (n as BaseNodeView)?.NodeData.guid == connection.InputNodeGuid) as BaseNodeView;

                if (outputNode != null && inputNode != null)
                {
                    var outputPorts = outputNode.outputContainer.Query<Port>().ToList();
                    var inputPort = inputNode.inputContainer.Q<Port>();

                    if (connection.OutputPortIndex < outputPorts.Count && inputPort != null)
                    {
                        var edge = outputPorts[connection.OutputPortIndex].ConnectTo(inputPort);
                        AddElement(edge);
                    }
                }
            }
        }

        /// <summary>
        /// Popula a view com um novo DialogueAsset
        /// </summary>
        /// <param name="asset">O asset a ser carregado</param>
        public void PopulateView(DialogueAsset asset)
        {
            dialogueAsset = asset;
            PopulateView();
        }

        private void CreateNodeView(BaseNodeData nodeData)
        {
            BaseNodeView nodeView = null;

            if (nodeData is RootNodeData rootData)
            {
                nodeView = new RootNodeView(rootData);
            }
            else if (nodeData is SpeechNodeData speechData)
            {
                nodeView = new SpeechNodeView(speechData);
            }
            else if (nodeData is OptionNodeData optionData)
            {
                nodeView = new OptionNodeView(optionData);
            }

            if (nodeView != null)
            {
                nodeView.SetPosition(new Rect(nodeData.EditorPosition, Vector2.zero));
                SetupNodePorts(nodeView);
                AddElement(nodeView);
            }
        }

        // ==================== CONTEXT MENU ====================

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (evt.target is DialogueGraphView)
            {
                Vector2 screenPosition = GUIUtility.GUIToScreenPoint(evt.originalMousePosition);

                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Add Node...",
                    action => OpenSearchWindow(null, screenPosition));
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
    }
}