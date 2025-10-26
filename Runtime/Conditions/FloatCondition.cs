using UnityEngine;

/// <summary>
/// Condição que compara uma variável float.
/// </summary>
[System.Serializable]
public class FloatCondition : BaseCondition
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
    [SerializeField] private float compareValue;
    [SerializeField] private float tolerance = 0.001f;

    public ComparisonType Comparison { get => comparison; set => comparison = value; }
    public float CompareValue { get => compareValue; set => compareValue = value; }
    public float Tolerance { get => tolerance; set => tolerance = value; }

    public override bool Evaluate()
    {
        var value = GetVariableValue();

        if (value == null || !(value is float floatValue))
        {
            Debug.LogWarning($"Variable '{variableName}' is not a float or doesn't exist.");
            return false;
        }

        return comparison switch
        {
            ComparisonType.Equal => Mathf.Abs(floatValue - compareValue) < tolerance,
            ComparisonType.NotEqual => Mathf.Abs(floatValue - compareValue) >= tolerance,
            ComparisonType.Greater => floatValue > compareValue,
            ComparisonType.GreaterOrEqual => floatValue >= compareValue,
            ComparisonType.Less => floatValue < compareValue,
            ComparisonType.LessOrEqual => floatValue <= compareValue,
            _ => false
        };
    }
}