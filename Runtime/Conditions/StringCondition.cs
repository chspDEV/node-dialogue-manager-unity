using System;
using UnityEngine;

/// <summary>
/// Condição que compara uma variável string.
/// </summary>
[System.Serializable]
public class StringCondition : BaseCondition
{
    public enum ComparisonType
    {
        Equal,
        NotEqual,
        Contains,
        StartsWith,
        EndsWith
    }

    [SerializeField] private ComparisonType comparison = ComparisonType.Equal;
    [SerializeField] private string compareValue;
    [SerializeField] private bool caseSensitive = false;

    public ComparisonType Comparison { get => comparison; set => comparison = value; }
    public string CompareValue { get => compareValue; set => compareValue = value; }
    public bool CaseSensitive { get => caseSensitive; set => caseSensitive = value; }

    public override bool Evaluate()
    {
        var value = GetVariableValue();

        if (value == null || !(value is string stringValue))
        {
            Debug.LogWarning($"Variable '{variableName}' is not a string or doesn't exist.");
            return false;
        }

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        return this.comparison switch
        {
            ComparisonType.Equal => string.Equals(stringValue, compareValue, comparison),
            ComparisonType.NotEqual => !string.Equals(stringValue, compareValue, comparison),
            ComparisonType.Contains => stringValue.Contains(compareValue, comparison),
            ComparisonType.StartsWith => stringValue.StartsWith(compareValue, comparison),
            ComparisonType.EndsWith => stringValue.EndsWith(compareValue, comparison),
            _ => false
        };
    }
}