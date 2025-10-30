using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 💎 Nó de Lógica Condicional (Branch)
/// Avalia uma lista de condições do Blackboard e desvia o fluxo
/// para a porta "True" (Índice 0) ou "False" (Índice 1).
/// </summary>
[System.Serializable]
public class BranchNodeData : BaseNodeData
{
    [Header("Bifurcação Condicional")]
    [Tooltip("Lista de condições a serem avaliadas. Todas devem ser verdadeiras para o caminho 'True' ser escolhido.")]
    [SerializeReference]
    public List<BaseCondition> conditions = new List<BaseCondition>();

    public override string GetDisplayTitle() => "💎 Branch (If)";
    public override int GetOutputPortCount() => 2; // Saída "True" e "False"
    public override int GetInputPortCount() => 1;

    /// <summary>
    /// Avalia todas as condições da lista.
    /// </summary>
    public bool EvaluateConditions()
    {
        if (conditions == null || conditions.Count == 0)
        {
            // --- ⬇️ LOG DE DEBUG ⬇️ ---
            Debug.Log($"[DEBUG] {GetDisplayTitle()}: EvaluateConditions() chamada, mas não há condições. Retornando 'True' por defeito.");
            // -------------------------
            return true;
        }

        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] {GetDisplayTitle()}: EvaluateConditions() chamada. A avaliar {conditions.Count} condição(ões)...");
        // -------------------------

        // Verifica cada condição
        for (int i = 0; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            if (condition == null)
            {
                Debug.LogWarning($"[DEBUG] {GetDisplayTitle()}: Condição no índice {i} é NULA.");
                continue; // Pula condições nulas
            }

            bool result = condition.Evaluate();

            // --- ⬇️ LOG DE DEBUG ⬇️ ---
            Debug.Log($"[DEBUG] {GetDisplayTitle()}: Condição {i} ('{condition.VariableName}') avaliada como: {result}");
            // -------------------------

            if (result == false)
            {
                // Se UMA falhar, o AND falha
                return false;
            }
        }

        // Se todas passaram (ou estavam nulas)
        return true;
    }
}