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

    private void OnEnable()
    {
        nodeData = target as BaseNodeData;
        actionsProperty = serializedObject.FindProperty("actions");
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
            optionNode.Options.Add(new OptionNodeData.Option());
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.Space(5);

        // Desenha cada opção
        for (int i = 0; i < optionNode.Options.Count; i++)
        {
            DrawOptionField(optionNode.Options[i], i, optionNode);
        }
    }

    private void DrawOptionField(OptionNodeData.Option option, int index, OptionNodeData optionNode)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Option {index + 1}", EditorStyles.boldLabel);

        if (GUILayout.Button("Remove", GUILayout.Width(70)))
        {
            optionNode.Options.RemoveAt(index);
            EditorUtility.SetDirty(target);
            return;
        }
        EditorGUILayout.EndHorizontal();

        option.optionText = EditorGUILayout.TextField("Text", option.optionText);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);

        DrawConditionsList(option.conditions);

        EditorGUILayout.Space(5);

        SerializedProperty optionsProp = serializedObject.FindProperty("options");
        SerializedProperty optionProp = optionsProp.GetArrayElementAtIndex(index);
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
            ShowActionTypeMenu();
        }

        EditorGUILayout.Space(5);

        for (int i = 0; i < nodeData.Actions.Count; i++)
        {
            DrawActionField(nodeData.Actions[i], i);
        }
    }

    private void DrawActionField(BaseAction action, int index)
    {
        if (action == null) return;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(action.GetType().Name, EditorStyles.boldLabel);

        if (GUILayout.Button("Remove", GUILayout.Width(70)))
        {
            nodeData.Actions.RemoveAt(index);
            EditorUtility.SetDirty(target);
            return;
        }
        EditorGUILayout.EndHorizontal();

        // Desenha campos da ação usando SerializedProperty
        SerializedProperty actionProp = actionsProperty.GetArrayElementAtIndex(index);
        var iterator = actionProp.Copy();
        var endProperty = iterator.GetEndProperty();

        iterator.NextVisible(true); // Pula para o primeiro filho

        while (!SerializedProperty.EqualContents(iterator, endProperty))
        {
            EditorGUILayout.PropertyField(iterator, true);
            if (!iterator.NextVisible(false)) break;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(3);
    }

    private void DrawConditionsList(List<BaseCondition> conditions)
    {
        if (GUILayout.Button("+ Add Condition", GUILayout.Width(120)))
        {
            ShowConditionTypeMenu(conditions);
        }

        for (int i = 0; i < conditions.Count; i++)
        {
            DrawConditionField(conditions[i], i, conditions);
        }
    }

    private void DrawConditionField(BaseCondition condition, int index, List<BaseCondition> conditions)
    {
        if (condition == null) return;

        EditorGUILayout.BeginHorizontal("box");

        EditorGUILayout.LabelField($"{condition.GetType().Name}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"Var: {condition.VariableName}");

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            conditions.RemoveAt(index);
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ShowActionTypeMenu()
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Set Bool"), false, () => AddAction<SetBoolAction>());
        menu.AddItem(new GUIContent("Set Int"), false, () => AddAction<SetIntAction>());
        menu.AddItem(new GUIContent("Set Float"), false, () => AddAction<SetFloatAction>());
        menu.AddItem(new GUIContent("Set String"), false, () => AddAction<SetStringAction>());
        menu.ShowAsContext();
    }

    private void ShowConditionTypeMenu(List<BaseCondition> conditions)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Bool Condition"), false, () => AddCondition<BoolCondition>(conditions));
        menu.AddItem(new GUIContent("Int Condition"), false, () => AddCondition<IntCondition>(conditions));
        menu.AddItem(new GUIContent("Float Condition"), false, () => AddCondition<FloatCondition>(conditions));
        menu.AddItem(new GUIContent("String Condition"), false, () => AddCondition<StringCondition>(conditions));
        menu.ShowAsContext();
    }

    private void AddAction<T>() where T : BaseAction, new()
    {
        nodeData.Actions.Add(new T());
        EditorUtility.SetDirty(target);
    }

    private void AddCondition<T>(List<BaseCondition> conditions) where T : BaseCondition, new()
    {
        conditions.Add(new T());
        EditorUtility.SetDirty(target);
    }
}