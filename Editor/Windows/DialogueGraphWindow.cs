using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

/// <summary>
/// Janela principal do editor de gr�fico de di�logo.
/// </summary>
public class DialogueGraphWindow : EditorWindow
{
    private DialogueAsset currentAsset;
    private DialogueGraphView graphView;
    private Label assetNameLabel;

    [MenuItem("Window/Dialogue System/Dialogue Editor")]
    public static DialogueGraphWindow OpenWindow()
    {
        var window = GetWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent("Dialogue Editor");
        window.minSize = new Vector2(800, 600);
        return window; 
    }

    public static DialogueGraphWindow OpenWindow(DialogueAsset asset)
    {
        var window = OpenWindow(); 
        window.LoadAsset(asset);
        return window; 
    }

    private void CreateGUI()
    {
        InitializeToolbar();
        InitializeGraphView();
        InitializeStyles();

        if (currentAsset != null)
        {
            graphView.PopulateView(currentAsset);
        }
    }

    private void InitializeToolbar()
    {
        var toolbar = new Toolbar();

        // Bot�o New
        var btnNew = new ToolbarButton(() => CreateNewAsset()) { text = "New" };
        toolbar.Add(btnNew);

        // Bot�o Load
        var btnLoad = new ToolbarButton(() => LoadAssetFromSelection()) { text = "Load" };
        toolbar.Add(btnLoad);

        // Bot�o Save
        var btnSave = new ToolbarButton(() => SaveAsset()) { text = "Save" };
        toolbar.Add(btnSave);

        toolbar.Add(new ToolbarSpacer());

        // Label do asset atual
        assetNameLabel = new Label("No Asset Loaded");
        assetNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        assetNameLabel.style.flexGrow = 1;
        toolbar.Add(assetNameLabel);

        rootVisualElement.Add(toolbar);
    }

    private void InitializeGraphView()
    {
        graphView = new DialogueGraphView(this);
        graphView.style.flexGrow = 1;
        rootVisualElement.Add(graphView);
    }

    private void InitializeStyles()
    {
        var styleSheet = Resources.Load<StyleSheet>("USS/DialogueGraphStyles");
        if (styleSheet != null)
        {
            rootVisualElement.styleSheets.Add(styleSheet);
        }
    }

    private void CreateNewAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Dialogue Asset",
            "NewDialogue",
            "asset",
            "Choose a location to save the dialogue asset"
        );

        if (string.IsNullOrEmpty(path)) return;

        var newAsset = CreateInstance<DialogueAsset>();

        // Cria o n� raiz automaticamente
        var rootNode = new RootNodeData();
        rootNode.EditorPosition = new Vector2(100, 200);
        newAsset.AddNode(rootNode);

        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LoadAsset(newAsset);
    }

    private void LoadAssetFromSelection()
    {
        var selected = Selection.activeObject as DialogueAsset;
        if (selected != null)
        {
            LoadAsset(selected);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Please select a Dialogue Asset in the Project window.", "OK");
        }
    }

    public void LoadAsset(DialogueAsset asset)
    {
        currentAsset = asset;
        assetNameLabel.text = asset != null ? asset.name : "No Asset Loaded";

        if (graphView != null)
        {
            graphView.PopulateView(asset);
        }
    }

    private void SaveAsset()
    {
        if (currentAsset == null)
        {
            EditorUtility.DisplayDialog("Error", "No asset loaded to save.", "OK");
            return;
        }

        EditorUtility.SetDirty(currentAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Saved {currentAsset.name}");
    }

    private void OnSelectionChange()
    {
        var selected = Selection.activeObject as DialogueAsset;
        if (selected != null && selected != currentAsset)
        {
            LoadAsset(selected);
        }
    }
}