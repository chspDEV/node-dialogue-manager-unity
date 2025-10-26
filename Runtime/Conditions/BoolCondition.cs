using UnityEngine;

/// <summary>
/// Condição que compara uma variável booleana.
/// </summary>
[System.Serializable]
public class BoolCondition : BaseCondition
{
    public enum ComparisonType
    {
        IsTrue,
        IsFalse
    }

    [SerializeField] private ComparisonType comparison = ComparisonType.IsTrue;

    public ComparisonType Comparison { get => comparison; set => comparison = value; }

    public override bool Evaluate()
    {
        var value = GetVariableValue();

        if (value == null || !(value is bool boolValue))
        {
            Debug.LogWarning($"Variable '{variableName}' is not a bool or doesn't exist.");
            return false;
        }

        return comparison == ComparisonType.IsTrue ? boolValue : !boolValue;
    }
}