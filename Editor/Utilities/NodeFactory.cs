using UnityEngine;

/// <summary>
/// Factory para criação de nós com valores padrão apropriados.
/// </summary>
public static class NodeFactory
{
    /// <summary>
    /// Cria um novo nó do tipo especificado com valores padrão.
    /// </summary>
    public static T CreateNode<T>() where T : BaseNodeData, new()
    {
        var node = new T();

        // Configurações específicas por tipo
        if (node is SpeechNodeData speechNode)
        {
            speechNode.CharacterName = "Character";
            speechNode.DialogueText = "Enter dialogue text here...";
        }
        else if (node is OptionNodeData optionNode)
        {
            // Adiciona duas opções padrão
            optionNode.Options.Add(new OptionNodeData.Option
            {
                optionText = "Option 1"
            });
            optionNode.Options.Add(new OptionNodeData.Option
            {
                optionText = "Option 2"
            });
        }

        return node;
    }

    /// <summary>
    /// Clona um nó existente com novo GUID.
    /// </summary>
    public static T CloneNode<T>(T original) where T : BaseNodeData, new()
    {
        var json = JsonUtility.ToJson(original);
        var clone = JsonUtility.FromJson<T>(json);

        // Gera novo GUID para o clone
        var guidField = typeof(BaseNodeData).GetField("guid",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        guidField?.SetValue(clone, System.Guid.NewGuid().ToString());

        return clone;
    }
}