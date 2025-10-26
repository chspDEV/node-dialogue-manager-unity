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
}