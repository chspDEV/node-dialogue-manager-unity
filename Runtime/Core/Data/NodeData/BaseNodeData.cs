// Arquivo: BaseNodeData.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe base abstrata para todos os tipos de nós.
/// </summary>
public abstract class BaseNodeData : ScriptableObject
{
    [SerializeField] public string guid;
    [SerializeField] private Vector2 editorPosition;
    [SerializeField] private string nodeTitle = "Untitled Node";

    [SerializeReference]
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

    // Métodos abstratos
    public abstract string GetDisplayTitle();
    public abstract int GetOutputPortCount();
    public abstract int GetInputPortCount();


    /// <summary>
    /// Chamado pelo DialogueRunner quando este nó é ativado.
    /// Executa todas as ações definidas na lista 'actions'.
    /// </summary>
    public virtual void OnNodeEnter()
    {
        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] {GetDisplayTitle()} (GUID: {GUID}): OnNodeEnter() chamado.", this);
        // -------------------------

        // Esta linha faz com que as 'Actions' sejam realmente executadas
        ExecuteActions();
    }


    public virtual void OnNodeExit() { }

    /// <summary>
    /// Executa todas as BaseActions na lista 'actions'.
    /// </summary>
    protected void ExecuteActions()
    {
        if (actions == null || actions.Count == 0)
        {
            // --- ⬇️ LOG DE DEBUG ⬇️ ---
            Debug.Log($"[DEBUG] {GetDisplayTitle()}: ExecuteActions() chamado, mas a lista de ações está nula ou vazia.");
            // -------------------------
            return;
        }

        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] {GetDisplayTitle()}: ExecuteActions() chamado. {actions.Count} ação(ões) encontradas. A processar...");
        // -------------------------

        foreach (var action in actions)
        {
            if (action != null)
            {
                action.Execute();
            }
            else
            {
                Debug.LogWarning($"[DEBUG] {GetDisplayTitle()}: Encontrada uma Ação nula na lista.");
            }
        }
    }
}