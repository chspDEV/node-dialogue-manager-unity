// DialogueGraphViewShortcutProvider.cs - Novo arquivo
using ChspDev.DialogueSystem.Editor;
using UnityEditor.Experimental;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public static class DialogueGraphViewShortcuts
{
    private static DialogueGraphView s_lastFocusedGraphView;

    [Shortcut("GraphTools/Dialogue/Open Search (Cmd+Shift+D)")]
    public static void OpenNodeSearchWindow()
    {
        if (s_lastFocusedGraphView != null)
        {
            var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            s_lastFocusedGraphView.OpenSearchWindow(mousePos);
        }
    }

    // Registar a GraphView no foco
    public static void RegisterGraphView(DialogueGraphView graphView)
    {
        s_lastFocusedGraphView = graphView;
    }
}