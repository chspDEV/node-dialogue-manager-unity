using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChspDev.DialogueSystem.Editor
{
    public class SpeechNodeView : BaseNodeView
    {
        private SpeechNodeData speechNodeData;
        private Label dialoguePreview;

        public SpeechNodeView(SpeechNodeData nodeData) : base(nodeData)
        {
            this.speechNodeData = nodeData;
            title = "Speech Node";

            // Listener para mudanças nos dados
            DialogueEditorEvents.OnNodeDataChanged += OnNodeDataChanged;
        }

        /// <summary>
        /// Cria o conteúdo customizado do nó (preview do diálogo).
        /// Chamado automaticamente pela classe base.
        /// </summary>
        protected override void CreateNodeContent()
        {
            // Cria o preview do texto do diálogo
            dialoguePreview = new Label();
            dialoguePreview.AddToClassList("dialogue-preview");
            dialoguePreview.style.whiteSpace = WhiteSpace.Normal;
            dialoguePreview.style.maxWidth = 200;
            dialoguePreview.style.minHeight = 40;
            dialoguePreview.style.paddingTop = 8;
            dialoguePreview.style.paddingBottom = 8;
            dialoguePreview.style.paddingLeft = 8;
            dialoguePreview.style.paddingRight = 8;

            mainContainer.Add(dialoguePreview);

            // Atualiza o preview com dados iniciais
            UpdatePreview();
        }

        /// <summary>
        /// Atualiza a visualização quando os dados mudam.
        /// </summary>
        private void OnNodeDataChanged(BaseNodeData changedNodeData)
        {
            if (changedNodeData == speechNodeData)
            {
                UpdatePreview();
            }
        }

        /// <summary>
        /// Atualiza o título e preview do nó.
        /// </summary>
        private void UpdatePreview()
        {
            if (speechNodeData == null) return;

            // Atualiza título
            title = string.IsNullOrEmpty(speechNodeData.CharacterName)
                ? "Speech Node"
                : speechNodeData.CharacterName;

            // Atualiza preview
            if (dialoguePreview != null)
            {
                dialoguePreview.text = string.IsNullOrEmpty(speechNodeData.DialogueText)
                    ? "<i>Empty dialogue...</i>"
                    : speechNodeData.DialogueText;
            }
        }

        /// <summary>
        /// Atualiza a aparência do nó (título, portas, conteúdo).
        /// Chamado pelo GraphView após Undo/Redo.
        /// </summary>
        public override void UpdateNodeView()
        {
            base.UpdateNodeView(); // Chama a implementação base para portas
            UpdatePreview();       // Atualiza preview adicional
        }

        ~SpeechNodeView()
        {
            DialogueEditorEvents.OnNodeDataChanged -= OnNodeDataChanged;
        }
    }
}