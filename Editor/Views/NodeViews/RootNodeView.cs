using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

/// <summary>
/// Visualização para o RootNodeData.
/// </summary>
namespace ChspDev.DialogueSystem.Editor 
{
    public class RootNodeView : BaseNodeView
    {
        // O construtor apenas passa os dados para a classe base
        public RootNodeView(BaseNodeData data) : base(data) { AddToClassList("root-node"); }

        // Implementação obrigatória do método abstrato
        protected override void CreateNodeContent()
        {
            // O nó raiz não precisa de nenhum conteúdo customizado no 'mainContainer'.
            // O título (de data.GetDisplayTitle()) e as portas (criadas pela
            // classe base) já são suficientes.
        }

        // Nota: O CreatePorts() da sua classe base já cuida de tudo,
        // desde que seu RootNodeData.GetInputPortCount() retorne 0
        // e RootNodeData.GetOutputPortCount() retorne 1.
    }
}
