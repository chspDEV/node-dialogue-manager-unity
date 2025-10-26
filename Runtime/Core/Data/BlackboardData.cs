using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Armazena variáveis locais da conversa (blackboard).
/// Suporta bool, int, float, string.
/// </summary>
[System.Serializable]
public class BlackboardData
{
    [System.Serializable]
    public class Variable
    {
        public string name;
        public VariableType type;
        public string stringValue;

        public object GetValue()
        {
            return type switch
            {
                VariableType.Bool => bool.Parse(stringValue),
                VariableType.Int => int.Parse(stringValue),
                VariableType.Float => float.Parse(stringValue),
                VariableType.String => stringValue,
                _ => null
            };
        }

        public void SetValue(object value)
        {
            stringValue = value?.ToString() ?? "";
        }
    }

    public enum VariableType { Bool, Int, Float, String }

    [SerializeField] private List<Variable> variables = new List<Variable>();

    public List<Variable> Variables => variables;

    public void SetVariable(string name, object value)
    {
        var variable = variables.FirstOrDefault(v => v.name == name);
        if (variable != null)
        {
            variable.SetValue(value);
        }
        else
        {
            Debug.LogWarning($"Variable '{name}' not found in blackboard.");
        }
    }

    public object GetVariable(string name)
    {
        var variable = variables.FirstOrDefault(v => v.name == name);
        return variable?.GetValue();
    }

    public T GetVariable<T>(string name)
    {
        var value = GetVariable(name);
        if (value is T typedValue)
            return typedValue;

        Debug.LogWarning($"Variable '{name}' is not of type {typeof(T)}");
        return default;
    }

    public bool HasVariable(string name)
    {
        return variables.Any(v => v.name == name);
    }
}