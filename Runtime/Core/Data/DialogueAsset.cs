using ChspDev.DialogueSystem.Editor;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
    using UnityEditor;
#endif
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

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(assetGUID))
        {

            assetGUID = GUID.Generate().ToString();

        }

        // ✅ NOVO: Garante que sempre há um root node
        EnsureRootNodeExists();
    }
#endif

#if UNITY_EDITOR
    /// <summary>
    /// Garante que este asset possui um nó raiz (RootNode).
    /// Se não existir, cria um automaticamente.
    /// </summary>
    private void EnsureRootNodeExists()
    {
        // Se já existe um root node, não faz nada
        if (RootNode != null)
            return;

        Debug.Log($"[DialogueAsset] Root node não encontrado em '{name}'. Criando automaticamente...");

        // Cria uma nova instância de RootNodeData
        RootNodeData rootNode = ScriptableObject.CreateInstance<RootNodeData>();
        rootNode.name = "RootNode";

        rootNode.SetGUID(GUID.Generate().ToString());

        rootNode.EditorPosition = Vector2.zero; // Posição padrão (canto superior esquerdo)

        // Adiciona como sub-asset

        AssetDatabase.AddObjectToAsset(rootNode, this);


        // Adiciona à lista de nós
        nodes.Add(rootNode);

        // Marca como sujo para salvamento
        EditorUtility.SetDirty(rootNode);
        EditorUtility.SetDirty(this);

        // Força salvamento
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DialogueAsset] Root node criado e salvo em '{name}'");

    }
#endif

#if UNITY_EDITOR
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

#endif
    public BaseNodeData GetNodeByGUID(string guid)
    {
        return nodes.FirstOrDefault(n => n.GUID == guid);
    }

    /// <summary>
    /// Encontra e retorna o nó raiz (RootNode) deste diálogo.
    /// </summary>
    public RootNodeData GetRootNode()
    {
        return nodes.OfType<RootNodeData>().FirstOrDefault();
    }

    /// <summary>
    /// Encontra o próximo nó conectado a uma porta de saída específica.
    /// </summary>
    public BaseNodeData GetNextNode(BaseNodeData fromNode, int portIndex = 0)
    {
        // ✅ Usa FromNodeGUID e FromPortIndex (conjunto antigo)
        var connection = connections.FirstOrDefault(c =>
            c.FromNodeGUID == fromNode.GUID &&
            c.FromPortIndex == portIndex
        );

        if (connection == null)
            return null;

        // ✅ Usa ToNodeGUID (conjunto antigo)
        return nodes.FirstOrDefault(n => n.GUID == connection.ToNodeGUID);
    }
}