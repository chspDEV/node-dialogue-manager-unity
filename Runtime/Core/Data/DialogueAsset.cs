using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScriptableObject principal que armazena toda a árvore de diálogo.
/// Este é o asset que será criado e editado no editor.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue System/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    [SerializeField] private string assetGUID;
    [SerializeField] private List<BaseNodeData> nodes = new List<BaseNodeData>();
    [SerializeField] private List<ConnectionData> connections = new List<ConnectionData>();
    [SerializeField] private BlackboardData blackboard = new BlackboardData();
    [SerializeField] private string localizationTableReference;

    // Metadata
    [SerializeField] private Vector2 graphViewPosition;
    [SerializeField] private Vector3 graphViewScale = Vector3.one;

    public string AssetGUID => assetGUID;
    public List<BaseNodeData> Nodes => nodes;
    public List<ConnectionData> Connections => connections;
    public BlackboardData Blackboard => blackboard;
    public string LocalizationTableReference => localizationTableReference;

    public RootNodeData RootNode => nodes.OfType<RootNodeData>().FirstOrDefault();

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(assetGUID))
        {
            assetGUID = GUID.Generate().ToString();
        }
    }

    public void AddNode(BaseNodeData node)
    {
        nodes.Add(node);
        EditorUtility.SetDirty(this);
    }

    public void RemoveNode(BaseNodeData node)
    {
        nodes.Remove(node);
        connections.RemoveAll(c => c.FromNodeGUID == node.GUID || c.ToNodeGUID == node.GUID);
        EditorUtility.SetDirty(this);
    }

    public void AddConnection(ConnectionData connection)
    {
        connections.Add(connection);
        EditorUtility.SetDirty(this);
    }

    public BaseNodeData GetNodeByGUID(string guid)
    {
        return nodes.FirstOrDefault(n => n.GUID == guid);
    }

    /// <summary>
    /// Encontra e retorna o nó raiz (RootNode) deste diálogo.
    /// </summary>
    public RootNodeData GetRootNode()
    {
        // Usa LINQ para encontrar o primeiro nó que é do tipo RootNodeData
        return nodes.OfType<RootNodeData>().FirstOrDefault();
    }

    /// <summary>
    /// Encontra o próximo nó conectado a uma porta de saída específica.
    /// </summary>
    /// <param name="fromNode">O nó de origem.</param>
    /// <param name="portIndex">O índice da porta de saída.</param>
    /// <returns>O nó de destino, ou null se não houver conexão.</returns>
    public BaseNodeData GetNextNode(BaseNodeData fromNode, int portIndex = 0)
    {
        // 1. Encontra a conexão que sai deste nó e desta porta
        var connection = connections.FirstOrDefault(c =>
            c.FromNodeGUID == fromNode.GUID &&
            c.FromPortIndex == portIndex
        );

        if (connection == null)
        {
            // Não há mais conexões, o diálogo termina
            return null;
        }

        // 2. Encontra o nó de destino usando o GUID da conexão
        return nodes.FirstOrDefault(n => n.GUID == connection.ToNodeGUID);
    }
}