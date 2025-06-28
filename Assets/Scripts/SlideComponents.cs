using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SlideComponents : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject topComponent;    // Assign your first PNG component
    public GameObject bottomComponent; // Assign your second PNG component
    public GameObject pressEnterImage; // Assign your "Press Enter to Begin" image

    [Header("Settings Logo")]
    public GameObject settingsLogo;    // Assign your settings logo
    public float logoFadeDelay = 1f;   // Delay before logo starts fading in
    public float logoFadeDuration = 2f; // Duration of logo fade

    [Header("Settings Integration")]
    public EnhancedSettingsManager settingsManager; // Reference to settings manager

    [Header("Animation Settings")]
    public float slideDistance = 500f; // Distance to slide in pixels
    public float slideSpeed = 2f;      // Speed of animation

    private Vector3 topOriginalPos;
    private Vector3 bottomOriginalPos;
    private bool isSliding = false;
    private bool hasSlid = false; // Prevent multiple slides
    private CanvasGroup settingsLogoCanvasGroup;
    private CanvasGroup pressEnterCanvasGroup;

    void Start()
    {
        // Store original positions
        topOriginalPos = topComponent.transform.localPosition;
        bottomOriginalPos = bottomComponent.transform.localPosition;

        // Setup settings logo for fading
        SetupSettingsLogo();

        // Setup press enter image for enabling/disabling
        SetupPressEnterImage();

        // Start the settings logo fade IN sequence
        StartCoroutine(FadeInSettingsLogo());

        // Subscribe to settings events if settings manager exists
        if (settingsManager != null)
        {
            settingsManager.OnSettingsOpened.AddListener(OnSettingsOpened);
            settingsManager.OnSettingsClosed.AddListener(OnSettingsClosed);
        }
    }

    void SetupSettingsLogo()
    {
        if (settingsLogo != null)
        {
            // Get or add CanvasGroup component for smooth fading
            settingsLogoCanvasGroup = settingsLogo.GetComponent<CanvasGroup>();
            if (settingsLogoCanvasGroup == null)
            {
                settingsLogoCanvasGroup = settingsLogo.AddComponent<CanvasGroup>();
            }

            // Start with logo invisible
            settingsLogoCanvasGroup.alpha = 0f;
            settingsLogo.SetActive(true);
        }
    }

    void SetupPressEnterImage()
    {
        if (pressEnterImage != null)
        {
            // Get or add CanvasGroup component for smooth enabling/disabling
            pressEnterCanvasGroup = pressEnterImage.GetComponent<CanvasGroup>();
            if (pressEnterCanvasGroup == null)
            {
                pressEnterCanvasGroup = pressEnterImage.AddComponent<CanvasGroup>();
            }

            // Start with press enter image visible
            pressEnterCanvasGroup.alpha = 1f;
            pressEnterCanvasGroup.interactable = true;
            pressEnterCanvasGroup.blocksRaycasts = true;
            pressEnterImage.SetActive(true);
        }
    }

    IEnumerator FadeInSettingsLogo()
    {
        if (settingsLogo == null || settingsLogoCanvasGroup == null) yield break;

        // Wait for the specified delay
        yield return new WaitForSeconds(logoFadeDelay);

        float elapsedTime = 0f;

        // Fade in the settings logo
        while (elapsedTime < logoFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / logoFadeDuration);
            settingsLogoCanvasGroup.alpha = alpha;
            yield return null;
        }

        // Ensure final alpha value
        settingsLogoCanvasGroup.alpha = 1f;

        Debug.Log("Settings logo fade-in complete");
    }

    void Update()
    {
        // Check if game is paused first
        PauseManager pauseManager = FindObjectOfType<PauseManager>();
        bool gamePaused = pauseManager != null && pauseManager.IsPaused();

        // Don't process any input if game is paused
        if (gamePaused) return;

        // Check if settings panel is open and disable/enable press enter accordingly
        bool settingsOpen = IsSettingsOpen();
        SetPressEnterEnabled(!settingsOpen);

        // FIXED: Re-enable settings button when settings close and game hasn't started
        if (settingsManager != null && settingsManager.settingsButton != null)
        {
            bool shouldEnableSettings = !settingsOpen && !hasSlid;
            settingsManager.settingsButton.interactable = shouldEnableSettings;

            // Make sure settings button is visible when it should be enabled
            if (shouldEnableSettings)
            {
                settingsManager.settingsButton.gameObject.SetActive(true);
            }
        }

        // Check for Enter key press - only allow if NOT paused, settings are closed, not sliding and hasn't slid
        if (Input.GetKeyDown(KeyCode.Return) && !isSliding && !hasSlid && !settingsOpen && !gamePaused)
        {
            // FIXED: Enable pause button when enter is pressed
            StoryDisplayManager.OnEnterPressedInOpening();

            StartCoroutine(SlideComponents_Coroutine());
        }
    }

    bool IsSettingsOpen()
    {
        if (settingsManager != null && settingsManager.settingsPanel != null)
        {
            return settingsManager.settingsPanel.activeInHierarchy;
        }
        return false;
    }

    void SetPressEnterEnabled(bool enabled)
    {
        if (pressEnterCanvasGroup != null)
        {
            if (enabled)
            {
                // Enable press enter image
                pressEnterCanvasGroup.alpha = 1f;
                pressEnterCanvasGroup.interactable = true;
                pressEnterCanvasGroup.blocksRaycasts = true;
            }
            else
            {
                // Disable press enter image (make it invisible and non-interactive)
                pressEnterCanvasGroup.alpha = 0.3f; // Dim it instead of completely hiding
                pressEnterCanvasGroup.interactable = false;
                pressEnterCanvasGroup.blocksRaycasts = false;
            }
        }
    }

    void OnSettingsOpened()
    {
        SetPressEnterEnabled(false);
        if (settingsManager != null && settingsManager.settingsButton != null)
        {
            settingsManager.settingsButton.interactable = false;
        }
    }

    void OnSettingsClosed()
    {
        SetPressEnterEnabled(true);
        if (settingsManager != null && settingsManager.settingsButton != null && !hasSlid)
        {
            settingsManager.settingsButton.interactable = true;
            settingsManager.settingsButton.gameObject.SetActive(true);
        }
    }

    IEnumerator SlideComponents_Coroutine()
    {
        isSliding = true;
        hasSlid = true;

        // Notify pause manager that game has started
        PauseManager pauseManager = FindObjectOfType<PauseManager>();
        if (pauseManager != null)
        {
            pauseManager.OnGameStarted();
        }

        // FADE OUT settings logo when game begins
        if (settingsLogo != null && settingsLogoCanvasGroup != null)
        {
            StartCoroutine(FadeOutSettingsLogo(1f)); // Fade out over 1 second
        }

        // Hide settings button when game starts
        if (settingsManager != null && settingsManager.settingsButton != null)
        {
            settingsManager.settingsButton.gameObject.SetActive(false);
        }

        Vector3 topTargetPos = topOriginalPos + Vector3.up * slideDistance;
        Vector3 bottomTargetPos = bottomOriginalPos + Vector3.down * slideDistance;

        float elapsedTime = 0f;

        while (elapsedTime < slideSpeed)
        {
            float t = elapsedTime / slideSpeed;

            // Slide top component up
            topComponent.transform.localPosition = Vector3.Lerp(topOriginalPos, topTargetPos, t);

            // Slide bottom component down
            bottomComponent.transform.localPosition = Vector3.Lerp(bottomOriginalPos, bottomTargetPos, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final positions
        topComponent.transform.localPosition = topTargetPos;
        bottomComponent.transform.localPosition = bottomTargetPos;

        isSliding = false;

        // Hide press enter image completely after sliding
        if (pressEnterImage != null)
        {
            pressEnterImage.SetActive(false);
        }

        // Destroy the game objects after sliding
        Destroy(topComponent);
        Destroy(bottomComponent);

        // Disable this script component permanently
        this.enabled = false;
    }

    public IEnumerator FadeOutSettingsLogo(float duration = 1f)
    {
        if (settingsLogo == null || settingsLogoCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        float startAlpha = settingsLogoCanvasGroup.alpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / duration);
            settingsLogoCanvasGroup.alpha = alpha;
            yield return null;
        }

        settingsLogoCanvasGroup.alpha = 0f;
        settingsLogo.SetActive(false);
    }

    public void TriggerSettingsLogoFade()
    {
        if (!hasSlid) // Only allow if components haven't slid yet
        {
            StartCoroutine(FadeInSettingsLogo());
        }
    }

    public bool CanStartGame()
    {
        return !isSliding && !hasSlid && !IsSettingsOpen();
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (settingsManager != null)
        {
            settingsManager.OnSettingsOpened.RemoveListener(OnSettingsOpened);
            settingsManager.OnSettingsClosed.RemoveListener(OnSettingsClosed);
        }
    }
}
