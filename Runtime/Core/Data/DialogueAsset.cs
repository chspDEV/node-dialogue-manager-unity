using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScriptableObject principal que armazena toda a �rvore de di�logo.
/// Este � o asset que ser� criado e editado no editor.
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
    /// Encontra e retorna o n� raiz (RootNode) deste di�logo.
    /// </summary>
    public RootNodeData GetRootNode()
    {
        // Usa LINQ para encontrar o primeiro n� que � do tipo RootNodeData
        return nodes.OfType<RootNodeData>().FirstOrDefault();
    }

    /// <summary>
    /// Encontra o pr�ximo n� conectado a uma porta de sa�da espec�fica.
    /// </summary>
    /// <param name="fromNode">O n� de origem.</param>
    /// <param name="portIndex">O �ndice da porta de sa�da.</param>
    /// <returns>O n� de destino, ou null se n�o houver conex�o.</returns>
    public BaseNodeData GetNextNode(BaseNodeData fromNode, int portIndex = 0)
    {
        // 1. Encontra a conex�o que sai deste n� e desta porta
        var connection = connections.FirstOrDefault(c =>
            c.FromNodeGUID == fromNode.GUID &&
            c.FromPortIndex == portIndex
        );

        if (connection == null)
        {
            // N�o h� mais conex�es, o di�logo termina
            return null;
        }

        // 2. Encontra o n� de destino usando o GUID da conex�o
        return nodes.FirstOrDefault(n => n.GUID == connection.ToNodeGUID);
    }
}