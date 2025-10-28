using UnityEditor;
using UnityEngine;

namespace ChspDev.DialogueSystem.Editor
{
    /// <summary>
    /// Inspector customizado para edição de nós individuais.
    /// Exibe APENAS campos relevantes para o usuário final, ocultando dados internos.
    /// </summary>
    [CustomEditor(typeof(BaseNodeData), true)]
    public class NodeDataInspector : UnityEditor.Editor
    {
        private BaseNodeData nodeData;
        private SerializedProperty actionsProperty;
        private SerializedProperty optionsProperty; // Cache para OptionNode

        private void OnEnable()
        {
            nodeData = target as BaseNodeData;
            // IMPORTANTE: Verifique se os nomes "actions" e "options" estão corretos (case-sensitive)
            actionsProperty = serializedObject.FindProperty("actions");
            optionsProperty = serializedObject.FindProperty("options"); // Será null se não for OptionNodeData
        }

        // Mantém a notificação para sincronizar com o GraphView
        private void NotifyViewOfChange()
        {
            if (target is BaseNodeData data)
            {
                DialogueEditorEvents.TriggerNodeDataChanged(data);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Registra Undo para modificações
            Undo.RecordObject(target, "Modify Node Data");

            EditorGUI.BeginChangeCheck(); // Detecta mudanças

            // --- NÃO DESENHA MAIS OS CAMPOS PADRÃO DIRETAMENTE ---
            // DrawDefaultInspector(); // REMOVIDO!

            // --- Desenha campos específicos baseados no tipo de nó ---
            EditorGUILayout.Space(10); // Espaço inicial

            if (nodeData is SpeechNodeData speechNode)
            {
                DrawSpeechNodeFields(); // Desenha campos específicos do SpeechNode
            }
            else if (nodeData is OptionNodeData optionNode)
            {
                DrawOptionNodeFields(); // Desenha campos específicos do OptionNode (inclui a lista customizada)
            }
            else if (nodeData is RootNodeData)
            {
                // RootNode geralmente não tem campos editáveis
                EditorGUILayout.HelpBox("▶️ This is the starting node of the dialogue.", MessageType.Info);
            }
            // Adicione 'else if' para outros tipos de nós aqui

            // --- Desenha a seção de Ações (comum a todos os nós) ---
            DrawActionsSection();

            // Aplica mudanças detectadas e lida com Undo/Notificação
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                NotifyViewOfChange(); // Notifica após aplicar
                EditorUtility.SetDirty(target); // Garante que o asset seja salvo
            }

            // Garante aplicação final (importante para Add/Remove)
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Desenha os campos editáveis específicos para SpeechNodeData.
        /// </summary>
        private void DrawSpeechNodeFields()
        {
            EditorGUILayout.LabelField("💬 Speech Node Settings", EditorStyles.boldLabel);

            // Encontra as propriedades relevantes (nomes devem bater com BaseNodeData.cs/SpeechNodeData.cs)
            SerializedProperty characterNameProp = serializedObject.FindProperty("characterName");
            SerializedProperty dialogueTextProp = serializedObject.FindProperty("dialogueText");
            SerializedProperty characterIconProp = serializedObject.FindProperty("characterIcon");
            SerializedProperty audioSignalIDProp = serializedObject.FindProperty("audioSignalID");
            SerializedProperty displayDurationProp = serializedObject.FindProperty("displayDuration");
            SerializedProperty onNodeActivatedProp = serializedObject.FindProperty("onNodeActivated");
            SerializedProperty onNodeCompletedProp = serializedObject.FindProperty("onNodeCompleted");

            // Desenha usando PropertyField para ter labels e edição padrão
            EditorGUILayout.PropertyField(characterNameProp);
            EditorGUILayout.PropertyField(dialogueTextProp, GUILayout.MinHeight(60)); // Texto maior
            EditorGUILayout.PropertyField(characterIconProp);
            EditorGUILayout.PropertyField(audioSignalIDProp);

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(displayDurationProp, new GUIContent("Auto-Advance Duration (Sec)"));
            if (displayDurationProp.floatValue <= 0) // Mostra ajuda apenas se relevante
            {
                EditorGUILayout.HelpBox("Set to 0 to wait for player input.", MessageType.None);
            }


            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("✨ Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onNodeActivatedProp);
            EditorGUILayout.PropertyField(onNodeCompletedProp);
        }

        /// <summary>
        /// Desenha os campos editáveis específicos para OptionNodeData.
        /// </summary>
        private void DrawOptionNodeFields()
        {
            if (optionsProperty == null) return;

            EditorGUILayout.LabelField("❓ Option Node Settings", EditorStyles.boldLabel);

            SerializedProperty timeoutProp = serializedObject.FindProperty("timeoutDuration");
            SerializedProperty defaultOptionProp = serializedObject.FindProperty("defaultOptionIndex");

            EditorGUILayout.PropertyField(timeoutProp, new GUIContent("Timeout Duration (Sec)"));
            if (timeoutProp.floatValue > 0)
            {
                // Mostra o índice padrão apenas se o timeout estiver ativo
                EditorGUILayout.PropertyField(defaultOptionProp, new GUIContent("Default Option Index (-1 = None)"));
                // Validação visual (opcional):
                if (defaultOptionProp.intValue >= optionsProperty.arraySize)
                {
                    EditorGUILayout.HelpBox("Default index is out of range!", MessageType.Error);
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("✨ Options", EditorStyles.boldLabel);

            // Botão Adicionar
            if (GUILayout.Button("+ Add Option"))
            {
                Undo.RecordObject(target, "Add Option");
                optionsProperty.InsertArrayElementAtIndex(optionsProperty.arraySize);
                serializedObject.ApplyModifiedProperties();
                NotifyViewOfChange();
            }

            EditorGUILayout.Space(5);

            // Desenha cada opção
            for (int i = 0; i < optionsProperty.arraySize; i++)
            {
                SerializedProperty optionProp = optionsProperty.GetArrayElementAtIndex(i);
                DrawOptionField(optionProp, i); // Chama a lógica de desenho da opção
            }
        }

        /// <summary>
        /// Desenha uma única opção dentro da lista do OptionNode.
        /// </summary>
        private void DrawOptionField(SerializedProperty optionProp, int index)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            // Mostra o texto da opção no header para identificação rápida
            SerializedProperty optionTextHeaderProp = optionProp.FindPropertyRelative("optionText");
            string headerText = string.IsNullOrEmpty(optionTextHeaderProp.stringValue) ? $"Option {index + 1}" : optionTextHeaderProp.stringValue;
            // Trunca se muito longo
            if (headerText.Length > 30) headerText = headerText.Substring(0, 30) + "...";
            EditorGUILayout.LabelField(headerText, EditorStyles.boldLabel);


            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                Undo.RecordObject(target, "Remove Option");
                if (optionsProperty.arraySize > index)
                {
                    optionsProperty.DeleteArrayElementAtIndex(index);
                }
                serializedObject.ApplyModifiedProperties();
                NotifyViewOfChange();
                return;
            }
            EditorGUILayout.EndHorizontal();

            // Campos da Opção
            SerializedProperty optionTextProp = optionProp.FindPropertyRelative("optionText");
            SerializedProperty conditionsProp = optionProp.FindPropertyRelative("conditions");
            SerializedProperty onSelectedProp = optionProp.FindPropertyRelative("onOptionSelected");

            EditorGUILayout.PropertyField(optionTextProp, new GUIContent("Text")); // Desenha o campo de texto

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🔐 Conditions", EditorStyles.boldLabel);
            DrawConditionsList(conditionsProp); // Desenha a lista de condições

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("▶️ Events on Selected", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onSelectedProp); // Desenha o UnityEvent

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // --- Seção Condições (Usada por Opções) ---

        private void DrawConditionsList(SerializedProperty conditionsProp)
        {
            // Botão Adicionar
            if (GUILayout.Button("+ Add Condition", GUILayout.Width(120)))
            {
                ShowConditionTypeMenu(conditionsProp); // Passa a propriedade
            }
            EditorGUILayout.Space(2);

            // Desenha cada condição
            if (conditionsProp.arraySize == 0)
            {
                EditorGUILayout.LabelField("   (No conditions - always available)", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < conditionsProp.arraySize; i++)
                {
                    SerializedProperty conditionProp = conditionsProp.GetArrayElementAtIndex(i);
                    DrawConditionField(conditionProp, i, conditionsProp); // Chama a lógica de desenho
                }
            }
        }

        private void DrawConditionField(SerializedProperty conditionProp, int index, SerializedProperty conditionsProp)
        {
            if (conditionProp.managedReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Condition is null/invalid.", MessageType.Warning);
                if (GUILayout.Button("Remove Invalid", GUILayout.Width(100))) { /* Código Undo/Remove */ }
                return;
            }

            string typeName = conditionProp.managedReferenceValue.GetType().Name.Replace("Condition", ""); // Nome mais curto
            SerializedProperty varNameProp = conditionProp.FindPropertyRelative("variableName");
            //string varName = varNameProp != null ? varNameProp.stringValue : "[ERROR]";

            EditorGUILayout.BeginVertical("box"); // Caixa para cada condição
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"If {typeName}", EditorStyles.boldLabel, GUILayout.Width(80)); // Tipo como label
            EditorGUILayout.PropertyField(varNameProp, GUIContent.none, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true)); // Campo nome variável

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                Undo.RecordObject(target, "Remove Condition");
                if (conditionsProp.arraySize > index) conditionsProp.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                NotifyViewOfChange();
                EditorGUILayout.EndHorizontal(); // Fecha horizontal antes de retornar
                EditorGUILayout.EndVertical(); // Fecha vertical antes de retornar
                return;
            }
            EditorGUILayout.EndHorizontal();

            // Desenha campos específicos da condição (exceto nome da variável)
            var iterator = conditionProp.Copy();
            var endProperty = iterator.GetEndProperty();
            iterator.NextVisible(true); // Pula m_ManagedReferenceId

            while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                if (iterator.name != "variableName")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
            EditorGUILayout.EndVertical(); // Fecha a caixa da condição
            EditorGUILayout.Space(2);
        }

