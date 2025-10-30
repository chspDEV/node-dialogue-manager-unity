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
        private SerializedProperty conditionsProperty; // Cache para BranchNode

        private void OnEnable()
        {
            nodeData = target as BaseNodeData;
            actionsProperty = serializedObject.FindProperty("actions");
            optionsProperty = serializedObject.FindProperty("options");
            conditionsProperty = serializedObject.FindProperty("conditions");
        }

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
            Undo.RecordObject(target, "Modify Node Data");
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space(10);

            if (nodeData is SpeechNodeData speechNode)
            {
                DrawSpeechNodeFields();
            }
            else if (nodeData is OptionNodeData optionNode)
            {
                DrawOptionNodeFields();
            }
            else if (nodeData is BranchNodeData branchNode)
            {
                DrawBranchNodeFields();
            }
            else if (nodeData is RootNodeData)
            {
                EditorGUILayout.HelpBox("▶️ This is the starting node of the dialogue.", MessageType.Info);
            }

            DrawActionsSection();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                NotifyViewOfChange();
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Desenha os campos editáveis específicos para SpeechNodeData.
        /// </summary>
        private void DrawSpeechNodeFields()
        {
            EditorGUILayout.LabelField("💬 Speech Node Settings", EditorStyles.boldLabel);

            SerializedProperty characterNameProp = serializedObject.FindProperty("characterName");
            SerializedProperty dialogueTextProp = serializedObject.FindProperty("dialogueText");
            SerializedProperty characterIconProp = serializedObject.FindProperty("characterIcon");
            SerializedProperty audioSignalIDProp = serializedObject.FindProperty("audioSignalID");
            SerializedProperty displayDurationProp = serializedObject.FindProperty("displayDuration");
            SerializedProperty onNodeActivatedProp = serializedObject.FindProperty("onNodeActivated");
            SerializedProperty onNodeCompletedProp = serializedObject.FindProperty("onNodeCompleted");

            EditorGUILayout.PropertyField(characterNameProp);
            EditorGUILayout.PropertyField(dialogueTextProp, GUILayout.MinHeight(60));
            EditorGUILayout.PropertyField(characterIconProp);
            EditorGUILayout.PropertyField(audioSignalIDProp);

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(displayDurationProp, new GUIContent("Auto-Advance Duration (Sec)"));
            if (displayDurationProp.floatValue <= 0)
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
                EditorGUILayout.PropertyField(defaultOptionProp, new GUIContent("Default Option Index (-1 = None)"));
                if (defaultOptionProp.intValue >= optionsProperty.arraySize)
                {
                    EditorGUILayout.HelpBox("Default index is out of range!", MessageType.Error);
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("✨ Options", EditorStyles.boldLabel);

            if (GUILayout.Button("+ Add Option"))
            {
                Undo.RecordObject(target, "Add Option");
                optionsProperty.InsertArrayElementAtIndex(optionsProperty.arraySize);
                serializedObject.ApplyModifiedProperties();
                NotifyViewOfChange();
            }

            EditorGUILayout.Space(5);

            for (int i = 0; i < optionsProperty.arraySize; i++)
            {
                SerializedProperty optionProp = optionsProperty.GetArrayElementAtIndex(i);
                DrawOptionField(optionProp, i);
            }
        }

        /// <summary>
        /// Desenha os campos para o BranchNode (a lista de condições).
        /// </summary>
        private void DrawBranchNodeFields()
        {
            if (conditionsProperty == null)
            {
                EditorGUILayout.HelpBox("Could not find 'conditions' property.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("💎 Branch (If) Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("If ALL conditions below are TRUE, the 'True' path is taken.\nOtherwise, the 'False' path is taken.", MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🔐 Conditions", EditorStyles.boldLabel);

            DrawConditionsList(conditionsProperty);
        }

        /// <summary>
        /// Desenha uma única opção dentro da lista do OptionNode.
        /// </summary>
        private void DrawOptionField(SerializedProperty optionProp, int index)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            SerializedProperty optionTextHeaderProp = optionProp.FindPropertyRelative("optionText");
            string headerText = string.IsNullOrEmpty(optionTextHeaderProp.stringValue) ? $"Option {index + 1}" : optionTextHeaderProp.stringValue;
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

            SerializedProperty optionTextProp = optionProp.FindPropertyRelative("optionText");
            SerializedProperty conditionsProp = optionProp.FindPropertyRelative("conditions");
            SerializedProperty onSelectedProp = optionProp.FindPropertyRelative("onOptionSelected");

            EditorGUILayout.PropertyField(optionTextProp, new GUIContent("Text"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🔐 Conditions", EditorStyles.boldLabel);
            DrawConditionsList(conditionsProp);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("▶️ Events on Selected", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onSelectedProp);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // --- Seção Condições (Reutilizável) ---

        private void DrawConditionsList(SerializedProperty conditionsProp)
        {
            if (conditionsProp == null) return;

            if (GUILayout.Button("+ Add Condition", GUILayout.Width(120)))
            {
                ShowConditionTypeMenu(conditionsProp);
            }
            EditorGUILayout.Space(2);

            if (conditionsProp.arraySize == 0)
            {
                EditorGUILayout.LabelField("   (No conditions - always available)", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < conditionsProp.arraySize; i++)
                {
                    SerializedProperty conditionProp = conditionsProp.GetArrayElementAtIndex(i);
                    DrawConditionField(conditionProp, i, conditionsProp);
                }
            }
        }

        private void DrawConditionField(SerializedProperty conditionProp, int index, SerializedProperty conditionsProp)
        {
            if (conditionProp == null) return;

            if (conditionProp.managedReferenceValue == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Condition is null/invalid.", MessageType.Warning);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(target, "Remove Invalid Condition");
                    if (conditionsProp.arraySize > index) conditionsProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    NotifyViewOfChange();
                }
                EditorGUILayout.EndHorizontal();
                return;
            }

            string typeName = conditionProp.managedReferenceValue.GetType().Name.Replace("Condition", "");
            SerializedProperty varNameProp = conditionProp.FindPropertyRelative("variableName");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"If {typeName}", EditorStyles.boldLabel, GUILayout.Width(80));

            if (varNameProp != null)
            {
                EditorGUILayout.PropertyField(varNameProp, GUIContent.none, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
            }
            else
            {
                EditorGUILayout.LabelField("[No Variable Name]", GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
            }

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                Undo.RecordObject(target, "Remove Condition");
                if (conditionsProp.arraySize > index) conditionsProp.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                NotifyViewOfChange();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            var iterator = conditionProp.Copy();
            var endProperty = iterator.GetEndProperty();
            iterator.NextVisible(true);

            while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                if (iterator.name != "variableName")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void ShowConditionTypeMenu(SerializedProperty conditionsProp)
        {
            GenericMenu menu = new GenericMenu();
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

            if (GUILayout.Button("+ Add Action"))
            {
                ShowActionTypeMenu(actionsProperty);
            }
            EditorGUILayout.Space(2);

            if (actionsProperty.arraySize == 0)
            {
                EditorGUILayout.LabelField("   (No actions)", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < actionsProperty.arraySize; i++)
                {
                    SerializedProperty actionProp = actionsProperty.GetArrayElementAtIndex(i);
                    DrawActionField(actionProp, i, actionsProperty);
                }
            }
        }

        // ---
        // --- ⬇️ ESTA É A FUNÇÃO CORRIGIDA ⬇️ ---
        // ---
        private void DrawActionField(SerializedProperty actionProp, int index, SerializedProperty actionsProp)
        {
            if (actionProp == null) return;

            if (actionProp.managedReferenceValue == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("Action is null/invalid.", MessageType.Warning);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(target, "Remove Invalid Action");
                    if (actionsProp.arraySize > index) actionsProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    NotifyViewOfChange();
                }
                EditorGUILayout.EndHorizontal();
                return;
            }

            string typeName = actionProp.managedReferenceValue.GetType().Name.Replace("Action", "");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Do {typeName}", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                Undo.RecordObject(target, "Remove Action");
                if (actionsProp.arraySize > index) actionsProp.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                NotifyViewOfChange();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            // --- ✨ CORREÇÃO INICIA AQUI ---

            // 1. Encontra manualmente a propriedade 'variableName' da classe BaseAction
            SerializedProperty varNameProp = actionProp.FindPropertyRelative("variableName");
            if (varNameProp != null)
            {
                // 2. Desenha o campo 'variableName' primeiro
                EditorGUILayout.PropertyField(varNameProp, new GUIContent("Variable Name"));
            }

            // 3. Itera sobre o RESTO das propriedades
            var iterator = actionProp.Copy();
            var endProperty = iterator.GetEndProperty();
            iterator.NextVisible(true); // Pula m_ManagedReferenceId

            while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                // 4. Desenha as outras propriedades, MAS PULA 'variableName' (que já desenhamos)
                if (iterator.name != "variableName")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
            // --- ✨ CORREÇÃO TERMINA AQUI ---

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private void ShowActionTypeMenu(SerializedProperty actionsProp)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Set Bool"), false, () => AddManagedReference<SetBoolAction>(actionsProp));
            menu.AddItem(new GUIContent("Set Int"), false, () => AddManagedReference<SetIntAction>(actionsProp));
            menu.AddItem(new GUIContent("Set Float"), false, () => AddManagedReference<SetFloatAction>(actionsProp));
            menu.AddItem(new GUIContent("Set String"), false, () => AddManagedReference<SetStringAction>(actionsProp));
            menu.ShowAsContext();
        }

        private void AddManagedReference<T>(SerializedProperty listProperty) where T : new()
        {
            Undo.RecordObject(target, $"Add {typeof(T).Name}");
            int newIndex = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newElementProp = listProperty.GetArrayElementAtIndex(newIndex);
            newElementProp.managedReferenceValue = new T();
            serializedObject.ApplyModifiedProperties();
            NotifyViewOfChange();
        }
    }
}