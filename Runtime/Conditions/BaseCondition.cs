using UnityEngine;

/// <summary>
/// Classe base abstrata para condições.
/// Usa [SerializeReference] para polimorfismo no Inspector.
/// </summary>
[System.Serializable]
public abstract class BaseCondition : ICondition
{
    [SerializeField] protected string variableName;

    public string VariableName { get => variableName; set => variableName = value; }

    public abstract bool Evaluate();

    protected object GetVariableValue()
    {
        return ConversationManager.Instance?.GetVariable(variableName);
    }
}