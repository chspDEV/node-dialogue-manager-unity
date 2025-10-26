using System.Collections.Generic;

/// <summary>
/// Avalia condições em conexões e opções.
/// </summary>
public static class ConditionEvaluator
{
    public static bool EvaluateConditions(List<BaseCondition> conditions)
    {
        if (conditions == null || conditions.Count == 0) return true;

        foreach (var condition in conditions)
        {
            if (!condition.Evaluate())
                return false;
        }

        return true;
    }
}