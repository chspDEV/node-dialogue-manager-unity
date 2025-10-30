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
        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] SetBoolAction: Execute() chamado para a variável '{variableName}' com o valor '{this.value}'.");
        // -------------------------
        SetVariableValue(this.value);
    }
}