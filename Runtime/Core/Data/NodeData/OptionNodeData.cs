using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Representa uma escolha que o jogador pode fazer.
/// Cada saída corresponde a uma opção diferente.
/// </summary>

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
            foreach (var condition in conditions)
            {
                if (!condition.Evaluate())
                    return false;
            }
            return true;
        }
    }

    [SerializeField] private List<Option> options = new List<Option>();
    [SerializeField] private float timeoutDuration = 0f; // 0 = sem timeout
    [SerializeField] private int defaultOptionIndex = -1; // -1 = nenhum

    public List<Option> Options => options;
    public float TimeoutDuration { get => timeoutDuration; set => timeoutDuration = value; }
    public int DefaultOptionIndex { get => defaultOptionIndex; set => defaultOptionIndex = value; }

    public override string GetDisplayTitle() => "🔀 Player Choice";
    public override int GetOutputPortCount() => options.Count;
    public override int GetInputPortCount() => 1;

    public List<Option> GetAvailableOptions()
    {
        return options.Where(o => o.AreConditionsMet()).ToList();
    }
}