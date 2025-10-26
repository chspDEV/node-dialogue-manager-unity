/// <summary>
/// Nó de entrada único. Toda conversa começa aqui.
/// </summary>

public class RootNodeData : BaseNodeData
{
    public override string GetDisplayTitle() => "▶ START";
    public override int GetOutputPortCount() => 1;
    public override int GetInputPortCount() => 0;
}