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

            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "In";
            inputContainer.Add(inputPort);

            DialogueEditorEvents.OnNodeDataChanged += OnNodeDataChanged;

            UpdateOptionPorts();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void OnNodeDataChanged(BaseNodeData changedNodeData)
        {
            if (changedNodeData == optionNodeData)
            {
                UpdateOptionPorts();
            }
        }

        private void UpdateOptionPorts()
        {
            var existingPorts = outputContainer.Query<Port>().ToList();
            foreach (var port in existingPorts)
            {
                outputContainer.Remove(port);
            }

            if (optionNodeData.options != null)
            {
                for (int i = 0; i < optionNodeData.options.Count; i++)
                {
                    var option = optionNodeData.options[i];
                    var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    port.portName = string.IsNullOrEmpty(option.optionText)
                        ? $"Option {i + 1}"
                        : option.optionText;
                    port.userData = i;
                    outputContainer.Add(port);
                }
            }

            RefreshPorts();
        }

        protected override void CreateNodeContent()
        {
            // OptionNode não precisa de conteúdo extra além das portas
            // As opções são exibidas como nomes das portas de saída
        }

        public override void UpdateNodeView()
        {
            // Atualiza as portas quando os dados mudam
            UpdateOptionPorts();
        }

        ~OptionNodeView()
        {
            DialogueEditorEvents.OnNodeDataChanged -= OnNodeDataChanged;
        }
    }
}