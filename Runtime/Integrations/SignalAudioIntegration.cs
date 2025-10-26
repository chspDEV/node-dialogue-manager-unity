using UnityEngine;

/// <summary>
/// Integração com Signal Audio Manager (ou similar baseado em string-ID).
/// </summary>
public class SignalAudioIntegration : IAudioIntegration
{
    public void PlayDialogueAudio(string audioID)
    {
        if (string.IsNullOrEmpty(audioID)) return;

        // Chama a API do Signal Audio Manager
        // Signal.PlaySFX(audioID);

        // Como não temos acesso direto, usamos reflection ou assume que existe
        try
        {
            var signalType = System.Type.GetType("Signal, Assembly-CSharp");
            if (signalType != null)
            {
                var playSFXMethod = signalType.GetMethod("PlaySFX", new[] { typeof(string) });
                playSFXMethod?.Invoke(null, new object[] { audioID });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to play audio via Signal: {e.Message}");
        }
    }

    public void PlayUISound(string audioID)
    {
        PlayDialogueAudio(audioID);
    }

    public void StopDialogueAudio()
    {
        // Implementar se Signal tiver método de stop
        try
        {
            var signalType = System.Type.GetType("Signal, Assembly-CSharp");
            if (signalType != null)
            {
                var stopMethod = signalType.GetMethod("StopSFX", new[] { typeof(string) });
                stopMethod?.Invoke(null, new object[] { "dialogue" });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to stop audio via Signal: {e.Message}");
        }
    }
}