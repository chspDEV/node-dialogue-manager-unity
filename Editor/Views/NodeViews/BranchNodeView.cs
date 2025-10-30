using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

// ❗ Mantenha o namespace consistente com seus outros NodeViews
namespace ChspDev.DialogueSystem.Editor
{
    /// <summary>
    /// Visualização para o BranchNodeData.
    /// Exibe portas de saída "True" e "False".
    /// </summary>
    public class BranchNodeView : BaseNodeView
    {
        public BranchNodeView(BaseNodeData data) : base(data)
        {
            // Adiciona uma classe USS específica para estilização (opcional)
            AddToClassList("branch-node");
            // Atualiza o título (pode não ser necessário se a base já o fizer)
            title = data.GetDisplayTitle();
        }

        /// <summary>
        /// Implementação obrigatória (pode ficar vazia).
        /// </summary>
        protected override void CreateNodeContent()
        {
            // O BranchNode não precisa de conteúdo customizado no corpo,
            // as portas já são descritivas o suficiente.
        }

        /// <summary>
        /// Sobrescreve o nome das portas de saída para "True" e "False".
        /// </summary>
        protected override string GetPortName(Direction direction, int index)
        {
            if (direction == Direction.Output)
            {
                // A porta 0 é "True", a porta 1 é "False"
                return index == 0 ? "True" : "False";
            }
            return string.Empty; // Entradas permanecem vazias
        }

        /// <summary>
        /// Define a capacidade das portas de saída (ambas Single).
        /// </summary>
        protected override Port.Capacity GetOutputCapacityForPort(int index)
        {
            return Port.Capacity.Single; // Cada saída (True/False) só pode ter uma conexão
        }
    }
}