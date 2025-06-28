using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class PrologueMediaPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoClip[] videoClips;
    public RawImage videoDisplay;
    public RenderTexture videoRenderTexture;
    
    [Header("UI Prompts")]
    public GameObject continuePromptImage;
    
    [Header("Control Settings")]
    public float skipToEndOffset = 0.1f;

    [Header("UI References")]
    public GameObject speechCloud; // Drag your Speech Cloud GameObject here

    [Header("Character Creation")]
    public CharacterCreationPanel characterCreationPanel; // Add this reference

    
    private VideoPlayer videoPlayer;
    private AudioSource audioSource; // Store reference to AudioSource
    private int currentVideoIndex = 0;
    private bool isPlaying = false;
    private bool waitingForNextVideo = false;
    private bool prologueStarted = false;
    private bool isProcessingInput = false; // Prevent multiple rapid inputs
    
    public bool IsPrologueActive() { return prologueStarted; }
    
    void Start()
    {
        // Clear RenderTexture with white color at start
        if (videoRenderTexture != null)
        {
            RenderTexture.active = videoRenderTexture;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = null;
        }
        
        // Hide prompt image initially
        if (continuePromptImage != null)
            continuePromptImage.SetActive(false);
        
        // Create VideoPlayer with RenderTexture mode
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        
        // Add AudioSource for audio and store reference
        audioSource = gameObject.AddComponent<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
    }
    
    void Update()
    {
        // Force RenderTexture update every frame
        if (videoPlayer != null && videoPlayer.isPlaying && videoDisplay != null && videoRenderTexture != null)
        {
            videoDisplay.texture = videoRenderTexture;
        }
        
        if (!prologueStarted || isProcessingInput) return;
        
        // Accept both Space and Enter keys
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(ProcessInput());
        }
        
        CheckVideoCompletion();
    }
    
    // Coroutine to prevent rapid input processing
    IEnumerator ProcessInput()
    {
        isProcessingInput = true;
        
        if (isPlaying && !waitingForNextVideo)
        {
            // First press: Skip to end of current video
            SkipToEnd();
        }
        else if (waitingForNextVideo)
        {
            // Second press: Move to next video
            HideContinuePrompt();
            MoveToNextVideo();
        }
        
        // Small delay to prevent rapid input
        yield return new WaitForSeconds(0.2f);
        isProcessingInput = false;
    }
    
    public void StartPrologue()
    {
        if (videoClips.Length == 0) return;
        
        prologueStarted = true;
        
        // Enable display and set texture
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(true);
            videoDisplay.texture = videoRenderTexture;
        }
        
        // Start immediately with direct play
        StartCoroutine(PlayVideoDirectly(currentVideoIndex));
    }
    
    IEnumerator PlayVideoDirectly(int index)
    {
        if (index >= videoClips.Length || videoClips[index] == null) yield break;
        
        Debug.Log("Playing video directly: " + videoClips[index].name);
        
        // Hide prompt when starting new video
        HideContinuePrompt();
        
        // FORCE STOP both video and audio
        ForceStopVideoAndAudio();
        
        // Clear render texture to transparent (prevents black flicker)
        ClearRenderTexture(videoRenderTexture);
        
        // Set clip and play immediately
        videoPlayer.clip = videoClips[index];
        videoPlayer.time = 0;
        
        // Force UI refresh
        if (videoDisplay != null)
        {
            videoDisplay.enabled = false;
            yield return null;
            videoDisplay.enabled = true;
            videoDisplay.texture = videoRenderTexture;
        }
        
        videoPlayer.Play();
        
        // Wait a frame then check
        yield return null;
        
        // Force play if not playing
        if (!videoPlayer.isPlaying)
        {
            yield return new WaitForSeconds(0.2f);
            videoPlayer.Play();
        }
        
        isPlaying = true;
        waitingForNextVideo = false;
        
        Debug.Log("Video should be playing now");
    }
    
    void SkipToEnd()
    {
        if (videoPlayer.clip != null)
        {
            double endTime = videoPlayer.length - skipToEndOffset;
            videoPlayer.time = endTime;
            
            // Also sync audio to the same time position
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.time = (float)endTime;
            }
            
            Debug.Log("Skipped to end of video");
        }
    }
    
    // Method to force stop both video and audio
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
    
void CheckVideoCompletion()
{
    if (!isPlaying) return;
    
    // Check if video has finished (either naturally or by skipping)
    if (!videoPlayer.isPlaying && videoPlayer.time >= videoPlayer.length - skipToEndOffset)
    {
        // Force stop audio when video ends
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        isPlaying = false;
        
        // Check if this is the last video
        if (currentVideoIndex >= videoClips.Length - 1)
        {
            // Last video completed - start character creation instead of showing prompt
            StartCharacterCreation();
        }
        else
        {
            // Not last video - show continue prompt as usual
            waitingForNextVideo = true;
            ShowContinuePrompt();
            Debug.Log("Video finished - press Space or Enter to continue");
        }
    }
}

void StartCharacterCreation()
{
    // Hide video display and cloud
    if (videoDisplay != null)
        videoDisplay.gameObject.SetActive(false);
    
    // Debug the speech cloud reference
    if (speechCloud != null)
    {
        Debug.Log("Hiding speech cloud: " + speechCloud.name);
        speechCloud.SetActive(false);
        Debug.Log("Speech cloud active state: " + speechCloud.activeInHierarchy);
    }
    else
    {
        Debug.LogError("Speech Cloud reference is null! Please assign it in the Inspector.");
    }
    
    // Show character creation panel
    if (characterCreationPanel != null)
        characterCreationPanel.ShowPanel();
    
    prologueStarted = false;
    Debug.Log("Starting character creation...");
}


    
    void ShowContinuePrompt()
    {
        if (continuePromptImage != null)
            continuePromptImage.SetActive(true);
    }
    
    void HideContinuePrompt()
    {
        if (continuePromptImage != null)
            continuePromptImage.SetActive(false);
    }
    
    void MoveToNextVideo()
    {
        currentVideoIndex++;
        
        if (currentVideoIndex < videoClips.Length)
        {
            Debug.Log("Moving to next video: " + (currentVideoIndex + 1));
            StartCoroutine(PlayVideoDirectly(currentVideoIndex));
        }
        else
        {
            EndPrologue();
        }
    }
    
    void EndPrologue()
    {
        if (videoDisplay != null)
            videoDisplay.gameObject.SetActive(false);
        
        // Hide prompt
        HideContinuePrompt();
        
        // Force stop both video and audio
        ForceStopVideoAndAudio();
        
        isPlaying = false;
        waitingForNextVideo = false;
        prologueStarted = false;
        
        Debug.Log("Prologue ended - all videos completed");
    }
    
    // Method to clear render texture and prevent black flicker
    void ClearRenderTexture(RenderTexture renderTexture)
    {
        if (renderTexture == null) return;
        
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;
    }
}
