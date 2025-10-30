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
        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] BoolCondition: Evaluate() chamado para a variável '{variableName}'. Comparação desejada: '{comparison}'.");
        // -------------------------

        var value = GetVariableValue(); // Chama BaseCondition.GetVariableValue()

        if (value == null)
        {
            // --- ⬇️ LOG DE DEBUG ⬇️ ---
            Debug.LogWarning($"[DEBUG] BoolCondition: GetVariableValue() retornou NULO para '{variableName}'. Retornando 'false'.");
            // -------------------------
            return false;
        }

        if (!(value is bool boolValue))
        {
            // --- ⬇️ LOG DE DEBUG ⬇️ ---
            Debug.LogWarning($"[DEBUG] BoolCondition: Valor da variável '{variableName}' NÃO É UM BOOL. É do tipo '{value.GetType()}' com valor '{value}'. Tentando conversão... ");
            // -------------------------

            // Tenta forçar a conversão de string para bool, já que o Blackboard armazena strings
            try
            {
                boolValue = System.Convert.ToBoolean(value.ToString());
                Debug.Log($"[DEBUG] BoolCondition: Conversão bem-sucedida. Valor é: {boolValue}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DEBUG] BoolCondition: FALHA NA CONVERSÃO. Não foi possível converter '{value}' para bool. Erro: {e.Message}. Retornando 'false'.");
                return false; // Falha na conversão
            }
        }
        else
        {
            // --- ⬇️ LOG DE DEBUG ⬇️ ---
            Debug.Log($"[DEBUG] BoolCondition: Valor obtido com sucesso. '{variableName}' é '{boolValue}' (do tipo bool).");
            // -------------------------
        }


        // Lógica de comparação
        bool finalResult = (comparison == ComparisonType.IsTrue) ? boolValue : !boolValue;

        // --- ⬇️ LOG DE DEBUG ⬇️ ---
        Debug.Log($"[DEBUG] BoolCondition: Avaliação Final: (Valor '{boolValue}' é '{comparison}') = {finalResult}.");
        // -------------------------

        return finalResult;
    }
}