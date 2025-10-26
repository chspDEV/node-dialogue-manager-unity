/// <summary>
/// Define um contrato para qualquer classe que possa fornecer
/// valores de variáveis para o sistema de diálogo.
/// </summary>
public interface IVariableProvider
{
    /// <summary>
    /// Tenta obter o valor de uma variável com base em seu nome (chave).
    /// </summary>
    /// <param name="variableName">O nome da variável (ex: "playerName").</param>
    /// <param name="value">O valor da variável (saída).</param>
    /// <returns>True se a variável foi encontrada, false caso contrário.</returns>
    bool TryGetVariable(string variableName, out string value);
}