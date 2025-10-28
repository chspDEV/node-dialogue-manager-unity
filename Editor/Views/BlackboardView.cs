using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq; // Necessário para LINQ

namespace ChspDev.DialogueSystem.Editor // <- Adicione seu namespace
{
    /// <summary>
    /// Painel lateral para editar variáveis do Blackboard com suporte a Undo/Redo.
    /// </summary>
    public class BlackboardView : VisualElement
    {
        private DialogueAsset currentAsset;
        private ScrollView variableList;
        private BlackboardData blackboardData; // Cache para BlackboardData

        public BlackboardView(DialogueAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("BlackboardView requires a valid DialogueAsset.");
                return;
            }

            currentAsset = asset;
            blackboardData = currentAsset.Blackboard; // Cacheia o blackboard

            // --- Estilo ---
            name = "blackboard-view"; // Para USS
            style.width = 300;
            style.minWidth = 250; // Para responsividade
            style.maxWidth = 400;
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f); // Cor base do editor Unity
            style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
            style.borderLeftWidth = 1;
            style.paddingTop = 10;
            style.paddingBottom = 10;
            style.paddingLeft = 10;
            style.paddingRight = 10;
            // --------------

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
            header.style.marginBottom = 5;
            header.style.unityTextAlign = TextAnchor.MiddleLeft;
            Add(header);

