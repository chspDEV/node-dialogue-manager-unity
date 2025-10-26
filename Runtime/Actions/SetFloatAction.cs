using UnityEngine;

/// <summary>
/// Ação que define ou modifica uma variável float.
/// </summary>
[System.Serializable]
public class SetFloatAction : BaseAction
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
    [SerializeField] private float value;

    public OperationType Operation { get => operation; set => operation = value; }
    public float Value { get => value; set => this.value = value; }

    public override void Execute()
    {
        if (operation == OperationType.Set)
        {
            SetVariableValue(value);
            return;
        }

        var currentValue = ConversationManager.Instance?.GetVariable<float>(variableName) ?? 0f;

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