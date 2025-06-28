using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

public class UnskippableVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoClip videoClip;

    [Header("Video Display")]
    public RawImage videoDisplay;
    public RenderTexture videoRenderTexture;

    [Header("UI References")]
    public GameObject continuePromptImage;
    public GameObject speechCloud;

    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private bool isPlaying = false;
    private bool videoCompleted = false;
    private bool continuePressed = false;
    private bool waitingForInput = false;

    // Pause system - use Time.timeScale instead of video pause
    private bool isPausedBySystem = false;
    private float pausedVideoTime = 0f;
    private bool wasPlayingWhenPaused = false;

    void Start()
    {
        // Hide prompt initially
        if (continuePromptImage != null)
            continuePromptImage.SetActive(false);

        // Setup VideoPlayer
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;

        // Add event subscription for video completion
        videoPlayer.loopPointReached += OnVideoFinished;

        // Add AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);

        Debug.Log("UnskippableVideoPlayer setup complete");
    }

    void Update()
    {
        // Always show the video display when playing
        if (videoPlayer != null && videoDisplay != null && videoRenderTexture != null)
        {
            if (videoPlayer.isPlaying)
            {
                videoDisplay.texture = videoRenderTexture;
            }
        }

        // Check video completion only if not system paused
        if (!isPausedBySystem)
        {
            CheckVideoCompletion();
        }

        // Handle input only when waiting and not system paused
        if (waitingForInput && !isPausedBySystem)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                Debug.Log("RAW INPUT DETECTED IN UPDATE!");
                continuePressed = true;
                waitingForInput = false;
            }

            if (!string.IsNullOrEmpty(Input.inputString))
            {
                foreach (char c in Input.inputString)
                {
                    if (c == ' ' || c == '\n' || c == '\r')
                    {
                        Debug.Log($"INPUT STRING DETECTED: '{c}'");
                        continuePressed = true;
                        waitingForInput = false;
                        break;
                    }
                }
            }
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // Only process if not system paused and actually finished
        if (!isPausedBySystem && vp.time >= vp.length - 0.1f)
        {
            Debug.Log("Video finished - loopPointReached event triggered");
            isPlaying = false;
            videoCompleted = true;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    public IEnumerator PlayVideoOnly()
    {
        if (videoClip == null)
        {
            Debug.LogError("No video clip assigned!");
            yield break;
        }

        Debug.Log("Starting video: " + videoClip.name);

        // Show video display
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(true);
        }

        // Play video and wait for completion
        yield return StartCoroutine(PlayVideoAndWaitForCompletion());
    }

    IEnumerator PlayVideoAndWaitForCompletion()
    {
        Debug.Log("Playing video and waiting for completion: " + videoClip.name);

        // Force stop any existing playback
        ForceStopVideoAndAudio();

        // Clear render texture
        ClearRenderTexture(videoRenderTexture);

        // Set clip and prepare
        videoPlayer.clip = videoClip;
        videoPlayer.time = 0;
        videoPlayer.frame = 0;

        // Force UI refresh
        if (videoDisplay != null)
        {
            videoDisplay.enabled = false;
            yield return null;
            videoDisplay.enabled = true;
            videoDisplay.texture = videoRenderTexture;
        }

        // Prepare the video first
        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        Debug.Log("Video prepared, starting playback");

        // Start playing
        videoPlayer.Play();

        // Wait for actual playback to start
        yield return new WaitUntil(() => videoPlayer.isPlaying);

        // Additional safety wait
        yield return new WaitForSeconds(0.2f);

        isPlaying = true;
        videoCompleted = false;
        isPausedBySystem = false;

        Debug.Log($"Video confirmed playing: {videoPlayer.isPlaying}, Length: {videoPlayer.length}");

        // Wait for video to actually finish playing
        while (isPlaying && !videoCompleted)
        {
            // Don't check completion if system paused
            if (isPausedBySystem)
            {
                yield return null;
                continue;
            }

            // Check if video actually finished (not just paused)
            if (!videoPlayer.isPlaying && videoPlayer.time >= videoPlayer.length - 0.1f)
            {
                videoCompleted = true;
                isPlaying = false;
                break;
            }

            yield return null;
        }

        Debug.Log("Video playback actually completed");

        // FIXED: Call when video ends
        StoryDisplayManager.OnVideoEnded();

        // Ensure video is stopped
        ForceStopVideoAndAudio();
        isPlaying = false;
        videoCompleted = true;
    }

    void ForceStopVideoAndAudio()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void ClearRenderTexture(RenderTexture renderTexture)
    {
        if (renderTexture == null) return;

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;
    }

    void CheckVideoCompletion()
    {
        if (!isPlaying || videoCompleted || isPausedBySystem) return;

        // More accurate completion check using frame count
        if (videoPlayer != null)
        {
            // Check if we're at the end using frame count (more reliable)
            if (videoPlayer.frameCount > 0 && (float)videoPlayer.frame >= videoPlayer.frameCount - 3)
            {
                Debug.Log($"Video completed via frame check. Frame: {videoPlayer.frame}, FrameCount: {videoPlayer.frameCount}");
                isPlaying = false;
                videoCompleted = true;

                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
            // Fallback time-based check
            else if (videoPlayer.time >= videoPlayer.length - 0.1f)
            {
                Debug.Log($"Video completed via time check. Time: {videoPlayer.time}, Length: {videoPlayer.length}");
                isPlaying = false;
                videoCompleted = true;

                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }
    }

    // System pause methods - DON'T pause video, just disable input
    public void PauseVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying && !isPausedBySystem)
        {
            isPausedBySystem = true;
            wasPlayingWhenPaused = true;
            pausedVideoTime = (float)videoPlayer.time;

            // DON'T pause the video - let it continue playing
            // Just disable input processing

            Debug.Log($"UnskippableVideoPlayer system paused at time: {pausedVideoTime} (video continues playing)");
        }
    }

    public void ResumeVideo()
    {
        if (isPausedBySystem && wasPlayingWhenPaused)
        {
            isPausedBySystem = false;
            wasPlayingWhenPaused = false;

            // Video was never actually paused, just re-enable input

            Debug.Log("UnskippableVideoPlayer system resumed (video was never actually paused)");
        }
    }

    public IEnumerator ShowContinuePromptOnly()
    {
        Debug.Log("Showing continue prompt after video completion");

        // Wait a moment to ensure video has fully stopped
        yield return new WaitForSeconds(0.5f);

        if (continuePromptImage != null)
        {
            // Activate all parent objects to ensure visibility
            Transform current = continuePromptImage.transform;
            while (current != null)
            {
                if (!current.gameObject.activeInHierarchy)
                {
                    current.gameObject.SetActive(true);
                    Debug.Log($"Activated parent: {current.name}");
                }
                current = current.parent;
            }

            // Force activate the prompt
            continuePromptImage.SetActive(true);
            yield return new WaitForEndOfFrame();
            continuePromptImage.SetActive(true); // Double activation

            Debug.Log($"Continue prompt activated. Active state: {continuePromptImage.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("continuePromptImage is NULL! Check inspector assignment!");
        }

        // Reset continue state
        continuePressed = false;
        waitingForInput = true; // Enable input capture

        yield return new WaitForSeconds(0.1f);
    }

    public IEnumerator WaitForContinueInputOnly()
    {
        Debug.Log("Waiting for continue input - AGGRESSIVE MODE");

        // Reset state
        continuePressed = false;
        waitingForInput = true;

        Debug.Log($"Prompt exists: {continuePromptImage != null}");
        Debug.Log($"Prompt active: {continuePromptImage?.activeInHierarchy}");

        // Wait one frame to clear any existing input
        yield return new WaitForEndOfFrame();

        // Wait until continue is pressed
        while (!continuePressed)
        {
            // Don't process input if system paused
            if (isPausedBySystem)
            {
                yield return null;
                continue;
            }

            // Triple backup input check
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                Debug.Log("DIRECT INPUT CAPTURED IN COROUTINE!");
                continuePressed = true;
                break;
            }

            // Check for any key press as emergency backup
            if (Input.anyKeyDown)
            {
                Debug.Log($"ANY KEY PRESSED");
                if (Input.inputString.Contains(" ") || Input.inputString.Contains("\n"))
                {
                    Debug.Log("SPACE/ENTER FOUND IN ANY KEY!");
                    continuePressed = true;
                    break;
                }
            }

            // Debug every 2 seconds
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"Still waiting for SPACE/ENTER... Prompt visible: {continuePromptImage?.activeInHierarchy}");
            }

            yield return null;
        }

        waitingForInput = false;
        Debug.Log("Continue input finally received!");
    }

    public void HideContinuePrompt()
    {
        waitingForInput = false;

        if (continuePromptImage != null)
        {
            continuePromptImage.SetActive(false);
            Debug.Log("Continue prompt hidden");
        }

        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(false);
        }
    }

    public bool IsVideoPlaying()
    {
        return isPlaying && videoPlayer != null && videoPlayer.isPlaying && !isPausedBySystem;
    }

    public bool HasVideoCompleted()
    {
        return videoCompleted;
    }

    public bool HasContinueBeenPressed()
    {
        return continuePressed;
    }

    public bool IsSequenceComplete()
    {
        return videoCompleted && continuePressed;
    }

    void OnDestroy()
    {
        ForceStopVideoAndAudio();
        waitingForInput = false;

        // Unsubscribe from event
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    void OnDisable()
    {
        ForceStopVideoAndAudio();
        isPlaying = false;
        videoCompleted = false;
        continuePressed = false;
        waitingForInput = false;
        isPausedBySystem = false;
    }

    [ContextMenu("Force Continue")]
    public void ForceContinue()
    {
        continuePressed = true;
        waitingForInput = false;
        Debug.Log("Continue manually forced!");
    }

    [ContextMenu("Debug Video Status")]
    void DebugVideoStatus()
    {
        Debug.Log($"VideoPlayer: {videoPlayer != null}");
        Debug.Log($"VideoClip: {videoClip != null}");
        Debug.Log($"RenderTexture: {videoRenderTexture != null}");
        Debug.Log($"VideoDisplay: {videoDisplay != null}");
        Debug.Log($"ContinuePrompt: {continuePromptImage != null}");
        Debug.Log($"IsPlaying: {videoPlayer?.isPlaying}");
        Debug.Log($"IsPrepared: {videoPlayer?.isPrepared}");
        Debug.Log($"Video Length: {videoPlayer?.length}");
        Debug.Log($"Video Time: {videoPlayer?.time}");
        Debug.Log($"Video Frame: {videoPlayer?.frame}");
        Debug.Log($"Video FrameCount: {videoPlayer?.frameCount}");
        Debug.Log($"VideoCompleted: {videoCompleted}");
        Debug.Log($"ContinuePressed: {continuePressed}");
        Debug.Log($"WaitingForInput: {waitingForInput}");
        Debug.Log($"IsPausedBySystem: {isPausedBySystem}");
        Debug.Log($"Prompt Active: {continuePromptImage?.activeInHierarchy}");
    }

    // Add these methods to your existing UnskippableVideoPlayer class

    public void SetVideoClip(VideoClip newClip)
    {
        if (videoPlayer != null && newClip != null)
        {
            // Stop current video if playing
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            // Clear existing texture
            if (videoDisplay != null)
            {
                videoDisplay.texture = null;
            }

            // Set new clip
            videoPlayer.clip = newClip;
            videoClip = newClip; // Update the public field too
            Debug.Log($"Video clip changed to: {newClip.name}");
        }
        else
        {
            Debug.LogError("Cannot set video clip - videoPlayer or newClip is null");
        }
    }

    public IEnumerator PlayFinalVideo()
    {
        Debug.Log("Playing final video using existing setup...");

        if (videoPlayer == null || videoPlayer.clip == null)
        {
            Debug.LogError("VideoPlayer or clip missing for final video!");
            yield break;
        }

        // Reset all states
        isPlaying = false;
        videoCompleted = false;
        continuePressed = false;
        waitingForInput = false;
        isPausedBySystem = false;

        // Show video display
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(true);
            videoDisplay.texture = videoRenderTexture;
        }

        // Force stop any existing playback and clear texture
        ForceStopVideoAndAudio();
        ClearRenderTexture(videoRenderTexture);

        // Reset video player
        videoPlayer.time = 0;
        videoPlayer.frame = 0;
        videoPlayer.isLooping = false;

        // Prepare the video
        videoPlayer.Prepare();
        yield return new WaitUntil(() => videoPlayer.isPrepared);
        Debug.Log("Final video prepared");

        // Start playing
        videoPlayer.Play();
        yield return new WaitUntil(() => videoPlayer.isPlaying);

        isPlaying = true;
        videoCompleted = false;
        Debug.Log("Final video started playing");

        // Wait for completion (auto-continue, no input needed)
        while (isPlaying && !videoCompleted)
        {
            // Don't check completion if system paused
            if (isPausedBySystem)
            {
                yield return null;
                continue;
            }

            // Check if video finished
            if (!videoPlayer.isPlaying && videoPlayer.time >= videoPlayer.length - 0.1f)
            {
                videoCompleted = true;
                isPlaying = false;
                break;
            }

            yield return null;
        }

        Debug.Log("Final video completed - auto-continuing");

        // Clean up
        ForceStopVideoAndAudio();
        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
            videoDisplay.gameObject.SetActive(false);
        }
    }

}
