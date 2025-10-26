using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Efeito de máquina de escrever para texto usando unscaled time.
/// </summary>
public class TypewriterEffect
{
    private Label targetLabel;
    private float charactersPerSecond;
    private Coroutine typingCoroutine;
    private MonoBehaviour coroutineRunner;

    private string fullText;
    private int currentCharIndex;
    private bool isTyping;

    public bool IsTyping => isTyping;

    public TypewriterEffect(Label label, float speed)
    {
        targetLabel = label;
        charactersPerSecond = speed;

        // Cria um runner persistente para corrotinas
        var runnerGO = new GameObject("[TypewriterRunner]");
        coroutineRunner = runnerGO.AddComponent<TypewriterRunner>();
        UnityEngine.Object.DontDestroyOnLoad(runnerGO);
    }

    public void StartTyping(string text, Action onComplete = null)
    {
        Stop();

        fullText = text;
        currentCharIndex = 0;
        isTyping = true;

        targetLabel.text = "";

        typingCoroutine = coroutineRunner.StartCoroutine(TypeText(onComplete));
    }

    public void Stop()
    {
        if (typingCoroutine != null)
        {
            coroutineRunner.StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    public void CompleteInstantly()
    {
        Stop();
        targetLabel.text = fullText;
        isTyping = false;
    }

    private IEnumerator TypeText(Action onComplete)
    {
        float timePerCharacter = 1f / charactersPerSecond;

        while (currentCharIndex < fullText.Length)
        {
            currentCharIndex++;
            targetLabel.text = fullText.Substring(0, currentCharIndex);

            yield return new WaitForSecondsRealtime(timePerCharacter);
        }

        isTyping = false;
        onComplete?.Invoke();
    }

    private class TypewriterRunner : MonoBehaviour { }
}