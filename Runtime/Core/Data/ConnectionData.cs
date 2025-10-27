using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Representa uma conexão entre dois nós.
/// Contém as condições que determinam se o fluxo pode seguir por esta conexão.
/// </summary>
[System.Serializable]
public class ConnectionData
{
    [SerializeField] private string guid;
    [SerializeField] private string fromNodeGUID;
    [SerializeField] private int fromPortIndex;
    [SerializeField] private string toNodeGUID;
    [SerializeField] private int toPortIndex;

    // Condições que devem ser atendidas para o diálogo seguir esta conexão
    [SerializeReference] private List<BaseCondition> conditions = new List<BaseCondition>();

    public string GUID
    {
        get
        {
            if (string.IsNullOrEmpty(guid))
                guid = System.Guid.NewGuid().ToString();
            return guid;
        }
    }

    public string FromNodeGUID { get => fromNodeGUID; set => fromNodeGUID = value; }
    public int FromPortIndex { get => fromPortIndex; set => fromPortIndex = value; }
    public string ToNodeGUID { get => toNodeGUID; set => toNodeGUID = value; }
    public int ToPortIndex { get => toPortIndex; set => toPortIndex = value; }
    public List<BaseCondition> Conditions => conditions;

    public string OutputNodeGuid { get; set; }
    public int OutputPortIndex { get; set; }
    public string InputNodeGuid { get; set; }

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