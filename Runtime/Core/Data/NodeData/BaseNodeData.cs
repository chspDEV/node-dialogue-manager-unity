// Arquivo: BaseNodeData.cs
using System.Collections.Generic;
using UnityEngine;
// Remova 'using UnityEngine.TextCore.Text;' (n�o est� sendo usado)

/// <summary>
/// Classe base abstrata para todos os tipos de n�s.
/// AGORA � um ScriptableObject.
/// </summary>
// [System.Serializable] N�O � mais necess�rio na classe base, 
// pois ScriptableObject j� � serializ�vel pelo Unity.
public abstract class BaseNodeData : ScriptableObject // <-- MUDAN�A CRUCIAL
{
    [SerializeField] private string guid;
    [SerializeField] private Vector2 editorPosition;
    [SerializeField] private string nodeTitle = "Untitled Node";

    // A��es executadas quando o n� � ativado
    [SerializeReference] // Mantenha isso se BaseAction N�O for um ScriptableObject
    private List<BaseAction> actions = new List<BaseAction>();

    public string GUID
    {
        get
        {
            if (string.IsNullOrEmpty(guid))
                guid = System.Guid.NewGuid().ToString();
            return guid;
        }
    }

    public Vector2 EditorPosition { get => editorPosition; set => editorPosition = value; }
    public string NodeTitle { get => nodeTitle; set => nodeTitle = value; }
    public List<BaseAction> Actions => actions;

    // O resto do seu c�digo permanece igual
    public abstract string GetDisplayTitle();
    public abstract int GetOutputPortCount();
    public abstract int GetInputPortCount();
    public virtual void OnNodeEnter() { /* ... */ }
    public virtual void OnNodeExit() { /* ... */ }
    protected void ExecuteActions() { /* ... */ }
}