            var description = new Label("Local conversation variables");
            description.style.fontSize = 11;
            description.style.color = Color.gray;
            description.style.marginBottom = 15;
            description.style.unityTextAlign = TextAnchor.MiddleLeft;
            Add(description);
        }

        private void CreateVariableList()
        {
            variableList = new ScrollView(ScrollViewMode.Vertical);
            variableList.style.flexGrow = 1;
            Add(variableList);
        }

        private void CreateAddButton()
        {
            var addButton = new Button(ShowAddVariableMenu) { text = "+ Add Variable" };
            addButton.style.marginTop = 10;
            addButton.style.height = 30;
            Add(addButton);
        }

        private void ShowAddVariableMenu()
        {
            GenericMenu menu = new GenericMenu();
            // Usa System.Enum.GetValues para adicionar todos os tipos dinamicamente
            foreach (BlackboardData.VariableType type in System.Enum.GetValues(typeof(BlackboardData.VariableType)))
            {
                // Passa o tipo para a função AddVariable
                menu.AddItem(new GUIContent(type.ToString()), false, () => AddVariable(type));
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// Adiciona uma nova variável ao Blackboard, registrando Undo.
        /// </summary>
        private void AddVariable(BlackboardData.VariableType type)
        {
            if (blackboardData == null) return;

            // Cria a nova variável
            var newVariable = new BlackboardData.Variable
            {
                name = GetUniqueVariableName("newVariable"),
                type = type,
                stringValue = BlackboardData.Variable.GetDefaultValue(type) // Usa método estático
            };


            // Registra Undo ANTES de modificar a lista
            Undo.RecordObject(currentAsset, "Add Blackboard Variable"); // Registra o DialogueAsset

            // Adiciona à lista DENTRO do BlackboardData
            //if (blackboardData.Variables == null) blackboardData.Variables = new List<BlackboardData.Variable>();
            blackboardData.Variables.Add(newVariable);

            // Marca o DialogueAsset como sujo
            EditorUtility.SetDirty(currentAsset);

            RefreshVariableList(); // Atualiza a UI
        }


        private string GetUniqueVariableName(string baseName)
        {
            if (blackboardData?.Variables == null) return baseName;

            string name = baseName;
            int counter = 1;
            // Usa Any() do LINQ para verificar existência
            while (blackboardData.Variables.Any(v => v.name == name))
            {
                name = $"{baseName}{counter}";
                counter++;
            }
            return name;
        }

        /// <summary>
        /// Atualiza a lista visual de variáveis.
        /// </summary>
        public void RefreshVariableList()
        {
            variableList.Clear();
            if (blackboardData?.Variables == null) return;

            foreach (var variable in blackboardData.Variables)
            {
                var variableElement = CreateVariableElement(variable);
                variableList.Add(variableElement);
            }
        }

        /// <summary>
        /// Cria o elemento visual para uma única variável.
        /// </summary>
        private VisualElement CreateVariableElement(BlackboardData.Variable variable)
        {
            var container = new VisualElement();
            // Adiciona estilos USS se definidos
            container.AddToClassList("blackboard-variable-container");

            // Container Horizontal Principal
            var mainRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 5 } };

            // Campo Nome (cresce para preencher espaço)
            var nameField = new TextField { value = variable.name, style = { flexGrow = 1, marginRight = 5 } };
            nameField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(currentAsset, "Rename Variable"); // Registra o Asset
                variable.name = evt.newValue; // Modifica o dado no BlackboardData
                EditorUtility.SetDirty(currentAsset);
                // Não precisa RefreshVariableList aqui
            });
            mainRow.Add(nameField);

            // Label Tipo (largura fixa)
            var typeLabel = new Label($"({variable.type})") { style = { width = 55, unityTextAlign = TextAnchor.MiddleRight, color = Color.cyan } };
            mainRow.Add(typeLabel);

            // Botão Deletar (largura fixa)
            var deleteButton = new Button(() => DeleteVariable(variable)) { text = "X", style = { width = 25, height = 20, marginLeft = 5 } };
            deleteButton.AddToClassList("blackboard-delete-button"); // Para USS
            mainRow.Add(deleteButton);

            container.Add(mainRow);

            // Campo de Valor (ocupa linha abaixo)
            VisualElement valueField = CreateValueField(variable);
            container.Add(valueField);

            return container;
        }

        /// <summary>
        /// Cria o campo de edição apropriado para o tipo da variável.
        /// </summary>
        private VisualElement CreateValueField(BlackboardData.Variable variable)
        {
            // Usa PropertyField para integração automática com Undo e tipos complexos no futuro
            // Precisamos de um SerializedObject temporário ou encontrar a propriedade no DialogueAsset
            // Abordagem mais simples por agora: campos manuais com Undo.RecordObject

            VisualElement field = null;
            switch (variable.type)
            {
                case BlackboardData.VariableType.Bool:
                    var boolToggle = new Toggle("Value");
                    try { boolToggle.value = bool.Parse(variable.stringValue ?? "false"); } catch { boolToggle.value = false; }
                    boolToggle.RegisterValueChangedCallback(evt => {
                        Undo.RecordObject(currentAsset, "Change Variable Value");
                        variable.stringValue = evt.newValue.ToString().ToLower(); // Salva como "true" ou "false"
                        EditorUtility.SetDirty(currentAsset);
                    });
                    field = boolToggle;
                    break;
                case BlackboardData.VariableType.Int:
                    var intField = new IntegerField("Value");
                    try { intField.value = int.Parse(variable.stringValue ?? "0"); } catch { intField.value = 0; }
                    intField.RegisterValueChangedCallback(evt => {
                        Undo.RecordObject(currentAsset, "Change Variable Value");
                        variable.stringValue = evt.newValue.ToString();
                        EditorUtility.SetDirty(currentAsset);
                    });
                    field = intField;
                    break;
                case BlackboardData.VariableType.Float:
                    var floatField = new FloatField("Value");
                    try { floatField.value = float.Parse(variable.stringValue ?? "0"); } catch { floatField.value = 0f; }
                    floatField.RegisterValueChangedCallback(evt => {
                        Undo.RecordObject(currentAsset, "Change Variable Value");
                        variable.stringValue = evt.newValue.ToString(); // TODO: Considerar cultura
                        EditorUtility.SetDirty(currentAsset);
                    });
                    field = floatField;
                    break;
                case BlackboardData.VariableType.String:
                    var stringField = new TextField("Value") { multiline = false }; // Começa como linha única
                    stringField.value = variable.stringValue ?? "";
                    stringField.RegisterValueChangedCallback(evt => {
                        Undo.RecordObject(currentAsset, "Change Variable Value");
                        variable.stringValue = evt.newValue;
                        EditorUtility.SetDirty(currentAsset);
                    });
                    // Adiciona botão para multiline? Ou detecta quebra de linha? Simples por agora.
                    field = stringField;
                    break;
                default:
                    field = new Label($"Unknown type: {variable.type}");
                    break;
            }
            if (field != null) field.style.marginLeft = 10; // Indenta o campo de valor
            return field;
        }


        /// <summary>
        /// Deleta uma variável do Blackboard, registrando Undo.
        /// </summary>
        private void DeleteVariable(BlackboardData.Variable variable)
        {
            if (blackboardData == null) return;

            // Diálogo de confirmação (opcional, mas bom UX)
            // if (!EditorUtility.DisplayDialog(...)) return;

            Undo.RecordObject(currentAsset, "Delete Blackboard Variable"); // Registra o Asset
            if (blackboardData.Variables != null)
            {
                blackboardData.Variables.Remove(variable);
            }
            EditorUtility.SetDirty(currentAsset);
            RefreshVariableList(); // Atualiza a UI
        }
    }
}