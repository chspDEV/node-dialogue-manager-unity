using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Classe utilitária com funções auxiliares para o editor de diálogo.
/// </summary>
public static class DialogueEditorUtility
{
    /// <summary>
    /// Cria um novo DialogueAsset via menu de contexto.
    /// </summary>
    [MenuItem("Assets/Create/Dialogue System/Dialogue Asset")]
    public static void CreateDialogueAsset()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(path))
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(path), "");
        }

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/NewDialogue.asset");

        var asset = ScriptableObject.CreateInstance<DialogueAsset>();

        // Cria o nó raiz automaticamente
        var rootNode = NodeFactory.CreateNode<RootNodeData>();
        rootNode.EditorPosition = new Vector2(100, 200);
        asset.AddNode(rootNode);

        AssetDatabase.CreateAsset(asset, assetPathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        // Abre automaticamente no editor
        DialogueGraphWindow.OpenWindow(asset);
    }

    /// <summary>
    /// Valida a integridade de um DialogueAsset.
    /// </summary>
    public static bool ValidateDialogueAsset(DialogueAsset asset, out string errorMessage)
    {
        errorMessage = "";

        if (asset == null)
        {
            errorMessage = "Asset is null";
            return false;
        }

        // Verifica se existe um nó raiz
        var rootNode = asset.RootNode;
        if (rootNode == null)
        {
            errorMessage = "No Root node found. Every dialogue must have a Root node.";
            return false;
        }

        // Verifica nós órfãos (sem conexões)
        foreach (var node in asset.Nodes)
        {
            if (node is RootNodeData) continue;

            bool hasIncoming = asset.Connections.Any(c => c.ToNodeGUID == node.GUID);

            if (!hasIncoming)
            {
                errorMessage = $"Node '{node.GetDisplayTitle()}' is unreachable (no incoming connections).";
                return false;
            }
        }

        // Verifica conexões inválidas
        foreach (var connection in asset.Connections)
        {
            var fromNode = asset.GetNodeByGUID(connection.FromNodeGUID);
            var toNode = asset.GetNodeByGUID(connection.ToNodeGUID);

            if (fromNode == null || toNode == null)
            {
                errorMessage = "Found connection with invalid node reference.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Exporta o DialogueAsset para JSON.
    /// </summary>
    [MenuItem("Assets/Dialogue System/Export to JSON", true)]
    private static bool ValidateExportToJSON()
    {
        return Selection.activeObject is DialogueAsset;
    }

    [MenuItem("Assets/Dialogue System/Export to JSON")]
    public static void ExportToJSON()
    {
        var asset = Selection.activeObject as DialogueAsset;
        if (asset == null) return;

        string path = EditorUtility.SaveFilePanel("Export Dialogue to JSON", "", asset.name + ".json", "json");

        if (string.IsNullOrEmpty(path)) return;

        string json = JsonUtility.ToJson(asset, true);
        File.WriteAllText(path, json);

        Debug.Log($"Exported dialogue to: {path}");
    }

    /// <summary>
    /// Duplica um DialogueAsset com novo GUID.
    /// </summary>
    [MenuItem("Assets/Dialogue System/Duplicate Dialogue", true)]
    private static bool ValidateDuplicateDialogue()
    {
        return Selection.activeObject is DialogueAsset;
    }

    [MenuItem("Assets/Dialogue System/Duplicate Dialogue")]
    public static void DuplicateDialogue()
    {
        var asset = Selection.activeObject as DialogueAsset;
        if (asset == null) return;

        string path = AssetDatabase.GetAssetPath(asset);
        string directory = Path.GetDirectoryName(path);
        string filename = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);

        string newPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{filename}_Copy{extension}");

        AssetDatabase.CopyAsset(path, newPath);
        AssetDatabase.Refresh();

        // Carrega o novo asset e regenera o GUID
        var newAsset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(newPath);
        if (newAsset != null)
        {
            SerializedObject so = new SerializedObject(newAsset);
            SerializedProperty guidProp = so.FindProperty("assetGUID");
            guidProp.stringValue = System.Guid.NewGuid().ToString();
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(newAsset);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = newAsset;
    }

    /// <summary>
    /// Abre a janela de configurações do Dialogue System.
    /// </summary>
    [MenuItem("Window/Dialogue System/Settings")]
    public static void OpenSettings()
    {
        // Implementar janela de configurações se necessário
        EditorUtility.DisplayDialog("Settings", "Dialogue System Settings (Coming Soon)", "OK");
    }

    /// <summary>
    /// Valida todos os DialogueAssets no projeto.
    /// </summary>
    [MenuItem("Window/Dialogue System/Validate All Dialogues")]
    public static void ValidateAllDialogues()
    {
        var guids = AssetDatabase.FindAssets("t:DialogueAsset");
        int validCount = 0;
        int invalidCount = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);

            if (ValidateDialogueAsset(asset, out string error))
            {
                validCount++;
            }
            else
            {
                invalidCount++;
                Debug.LogError($"[{asset.name}] Validation failed: {error}", asset);
            }
        }

        EditorUtility.DisplayDialog(
            "Validation Complete",
            $"Valid: {validCount}\nInvalid: {invalidCount}\n\nCheck Console for details.",
            "OK"
        );
    }
}