using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;

public class StoryDisplayManager : MonoBehaviour
{
    [Header("Story Display UI")]
    public TextMeshProUGUI storyText;
    public AudioSource storyAudioSource;

    [Header("Cloud Integration")]
    public GameObject speechCloud;

    [Header("Prompt GameObjects")]
    public GameObject skipPrompt;
    public GameObject continuePrompt;

    [Header("Display Settings")]
    public float segmentDelay = 0.5f;
    public float minimumListenTime = 5f;

    [Header("Chapter Control")]
    public int totalChapters = 5;
    private static int currentChapter = 0;
    private static bool chaptersActive = false;

    [Header("Final Sequence")]
    public VideoClip finalVideo;
    public GameObject creditsPanel;
    public TextMeshProUGUI creditsText;
    public Button quitButton;

    [Header("UI Items To Disable Before Ending Video")]
    public GameObject[] itemsToDisableBeforeEndingVideo; // Assign in Inspector

    // Input state management
    private bool waitingForSkipInput = false;
    private bool waitingForContinueInput = false;
    private bool inputProcessed = false;
    private bool videoIsPlaying = false;

    // Skip tracking
    private bool canSkip = false;
    private bool skipRequested = false;
    private bool segmentComplete = false;
    private Coroutine skipTimerCoroutine;

    // Typewriter effect variables
    private bool isTextAnimationPaused = false;
    private bool isAudioPaused = false;
    private Coroutine currentTypewriterCoroutine;
    private int pausedCharacterIndex = 0;
    private string currentText = "";
    private float currentTotalDuration = 0f;
    private AudioClip currentAudioClip = null;
    private float typewriterStartTime = 0f;
    private float pausedTypewriterTime = 0f;
    private bool wasAudioPlayingWhenPaused = false;

    // Story segment tracking and Alt+Tab handling
    private bool isCurrentlyPlayingSegment = false;
    private bool applicationHasFocus = true;
    private bool inputBlockedDueToAltTab = false;
    private float altTabResumeTime = 0f;
    private const float ALT_TAB_INPUT_DELAY = 0.5f;

    // Final sequence tracking
    private bool finalSequenceActive = false;

    // Direct pause button control
    private static PauseManager pauseManagerInstance;
    private static Button pauseButtonInstance;

    void Start()
    {
        SetupAudioSource();

        if (storyText != null)
            storyText.text = "";

        HideAllPrompts();

        // Get direct references
        pauseManagerInstance = FindObjectOfType<PauseManager>();
        if (pauseManagerInstance != null)
        {
            pauseButtonInstance = pauseManagerInstance.pauseButton;
        }

        // Reset chapter state
        chaptersActive = false;
        currentChapter = 0;
        finalSequenceActive = false;

        // Setup credits panel
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }

        // Register with Voice Audio Manager
        VoiceAudioManager voiceManager = FindObjectOfType<VoiceAudioManager>();
        if (voiceManager != null && storyAudioSource != null)
        {
            voiceManager.RegisterVoiceAudioSource(storyAudioSource);
            Debug.Log("StoryDisplayManager registered with VoiceAudioManager");
        }

