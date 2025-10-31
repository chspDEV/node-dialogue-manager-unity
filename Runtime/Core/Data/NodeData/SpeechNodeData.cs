using UnityEngine;

using UnityEngine.Events;

/// <summary>
/// Representa uma linha de diálogo falada por um personagem.
/// </summary>

public class SpeechNodeData : BaseNodeData
{
    [SerializeField] private string characterName = "Character";
    [SerializeField][TextArea(3, 10)] private string dialogueText = "Dialogue text here...";
    [SerializeField] private Sprite characterIcon;
    [SerializeField] private string audioSignalID; // String-ID para integração com Signal Audio Manager
    [SerializeField] private float displayDuration = 0f; // 0 = aguardar input do jogador

    // UnityEvents para hooks externos
    [SerializeField] private UnityEvent onNodeActivated = new UnityEvent();
    [SerializeField] private UnityEvent onNodeCompleted = new UnityEvent();

    public string CharacterName { get => characterName; set => characterName = value; }
    public string DialogueText { get => dialogueText; set => dialogueText = value; }
    public Sprite CharacterIcon { get => characterIcon; set => characterIcon = value; }
    public string AudioSignalID { get => audioSignalID; set => audioSignalID = value; }
    public float DisplayDuration { get => displayDuration; set => displayDuration = value; }
    public UnityEvent OnNodeActivated => onNodeActivated;
    public UnityEvent OnNodeCompleted => onNodeCompleted;

    public override string GetDisplayTitle() => $"💬 {characterName}";
    public override int GetOutputPortCount() => 1;
    public override int GetInputPortCount() => 1;

    public override void OnNodeEnter()
    {
        base.OnNodeEnter();
        onNodeActivated?.Invoke();
    }

    public override void OnNodeExit()
    {
        base.OnNodeExit();
        CompleteNode();
    }

    public void CompleteNode()
    {
        onNodeCompleted?.Invoke();
    }
}