        private void ShowConditionTypeMenu(SerializedProperty conditionsProp)
        {
            GenericMenu menu = new GenericMenu();
            // TODO: Popular com tipos de BaseCondition
            menu.AddItem(new GUIContent("Bool"), false, () => AddManagedReference<BoolCondition>(conditionsProp));
            menu.AddItem(new GUIContent("Int"), false, () => AddManagedReference<IntCondition>(conditionsProp));
            menu.AddItem(new GUIContent("Float"), false, () => AddManagedReference<FloatCondition>(conditionsProp));
            menu.AddItem(new GUIContent("String"), false, () => AddManagedReference<StringCondition>(conditionsProp));
            menu.ShowAsContext();
        }

        // --- Seção Ações (Comum a todos os Nós) ---

        private void DrawActionsSection()
        {
            if (actionsProperty == null) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("⚡ Actions (On Node Enter)", EditorStyles.boldLabel);

            // Botão Adicionar
            if (GUILayout.Button("+ Add Action"))
            {
                ShowActionTypeMenu(actionsProperty); // Passa a propriedade
            }
            EditorGUILayout.Space(2);

            // Desenha cada ação
            if (actionsProperty.arraySize == 0)
            {
                EditorGUILayout.LabelField("   (No actions)", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < actionsProperty.arraySize; i++)
                {
                    SerializedProperty actionProp = actionsProperty.GetArrayElementAtIndex(i);
                    DrawActionField(actionProp, i, actionsProperty); // Chama a lógica de desenho
                }
            }
        }

