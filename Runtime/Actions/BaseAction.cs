using UnityEngine;

/// <summary>
/// Classe base abstrata para ações.
/// </summary>
[System.Serializable]
public abstract class BaseAction : IAction
{
    [SerializeField] protected string variableName;

    public string VariableName { get => variableName; set => variableName = value; }

    public abstract void Execute();

    protected void SetVariableValue(object value)
    {
        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] BaseAction: SetVariableValue() chamado. Tentando definir '{variableName}' para '{value}'.");
        // -------------------------

        if (ConversationManager.Instance != null)
        {
            ConversationManager.Instance.SetVariable(variableName, value);
        }
        else
        {
            // --- ⬇️ LOG DE DEBUG ⬇️ ---
            Debug.LogError("[DEBUG] BaseAction: FALHA! ConversationManager.Instance é NULO. A variável não pode ser definida.");
            // -------------------------
        }
    }
}