        // Subscribe to application focus events
        Application.focusChanged += OnApplicationFocusChanged;
    }

    void OnDestroy()
    {
        Application.focusChanged -= OnApplicationFocusChanged;
    }

    void OnApplicationFocusChanged(bool hasFocus)
    {
        applicationHasFocus = hasFocus;

        if (!hasFocus)
        {
            inputBlockedDueToAltTab = true;
            Debug.Log("Application lost focus - blocking input temporarily");
        }
        else
        {
            altTabResumeTime = Time.unscaledTime;
            StartCoroutine(UnblockInputAfterDelay());
            Debug.Log("Application gained focus - will unblock input after delay");
        }
    }

    IEnumerator UnblockInputAfterDelay()
    {
        yield return new WaitForSecondsRealtime(ALT_TAB_INPUT_DELAY);
        inputBlockedDueToAltTab = false;
        Debug.Log("Input unblocked after Alt+Tab delay");
    }

    void Update()
    {
        // Enhanced input blocking for Alt+Tab scenarios
        bool shouldBlockInput = !applicationHasFocus || inputBlockedDueToAltTab || videoIsPlaying;

        // Check if game is paused
        PauseManager pauseManager = FindObjectOfType<PauseManager>();
        bool gamePaused = pauseManager != null && pauseManager.IsPaused();

        // Block input if paused (unless we're in story segment during Alt+Tab)
        if (gamePaused && !(isCurrentlyPlayingSegment && !applicationHasFocus))
        {
            shouldBlockInput = true;
        }

        if (shouldBlockInput)
        {
            return;
        }

        // Handle skip input with Alt+Tab protection
        if (waitingForSkipInput && canSkip && !skipRequested && !segmentComplete)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (!inputProcessed)
                {
                    Debug.Log("SKIP TRIGGERED");
                    inputProcessed = true;
                    skipRequested = true;
                    waitingForSkipInput = false;
                    canSkip = false;

                    if (storyAudioSource != null && storyAudioSource.isPlaying)
                    {
                        storyAudioSource.Stop();
                    }

                    if (skipTimerCoroutine != null)
                    {
                        StopCoroutine(skipTimerCoroutine);
                        skipTimerCoroutine = null;
                    }

                    HideSkipPrompt();
                    StartCoroutine(ForceSegmentCompletion());
                }
            }
        }
    }

    // Method to check if story segment is playing
    public bool IsPlayingStorySegment()
    {
        return isCurrentlyPlayingSegment;
    }

    public bool IsFinalSequenceActive()
    {
        return finalSequenceActive;
    }

    // All missing static methods for other scripts
    public static void StartChapterSequence()
    {
        chaptersActive = true;
        currentChapter = 0;
        DisablePauseButtonDirect();
        Debug.Log("*** CHAPTER SEQUENCE STARTED - PAUSE DISABLED ***");
    }

    public static void StartChapter(int chapterNumber)
    {
        currentChapter = chapterNumber;
        chaptersActive = true;
        DisablePauseButtonDirect();
        Debug.Log($"*** CHAPTER {chapterNumber} STARTED - PAUSE DISABLED ***");
    }

    public static void EndChapter(int chapterNumber)
    {
        Debug.Log($"*** CHAPTER {chapterNumber} ENDED ***");

        if (chapterNumber >= 5)
        {
            chaptersActive = false;
            Debug.Log("*** ALL 5 CHAPTERS COMPLETED ***");
        }
    }

    public static void EndAllChapters()
    {
        chaptersActive = false;
        Debug.Log("*** ALL CHAPTERS COMPLETED ***");
    }

    public static void OnEnterPressedInOpening()
    {
        if (!chaptersActive)
        {
            EnablePauseButtonDirect();
            Debug.Log("*** ENTER PRESSED - PAUSE ENABLED ***");
        }
    }

    public static void OnVideoEnded()
    {
        if (!chaptersActive)
        {
            DisablePauseButtonDirect();
            Debug.Log("*** VIDEO ENDED - PAUSE DISABLED BEFORE CHAPTERS ***");
        }
    }

    // Direct pause button control methods
    public static void EnablePauseButtonDirect()
    {
        if (pauseButtonInstance != null)
        {
            pauseButtonInstance.gameObject.SetActive(true);
            pauseButtonInstance.interactable = true;
            Debug.Log("*** PAUSE BUTTON DIRECTLY ENABLED ***");
        }

        if (pauseManagerInstance != null)
        {
            pauseManagerInstance.SetCriticalUIState(false);
        }
    }

    public static void DisablePauseButtonDirect()
    {
        if (pauseButtonInstance != null)
        {
            pauseButtonInstance.gameObject.SetActive(false);
            pauseButtonInstance.interactable = false;
            Debug.Log("*** PAUSE BUTTON DIRECTLY DISABLED ***");
        }

        if (pauseManagerInstance != null)
        {
            pauseManagerInstance.SetCriticalUIState(true);
        }
    }

    // ARRAY DISABLE IMPLEMENTATION
    /// <summary>
    /// Disables all GameObjects in the itemsToDisableBeforeEndingVideo array.
    /// </summary>
    public void DisableEndingUIItems()
    {
        if (itemsToDisableBeforeEndingVideo == null) return;

        foreach (GameObject item in itemsToDisableBeforeEndingVideo)
        {
            if (item != null && item.activeSelf)
            {
                item.SetActive(false);
                Debug.Log($"Disabled: {item.name}");
            }
        }
        Debug.Log($"Disabled {itemsToDisableBeforeEndingVideo.Length} UI items before ending video");
    }

    // FINAL SEQUENCE METHODS
    public IEnumerator StartFinalSequence()
    {
        Debug.Log("*** STARTING FINAL SEQUENCE ***");
        finalSequenceActive = true;

        // Clear story text
        ClearStoryText();

        // Disable all specified UI items (cloud, overlays, etc.)
        DisableEndingUIItems();

        // Re-enable pause button for final video
        EnablePauseButtonDirect();
        Debug.Log("Pause button re-enabled for final video");

        // Play final video directly
        yield return StartCoroutine(PlayFinalVideoDirectly());

        // After video ends, show credits
        yield return StartCoroutine(ShowCreditsSequence());
    }

    IEnumerator PlayFinalVideoDirectly()
    {
        Debug.Log("Playing final video directly (UI items disabled)...");

        UnskippableVideoPlayer videoPlayer = FindObjectOfType<UnskippableVideoPlayer>();
        if (videoPlayer != null && finalVideo != null)
        {
            // Set the final video clip and play directly
            videoPlayer.SetVideoClip(finalVideo);
            yield return StartCoroutine(videoPlayer.PlayFinalVideo());

            Debug.Log("Final video completed");
        }
        else
        {
            Debug.LogError("UnskippableVideoPlayer or final video not found!");
            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator ShowCreditsSequence()
    {
        Debug.Log("Starting credits sequence...");

        // Disable pause button during credits
        DisablePauseButtonDirect();

        // Setup credits panel
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);

            // Setup credits text
            if (creditsText != null)
            {
                creditsText.text = "THANK YOU\n\nDevelopment and Art Credits:\nAayush Beura\n\nStoryline Credits:\nAshmit Mandal\n\nBy Team C0xFFEE OverFlow";
                creditsText.color = new Color(1f, 1f, 1f, 0f); // Start transparent
            }

            // Setup quit button
            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(true);
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(QuitGame);

                // Start button transparent
                Image buttonImage = quitButton.GetComponent<Image>();
                if (buttonImage != null)
                    buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 0f);

                TextMeshProUGUI buttonText = quitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.color = new Color(buttonText.color.r, buttonText.color.g, buttonText.color.b, 0f);
            }

            // Fade in credits
            yield return StartCoroutine(FadeInCredits());
        }
        else
        {
            Debug.LogError("Credits panel not assigned!");
        }
    }

    IEnumerator FadeInCredits()
    {
        float fadeDuration = 2f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);

            // Fade in credits text
            if (creditsText != null)
            {
                Color textColor = creditsText.color;
                textColor.a = alpha;
                creditsText.color = textColor;
            }

            // Fade in quit button
            if (quitButton != null)
            {
                Image buttonImage = quitButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    Color buttonColor = buttonImage.color;
                    buttonColor.a = alpha;
                    buttonImage.color = buttonColor;
                }

                TextMeshProUGUI buttonText = quitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    Color textColor = buttonText.color;
                    textColor.a = alpha;
                    buttonText.color = textColor;
                }
            }

            yield return null;
        }

        Debug.Log("Credits fade-in complete");
    }

    void QuitGame()
    {
        Debug.Log("Quitting game from credits...");

        // Save any final data if needed
        PlayerPrefs.Save();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("Quit in Editor - Stopping play mode");
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        Application.Quit();
        Debug.Log("Quit standalone application");
#elif UNITY_WEBGL
        Application.ExternalEval("window.close();");
#else
        Application.Quit();
#endif
    }

    IEnumerator ForceSegmentCompletion()
    {
        yield return new WaitForEndOfFrame();
        segmentComplete = true;
        ShowContinuePrompt();
        Debug.Log("Segment force-completed after skip");
    }

    public void SetVideoPlayingState(bool isPlaying)
    {
        videoIsPlaying = isPlaying;
        Debug.Log($"StoryDisplayManager input disabled: {isPlaying}");

        if (isPlaying)
        {
            ResetInputStates();
            HideAllPrompts();
        }
    }

    // Enhanced pause/resume methods that work with Alt+Tab
    public void PauseTextAnimation()
    {
        if (!isTextAnimationPaused)
        {
            isTextAnimationPaused = true;
            pausedTypewriterTime = Time.unscaledTime - typewriterStartTime;

            if (currentTypewriterCoroutine != null)
            {
                StopCoroutine(currentTypewriterCoroutine);
                currentTypewriterCoroutine = null;
            }

            // Only pause audio if not in story segment during Alt+Tab
            if (storyAudioSource != null && storyAudioSource.isPlaying && !isAudioPaused)
            {
                bool shouldPauseAudio = !(isCurrentlyPlayingSegment && !applicationHasFocus);

                if (shouldPauseAudio)
                {
                    wasAudioPlayingWhenPaused = true;
                    storyAudioSource.Pause();
                    isAudioPaused = true;
                    Debug.Log("Story audio paused");
                }
                else
                {
                    Debug.Log("Story audio continues during Alt+Tab");
                }
            }

            Debug.Log($"Text animation paused at character {pausedCharacterIndex}");
        }
    }

    public void ResumeTextAnimation()
    {
        if (isTextAnimationPaused)
        {
            isTextAnimationPaused = false;
            typewriterStartTime = Time.unscaledTime - pausedTypewriterTime;

            if (storyAudioSource != null && isAudioPaused && wasAudioPlayingWhenPaused)
            {
                storyAudioSource.UnPause();
                isAudioPaused = false;
                Debug.Log("Story audio resumed");
            }

            if (!string.IsNullOrEmpty(currentText))
            {
                currentTypewriterCoroutine = StartCoroutine(ResumeTypewriterEffect());
            }

            Debug.Log("Text animation resumed");
        }
    }

    public void OnGameUnpaused()
    {
        Debug.Log("Game unpaused - story display manager notified");
        StartCoroutine(ClearInputAfterUnpause());
    }

    IEnumerator ClearInputAfterUnpause()
    {
        for (int i = 0; i < 15; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        int clearCount = 0;
        while (Input.anyKeyDown && clearCount < 30)
        {
            clearCount++;
            yield return new WaitForEndOfFrame();
        }

        Debug.Log($"Input cleared after unpause - {clearCount} attempts");
    }

    IEnumerator ResumeTypewriterEffect()
    {
        if (storyText == null) yield break;

        float timePerCharacter = currentTotalDuration / currentText.Length;
        float elapsedTime = pausedTypewriterTime;

        int targetCharacterIndex = Mathf.FloorToInt(elapsedTime / timePerCharacter);
        pausedCharacterIndex = Mathf.Min(targetCharacterIndex, currentText.Length);

        storyText.text = currentText.Substring(0, pausedCharacterIndex);

        for (int i = pausedCharacterIndex; i <= currentText.Length; i++)
        {
            // Enhanced pause checking for Alt+Tab scenarios
            while (isTextAnimationPaused && !(isCurrentlyPlayingSegment && !applicationHasFocus))
            {
                pausedCharacterIndex = i;
                pausedTypewriterTime = Time.unscaledTime - typewriterStartTime;
                yield return null;
            }

            storyText.text = currentText.Substring(0, i);

            if (skipRequested)
            {
                storyText.text = currentText;
                break;
            }

            yield return new WaitForSecondsRealtime(timePerCharacter);
        }

        storyText.text = currentText;
        currentTypewriterCoroutine = null;
    }

    void SetupAudioSource()
    {
        if (storyAudioSource == null)
        {
            storyAudioSource = gameObject.AddComponent<AudioSource>();
        }
        storyAudioSource.playOnAwake = false;
        storyAudioSource.volume = 1.0f;
        storyAudioSource.spatialBlend = 0f;
        storyAudioSource.loop = false;
    }

    public IEnumerator PlayStorySegmentInCloud(string text, AudioClip audio)
    {
        Debug.Log($"Playing story segment: {text.Substring(0, Mathf.Min(30, text.Length))}...");

        // Mark that we're playing a story segment
        isCurrentlyPlayingSegment = true;

        ResetAllStates();
        HideAllPrompts();

        if (storyText != null)
            storyText.text = "";

        float audioDuration = 0f;

        if (audio != null && storyAudioSource != null)
        {
            Debug.Log($"Audio clip found: {audio.name}, Duration: {audio.length}s");

            if (storyAudioSource.isPlaying)
            {
                storyAudioSource.Stop();
            }

            storyAudioSource.clip = audio;
            currentAudioClip = audio;
            audioDuration = audio.length;

            storyAudioSource.Play();
            Debug.Log($"Audio started playing. Duration: {audioDuration} seconds");

            skipTimerCoroutine = StartCoroutine(SkipTimer());
        }
        else
        {
            Debug.LogWarning($"No audio clip available.");
            currentAudioClip = null;
            audioDuration = text.Length * 0.04f;
            canSkip = true;
            ShowSkipPrompt();
            waitingForSkipInput = true;
        }

        typewriterStartTime = Time.unscaledTime;
        yield return StartCoroutine(SynchronizedTypewriterEffect(text, audioDuration));

        if (!segmentComplete)
        {
            if (skipTimerCoroutine != null)
            {
                StopCoroutine(skipTimerCoroutine);
                skipTimerCoroutine = null;
            }

            HideSkipPrompt();

            if (skipRequested)
            {
                Debug.Log("Skip completed - proceeding to continue");
            }
            else
            {
                if (storyAudioSource != null && currentAudioClip != null)
                {
                    Debug.Log("Waiting for audio to finish naturally...");
                    while ((storyAudioSource.isPlaying && !isAudioPaused) || isAudioPaused)
                    {
                        yield return null;
                    }
                    Debug.Log("Audio finished naturally");
                }
            }

            segmentComplete = true;
            HideSkipPrompt();
            yield return new WaitForEndOfFrame();
            ShowContinuePrompt();
        }

        // Enhanced continue input wait with Alt+Tab protection
        yield return StartCoroutine(RobustWaitForContinue());

        HideContinuePrompt();
        currentAudioClip = null;

        // Mark that we're no longer playing a story segment
        isCurrentlyPlayingSegment = false;

        yield return new WaitForSecondsRealtime(segmentDelay);

        Debug.Log("Segment completed successfully");
    }

    // Robust continue input waiting with Alt+Tab protection
    IEnumerator RobustWaitForContinue()
    {
        Debug.Log("=== STARTING ROBUST CONTINUE INPUT WAIT ===");
        waitingForContinueInput = true;

        // Wait for input system to stabilize
        for (int i = 0; i < 10; i++)
            yield return new WaitForEndOfFrame();

        // Wait for all keys to be released before accepting new input
        yield return new WaitUntil(() =>
            !Input.GetKey(KeyCode.Return) &&
            !Input.GetKey(KeyCode.KeypadEnter) &&
            !Input.GetKey(KeyCode.Space) &&
            !Input.GetMouseButton(0) &&
            !Input.anyKey);

        Debug.Log("All keys released - ready to accept continue input");

        // Additional delay to ensure clean input state
        yield return new WaitForSecondsRealtime(0.2f);

        // Enhanced input loop with Alt+Tab protection
        while (waitingForContinueInput)
        {
            // Check if we should block input
            bool shouldBlockInput = !applicationHasFocus || inputBlockedDueToAltTab || videoIsPlaying;

            // Check if game is paused (but allow input during story segment Alt+Tab)
            PauseManager pauseManager = FindObjectOfType<PauseManager>();
            bool gamePaused = pauseManager != null && pauseManager.IsPaused();

            if (gamePaused && !(isCurrentlyPlayingSegment && !applicationHasFocus))
            {
                shouldBlockInput = true;
            }

            if (!shouldBlockInput)
            {
                // Only use GetKeyDown for reliable single-press detection
                bool keyDownDetected = Input.GetKeyDown(KeyCode.Return) ||
                                       Input.GetKeyDown(KeyCode.KeypadEnter) ||
                                       Input.GetKeyDown(KeyCode.Space) ||
                                       Input.GetMouseButtonDown(0);

                if (keyDownDetected)
                {
                    Debug.Log("*** CONTINUE INPUT DETECTED - KEY_DOWN! ***");
                    waitingForContinueInput = false;

                    // Clear input immediately after detection
                    yield return StartCoroutine(ClearAllInputStates());
                    break;
                }
            }

            yield return null;
        }

        Debug.Log("=== CONTINUE INPUT RECEIVED ===");
    }

    // Method to clear all input states
    IEnumerator ClearAllInputStates()
    {
        Debug.Log("Clearing all input states...");

        // Reset Unity's input axes
        Input.ResetInputAxes();

        // Wait several frames for input system to clear
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        // Clear any remaining input
        int clearAttempts = 0;
        while (Input.anyKeyDown && clearAttempts < 30)
        {
            clearAttempts++;
            yield return new WaitForEndOfFrame();
        }

        Debug.Log($"Input cleared after {clearAttempts} attempts");
    }

    IEnumerator SkipTimer()
    {
        yield return new WaitForSecondsRealtime(minimumListenTime);
        canSkip = true;

        if (!segmentComplete && !skipRequested)
        {
            ShowSkipPrompt();
            waitingForSkipInput = true;
            Debug.Log($"Skip enabled after {minimumListenTime} seconds");
        }
    }

    IEnumerator SynchronizedTypewriterEffect(string text, float totalDuration)
    {
        if (storyText == null) yield break;

        currentText = text;
        currentTotalDuration = totalDuration;
        pausedCharacterIndex = 0;

        storyText.text = "";
        float timePerCharacter = totalDuration / text.Length;

        currentTypewriterCoroutine = StartCoroutine(TypewriterLoop(text, timePerCharacter));
        yield return currentTypewriterCoroutine;

        storyText.text = text;
        currentTypewriterCoroutine = null;
    }

    // Enhanced typewriter loop with Alt+Tab handling
    IEnumerator TypewriterLoop(string text, float timePerCharacter)
    {
        for (int i = 0; i <= text.Length; i++)
        {
            // Enhanced pause checking - continue during story segment Alt+Tab
            while (isTextAnimationPaused && !(isCurrentlyPlayingSegment && !applicationHasFocus))
            {
                pausedCharacterIndex = i;
                pausedTypewriterTime = Time.unscaledTime - typewriterStartTime;
                yield return null;
            }

            storyText.text = text.Substring(0, i);
            pausedCharacterIndex = i;

            if (skipRequested)
            {
                storyText.text = text;
                Debug.Log("Skip detected - showing complete text");
                break;
            }

            // Use unscaled time so it works during Time.timeScale = 0
            yield return new WaitForSecondsRealtime(timePerCharacter);
        }
    }

    void ResetAllStates()
    {
        waitingForSkipInput = false;
        waitingForContinueInput = false;
        inputProcessed = false;
        canSkip = false;
        skipRequested = false;
        segmentComplete = false;
        isAudioPaused = false;
        pausedTypewriterTime = 0f;
        wasAudioPlayingWhenPaused = false;
    }

    void ResetInputStates()
    {
        waitingForSkipInput = false;
        waitingForContinueInput = false;
        inputProcessed = false;
    }

    void ShowSkipPrompt()
    {
        if (skipPrompt != null)
        {
            skipPrompt.SetActive(true);
            Debug.Log("Skip prompt shown");
        }
    }

    void HideSkipPrompt()
    {
        if (skipPrompt != null)
        {
            skipPrompt.SetActive(false);
            Debug.Log("Skip prompt hidden");
        }
        waitingForSkipInput = false;
        canSkip = false;
    }

    void ShowContinuePrompt()
    {
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
            Debug.Log("Continue prompt shown - ready for input");
        }
        else
        {
            Debug.LogError("Continue prompt is NULL! Check inspector assignment!");
        }
    }

    void HideContinuePrompt()
    {
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false);
            Debug.Log("Continue prompt hidden");
        }
        waitingForContinueInput = false;
    }

    void HideAllPrompts()
    {
        HideSkipPrompt();
        HideContinuePrompt();
    }

    public void ClearStoryText()
    {
        if (storyText != null)
        {
            storyText.text = "";
            Debug.Log("Story text cleared");
        }

        if (storyAudioSource != null && storyAudioSource.isPlaying)
        {
            storyAudioSource.Stop();
            Debug.Log("Audio stopped and cleared");
        }

        ResetAllStates();
        isTextAnimationPaused = false;
        currentAudioClip = null;
        isCurrentlyPlayingSegment = false;

        if (skipTimerCoroutine != null)
        {
            StopCoroutine(skipTimerCoroutine);
            skipTimerCoroutine = null;
        }

        if (currentTypewriterCoroutine != null)
        {
            StopCoroutine(currentTypewriterCoroutine);
            currentTypewriterCoroutine = null;
        }

        HideAllPrompts();
    }

    public void SetStoryText(string text)
    {
        if (storyText != null)
            storyText.text = text;
    }

    public IEnumerator PlayStorySegment(string text, AudioClip audio)
    {
        return PlayStorySegmentInCloud(text, audio);
    }
}
