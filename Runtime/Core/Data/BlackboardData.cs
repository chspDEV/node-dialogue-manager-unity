using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                VariableType.Bool => bool.Parse(stringValue ?? "false"),
                VariableType.Int => int.Parse(stringValue ?? "0"),
                VariableType.Float => float.Parse(stringValue ?? "0.0"), // Use padrão com ponto
                VariableType.String => stringValue ?? "",
                _ => null
            };
        }

        public void SetValue(object value)
        {
            stringValue = value?.ToString() ?? "";
            // Consistência para float (opcional, depende da cultura)
            if (type == VariableType.Float && value is float fVal)
            {
                stringValue = fVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public static string GetDefaultValue(VariableType type)
        {
            return type switch
            {
                VariableType.Bool => "false",
                VariableType.Int => "0",
                VariableType.Float => "0.0", // Consistente com GetValue/SetValue
                VariableType.String => "",
                _ => ""
            };
        }
    }

    public enum VariableType { Bool, Int, Float, String }

    // Garante a inicialização AQUI
    [SerializeField] public List<Variable> Variables = new List<Variable>();

    public void SetVariable(string name, object value)
    {
        var variable = Variables.FirstOrDefault(v => v.name == name); // Acessa a lista diretamente
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
        var variable = Variables.FirstOrDefault(v => v.name == name); // Acessa a lista diretamente
        return variable?.GetValue();
    }

    public T GetVariable<T>(string name)
    {
        var value = GetVariable(name);
        try
        {
            if (value != null)
            {
                // Tenta conversão explícita ou implícita
                return (T)System.Convert.ChangeType(value, typeof(T));
            }
        }
        catch (System.InvalidCastException)
        {
            Debug.LogWarning($"Variable '{name}' of type {value?.GetType()} cannot be converted to {typeof(T)}");
        }
        catch (System.FormatException)
        {
            Debug.LogWarning($"Variable '{name}' has invalid format for type {typeof(T)}");
        }
        return default;
    }


    public bool HasVariable(string name)
    {
        return Variables.Any(v => v.name == name); // Acessa a lista diretamente
    }
}