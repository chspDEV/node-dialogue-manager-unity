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
        ConversationManager.Instance?.SetVariable(variableName, value);
    }
}