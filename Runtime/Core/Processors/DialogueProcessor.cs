using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Processa a l�gica de navega��o entre n�s, avaliando condi��es.
/// </summary>
public class DialogueProcessor
{
    public BaseNodeData GetNextNode(DialogueAsset dialogue, BaseNodeData currentNode, int portIndex = 0)
    {
        if (currentNode == null || dialogue == null) return null;

        // Encontra todas as conex�es que saem do n� atual pela porta especificada
        var validConnections = dialogue.Connections
            .Where(c => c.FromNodeGUID == currentNode.GUID && c.FromPortIndex == portIndex)
            .Where(c => c.AreConditionsMet())
            .ToList();

        if (validConnections.Count == 0) return null;

        // Pega a primeira conex�o v�lida (pode ser expandido para prioridades)
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