using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector customizado para o DialogueAsset.
/// </summary>
[CustomEditor(typeof(DialogueAsset))]
public class DialogueAssetInspector : Editor
{
    private DialogueAsset asset;

    private void OnEnable()
    {
        asset = target as DialogueAsset;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space(10);

        // Botão para abrir no editor de grafo
        if (GUILayout.Button("Open in Dialogue Editor", GUILayout.Height(30)))
        {
            DialogueGraphWindow.OpenWindow(asset);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Asset Information", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("GUID", asset.AssetGUID);
        EditorGUILayout.IntField("Node Count", asset.Nodes.Count);
        EditorGUILayout.IntField("Connection Count", asset.Connections.Count);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Blackboard Variables", EditorStyles.boldLabel);

        // Exibe variáveis do blackboard
        if (asset.Blackboard.Variables.Count == 0)
        {
            EditorGUILayout.HelpBox("No variables defined. Add variables in the Dialogue Editor.", MessageType.Info);
        }
        else
        {
            foreach (var variable in asset.Blackboard.Variables)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(variable.name, GUILayout.Width(150));
                EditorGUILayout.LabelField($"({variable.type})", GUILayout.Width(80));
                EditorGUILayout.TextField(variable.stringValue);
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Localization", EditorStyles.boldLabel);

        SerializedProperty localizationProp = serializedObject.FindProperty("localizationTableReference");
        EditorGUILayout.PropertyField(localizationProp, new GUIContent("Localization Table"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);

        // Botão de teste rápido
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Test Conversation", GUILayout.Height(25)))
            {
                ConversationManager.Instance.StartConversation(asset);
            }
        }
    }
}