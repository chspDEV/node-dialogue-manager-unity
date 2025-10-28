using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

// Certifique-se de que o namespace corresponde ao seu projeto
namespace ChspDev.DialogueSystem.Editor
{
    /// <summary>
    /// Fornece a janela de busca (Command Palette) para criar nós no GraphView.
    /// </summary>
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DialogueGraphView graphView;
        private EditorWindow editorWindow;
        private Port originPort; // Porta de onde o arraste começou (pode ser null)

        /// <summary>
        /// Inicializa a janela de busca com as dependências necessárias.
        /// </summary>
        public void Initialize(DialogueGraphView graphView, EditorWindow window)
        {
            this.graphView = graphView;
            this.editorWindow = window;
            this.originPort = null; // Reseta a porta de origem por padrão
        }

        /// <summary>
        /// Define a porta de origem quando a janela é aberta arrastando de uma porta.
        /// </summary>
        public void SetOriginPort(Port port)
        {
            this.originPort = port;
        }

        /// <summary>
        /// Cria a estrutura de árvore para a janela de busca.
        /// </summary>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
    {
        // Título principal
        new SearchTreeGroupEntry(new GUIContent("Create Dialogue Node"), 0),
    };

            // ✅ NOVO: Só mostra root node se não existir um
            if (graphView?.dialogueAsset?.RootNode == null)
            {
                tree.Add(new SearchTreeEntry(new GUIContent("Root Node"))
                {
                    level = 1,
                    userData = typeof(RootNodeData)
                });

                // Separador visual (opcional)
                tree.Add(new SearchTreeGroupEntry(new GUIContent(""), 1));
            }

            // Nós disponíveis
            tree.Add(new SearchTreeEntry(new GUIContent("Speech Node"))
            {
                level = 1,
                userData = typeof(SpeechNodeData)
            });

            tree.Add(new SearchTreeEntry(new GUIContent("Option Node"))
            {
                level = 1,
                userData = typeof(OptionNodeData)
            });

            return tree;
        }

        /// <summary>
        /// Chamado quando uma entrada na árvore de busca é selecionada.
        /// </summary>
        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            // Validações básicas
            if (graphView == null || editorWindow == null || !(entry.userData is Type nodeType))
            {
                Debug.LogError("NodeSearchWindow não inicializada corretamente ou tipo de nó inválido.");
                return false;
            }

            // Calcula a posição correta no grafo
            // 1. Converte a posição da tela para a posição local da janela do editor
            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
                editorWindow.rootVisualElement.parent,
                context.screenMousePosition - editorWindow.position.position
            );
            // 2. Converte a posição da janela para a posição local dentro do container do grafo
            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(windowMousePosition);

            // Cria o nó usando os métodos do GraphView (que já lidam com Undo)
            BaseNodeView newNodeView = CreateNodeByType(nodeType, graphMousePosition);

            // Se a janela foi aberta arrastando de uma porta, tenta conectar
            if (originPort != null && newNodeView != null)
            {
                ConnectNodeToPort(newNodeView, originPort);
            }

            // Limpa a porta de origem para garantir que não seja usada na próxima abertura
            originPort = null;
            return true; // Indica que a seleção foi bem-sucedida
        }

        private BaseNodeView CreateNodeByType(Type nodeType, Vector2 position)
        {
            // ✅ NOVO: Root Node
            if (nodeType == typeof(RootNodeData))
            {
                return graphView.CreateRootNode(position);
            }
            else if (nodeType == typeof(SpeechNodeData))
            {
                return graphView.CreateSpeechNode(position);
            }
            else if (nodeType == typeof(OptionNodeData))
            {
                return graphView.CreateOptionNode(position);
            }

            Debug.LogWarning($"Criação de nó não implementada para o tipo: {nodeType.Name} em NodeSearchWindow.");
            return null;
        }

        /// <summary>
        /// Conecta o nó recém-criado à porta de origem (se aplicável).
        /// </summary>
        private void ConnectNodeToPort(BaseNodeView newNodeView, Port sourcePort)
        {
            Port targetPort = null;

            // Determina qual porta do *novo* nó usar baseado na direção da porta *original*
            if (sourcePort.direction == Direction.Output)
            {
                // Se saiu de uma porta de SAÍDA, conecta à PRIMEIRA porta de ENTRADA do novo nó
                targetPort = newNodeView.GetInputPort(0); // Usa o método seguro de BaseNodeView
            }
            else // sourcePort.direction == Direction.Input
            {
                // Se saiu de uma porta de ENTRADA, conecta à PRIMEIRA porta de SAÍDA do novo nó
                targetPort = newNodeView.GetOutputPort(0); // Usa o método seguro de BaseNodeView
            }

            // Verifica se a porta de destino foi encontrada
            if (targetPort != null)
            {
                // Cria a Edge (conexão visual). O salvamento dos dados ocorre no OnGraphViewChanged do GraphView.
                var edge = sourcePort.ConnectTo(targetPort);
                if (edge != null)
                {
                    graphView.AddElement(edge);
                }
                else
                {
                    Debug.LogError($"Falha ao criar a conexão visual entre '{sourcePort.node.title}' e '{newNodeView.title}'.");
                }
            }
            else
            {
                Debug.LogWarning($"Não foi encontrada uma porta compatível no nó '{newNodeView.title}' para conectar a partir de '{sourcePort.portName}' no nó '{sourcePort.node.title}'.");
            }
        }
    }
}