// Arquivo: BaseNodeData.cs
using System.Collections.Generic;
using UnityEngine;
// Remova 'using UnityEngine.TextCore.Text;' (não está sendo usado)

/// <summary>
/// Classe base abstrata para todos os tipos de nós.
/// AGORA é um ScriptableObject.
/// </summary>
// [System.Serializable] NÃO é mais necessário na classe base, 
// pois ScriptableObject já é serializável pelo Unity.
public abstract class BaseNodeData : ScriptableObject // <-- MUDANÇA CRUCIAL
{
    [SerializeField] private string guid;
    [SerializeField] private Vector2 editorPosition;
    [SerializeField] private string nodeTitle = "Untitled Node";

    // Ações executadas quando o nó é ativado
    [SerializeReference] // Mantenha isso se BaseAction NÃO for um ScriptableObject
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

    // O resto do seu código permanece igual
    public abstract string GetDisplayTitle();
    public abstract int GetOutputPortCount();
    public abstract int GetInputPortCount();
    public virtual void OnNodeEnter() { /* ... */ }
    public virtual void OnNodeExit() { /* ... */ }
    protected void ExecuteActions() { /* ... */ }
}