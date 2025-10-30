using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

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

            // Só mostra a opção de criar RootNode se ele não existir
            if (graphView?.dialogueAsset?.RootNode == null)
            {
                tree.Add(new SearchTreeEntry(new GUIContent("Root Node (▶ START)"))
                {
                    level = 1,
                    userData = typeof(RootNodeData)
                });
                // Adiciona um separador visual
                tree.Add(new SearchTreeEntry(new GUIContent("")) { level = 1, userData = null }); // Separador
            }

            // Nós de Diálogo
            tree.Add(new SearchTreeEntry(new GUIContent("💬 Speech Node"))
            {
                level = 1,
                userData = typeof(SpeechNodeData)
            });

            tree.Add(new SearchTreeEntry(new GUIContent("❓ Option Node"))
            {
                level = 1,
                userData = typeof(OptionNodeData)
            });

            // --- ✨ ADICIONADO BRANCH NODE ---
            tree.Add(new SearchTreeEntry(new GUIContent("💎 Branch Node (If)"))
            {
                level = 1,
                userData = typeof(BranchNodeData) // Aponta para a nova classe de dados
            });
            // ---------------------------------

            // TODO: Adicionar grupos para Lógica, Eventos, etc.
            // var tree = new List<SearchTreeEntry>
            // {
            //     new SearchTreeGroupEntry(new GUIContent("Dialogue"), 0),
            //     new SearchTreeEntry(new GUIContent("💬 Speech Node")) { level = 1, userData = typeof(SpeechNodeData) },
            //     new SearchTreeEntry(new GUIContent("❓ Option Node")) { level = 1, userData = typeof(OptionNodeData) },
            //     new SearchTreeGroupEntry(new GUIContent("Logic"), 0),
            //     new SearchTreeEntry(new GUIContent("💎 Branch Node (If)")) { level = 1, userData = typeof(BranchNodeData) },
            // };

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
                // Ignora entradas que não são tipos (como separadores ou grupos)
                if (entry.userData == null) return false;

                Debug.LogError("NodeSearchWindow não inicializada corretamente ou tipo de nó inválido.");
                return false;
            }

            // Calcula a posição correta no grafo
            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
                editorWindow.rootVisualElement.parent,
                context.screenMousePosition - editorWindow.position.position
            );
            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(windowMousePosition);

            // Cria o nó usando os métodos do GraphView (que já lidam com Undo)
            BaseNodeView newNodeView = CreateNodeByType(nodeType, graphMousePosition);

            // Se a janela foi aberta arrastando de uma porta, tenta conectar
            if (originPort != null && newNodeView != null)
            {
                ConnectNodeToPort(newNodeView, originPort);
            }

            // Limpa a porta de origem
            originPort = null;
            return true;
        }

        /// <summary>
        /// Chama o método de criação apropriado no GraphView com base no tipo de nó selecionado.
        /// </summary>
        private BaseNodeView CreateNodeByType(Type nodeType, Vector2 position)
        {
            if (nodeType == typeof(RootNodeData))
            {
                // O GraphView precisa ter o método CreateRootNode
                // (Adicioná-lo no Passo 4 se não existir)
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
            // --- ✨ ADICIONADO BRANCH NODE ---
            else if (nodeType == typeof(BranchNodeData))
            {
                // O GraphView precisa ter o método CreateBranchNode
                // (Nós o adicionaremos no Passo 4)
                return graphView.CreateBranchNode(position);
            }
            // ---------------------------------

            Debug.LogWarning($"Criação de nó não implementada para o tipo: {nodeType.Name} em NodeSearchWindow.");
            return null;
        }

        /// <summary>
        /// Conecta o nó recém-criado à porta de origem (se aplicável).
        /// </summary>
        private void ConnectNodeToPort(BaseNodeView newNodeView, Port sourcePort)
        {
            Port targetPort = null;

            if (sourcePort.direction == Direction.Output)
            {
                // Conecta à PRIMEIRA porta de ENTRADA do novo nó
                targetPort = newNodeView.GetInputPort(0);
            }
            else // sourcePort.direction == Direction.Input
            {
                // Conecta à PRIMEIRA porta de SAÍDA do novo nó
                targetPort = newNodeView.GetOutputPort(0);
            }

            if (targetPort != null)
            {
                // Cria a Edge (conexão visual). O salvamento ocorre no OnGraphViewChanged.
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