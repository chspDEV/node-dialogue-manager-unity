using UnityEngine;

/// <summary>
/// Ação que define uma variável string.
/// </summary>
[System.Serializable]
public class SetStringAction : BaseAction
{
    [SerializeField] private string value;

    public string Value { get => value; set => this.value = value; }

    public override void Execute()
    {
        SetVariableValue(value);
    }
}