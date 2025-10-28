using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace ChspDev.DialogueSystem.Editor
{
    /// <summary>
    /// Gerencia atalhos de teclado globais para o DialogueGraphView.
    /// Rastreia a instância focada e executa operações com Undo integrado.
    /// </summary>
    public static class DialogueGraphViewShortcuts
    {
        // Referência estática ao último GraphView que recebeu foco
        private static DialogueGraphView s_LastFocusedGraphView;

        /// <summary>
        /// Registra um DialogueGraphView como focado (chamado quando a view recebe foco).
        /// </summary>
        internal static void RegisterGraphViewFocus(DialogueGraphView graphView)
        {
            if (graphView != null)
            {
                s_LastFocusedGraphView = graphView;
            }
        }

        /// <summary>
        /// Hotkey: Delete - Remove todos os nós selecionados
        /// </summary>
        [Shortcut("Dialogue/Delete Selection", KeyCode.Delete)]
        public static void DeleteSelection()
        {
            if (s_LastFocusedGraphView == null)
            {
                EditorUtility.DisplayDialog("Delete", "Nenhum Dialogue Graph aberto.", "OK");
                return;
            }

            DeleteSelectedNodes(s_LastFocusedGraphView);
        }

        /// <summary>
        /// Implementação interna: remove todos os nós selecionados com Undo agrupado.
        /// </summary>
        private static void DeleteSelectedNodes(DialogueGraphView graphView)
        {
            if (graphView == null || graphView.selection.Count == 0)
                return;

            // Coleta os nós selecionados (copia para evitar modificação durante iteração)
            var selectedNodeViews = new System.Collections.Generic.List<BaseNodeView>();
            foreach (var element in graphView.selection)
            {
                if (element is BaseNodeView nodeView)
                {
                    selectedNodeViews.Add(nodeView);
                }
            }

            if (selectedNodeViews.Count == 0)
                return;

            // Agrupa todas as operações de remoção sob um único Undo group
            Undo.SetCurrentGroupName($"Delete {selectedNodeViews.Count} Node(s)");
            int undoGroup = Undo.GetCurrentGroup();

            // Remove cada nó (RemoveNodeData já lida com Undo individualmente)
            foreach (var nodeView in selectedNodeViews)
            {
                // Chama método privado via Reflection (pois RemoveNodeData é private)
                // OU podemos usar uma abordagem alternativa
                RemoveNodeDataViaGraphView(graphView, nodeView);
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"Deleted {selectedNodeViews.Count} node(s)");
        }

        /// <summary>
        /// Helper que remove um nó através do GraphView (contorna o método private).
        /// </summary>
        private static void RemoveNodeDataViaGraphView(DialogueGraphView graphView, BaseNodeView nodeView)
        {
            // Opção 1: Usar GraphViewChange para disparar o callback OnGraphViewChanged
            // Opção 2: Chamar via Reflection se RemoveNodeData for private
            // Opção 3: Fazer o trabalho diretamente aqui

            // Vamos usar Reflection (mais seguro e funciona mesmo se método mudar)
            var method = graphView.GetType().GetMethod(
                "RemoveNodeData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(BaseNodeView) },
                null
            );

            if (method != null)
            {
                method.Invoke(graphView, new object[] { nodeView });
            }
            else
            {
                Debug.LogError("Could not find RemoveNodeData method on DialogueGraphView");
            }
        }
    }
}