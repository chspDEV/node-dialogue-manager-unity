using System;

namespace ChspDev.DialogueSystem.Editor
{
    public static class DialogueEditorEvents
    {
        public static event Action<BaseNodeData> OnNodeDataChanged;

        public static void TriggerNodeDataChanged(BaseNodeData nodeData)
        {
            OnNodeDataChanged?.Invoke(nodeData);
        }
    }
}