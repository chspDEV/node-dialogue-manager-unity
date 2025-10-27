using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Representa uma escolha que o jogador pode fazer.
/// CORRIGIDO: Garante inicialização da lista de opções.
/// </summary>
[System.Serializable]
public class OptionNodeData : BaseNodeData
{
    [System.Serializable]
    public class Option
    {
        public string optionText = "Option text";
        [SerializeReference] public List<BaseCondition> conditions = new List<BaseCondition>();
        public UnityEvent onOptionSelected = new UnityEvent();

        public bool AreConditionsMet()
        {
            if (conditions == null) return true;

            foreach (var condition in conditions)
            {
                if (condition != null && !condition.Evaluate())
                    return false;
            }
            return true;
        }
    }

    // CORREÇÃO: Inicializa a lista no campo
    [SerializeField] private List<Option> options = new List<Option>();
    [SerializeField] private float timeoutDuration = 0f;
    [SerializeField] private int defaultOptionIndex = -1;

    public List<Option> Options
    {
        get
        {
            // Garante que nunca retorne null
            if (options == null)
                options = new List<Option>();
            return options;
        }
    }

    public float TimeoutDuration { get => timeoutDuration; set => timeoutDuration = value; }
    public int DefaultOptionIndex { get => defaultOptionIndex; set => defaultOptionIndex = value; }

    public override string GetDisplayTitle() => "🔀 Player Choice";
    public override int GetOutputPortCount() => Options.Count;
    public override int GetInputPortCount() => 1;

    public List<Option> GetAvailableOptions()
    {
        return Options.Where(o => o?.AreConditionsMet() ?? true).ToList();
    }
}