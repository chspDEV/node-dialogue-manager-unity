using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Integração padrão fallback usando AudioSource do Unity.
/// </summary>
public class DefaultAudioIntegration : IAudioIntegration
{
    private AudioSource audioSource;
    private Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();

    public DefaultAudioIntegration()
    {
        var go = new GameObject("[DialogueAudioSource]");
        audioSource = go.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    public void PlayDialogueAudio(string audioID)
    {
        if (string.IsNullOrEmpty(audioID)) return;

        var clip = LoadAudioClip(audioID);
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    public void PlayUISound(string audioID)
    {
        if (string.IsNullOrEmpty(audioID)) return;

        var clip = LoadAudioClip(audioID);
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void StopDialogueAudio()
    {
        audioSource.Stop();
    }

    private AudioClip LoadAudioClip(string audioID)
    {
        if (audioClipCache.TryGetValue(audioID, out var cachedClip))
        {
            return cachedClip;
        }

        var clip = Resources.Load<AudioClip>($"Audio/{audioID}");
        if (clip != null)
        {
            audioClipCache[audioID] = clip;
        }

        return clip;
    }
}