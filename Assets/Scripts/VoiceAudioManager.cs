using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class VoiceAudioManager : MonoBehaviour
{
    [Header("Voice Audio Sources")]
    public AudioSource unskippableVideoAudioSource;
    public AudioSource prologueMediaPlayerAudioSource;
    public AudioSource storyDisplayAudioSource;

    [Header("Audio Mixer")]
    public AudioMixerGroup voiceMixerGroup;

    private List<AudioSource> allVoiceAudioSources = new List<AudioSource>();
    private EnhancedSettingsManager settingsManager;

    void Awake()
    {
        // Collect all voice audio sources
        CollectAudioSources();

        // Setup mixer groups for all sources
        SetupMixerGroups();
    }

    void Start()
    {
        // Connect to settings manager
        settingsManager = FindObjectOfType<EnhancedSettingsManager>();
        if (settingsManager != null)
        {
            Debug.Log("Voice Audio Manager connected to settings");
        }
    }

    void CollectAudioSources()
    {
        // Find audio sources if not assigned
        if (unskippableVideoAudioSource == null)
        {
            UnskippableVideoPlayer videoPlayer = FindObjectOfType<UnskippableVideoPlayer>();
            if (videoPlayer != null)
            {
                unskippableVideoAudioSource = videoPlayer.GetComponent<AudioSource>();
            }
        }

        if (prologueMediaPlayerAudioSource == null)
        {
            // Find prologue media player audio source
            GameObject prologueObj = GameObject.Find("PrologueMediaPlayer");
            if (prologueObj != null)
            {
                prologueMediaPlayerAudioSource = prologueObj.GetComponent<AudioSource>();
            }
        }

        if (storyDisplayAudioSource == null)
        {
            StoryDisplayManager storyDisplay = FindObjectOfType<StoryDisplayManager>();
            if (storyDisplay != null)
            {
                storyDisplayAudioSource = storyDisplay.storyAudioSource;
            }
        }

        // Add all found sources to list
        allVoiceAudioSources.Clear();
        if (unskippableVideoAudioSource != null) allVoiceAudioSources.Add(unskippableVideoAudioSource);
        if (prologueMediaPlayerAudioSource != null) allVoiceAudioSources.Add(prologueMediaPlayerAudioSource);
        if (storyDisplayAudioSource != null) allVoiceAudioSources.Add(storyDisplayAudioSource);

        Debug.Log($"Voice Audio Manager found {allVoiceAudioSources.Count} voice audio sources");
    }

    void SetupMixerGroups()
    {
        // Route all voice audio sources to the same mixer group
        foreach (AudioSource audioSource in allVoiceAudioSources)
        {
            if (audioSource != null && voiceMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = voiceMixerGroup;
            }
        }
    }

    // Public methods for settings control
    public void SetVoiceVolume(float volume)
    {
        foreach (AudioSource audioSource in allVoiceAudioSources)
        {
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
        }
    }

    public void MuteAllVoice(bool muted)
    {
        foreach (AudioSource audioSource in allVoiceAudioSources)
        {
            if (audioSource != null)
            {
                audioSource.mute = muted;
            }
        }
    }

    // Method to add new voice audio sources at runtime
    public void RegisterVoiceAudioSource(AudioSource audioSource)
    {
        if (audioSource != null && !allVoiceAudioSources.Contains(audioSource))
        {
            allVoiceAudioSources.Add(audioSource);

            // Route to mixer group
            if (voiceMixerGroup != null)
            {
                audioSource.outputAudioMixerGroup = voiceMixerGroup;
            }

            // Apply current settings
            if (settingsManager != null)
            {
                audioSource.volume = settingsManager.GetVoiceVolume();
                audioSource.mute = settingsManager.IsVoiceMuted();
            }
        }
    }

    public void UnregisterVoiceAudioSource(AudioSource audioSource)
    {
        if (audioSource != null && allVoiceAudioSources.Contains(audioSource))
        {
            allVoiceAudioSources.Remove(audioSource);
        }
    }
}
