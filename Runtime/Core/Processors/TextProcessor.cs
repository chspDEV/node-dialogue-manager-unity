using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Classe utilitária estática para processar textos de diálogo.
/// Lida principalmente com a substituição de variáveis.
/// </summary>
public static class TextProcessor
{
    /// <summary>
    /// A fonte de onde as variáveis serão buscadas.
    /// </summary>
    private static IVariableProvider variableProvider;

    /// <summary>
    /// Regex compilado para encontrar variáveis no formato {nomeDaVariavel}.
    /// </summary>
    private static readonly Regex variableRegex = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Inicializa o processador de texto com um provedor de variáveis.
    /// Isso DEVE ser chamado no início do jogo (ex: por um GameManager).
    /// </summary>
    public static void Initialize(IVariableProvider provider)
    {
        variableProvider = provider;
        Debug.Log("TextProcessor inicializado.");
    }

    /// <summary>
    /// Processa o texto bruto, substituindo todas as ocorrências de variáveis.
    /// </summary>
    public static string ProcessText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
            return string.Empty;

        // Se o processador não foi inicializado, apenas retorna o texto original.
        if (variableProvider == null)
        {
            Debug.LogWarning("TextProcessor.ProcessText foi chamado, mas o processador " +
                             "não foi inicializado com um IVariableProvider. Variáveis não serão substituídas.");
            return rawText;
        }

        // Usa Regex.Replace com um "MatchEvaluator" para processar cada {variavel} encontrada.
        string processedText = variableRegex.Replace(rawText, VariableMatchEvaluator);

        // TODO: Futuramente, você pode adicionar mais processamento aqui
        // (ex: processar tags customizadas como {shake}, {color=red}, etc.)

        return processedText;
    }

    /// <summary>
    /// Esta função é chamada para CADA variável encontrada pelo Regex.
    /// </summary>
    private static string VariableMatchEvaluator(Match match)
    {
        // O Grupo 1 da nossa Regex é o texto *dentro* das chaves.
        // Ex: para "{playerName}", match.Groups[0].Value é "{playerName}"
        //      e match.Groups[1].Value é "playerName"
        string variableName = match.Groups[1].Value;

        if (variableProvider.TryGetVariable(variableName, out string variableValue))
        {
            // Encontrou! Retorna o valor para substituição.
            return variableValue;
        }
        else
        {
            // Não encontrou a variável.
            Debug.LogWarning($"[TextProcessor] Variável não encontrada: '{variableName}'");

            // Retorna o texto original (ex: "{playerName}") para facilitar o debug.
            // Alternativamente, poderia retornar "[VAR_NOT_FOUND]" ou string.Empty.
            return match.Value;
        }
    }
}