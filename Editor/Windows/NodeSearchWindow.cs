using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChspDev.DialogueSystem.Editor
{
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DialogueGraphView graphView;
        private EditorWindow editorWindow;
        private Port originPort;

        public void Initialize(DialogueGraphView graphView, EditorWindow window, Port port = null)
        {
            this.graphView = graphView;
            this.editorWindow = window;
            this.originPort = port;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Dialogue Node"), 0),
                
                // Apenas os nós que existem no projeto
                new SearchTreeEntry(new GUIContent("Speech Node"))
                {
                    level = 1,
                    userData = typeof(SpeechNodeData)
                },
                new SearchTreeEntry(new GUIContent("Option Node"))
                {
                    level = 1,
                    userData = typeof(OptionNodeData)
                },
            };

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            // Converte posição do mouse para coordenadas do GraphView
            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
                editorWindow.rootVisualElement.parent,
                context.screenMousePosition - editorWindow.position.position
            );

            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(windowMousePosition);

            // Cria o nó baseado no tipo selecionado
            Type nodeType = entry.userData as Type;
            BaseNodeView newNodeView = CreateNodeByType(nodeType, graphMousePosition);

            // Se foi arrastado de uma porta, conecta automaticamente
            if (originPort != null && newNodeView != null)
            {
                ConnectNodeToPort(newNodeView, originPort);
            }

            return true;
        }

        private BaseNodeView CreateNodeByType(Type nodeType, Vector2 position)
        {
            if (nodeType == typeof(SpeechNodeData))
            {
                return graphView.CreateSpeechNode(position);
            }
            else if (nodeType == typeof(OptionNodeData))
            {
                return graphView.CreateOptionNode(position);
            }

            Debug.LogWarning($"Unknown node type: {nodeType}");
            return null;
        }

        private void ConnectNodeToPort(BaseNodeView nodeView, Port originPort)
        {
            Port targetPort = null;

            // Determina qual porta usar baseado na direção da porta de origem
            if (originPort.direction == Direction.Output)
            {
                // Conecta à porta de entrada do novo nó
                targetPort = nodeView.inputContainer.Q<Port>();
            }
            else // Direction.Input
            {
                // Conecta à primeira porta de saída do novo nó
                targetPort = nodeView.outputContainer.Q<Port>();
            }

            if (targetPort != null)
            {
                // Cria a edge visual
                var edge = originPort.ConnectTo(targetPort);
                graphView.AddElement(edge);

                // Salva a conexão nos dados
                SaveConnection(originPort, targetPort);
            }
        }

        private void SaveConnection(Port outputPort, Port inputPort)
        {
            var outputNode = outputPort.node as BaseNodeView;
            var inputNode = inputPort.node as BaseNodeView;

            if (outputNode == null || inputNode == null)
            {
                return;
            }

            // Usa o método do GraphView para salvar a conexão
            graphView.SaveConnection(outputNode, inputNode, outputPort, inputPort);
        }
    }
}