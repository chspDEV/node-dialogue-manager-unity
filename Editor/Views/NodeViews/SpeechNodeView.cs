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

            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "In";
            inputContainer.Add(inputPort);

            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outputPort.portName = "Out";
            outputContainer.Add(outputPort);

            DialogueEditorEvents.OnNodeDataChanged += OnNodeDataChanged;

            UpdateNodeView();

            RefreshExpandedState();
            RefreshPorts();
        }

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
        }

        private void OnNodeDataChanged(BaseNodeData changedNodeData)
        {
            if (changedNodeData == speechNodeData)
            {
                UpdateNodeView();
            }
        }

        public override void UpdateNodeView()
        {
            title = string.IsNullOrEmpty(speechNodeData.CharacterName)
                ? "Speech Node"
                : speechNodeData.CharacterName;

            if (dialoguePreview != null)
            {
                dialoguePreview.text = string.IsNullOrEmpty(speechNodeData.DialogueText)
                    ? "<i>Empty dialogue...</i>"
                    : speechNodeData.DialogueText;
            }
        }

        ~SpeechNodeView()
        {
            DialogueEditorEvents.OnNodeDataChanged -= OnNodeDataChanged;
        }
    }
}