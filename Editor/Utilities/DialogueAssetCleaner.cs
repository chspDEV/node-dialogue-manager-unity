using UnityEditor;
using UnityEngine;
using System.Linq;

namespace ChspDev.DialogueSystem.Editor
{
    public static class DialogueAssetCleaner
    {
        [MenuItem("Assets/Dialogue/Clean Corrupted Connections", false, 2000)]
        private static void CleanCorruptedConnections()
        {
            var selectedAssets = Selection.GetFiltered<DialogueAsset>(SelectionMode.Assets);

            if (selectedAssets.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Dialogue Asset Selected",
                    "Please select one or more DialogueAsset files in the Project window.",
                    "OK"
                );
                return;
            }

            int totalCleaned = 0;

            foreach (var asset in selectedAssets)
            {
                int cleaned = CleanAsset(asset);
                totalCleaned += cleaned;
            }

            EditorUtility.DisplayDialog(
                "Cleanup Complete",
                $"Removed {totalCleaned} corrupted connection(s) from {selectedAssets.Length} asset(s).",
                "OK"
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Dialogue/Clean Corrupted Connections", true)]
        private static bool CleanCorruptedConnectionsValidation()
        {
            return Selection.GetFiltered<DialogueAsset>(SelectionMode.Assets).Length > 0;
        }

        private static int CleanAsset(DialogueAsset asset)
        {
            if (asset == null) return 0;

            Undo.RecordObject(asset, "Clean Corrupted Connections");

            var validNodeGuids = asset.Nodes
                .Where(n => n != null && !string.IsNullOrEmpty(n.GUID))
                .Select(n => n.GUID)
                .ToHashSet();

            var corruptedConnections = asset.Connections
                .Where(c => c == null ||
                           string.IsNullOrEmpty(c.FromNodeGUID) ||
                           string.IsNullOrEmpty(c.ToNodeGUID) ||
                           !validNodeGuids.Contains(c.FromNodeGUID) ||
                           !validNodeGuids.Contains(c.ToNodeGUID))
                .ToList();

            int count = corruptedConnections.Count;

            if (count > 0)
            {
                Debug.Log($"[DialogueCleaner] Removing {count} corrupted connection(s) from '{asset.name}'");

                foreach (var connection in corruptedConnections)
                {
                    asset.Connections.Remove(connection);

                    
                }

                EditorUtility.SetDirty(asset);
            }

            return count;
        }

        /// <summary>
        /// Limpa automaticamente ao carregar o asset (opcional)
        /// </summary>
        public static void AutoCleanOnLoad(DialogueAsset asset)
        {
            int cleaned = CleanAsset(asset);

            if (cleaned > 0)
            {
                Debug.LogWarning($"[DialogueCleaner] Auto-cleaned {cleaned} corrupted connections from '{asset.name}'");
            }
        }
    }
}