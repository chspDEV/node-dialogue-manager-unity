using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Processa a lógica de navegação entre nós, avaliando condições.
/// </summary>
public class DialogueProcessor
{
    public BaseNodeData GetNextNode(DialogueAsset dialogue, BaseNodeData currentNode, int portIndex = 0)
    {
        if (currentNode == null || dialogue == null) return null;

        // Encontra todas as conexões que saem do nó atual pela porta especificada
        var validConnections = dialogue.Connections
            .Where(c => c.FromNodeGUID == currentNode.GUID && c.FromPortIndex == portIndex)
            .Where(c => c.AreConditionsMet())
            .ToList();

        if (validConnections.Count == 0) return null;

        // Pega a primeira conexão válida (pode ser expandido para prioridades)
        var connection = validConnections.First();

        return dialogue.GetNodeByGUID(connection.ToNodeGUID);
    }

    public List<ConnectionData> GetValidConnectionsFromNode(DialogueAsset dialogue, BaseNodeData node)
    {
        return dialogue.Connections
            .Where(c => c.FromNodeGUID == node.GUID && c.AreConditionsMet())
            .ToList();
    }
}