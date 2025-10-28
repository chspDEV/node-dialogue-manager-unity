
using System;

/// <summary>
/// Nó de entrada único. Toda conversa começa aqui.
/// </summary>
namespace ChspDev.DialogueSystem.Editor 
{
    public class RootNodeData : BaseNodeData
    {
        public override string GetDisplayTitle() => "▶ START";
        public override int GetOutputPortCount() => 1;
        public override int GetInputPortCount() => 0;

        public void SetGUID(string v)
        {
            guid = v;
        }
    }
}