using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChapterTransition : MonoBehaviour
{
    [Header("Chapter Fade System")]
    public Image chapterFadePanel;
    public TextMeshProUGUI chapterText;

    [Header("Fade Settings")]
    public float chapterFadeDuration = 0.5f;
    public float chapterDisplayTime = 1f;

    [Header("Pause Integration")]
    public PauseManager pauseManager;

    // Transition state tracking
    private bool isTransitioning = false;
    private StoryDisplayManager storyDisplayManager;
    private static int chapterCounter = 0; // Track chapter numbers

    void Start()
    {
        if (chapterFadePanel != null)
        {
            chapterFadePanel.color = new Color(0f, 0f, 0f, 0f);
            chapterFadePanel.gameObject.SetActive(false);
        }

        if (chapterText != null)
        {
            chapterText.color = new Color(1f, 1f, 1f, 0f);
        }

        // Find pause manager if not assigned
        if (pauseManager == null)
        {
            pauseManager = FindObjectOfType<PauseManager>();
        }

        // Find story display manager
        storyDisplayManager = FindObjectOfType<StoryDisplayManager>();
    }

    public IEnumerator ShowChapterTitle(string title)
    {
        Debug.Log($"Showing chapter title: {title}");

        // Increment chapter counter
        chapterCounter++;

        // Use correct method names in StoryDisplayManager
        if (chapterCounter == 1)
        {
            // First chapter - start the sequence
            StoryDisplayManager.StartChapterSequence();
            Debug.Log("*** FIRST CHAPTER - PAUSE DISABLED ***");
        }
        else
        {
            // Subsequent chapters
            StoryDisplayManager.StartChapter(chapterCounter);
            Debug.Log($"*** CHAPTER {chapterCounter} STARTED - PAUSE DISABLED ***");
        }

        // Set transitioning state
        isTransitioning = true;
        if (pauseManager != null)
        {
            pauseManager.SetCriticalUIState(true);
        }

        // Show fade panel (black screen)
        if (chapterFadePanel != null)
        {
            chapterFadePanel.gameObject.SetActive(true);
        }

        yield return StartCoroutine(FadePanel(0f, 1f));

        if (chapterText != null)
        {
            chapterText.text = title;
            yield return StartCoroutine(FadeText(0f, 1f));
        }

        // FIXED: Trigger character evolution during black screen with chapter title
        if (chapterCounter > 1) // Only after completing previous chapters
        {
            int completedChapter = chapterCounter - 1; // Previous chapter that just completed
            CharacterEvolutionManager.OnChapterTransitionStarted(completedChapter);
            Debug.Log($"*** CHARACTER EVOLUTION TRIGGERED - Completed Chapter {completedChapter} ***");
        }

        yield return new WaitForSeconds(chapterDisplayTime);

        if (chapterText != null)
        {
            yield return StartCoroutine(FadeText(1f, 0f));
        }

        yield return StartCoroutine(FadePanel(1f, 0f));

        if (chapterFadePanel != null)
        {
            chapterFadePanel.gameObject.SetActive(false);
        }

        isTransitioning = false;
        if (pauseManager != null)
        {
            pauseManager.SetCriticalUIState(false);
        }

        Debug.Log($"Chapter {chapterCounter} title transition complete");
    }

    // Call this when a chapter ends
    public void OnChapterComplete()
    {
        StoryDisplayManager.EndChapter(chapterCounter);
        Debug.Log($"Chapter {chapterCounter} completed");

        // Check if this was the final chapter
        if (chapterCounter >= 5)
        {
            Debug.Log("*** ALL CHAPTERS COMPLETED ***");
        }
    }

    // Call this when ALL chapters are done
    public static void OnAllChaptersComplete()
    {
        StoryDisplayManager.EndAllChapters();

        // Reset character evolution for next playthrough
        CharacterEvolutionManager.ResetEvolution();

        chapterCounter = 0; // Reset for next time
        Debug.Log("All chapters completed - evolution reset");
    }

    // FIXED: Public method to get current chapter for external scripts
    public static int GetCurrentChapter()
    {
        return chapterCounter;
    }

    // FIXED: Public method to reset chapter counter
    public static void ResetChapterCounter()
    {
        chapterCounter = 0;
        CharacterEvolutionManager.ResetEvolution();
        Debug.Log("Chapter counter and evolution reset");
    }

    IEnumerator FadePanel(float startAlpha, float endAlpha)
    {
        if (chapterFadePanel == null) yield break;

        float elapsed = 0f;
        Color startColor = new Color(0f, 0f, 0f, startAlpha);
        Color endColor = new Color(0f, 0f, 0f, endAlpha);

        while (elapsed < chapterFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time for pause compatibility
            float progress = elapsed / chapterFadeDuration;
            chapterFadePanel.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }

        chapterFadePanel.color = endColor;
    }

    IEnumerator FadeText(float startAlpha, float endAlpha)
    {
        if (chapterText == null) yield break;

        float elapsed = 0f;
        Color startColor = new Color(1f, 1f, 1f, startAlpha);
        Color endColor = new Color(1f, 1f, 1f, endAlpha);

        while (elapsed < chapterFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time for pause compatibility
            float progress = elapsed / chapterFadeDuration;
            chapterText.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }

        chapterText.color = endColor;
    }

    // Public method for pause manager to check transition state
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
}
