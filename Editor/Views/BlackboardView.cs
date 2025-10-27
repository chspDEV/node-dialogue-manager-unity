using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// Painel lateral para editar variáveis do Blackboard.
/// </summary>
public class BlackboardView : VisualElement
{
    private DialogueAsset currentAsset;
    private ScrollView variableList;

    public BlackboardView(DialogueAsset asset)
    {
        currentAsset = asset;

        // Estilo do painel
        style.width = 300;
        style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
        style.borderLeftWidth = 2;
        style.paddingTop = 10;
        style.paddingBottom = 10;
        style.paddingLeft = 10;
        style.paddingRight = 10;

        CreateHeader();
        CreateVariableList();
        CreateAddButton();

        RefreshVariableList();
    }

    private void CreateHeader()
    {
        var header = new Label("BLACKBOARD");
        header.style.fontSize = 16;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginBottom = 10;
        header.style.unityTextAlign = TextAnchor.MiddleCenter;
        Add(header);

        var description = new Label("Local conversation variables");
        description.style.fontSize = 11;
        description.style.color = new Color(0.7f, 0.7f, 0.7f);
        description.style.marginBottom = 15;
        description.style.unityTextAlign = TextAnchor.MiddleCenter;
        Add(description);
    }

    private void CreateVariableList()
    {
        variableList = new ScrollView();
        variableList.style.flexGrow = 1;
        Add(variableList);
    }

    private void CreateAddButton()
    {
        var addButtonContainer = new VisualElement();
        addButtonContainer.style.flexDirection = FlexDirection.Row;
        addButtonContainer.style.marginTop = 10;

        var addButton = new Button(() => ShowAddVariableMenu()) { text = "+ Add Variable" };
        addButton.style.flexGrow = 1;
        addButton.style.height = 30;
        addButtonContainer.Add(addButton);

        Add(addButtonContainer);
    }

    private void ShowAddVariableMenu()
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Bool"), false, () => AddVariable(BlackboardData.VariableType.Bool));
        menu.AddItem(new GUIContent("Int"), false, () => AddVariable(BlackboardData.VariableType.Int));
        menu.AddItem(new GUIContent("Float"), false, () => AddVariable(BlackboardData.VariableType.Float));
        menu.AddItem(new GUIContent("String"), false, () => AddVariable(BlackboardData.VariableType.String));
        menu.ShowAsContext();
    }

    private void AddVariable(BlackboardData.VariableType type)
    {
        var newVariable = new BlackboardData.Variable
        {
            name = GetUniqueVariableName("newVariable"),
            type = type,
            stringValue = GetDefaultValue(type)
        };

        currentAsset.Blackboard.Variables.Add(newVariable);
        EditorUtility.SetDirty(currentAsset);
        AssetDatabase.SaveAssets();

        RefreshVariableList();
    }

    private string GetUniqueVariableName(string baseName)
    {
        string name = baseName;
        int counter = 1;

        while (currentAsset.Blackboard.Variables.Exists(v => v.name == name))
        {
            name = $"{baseName}{counter}";
            counter++;
        }

        return name;
    }

    private string GetDefaultValue(BlackboardData.VariableType type)
    {
        return type switch
        {
            BlackboardData.VariableType.Bool => "false",
            BlackboardData.VariableType.Int => "0",
            BlackboardData.VariableType.Float => "0",
            BlackboardData.VariableType.String => "",
            _ => ""
        };
    }

    public void RefreshVariableList()
    {
        variableList.Clear();

        if (currentAsset?.Blackboard?.Variables == null)
            return;

        foreach (var variable in currentAsset.Blackboard.Variables)
        {
            var variableElement = CreateVariableElement(variable);
            variableList.Add(variableElement);
        }
    }

    private VisualElement CreateVariableElement(BlackboardData.Variable variable)
    {
        var container = new VisualElement();
        container.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        container.style.borderBottomLeftRadius = 5;
        container.style.borderBottomRightRadius = 5;
        container.style.borderTopLeftRadius = 5;
        container.style.borderTopRightRadius = 5;
        container.style.marginBottom = 8;
        container.style.paddingTop = 8;
        container.style.paddingBottom = 8;
        container.style.paddingLeft = 8;
        container.style.paddingRight = 8;

        // Header com nome e tipo
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.marginBottom = 5;

        var nameField = new TextField();
        nameField.value = variable.name;
        nameField.style.flexGrow = 1;
        nameField.RegisterValueChangedCallback(evt =>
        {
            variable.name = evt.newValue;
            EditorUtility.SetDirty(currentAsset);
        });

        var typeLabel = new Label($"({variable.type})");
        typeLabel.style.width = 60;
        typeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        typeLabel.style.color = new Color(0.6f, 0.8f, 1f);

        var deleteButton = new Button(() => DeleteVariable(variable)) { text = "X" };
        deleteButton.style.width = 25;
        deleteButton.style.height = 20;
        deleteButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f);

        header.Add(nameField);
        header.Add(typeLabel);
        header.Add(deleteButton);
        container.Add(header);

        // Campo de valor
        VisualElement valueField = CreateValueField(variable);
        container.Add(valueField);

        return container;
    }

    private VisualElement CreateValueField(BlackboardData.Variable variable)
    {
        switch (variable.type)
        {
            case BlackboardData.VariableType.Bool:
                var boolToggle = new Toggle("Value");
                boolToggle.value = bool.Parse(variable.stringValue);
                boolToggle.RegisterValueChangedCallback(evt =>
                {
                    variable.stringValue = evt.newValue.ToString();
                    EditorUtility.SetDirty(currentAsset);
                });
                return boolToggle;

            case BlackboardData.VariableType.Int:
                var intField = new IntegerField("Value");
                intField.value = int.Parse(variable.stringValue);
                intField.RegisterValueChangedCallback(evt =>
                {
                    variable.stringValue = evt.newValue.ToString();
                    EditorUtility.SetDirty(currentAsset);
                });
                return intField;

            case BlackboardData.VariableType.Float:
                var floatField = new FloatField("Value");
                floatField.value = float.Parse(variable.stringValue);
                floatField.RegisterValueChangedCallback(evt =>
                {
                    variable.stringValue = evt.newValue.ToString();
                    EditorUtility.SetDirty(currentAsset);
                });
                return floatField;

            case BlackboardData.VariableType.String:
                var stringField = new TextField("Value");
                stringField.value = variable.stringValue;
                stringField.multiline = true;
                stringField.RegisterValueChangedCallback(evt =>
                {
                    variable.stringValue = evt.newValue;
                    EditorUtility.SetDirty(currentAsset);
                });
                return stringField;

            default:
                return new Label("Unknown type");
        }
    }

    private void DeleteVariable(BlackboardData.Variable variable)
    {
        if (EditorUtility.DisplayDialog(
            "Delete Variable",
            $"Are you sure you want to delete variable '{variable.name}'?",
            "Delete",
            "Cancel"))
        {
            currentAsset.Blackboard.Variables.Remove(variable);
            EditorUtility.SetDirty(currentAsset);
            AssetDatabase.SaveAssets();
            RefreshVariableList();
        }
    }
}