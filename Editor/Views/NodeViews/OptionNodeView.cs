using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Visualização do nó de opções (Option Node).
/// CORRIGIDO: Proteção contra valores null durante criação.
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

        // CORREÇÃO: Verifica se a lista de opções existe
        int optionCount = optionData?.Options?.Count ?? 0;

        // Uma porta de saída para cada opção
        for (int i = 0; i < optionCount; i++)
        {
            var outputPort = CreatePort(Direction.Output, Port.Capacity.Single, i);
            outputPorts.Add(outputPort);
            outputContainer.Add(outputPort);
        }
    }

    protected override string GetPortName(Direction direction, int index)
    {
        // Se for uma porta de entrada, usa o nome padrão (que agora é string.Empty, perfeito!)
        if (direction == Direction.Input)
            return base.GetPortName(direction, index); // Isso vai retornar string.Empty

        // Se for uma porta de saída, busca o texto da opção
        if (nodeData is OptionNodeData optionData && index < optionData.Options.Count)
        {
            string text = optionData.Options[index].optionText;

            if (string.IsNullOrEmpty(text))
                return $"Option {index + 1}";

            // Trunca o texto se for muito longo
            return text.Length > 20 ? text.Substring(0, 20) + "..." : text;
        }

        // Fallback (também string.Empty)
        return base.GetPortName(direction, index);
    }

    protected override void CreateNodeContent()
    {
        var contentContainer = new VisualElement();
        contentContainer.AddToClassList("node-content-preview");

        // CORREÇÃO: Verifica se a lista existe
        int optionCount = optionData?.Options?.Count ?? 0;

        // Mostra quantas opções existem
        var optionCountLabel = new Label($"{optionCount} options available");
        optionCountLabel.style.fontSize = 11;
        optionCountLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
        contentContainer.Add(optionCountLabel);

        // Timeout se houver
        float timeout = optionData?.TimeoutDuration ?? 0;
        if (timeout > 0)
        {
            var timeoutLabel = new Label($"⏱️ Timeout: {timeout}s");
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

        int optionCount = optionData?.Options?.Count ?? 0;

        for (int i = 0; i < optionCount; i++)
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