        private void DrawActionField(SerializedProperty actionProp, int index, SerializedProperty actionsProp)
        {
            if (actionProp.managedReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Action is null/invalid.", MessageType.Warning);
                if (GUILayout.Button("Remove Invalid", GUILayout.Width(100))) { /* Código Undo/Remove */ }
                return;
            }

            string typeName = actionProp.managedReferenceValue.GetType().Name.Replace("Action", ""); // Nome curto

            EditorGUILayout.BeginVertical("box"); // Caixa para cada ação
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Do {typeName}", EditorStyles.boldLabel, GUILayout.ExpandWidth(true)); // Tipo como label

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                Undo.RecordObject(target, "Remove Action");
                if (actionsProp.arraySize > index) actionsProp.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                NotifyViewOfChange();
                EditorGUILayout.EndHorizontal(); // Fecha antes de retornar
                EditorGUILayout.EndVertical(); // Fecha antes de retornar
                return;
            }
            EditorGUILayout.EndHorizontal();

            // Desenha campos específicos da ação
            var iterator = actionProp.Copy();
            var endProperty = iterator.GetEndProperty();
            iterator.NextVisible(true); // Pula m_ManagedReferenceId

            while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                EditorGUILayout.PropertyField(iterator, true);
            }
            EditorGUILayout.EndVertical(); // Fecha a caixa da ação
            EditorGUILayout.Space(3);
        }

        private void ShowActionTypeMenu(SerializedProperty actionsProp)
        {
            GenericMenu menu = new GenericMenu();
            // TODO: Popular com tipos de BaseAction
            menu.AddItem(new GUIContent("Set Bool"), false, () => AddManagedReference<SetBoolAction>(actionsProp));
            menu.AddItem(new GUIContent("Set Int"), false, () => AddManagedReference<SetIntAction>(actionsProp));
            menu.AddItem(new GUIContent("Set Float"), false, () => AddManagedReference<SetFloatAction>(actionsProp));
            menu.AddItem(new GUIContent("Set String"), false, () => AddManagedReference<SetStringAction>(actionsProp));
            menu.ShowAsContext();
        }

        // --- Método Genérico para Adicionar [SerializeReference] com Undo ---
        private void AddManagedReference<T>(SerializedProperty listProperty) where T : new()
        {
            Undo.RecordObject(target, $"Add {typeof(T).Name}"); // Registra ANTES
            int newIndex = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newElementProp = listProperty.GetArrayElementAtIndex(newIndex);
            newElementProp.managedReferenceValue = new T();
            serializedObject.ApplyModifiedProperties(); // Aplica IMEDIATAMENTE
            NotifyViewOfChange();
        }
    }
}