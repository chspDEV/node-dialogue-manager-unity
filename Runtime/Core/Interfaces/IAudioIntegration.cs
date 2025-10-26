/// <summary>
/// Interface para integração com sistemas de áudio de terceiros.
/// </summary>
public interface IAudioIntegration
{
    void PlayDialogueAudio(string audioID);
    void PlayUISound(string audioID);
    void StopDialogueAudio();
}