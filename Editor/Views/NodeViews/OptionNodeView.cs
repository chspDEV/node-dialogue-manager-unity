using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChspDev.DialogueSystem.Editor
{
    public class OptionNodeView : BaseNodeView
    {
        private OptionNodeData optionNodeData;

        public OptionNodeView(OptionNodeData nodeData) : base(nodeData)
        {
            this.optionNodeData = nodeData;
            title = "Option Node";

            // Listener para mudanças nos dados
            DialogueEditorEvents.OnNodeDataChanged += OnNodeDataChanged;
        }

        /// <summary>
        /// Cria o conteúdo customizado do nó.
        /// Para OptionNode, o conteúdo é gerenciado via portas de saída.
        /// </summary>
        protected override void CreateNodeContent()
        {
            // OptionNode não precisa de conteúdo extra no mainContainer.
            // As opções são exibidas como nomes das portas de saída (ver GetPortName).
        }

        /// <summary>
        /// Fornece nomes customizados para as portas de saída.
        /// </summary>
        protected override string GetPortName(Direction direction, int index)
        {
            // Para portas de entrada, usa o padrão vazio
            if (direction == Direction.Input)
                return string.Empty;

            // Para portas de saída, exibe o texto da opção
            if (optionNodeData?.options != null && index >= 0 && index < optionNodeData.options.Count)
            {
                var option = optionNodeData.options[index];
                return string.IsNullOrEmpty(option.optionText)
                    ? $"Option {index + 1}"
                    : option.optionText;
            }

            return $"Option {index + 1}";
        }

        /// <summary>
        /// Observa mudanças nos dados e atualiza a view.
        /// </summary>
        private void OnNodeDataChanged(BaseNodeData changedNodeData)
        {
            if (changedNodeData == optionNodeData)
            {
                // Força recriação de portas para refletir novas opções
                UpdateNodeView();
            }
        }

        /// <summary>
        /// Atualiza a visualização quando o número de opções muda.
        /// </summary>
        public override void UpdateNodeView()
        {
            // Recria todas as portas (base.UpdateNodeView chama CreatePorts)
            base.UpdateNodeView();
            title = "❓ Options";
        }

        ~OptionNodeView()
        {
            DialogueEditorEvents.OnNodeDataChanged -= OnNodeDataChanged;
        }
    }
}