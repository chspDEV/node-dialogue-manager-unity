using UnityEngine;

/// <summary>
/// Factory para cria��o de n�s com valores padr�o apropriados.
/// CORRIGIDO: Garante que todos os valores sejam inicializados.
/// </summary>
public static class NodeFactory
{
    /// <summary>
    /// Cria um novo n� do tipo especificado com valores padr�o.
    /// </summary>
    public static T CreateNode<T>() where T : BaseNodeData, new()
    {
        var node = new T();

        // CORRE��O: Inicializa TODAS as propriedades com valores n�o-null
        if (node is SpeechNodeData speechNode)
        {
            speechNode.CharacterName = "Character";
            speechNode.DialogueText = "Enter dialogue text here...";
            speechNode.AudioSignalID = "";
            speechNode.DisplayDuration = 0f;
            // Garante que as listas estejam inicializadas
            if (speechNode.Actions == null)
            {
                // A lista � criada na classe base, mas for�a inicializa��o
                var actionsField = typeof(BaseNodeData).GetField("actions",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                actionsField?.SetValue(speechNode, new System.Collections.Generic.List<BaseAction>());
            }
        }
        else if (node is OptionNodeData optionNode)
        {
            // CORRE��O CR�TICA: Garante que a lista Options seja criada
            if (optionNode.Options == null)
            {
                var optionsField = typeof(OptionNodeData).GetField("options",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var optionsList = new System.Collections.Generic.List<OptionNodeData.Option>();

                // Adiciona duas op��es padr�o
                optionsList.Add(new OptionNodeData.Option
                {
                    optionText = "Option 1",
                    conditions = new System.Collections.Generic.List<BaseCondition>()
                });
                optionsList.Add(new OptionNodeData.Option
                {
                    optionText = "Option 2",
                    conditions = new System.Collections.Generic.List<BaseCondition>()
                });

                optionsField?.SetValue(optionNode, optionsList);
            }

            optionNode.TimeoutDuration = 0f;
            optionNode.DefaultOptionIndex = -1;
        }

        return node;
    }

    /// <summary>
    /// Clona um n� existente com novo GUID.
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