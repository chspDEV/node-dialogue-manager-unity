using UnityEngine;

/// <summary>
/// Ação que define uma variável booleana.
/// </summary>
[System.Serializable]
public class SetBoolAction : BaseAction
{
    [SerializeField] private bool value;

    public bool Value { get => value; set => this.value = value; }

    public override void Execute()
    {
        SetVariableValue(value);
    }
}