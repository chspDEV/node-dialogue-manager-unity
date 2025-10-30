using UnityEditor;
using UnityEngine;

// ❗ O namespace do Editor DEVE ser consistente com sua estrutura
namespace ChspDev.DialogueSystem.Editor
{
    /// <summary>
    /// 🎮 Cria um Inspector customizado para o DialogueRunner
    /// com um botão de "Test Run" em Play Mode.
    /// </summary>
    [CustomEditor(typeof(DialogueRunner))]
    public class DialogueRunnerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Atualiza o objeto serializado (necessário para Undo e Prefabs)
            serializedObject.Update();

            // Desenha todos os campos que estão em DialogueRunner.cs
            // (uiManager e dialogueToRun)
            DrawDefaultInspector();

            EditorGUILayout.Space(10); // Adiciona um espaço

            // --- Seção de Debug ---
            EditorGUILayout.LabelField("🔧 Debugging", EditorStyles.boldLabel);

            // Verifica se o jogo está em Play Mode
            if (Application.isPlaying)
            {
                // Mostra um botão verde e maior
                GUI.backgroundColor = new Color(0.4f, 1f, 0.6f); // Verde
                if (GUILayout.Button("▶ Iniciar Diálogo (Debug)", GUILayout.Height(35)))
                {
                    // Pega o script DialogueRunner que está sendo inspecionado
                    DialogueRunner runner = (DialogueRunner)target;

                    // Chama o método público que criamos
                    runner.StartAssignedDialogue();
                }
                GUI.backgroundColor = Color.white; // Reseta a cor
            }
            else
            {
                // Mostra um aviso se estiver fora de Play Mode
                EditorGUILayout.HelpBox("Entre em Play Mode para testar o diálogo a partir daqui.", MessageType.Info);
            }

            // Aplica quaisquer mudanças feitas no Inspector
            serializedObject.ApplyModifiedProperties();
        }
    }
}