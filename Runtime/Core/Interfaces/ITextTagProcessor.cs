using UnityEngine;

public interface ITextTagProcessor
{
    /// <summary>
    /// Se precisar para processar texto com tags
    /// </summary>
    string ReplaceButtonTags(string text);
}
