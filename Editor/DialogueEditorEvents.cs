// Em Editor/DialogueEditorEvents.cs
using System;

/// <summary>
/// Classe utilitária estática para lidar com eventos globais do editor.
/// </summary>
public static class DialogueEditorEvents
{
    // Evento que dispara quando um nó (dados) é modificado
    // e sua visualização (view) precisa ser atualizada.
    public static event Action<BaseNodeData> OnNodeViewUpdateRequest;

    /// <summary>
    /// Chamado pelo Inspector quando um nó precisa ser redesenhado no GraphView.
    /// </summary>
    public static void RequestNodeViewUpdate(BaseNodeData nodeData)
    {
        OnNodeViewUpdateRequest?.Invoke(nodeData);
    }
}