using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// Inspector customizado para edição de nós individuais.
/// Exibe campos específicos para cada tipo de nó.
/// </summary>
[CustomEditor(typeof(BaseNodeData), true)]
public class NodeDataInspector : Editor
{
    private BaseNodeData nodeData;
    private SerializedProperty actionsProperty;
    private SerializedProperty optionsProperty;

    private void OnEnable()
    {
        nodeData = target as BaseNodeData;
        actionsProperty = serializedObject.FindProperty("actions"); // Verifique se é "actions" ou "Actions"

        // Cacheia a propriedade da lista de opções
        // Verifique se é "options" ou "Options" no seu OptionNodeData.cs
        optionsProperty = serializedObject.FindProperty("options");
    }

    private void NotifyViewOfChange()
    {
        // 'target' é o ScriptableObject (BaseNodeData) que está sendo inspecionado
        if (target is BaseNodeData nodeData)
        {
            DialogueEditorEvents.RequestNodeViewUpdate(nodeData);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Node Information", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("GUID", nodeData.GUID);
        EditorGUILayout.Vector2Field("Position", nodeData.EditorPosition);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        // Desenha campos específicos do tipo de nó
        if (nodeData is SpeechNodeData speechNode)
        {
            DrawSpeechNodeFields(speechNode);
        }
        else if (nodeData is OptionNodeData optionNode)
        {
            DrawOptionNodeFields(optionNode);
        }
        else if (nodeData is RootNodeData)
        {
            DrawRootNodeFields();
        }

        EditorGUILayout.Space(10);
        DrawActionsSection();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawSpeechNodeFields(SpeechNodeData speechNode)
    {
        EditorGUILayout.LabelField("Speech Node Settings", EditorStyles.boldLabel);

        SerializedProperty characterNameProp = serializedObject.FindProperty("characterName");
        SerializedProperty dialogueTextProp = serializedObject.FindProperty("dialogueText");
        SerializedProperty characterIconProp = serializedObject.FindProperty("characterIcon");
        SerializedProperty audioSignalIDProp = serializedObject.FindProperty("audioSignalID");
        SerializedProperty displayDurationProp = serializedObject.FindProperty("displayDuration");

        EditorGUILayout.PropertyField(characterNameProp, new GUIContent("Character Name"));
        EditorGUILayout.PropertyField(dialogueTextProp, new GUIContent("Dialogue Text"), GUILayout.Height(60));
        EditorGUILayout.PropertyField(characterIconProp, new GUIContent("Character Icon"));
        EditorGUILayout.PropertyField(audioSignalIDProp, new GUIContent("Audio Signal ID"));

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(displayDurationProp, new GUIContent("Auto-Advance Duration"));
        EditorGUILayout.HelpBox("Set to 0 to wait for player input.", MessageType.Info);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);

        SerializedProperty onNodeActivatedProp = serializedObject.FindProperty("onNodeActivated");
        SerializedProperty onNodeCompletedProp = serializedObject.FindProperty("onNodeCompleted");

        EditorGUILayout.PropertyField(onNodeActivatedProp, new GUIContent("On Node Activated"));
        EditorGUILayout.PropertyField(onNodeCompletedProp, new GUIContent("On Node Completed"));
    }

    private void DrawOptionNodeFields(OptionNodeData optionNode)
    {
        EditorGUILayout.LabelField("Option Node Settings", EditorStyles.boldLabel);

        SerializedProperty optionsProp = serializedObject.FindProperty("options");
        SerializedProperty timeoutProp = serializedObject.FindProperty("timeoutDuration");
        SerializedProperty defaultOptionProp = serializedObject.FindProperty("defaultOptionIndex");

        EditorGUILayout.Space(5);

        // Timeout
        EditorGUILayout.PropertyField(timeoutProp, new GUIContent("Timeout Duration"));
        if (timeoutProp.floatValue > 0)
        {
            EditorGUILayout.PropertyField(defaultOptionProp, new GUIContent("Default Option Index"));
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

        // Botão para adicionar opção
        if (GUILayout.Button("+ Add Option"))
        {
            optionsProperty.InsertArrayElementAtIndex(optionsProperty.arraySize);

            // FORÇA A ATUALIZAÇÃO DO OBJETO C# IMEDIATAMENTE
            serializedObject.ApplyModifiedProperties();

            NotifyViewOfChange(); // Agora a notificação é enviada APÓS o objeto estar atualizado
        }

        EditorGUILayout.Space(5);

        for (int i = 0; i < optionsProperty.arraySize; i++)
        {
            SerializedProperty optionProp = optionsProperty.GetArrayElementAtIndex(i);

            // Remove o 'optionNode' da chamada
            DrawOptionField(optionProp, i);
        }
    }

    private void DrawOptionField(SerializedProperty optionProp, int index)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Option {index + 1}", EditorStyles.boldLabel);

        if (GUILayout.Button("Remove", GUILayout.Width(70)))
        {
            optionsProperty.DeleteArrayElementAtIndex(index);

            // FORÇA A ATUALIZAÇÃO
            serializedObject.ApplyModifiedProperties();

            NotifyViewOfChange();
            return;
        }

        EditorGUILayout.EndHorizontal();

        SerializedProperty optionTextProp = optionProp.FindPropertyRelative("optionText");
        EditorGUILayout.PropertyField(optionTextProp, new GUIContent("Text"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);

        // --- ESTA É A CORREÇÃO ---
        // Pega a propriedade da lista de condições
        SerializedProperty conditionsProp = optionProp.FindPropertyRelative("conditions");

        // NÃO FAÇA: DrawConditionsList(optionNode.Options[index].conditions);
        // FAÇA ISSO:
        DrawConditionsList(conditionsProp);
        // --- FIM DA CORREÇÃO ---

        EditorGUILayout.Space(5);

        SerializedProperty onSelectedProp = optionProp.FindPropertyRelative("onOptionSelected");
        EditorGUILayout.PropertyField(onSelectedProp, new GUIContent("On Option Selected"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawRootNodeFields()
    {
        EditorGUILayout.HelpBox("This is the Root node. Every conversation starts here.", MessageType.Info);
    }

    private void DrawActionsSection()
    {
        EditorGUILayout.LabelField("Actions (Execute on Node Enter)", EditorStyles.boldLabel);

        if (GUILayout.Button("+ Add Action"))
        {
            // Passa a propriedade serializada para o menu
            ShowActionTypeMenu(actionsProperty);
        }

        EditorGUILayout.Space(5);

        // Itera sobre a PROPRIEDADE, não a lista C#
        for (int i = 0; i < actionsProperty.arraySize; i++)
        {
            SerializedProperty actionProp = actionsProperty.GetArrayElementAtIndex(i);
            DrawActionField(actionProp, i, actionsProperty);
        }
    }

    private void DrawActionField(SerializedProperty actionProp, int index, SerializedProperty actionsProp)
    {
        // Verifica se a referência é nula (comum com [SerializeReference])
        if (actionProp.managedReferenceValue == null)
        {
            actionsProp.DeleteArrayElementAtIndex(index);
            EditorUtility.SetDirty(target);
            return;
        }

        string typeName = actionProp.managedReferenceValue.GetType().Name;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel);

        if (GUILayout.Button("Remove", GUILayout.Width(70)))
        {
            actionsProp.DeleteArrayElementAtIndex(index);

            // FORÇA A ATUALIZAÇÃO
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
            return;
        }
        EditorGUILayout.EndHorizontal();

        // Desenha todos os campos dentro da ação automaticamente
        var iterator = actionProp.Copy();
        var endProperty = iterator.GetEndProperty();
        iterator.NextVisible(true); // Pula o campo "m_ManagedReference"

        while (!SerializedProperty.EqualContents(iterator, endProperty))
        {
            if (iterator.name == "variableName") // Exemplo de como pular um campo
            {
                // Poderia desenhar "variableName" de forma customizada aqui
            }

            EditorGUILayout.PropertyField(iterator, true);
            if (!iterator.NextVisible(false)) break;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(3);
    }

    
    private void DrawConditionsList(SerializedProperty conditionsProp)
    {
        if (GUILayout.Button("+ Add Condition", GUILayout.Width(120)))
        {
            // Passa a propriedade, não a lista C#
            ShowConditionTypeMenu(conditionsProp);
        }

        // Itera sobre a propriedade
        for (int i = 0; i < conditionsProp.arraySize; i++)
        {
            SerializedProperty conditionProp = conditionsProp.GetArrayElementAtIndex(i);
            // Passa a propriedade da condição e a propriedade da lista
            DrawConditionField(conditionProp, i, conditionsProp);
        }
    }

    private void DrawConditionField(SerializedProperty conditionProp, int index, SerializedProperty conditionsProp)
    {
        // [SerializeReference] pode deixar entradas nulas se algo der errado
        if (conditionProp.managedReferenceValue == null)
        {
            // Se for nulo, apenas remove e sai
            conditionsProp.DeleteArrayElementAtIndex(index);
            EditorUtility.SetDirty(target);
            return;
        }

        // Pega os dados de dentro da propriedade
        string typeName = conditionProp.managedReferenceValue.GetType().Name;
        SerializedProperty varNameProp = conditionProp.FindPropertyRelative("variableName");
        string varName = varNameProp != null ? varNameProp.stringValue : "[ERROR]";

        EditorGUILayout.BeginHorizontal("box");

        EditorGUILayout.LabelField($"{typeName}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"Var: {varName}");

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            // Remove da propriedade, não da lista C#
            conditionsProp.DeleteArrayElementAtIndex(index);

            // FORÇA A ATUALIZAÇÃO
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ShowActionTypeMenu(SerializedProperty actionsProp)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Set Bool"), false, () => AddAction<SetBoolAction>(actionsProp));
        menu.AddItem(new GUIContent("Set Int"), false, () => AddAction<SetIntAction>(actionsProp));
        menu.AddItem(new GUIContent("Set Float"), false, () => AddAction<SetFloatAction>(actionsProp));
        menu.AddItem(new GUIContent("Set String"), false, () => AddAction<SetStringAction>(actionsProp));
        menu.ShowAsContext();
    }

    private void ShowConditionTypeMenu(SerializedProperty conditionsProp)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Bool Condition"), false, () => AddCondition<BoolCondition>(conditionsProp));
        menu.AddItem(new GUIContent("Int Condition"), false, () => AddCondition<IntCondition>(conditionsProp));
        menu.AddItem(new GUIContent("Float Condition"), false, () => AddCondition<FloatCondition>(conditionsProp));
        menu.AddItem(new GUIContent("String Condition"), false, () => AddCondition<StringCondition>(conditionsProp));
        menu.ShowAsContext();
    }

    private void AddCondition<T>(SerializedProperty conditionsProp) where T : BaseCondition, new()
    {
        // Adiciona um novo elemento na lista de propriedades
        int newIndex = conditionsProp.arraySize;
        conditionsProp.InsertArrayElementAtIndex(newIndex);
        SerializedProperty newConditionProp = conditionsProp.GetArrayElementAtIndex(newIndex);

        serializedObject.ApplyModifiedProperties();

        // 'managedReferenceValue' é a forma correta de atribuir
        // um novo objeto C# a uma propriedade [SerializeReference]
        newConditionProp.managedReferenceValue = new T();

        EditorUtility.SetDirty(target);
    }

    private void AddAction<T>(SerializedProperty actionsProp) where T : BaseAction, new()
    {
        int newIndex = actionsProp.arraySize;
        actionsProp.InsertArrayElementAtIndex(newIndex);
        SerializedProperty newActionProp = actionsProp.GetArrayElementAtIndex(newIndex);
        newActionProp.managedReferenceValue = new T();

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

}