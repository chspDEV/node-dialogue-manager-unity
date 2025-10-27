using UnityEditor;

namespace ChspDev.DialogueSystem.Editor
{
    [CustomEditor(typeof(BaseNodeData), true)]
    public class NodeDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawDefaultInspector();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                BaseNodeData nodeData = target as BaseNodeData;
                if (nodeData != null)
                {
                    DialogueEditorEvents.TriggerNodeDataChanged(nodeData);
                    EditorUtility.SetDirty(nodeData);
                }
            }
        }
    }
}