using UnityEngine;

/// <summary>
/// Ação que define ou modifica uma variável inteira.
/// </summary>
[System.Serializable]
public class SetIntAction : BaseAction
{
    public enum OperationType
    {
        Set,
        Add,
        Subtract,
        Multiply,
        Divide
    }

    [SerializeField] private OperationType operation = OperationType.Set;
    [SerializeField] private int value;

    public OperationType Operation { get => operation; set => operation = value; }
    public int Value { get => value; set => this.value = value; }

    public override void Execute()
    {
        if (operation == OperationType.Set)
        {
            SetVariableValue(value);
            return;
        }

        var currentValue = ConversationManager.Instance?.GetVariable<int>(variableName) ?? 0;

        var newValue = operation switch
        {
            OperationType.Add => currentValue + value,
            OperationType.Subtract => currentValue - value,
            OperationType.Multiply => currentValue * value,
            OperationType.Divide => value != 0 ? currentValue / value : currentValue,
            _ => value
        };

        SetVariableValue(newValue);
    }
}