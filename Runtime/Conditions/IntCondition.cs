using UnityEngine;

/// <summary>
/// Condição que compara uma variável inteira.
/// </summary>
[System.Serializable]
public class IntCondition : BaseCondition
{
    public enum ComparisonType
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    [SerializeField] private ComparisonType comparison = ComparisonType.Equal;
    [SerializeField] private int compareValue;

    public ComparisonType Comparison { get => comparison; set => comparison = value; }
    public int CompareValue { get => compareValue; set => compareValue = value; }

    public override bool Evaluate()
    {
        var value = GetVariableValue();

        if (value == null || !(value is int intValue))
        {
            Debug.LogWarning($"Variable '{variableName}' is not an int or doesn't exist.");
            return false;
        }

        return comparison switch
        {
            ComparisonType.Equal => intValue == compareValue,
            ComparisonType.NotEqual => intValue != compareValue,
            ComparisonType.Greater => intValue > compareValue,
            ComparisonType.GreaterOrEqual => intValue >= compareValue,
            ComparisonType.Less => intValue < compareValue,
            ComparisonType.LessOrEqual => intValue <= compareValue,
            _ => false
        };
    }
}