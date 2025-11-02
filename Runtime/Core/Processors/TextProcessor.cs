using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Classe utilitária estática para processar textos de diálogo.
/// Lida principalmente com a substituição de variáveis.
/// 🛡️ VERSÃO NULL-SAFE com lazy initialization
/// </summary>
/// 
namespace NodeDialogue
{
    public static class TextProcessor
    {
        /// <summary>
        /// A fonte de onde as variáveis serão buscadas.
        /// </summary>
        private static IVariableProvider variableProvider;

        /// <summary>
        /// Provider padrão que não faz nada (evita null checks)
        /// </summary>
        private static IVariableProvider nullProvider = new NullVariableProvider();

        public static ITextTagProcessor externalTagProcessor;

        /// <summary>
        /// Flag para controlar se já foi inicializado
        /// </summary>
        private static bool isInitialized = false;

        /// <summary>
        /// Flag para controlar warnings (emite apenas 1x)
        /// </summary>
        private static bool hasWarnedAboutInit = false;

        /// <summary>
        /// Regex compilado para encontrar variáveis no formato {nomeDaVariavel}.
        /// </summary>
        private static readonly Regex variableRegex = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);

        /// <summary>
        /// 🛡️ Propriedade que garante provider válido
        /// </summary>
        private static IVariableProvider Provider
        {
            get
            {
                if (variableProvider == null)
                {
                    return nullProvider;
                }
                return variableProvider;
            }
        }

        /// <summary>
        /// Inicializa o processador de texto com um provedor de variáveis.
        /// Isso DEVE ser chamado no início do jogo (ex: por um GameManager).
        /// </summary>
        public static void Initialize(IVariableProvider provider)
        {
            variableProvider = provider ?? nullProvider;
            isInitialized = true;
            hasWarnedAboutInit = false;

            Debug.Log($"[TextProcessor] Inicializado com provider: {(provider != null ? provider.GetType().Name : "NullProvider")}");
        }

        /// <summary>
        /// 🔄 Reseta o processador (útil para testes)
        /// </summary>
        public static void Reset()
        {
            variableProvider = null;
            isInitialized = false;
            hasWarnedAboutInit = false;
            Debug.Log("[TextProcessor] Reset realizado");
        }

        /// <summary>
        /// 🛡️ Processa o texto bruto, substituindo todas as ocorrências de variáveis.
        /// VERSÃO NULL-SAFE: nunca retorna null, sempre processa algo.
        /// </summary>
        public static string ProcessText(string rawText)
        {
            // 🛡️ Proteção contra null/empty
            if (string.IsNullOrEmpty(rawText))
                return string.Empty;

            // ⚠️ Warning apenas na primeira vez
            if (!isInitialized && !hasWarnedAboutInit)
            {
                Debug.LogWarning("[TextProcessor] ProcessText foi chamado antes de Initialize(). " +
                               "Usando NullProvider (variáveis não serão substituídas). " +
                               "Chame TextProcessor.Initialize(provider) no início do jogo.");
                hasWarnedAboutInit = true;
            }

            // 🔍 Processa variáveis usando o provider (null-safe)
            string processedText = variableRegex.Replace(rawText, VariableMatchEvaluator);

            // ✨ ADICIONE ESTE BLOCO NOVO:
            // 🎮 Processa tags externas (ex: botões, cores) se um processador foi registrado
            if (externalTagProcessor != null)
            {
                processedText = externalTagProcessor.ReplaceButtonTags(processedText);
            }

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

            // 🛡️ Usa Provider null-safe
            if (Provider.TryGetVariable(variableName, out string variableValue))
            {
                // Encontrou! Retorna o valor para substituição.
                return variableValue;
            }
            else
            {
                // Não encontrou a variável.
                // Se estiver usando nullProvider, não loga warning (seria spam)
                if (isInitialized && variableProvider != null)
                {
                    Debug.LogWarning($"[TextProcessor] Variável não encontrada: '{variableName}'");
                }

                // Retorna o texto original (ex: "{playerName}") para facilitar o debug.
                return match.Value;
            }
        }

        /// <summary>
        /// 🔍 Verifica se uma string contém variáveis
        /// </summary>
        public static bool ContainsVariables(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return variableRegex.IsMatch(text);
        }

        /// <summary>
        /// 📋 Retorna lista de nomes de variáveis encontradas no texto
        /// </summary>
        public static string[] GetVariableNames(string text)
        {
            if (string.IsNullOrEmpty(text)) return new string[0];

            var matches = variableRegex.Matches(text);
            var names = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                names[i] = matches[i].Groups[1].Value;
            }

            return names;
        }
    }

    /// <summary>
    /// 🛡️ Provider nulo que não retorna nenhuma variável (evita null checks)
    /// </summary>
    internal class NullVariableProvider : IVariableProvider
    {
        public bool TryGetVariable(string variableName, out string value)
        {
            value = null;
            return false;
        }

        public void SetVariable(string variableName, string value)
        {
            // Não faz nada
        }
    }
}