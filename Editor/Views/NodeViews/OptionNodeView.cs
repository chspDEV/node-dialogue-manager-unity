using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Visualização do nó de opções (Option Node).
/// Cria uma porta de saída para cada opção.
/// </summary>
public class OptionNodeView : BaseNodeView
{
    private OptionNodeData optionData;

    public OptionNodeView(OptionNodeData data) : base(data)
    {
        optionData = data;
        AddToClassList("option-node");
    }

    protected override void CreatePorts()
    {
        // Porta de entrada
        var inputPort = CreatePort(Direction.Input, Port.Capacity.Multi, 0);
        inputPorts.Add(inputPort);
        inputContainer.Add(inputPort);

        // Uma porta de saída para cada opção
        for (int i = 0; i < optionData.Options.Count; i++)
        {
            var outputPort = CreatePort(Direction.Output, Port.Capacity.Single, i);
            outputPorts.Add(outputPort);
            outputContainer.Add(outputPort);
        }
    }

    protected override string GetPortName(Direction direction, int index)
    {
        if (direction == Direction.Input)
            return "In";

        if (index < optionData.Options.Count)
        {
            var optionText = optionData.Options[index].optionText;
            return optionText.Length > 20 ? optionText.Substring(0, 20) + "..." : optionText;
        }

        return $"Option {index + 1}";
    }

    protected override void CreateNodeContent()
    {
        var contentContainer = new VisualElement();
        contentContainer.AddToClassList("node-content-preview");

        // Mostra quantas opções existem
        var optionCountLabel = new Label($"{optionData.Options.Count} options available");
        optionCountLabel.style.fontSize = 11;
        optionCountLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        contentContainer.Add(optionCountLabel);

        // Timeout se houver
        if (optionData.TimeoutDuration > 0)
        {
            var timeoutLabel = new Label($"⏱️ Timeout: {optionData.TimeoutDuration}s");
            timeoutLabel.style.fontSize = 10;
            timeoutLabel.style.color = new Color(1f, 0.8f, 0.4f);
            contentContainer.Add(timeoutLabel);
        }

        extensionContainer.Add(contentContainer);
    }

    public override void UpdateNodeView()
    {
        base.UpdateNodeView();

        // Recria portas quando as opções mudam
        outputContainer.Clear();
        outputPorts.Clear();

        for (int i = 0; i < optionData.Options.Count; i++)
        {
            var outputPort = CreatePort(Direction.Output, Port.Capacity.Single, i);
            outputPorts.Add(outputPort);
            outputContainer.Add(outputPort);
        }

        RefreshPorts();

        // Recria o conteúdo
        extensionContainer.Clear();
        CreateNodeContent();
    }
}