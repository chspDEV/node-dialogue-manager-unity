using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Visualização customizada para conexões (edges) entre nós.
/// Mostra visualmente se há condições aplicadas.
/// </summary>
public class ConnectionView : Edge
{
    private ConnectionData connectionData;
    private Label conditionLabel;

    public ConnectionData ConnectionData => connectionData;

    public ConnectionView(ConnectionData data) : base()
    {
        connectionData = data;

        // Adiciona indicador visual se houver condições
        if (data.Conditions.Count > 0)
        {
            AddConditionIndicator();
        }

        // Muda cor baseada em condições
        UpdateEdgeColor();
    }

    private void AddConditionIndicator()
    {
        conditionLabel = new Label($"🔒 {connectionData.Conditions.Count}");
        conditionLabel.style.fontSize = 10;
        conditionLabel.style.color = Color.yellow;
        conditionLabel.style.position = Position.Absolute;
        conditionLabel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        conditionLabel.style.paddingLeft = 2;
        conditionLabel.style.paddingRight = 2;
        conditionLabel.style.borderBottomLeftRadius = 3;
        conditionLabel.style.borderBottomRightRadius = 3;
        conditionLabel.style.borderTopLeftRadius = 3;
        conditionLabel.style.borderTopRightRadius = 3;

        Add(conditionLabel);
    }

    private void UpdateEdgeColor()
    {
        if (connectionData.Conditions.Count > 0)
        {
            // Conexão condicional = amarelo
            edgeControl.inputColor = Color.yellow;
            edgeControl.outputColor = Color.yellow;
        }
        else
        {
            // Conexão normal = branco
            edgeControl.inputColor = Color.white;
            edgeControl.outputColor = Color.white;
        }
    }

    public void UpdateView()
    {
        if (conditionLabel != null)
        {
            conditionLabel.text = $"🔒 {connectionData.Conditions.Count}";
        }
        else if (connectionData.Conditions.Count > 0)
        {
            AddConditionIndicator();
        }

        UpdateEdgeColor();
    }
}