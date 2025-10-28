using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

/// <summary>
/// Visualiza��o para o RootNodeData.
/// </summary>
namespace ChspDev.DialogueSystem.Editor 
{
    public class RootNodeView : BaseNodeView
    {
        // O construtor apenas passa os dados para a classe base
        public RootNodeView(BaseNodeData data) : base(data) { AddToClassList("root-node"); }

        // Implementa��o obrigat�ria do m�todo abstrato
        protected override void CreateNodeContent()
        {
            // O n� raiz n�o precisa de nenhum conte�do customizado no 'mainContainer'.
            // O t�tulo (de data.GetDisplayTitle()) e as portas (criadas pela
            // classe base) j� s�o suficientes.
        }

        // Nota: O CreatePorts() da sua classe base j� cuida de tudo,
        // desde que seu RootNodeData.GetInputPortCount() retorne 0
        // e RootNodeData.GetOutputPortCount() retorne 1.
    }
}
