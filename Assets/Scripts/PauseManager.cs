using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class PauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pausePanel;
    public Button pauseButton;
    public Button resumeButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Settings Integration")]
    public EnhancedSettingsManager settingsManager;

    [Header("Pause Button Timing")]
    public float pauseButtonDelay = 2f;

    [Header("Critical UI States")]
    public ChapterTransition chapterTransition;
    public GameObject characterCreationPanel;
    public GameObject loadingPanel;

    // Pause state
    private bool isPaused = false;
    private bool gameStarted = false;
    private bool pauseButtonVisible = false;
    private bool isCriticalUIActive = false;
    private bool isAltTabPaused = false;

    // Video state tracking
    private VideoPlayer[] allVideoPlayers;
    private bool[] videoWasPlaying;
    private double[] videoPauseTime;

    // Text animation state tracking
    private StoryDisplayManager storyDisplay;
    private bool textAnimationWasPaused = false;

    // Audio state tracking for Alt+Tab
    private AudioSource[] allAudioSources;
    private bool[] audioWasPlaying;
    private float[] audioPauseTime;

    void Start()
    {
        SetupUI();
        FindVideoPlayers();
        FindStoryDisplay();
        FindAllAudioSources();

        // Hide pause elements initially
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);

        // Subscribe to application focus events
        Application.focusChanged += OnApplicationFocus;
    }

    void SetupUI()
    {
        pauseButton?.onClick.AddListener(PauseGame);
        resumeButton?.onClick.AddListener(ResumeGame);
        settingsButton?.onClick.AddListener(OpenSettings);
        quitButton?.onClick.AddListener(QuitGame);
    }

    void FindVideoPlayers()
    {
        allVideoPlayers = FindObjectsOfType<VideoPlayer>();
        videoWasPlaying = new bool[allVideoPlayers.Length];
        videoPauseTime = new double[allVideoPlayers.Length];
    }

    void FindStoryDisplay()
    {
        storyDisplay = FindObjectOfType<StoryDisplayManager>();
    }

    void FindAllAudioSources()
    {
        allAudioSources = FindObjectsOfType<AudioSource>();
        audioWasPlaying = new bool[allAudioSources.Length];
        audioPauseTime = new float[allAudioSources.Length];
    }

    void Update()
    {
        // Check for ESC key to toggle pause (only if not Alt+Tab paused)
        if (Input.GetKeyDown(KeyCode.Escape) && gameStarted && CanPause() && !isAltTabPaused)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        // Check if we should show/hide pause button
        UpdatePauseButtonVisibility();
    }

    void UpdatePauseButtonVisibility()
    {
        if (!gameStarted || pauseButton == null) return;

        bool shouldShowPauseButton = CanShowPauseButton();

        if (shouldShowPauseButton != pauseButtonVisible)
        {
            pauseButtonVisible = shouldShowPauseButton;
            pauseButton.gameObject.SetActive(shouldShowPauseButton);
        }
    }

    bool CanShowPauseButton()
    {
        // Don't show during critical UI states or Alt+Tab pause
        if (IsInCriticalUIState() || isAltTabPaused) return false;

        // Don't show if already paused
        if (isPaused) return false;

        return true;
    }

    bool CanPause()
    {
        // Can't pause during critical UI states
        if (IsInCriticalUIState()) return false;

        // Can't pause if settings are open (but can unpause)
        if (!isPaused && settingsManager != null && settingsManager.IsSettingsPanelOpen()) return false;

        return true;
    }

    bool IsInCriticalUIState()
    {
        // Check manual override first
        if (isCriticalUIActive) return true;

        // Check chapter transition
        if (chapterTransition != null && chapterTransition.IsTransitioning()) return true;

        // Check character creation
        if (characterCreationPanel != null && characterCreationPanel.activeInHierarchy) return true;

        // Check loading panel
        if (loadingPanel != null && loadingPanel.activeInHierarchy) return true;

        // Only check loading screen if game hasn't started yet
        if (!gameStarted)
        {
            PixelatedLoadingScreen loadingScreen = FindObjectOfType<PixelatedLoadingScreen>();
            if (loadingScreen != null && loadingScreen.gameObject.activeInHierarchy) return true;
        }

        // Check if settings panel is open
        if (settingsManager != null && settingsManager.IsSettingsPanelOpen()) return true;

        return false;
    }

    public void OnGameStarted()
    {
        gameStarted = true;
        StartCoroutine(ShowPauseButtonAfterDelay());
    }

    IEnumerator ShowPauseButtonAfterDelay()
    {
        yield return new WaitForSeconds(pauseButtonDelay);
    }

    public void PauseGame()
    {
        if (!CanPause()) return;

        isPaused = true;

        // Pause time scale
        Time.timeScale = 0f;

        // Save and pause video states
        SaveAndPauseVideos();

        // FIXED: Check if we're in story segment before pausing text
        bool isInStorySegment = storyDisplay != null && storyDisplay.IsPlayingStorySegment();

        if (!isInStorySegment)
        {
            PauseTextAnimations();
        }
        else
        {
            Debug.Log("Skipping text animation pause - in story segment");
        }

        // Show pause panel (only for manual pause, not Alt+Tab)
        if (!isAltTabPaused && pausePanel != null)
            pausePanel.SetActive(true);

        // Hide pause button
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        isAltTabPaused = false; // Clear Alt+Tab pause state

        // Resume time scale
        Time.timeScale = 1f;

        // Resume videos
        ResumeVideos();

        // Resume text animations if they were paused
        if (textAnimationWasPaused)
        {
            ResumeTextAnimations();
        }

        // Resume all audio
        ResumeAllAudio();

        // Reset story input states after unpause
        if (storyDisplay != null)
        {
            storyDisplay.OnGameUnpaused();
        }

        // Hide pause panel
        if (pausePanel != null)
            pausePanel.SetActive(false);

        Debug.Log("Game Resumed");
    }

    // FIXED: Enhanced Alt+Tab pause handling with story segment awareness
    void OnApplicationFocus(bool hasFocus)
    {
        if (!gameStarted) return;

        if (!hasFocus && !isPaused && CanPause())
        {
            // Alt+Tab pause
            isAltTabPaused = true;
            isPaused = true;

            // Check if we're in a story segment
            bool isInStorySegment = storyDisplay != null && storyDisplay.IsPlayingStorySegment();

            // Pause time scale
            Time.timeScale = 0f;

            // Save and pause ALL videos
            SaveAndPauseAllVideos();

            // FIXED: Conditional audio and text pausing based on story state
            if (!isInStorySegment)
            {
                SaveAndPauseAllAudio();
                PauseTextAnimations();
                Debug.Log("Game auto-paused due to Alt+Tab (focus loss) - Full pause");
            }
            else
            {
                // In story segment - pause everything EXCEPT story audio and text
                SaveAndPauseNonStoryAudio();
                Debug.Log("Game auto-paused due to Alt+Tab (focus loss) - Story continues");
            }
        }
        else if (hasFocus && isAltTabPaused)
        {
            // Alt+Tab resume
            ResumeGame();
            Debug.Log("Game auto-resumed due to Alt+Tab (focus gained)");
        }
    }

    // FIXED: Pause only non-story audio sources
    void SaveAndPauseNonStoryAudio()
    {
        // Refresh audio source list
        allAudioSources = FindObjectsOfType<AudioSource>();
        audioWasPlaying = new bool[allAudioSources.Length];
        audioPauseTime = new float[allAudioSources.Length];

        for (int i = 0; i < allAudioSources.Length; i++)
        {
            if (allAudioSources[i] != null)
            {
                audioWasPlaying[i] = allAudioSources[i].isPlaying;
                audioPauseTime[i] = allAudioSources[i].time;

                // Skip story audio source
                bool isStoryAudio = (storyDisplay != null && allAudioSources[i] == storyDisplay.storyAudioSource);

                if (audioWasPlaying[i] && !isStoryAudio)
                {
                    allAudioSources[i].Pause();
                    Debug.Log($"Alt+Tab paused non-story audio: {allAudioSources[i].name}");
                }
                else if (isStoryAudio)
                {
                    Debug.Log($"Skipping story audio pause: {allAudioSources[i].name}");
                }
            }
        }
    }

    void SaveAndPauseAllVideos()
    {
        // Refresh video player list
        allVideoPlayers = FindObjectsOfType<VideoPlayer>();
        videoWasPlaying = new bool[allVideoPlayers.Length];
        videoPauseTime = new double[allVideoPlayers.Length];

        for (int i = 0; i < allVideoPlayers.Length; i++)
        {
            if (allVideoPlayers[i] != null)
            {
                videoWasPlaying[i] = allVideoPlayers[i].isPlaying;
                videoPauseTime[i] = allVideoPlayers[i].time;

                if (videoWasPlaying[i])
                {
                    allVideoPlayers[i].Pause();
                    Debug.Log($"Alt+Tab paused video player: {allVideoPlayers[i].name}");
                }
            }
        }

        // Also pause UnskippableVideoPlayer specifically
        UnskippableVideoPlayer unskippableVideo = FindObjectOfType<UnskippableVideoPlayer>();
        if (unskippableVideo != null)
        {
            unskippableVideo.PauseVideo();
        }
    }

    void SaveAndPauseAllAudio()
    {
        // Refresh audio source list
        allAudioSources = FindObjectsOfType<AudioSource>();
        audioWasPlaying = new bool[allAudioSources.Length];
        audioPauseTime = new float[allAudioSources.Length];

        for (int i = 0; i < allAudioSources.Length; i++)
        {
            if (allAudioSources[i] != null)
            {
                audioWasPlaying[i] = allAudioSources[i].isPlaying;
                audioPauseTime[i] = allAudioSources[i].time;

                if (audioWasPlaying[i])
                {
                    allAudioSources[i].Pause();
                    Debug.Log($"Alt+Tab paused audio source: {allAudioSources[i].name}");
                }
            }
        }
    }

    void SaveAndPauseVideos()
    {
        SaveAndPauseAllVideos(); // Use the same method for consistency
    }

    void ResumeVideos()
    {
        for (int i = 0; i < allVideoPlayers.Length; i++)
        {
            if (allVideoPlayers[i] != null && videoWasPlaying[i])
            {
                allVideoPlayers[i].time = videoPauseTime[i];
                allVideoPlayers[i].Play();
                Debug.Log($"Resumed video player: {allVideoPlayers[i].name}");
            }
        }

        // Also resume UnskippableVideoPlayer specifically
        UnskippableVideoPlayer unskippableVideo = FindObjectOfType<UnskippableVideoPlayer>();
        if (unskippableVideo != null)
        {
            unskippableVideo.ResumeVideo();
        }
    }

    void ResumeAllAudio()
    {
        for (int i = 0; i < allAudioSources.Length; i++)
        {
            if (allAudioSources[i] != null && audioWasPlaying[i])
            {
                allAudioSources[i].time = audioPauseTime[i];
                allAudioSources[i].UnPause();
                Debug.Log($"Resumed audio source: {allAudioSources[i].name}");
            }
        }
    }

    void PauseTextAnimations()
    {
        if (storyDisplay != null)
        {
            storyDisplay.PauseTextAnimation();
            textAnimationWasPaused = true;
        }
    }

    void ResumeTextAnimations()
    {
        if (storyDisplay != null && textAnimationWasPaused)
        {
            storyDisplay.ResumeTextAnimation();
            textAnimationWasPaused = false;
        }
    }

    void OpenSettings()
    {
        if (settingsManager != null)
        {
            // Hide pause panel but keep game paused
            if (pausePanel != null)
                pausePanel.SetActive(false);

            // Open settings
            settingsManager.OpenSettings();

            // Subscribe to settings close event
            settingsManager.OnSettingsClosed.AddListener(OnSettingsClosed);
        }
    }

    void OnSettingsClosed()
    {
        // Unsubscribe from event
        if (settingsManager != null)
            settingsManager.OnSettingsClosed.RemoveListener(OnSettingsClosed);

        // Return to pause panel if still paused and not Alt+Tab paused
        if (isPaused && !isAltTabPaused && pausePanel != null)
            pausePanel.SetActive(true);
    }

    void QuitGame()
    {
        Debug.Log("Quitting game from pause menu");

        // Resume time scale before quitting
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        Application.Quit();
#elif UNITY_WEBGL
        Application.ExternalEval("window.close();");
#else
        Application.Quit();
#endif
    }

    // Public methods for external scripts
    public bool IsPaused() => isPaused;
    public bool IsGameStarted() => gameStarted;
    public bool IsAltTabPaused() => isAltTabPaused;

    // Method to disable pause during critical moments
    public void SetCriticalUIState(bool isCritical)
    {
        isCriticalUIActive = isCritical;
        if (isCritical && pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        Application.focusChanged -= OnApplicationFocus;

        if (settingsManager != null)
            settingsManager.OnSettingsClosed.RemoveListener(OnSettingsClosed);

        // Ensure time scale is reset
        Time.timeScale = 1f;
    }
}
