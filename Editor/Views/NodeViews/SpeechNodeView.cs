using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

/// <summary>
/// Visualização do nó de fala (Speech Node).
/// CORRIGIDO: Proteção contra valores null durante criação.
/// </summary>
public class SpeechNodeView : BaseNodeView
{
    private SpeechNodeData speechData;

    public SpeechNodeView(SpeechNodeData data) : base(data)
    {
        speechData = data;
        AddToClassList("speech-node");
    }

    protected override void CreateNodeContent()
    {
        // Container para preview do conteúdo
        var contentContainer = new VisualElement();
        contentContainer.AddToClassList("node-content-preview");

        // CORREÇÃO: Verifica se os valores existem antes de usar
        string characterName = speechData?.CharacterName ?? "Character";
        string dialogueText = speechData?.DialogueText ?? "Enter dialogue text...";
        string audioID = speechData?.AudioSignalID ?? "";

        // Preview do nome do personagem
        var characterLabel = new Label(characterName);
        characterLabel.AddToClassList("character-name-preview");
        characterLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        characterLabel.style.fontSize = 14;
        contentContainer.Add(characterLabel);

        // Preview do texto do diálogo (primeiras 50 caracteres)
        var dialoguePreview = dialogueText.Length > 50
            ? dialogueText.Substring(0, 50) + "..."
            : dialogueText;

        var dialogueLabel = new Label(dialoguePreview);
        dialogueLabel.AddToClassList("dialogue-preview");
        dialogueLabel.style.fontSize = 11;
        dialogueLabel.style.whiteSpace = WhiteSpace.Normal;
        dialogueLabel.style.maxWidth = 200;
        contentContainer.Add(dialogueLabel);

        // Ícone de áudio se houver Audio ID
        if (!string.IsNullOrEmpty(audioID))
        {
            var audioLabel = new Label($"🔊 {audioID}");
            audioLabel.style.fontSize = 10;
            audioLabel.style.color = new Color(0.6f, 0.8f, 1f);
            contentContainer.Add(audioLabel);
        }

        extensionContainer.Add(contentContainer);
    }

    public override void UpdateNodeView()
    {
        base.UpdateNodeView();

        // Recria o conteúdo quando os dados mudam
        extensionContainer.Clear();
        CreateNodeContent();
    }
}