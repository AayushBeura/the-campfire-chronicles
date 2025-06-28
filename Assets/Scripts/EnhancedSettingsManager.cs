using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class EnhancedSettingsManager : MonoBehaviour
{
    [Header("Settings UI")]
    public GameObject settingsPanel;
    public Button settingsButton;
    public Button closeButton;
    public Button gameEndQuitButton;

    [Header("Volume Controls")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider voiceVolumeSlider;
    public Button masterMuteButton;
    public Button musicMuteButton;
    public Button voiceMuteButton;

    [Header("Display Controls")]
    public Button fullscreenToggleButton;
    public Slider brightnessSlider;

    [Header("API Key Validation")]
    public TMP_InputField geminiApiKeyInput;
    public TMP_InputField murfApiKeyInput;
    public Button validateGeminiButton;
    public Button validateMurfButton;
    public Button clearGeminiButton;
    public Button clearMurfButton;
    public TextMeshProUGUI geminiStatusText;
    public TextMeshProUGUI murfStatusText;

    [Header("Audio System")]
    public AudioMixer audioMixer;
    public AudioSource backgroundMusicSource;
    public AudioSource voiceAudioSource;

    [Header("Button Sprites")]
    public Sprite muteButtonSprite;
    public Sprite unmuteButtonSprite;
    public Sprite fullscreenSprite;
    public Sprite windowedSprite;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnSettingsOpened;
    public UnityEngine.Events.UnityEvent OnSettingsClosed;

    // Settings values
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float voiceVolume = 1f;
    private bool isMasterMuted = false;
    private bool isMusicMuted = false;
    private bool isVoiceMuted = false;
    private bool isFullscreen = true;
    private float brightness = 0.5f;
    private string geminiApiKey = "";
    private string murfApiKey = "";

    // PlayerPrefs keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string VOICE_VOLUME_KEY = "VoiceVolume";
    private const string MASTER_MUTE_KEY = "MasterMuted";
    private const string MUSIC_MUTE_KEY = "MusicMuted";
    private const string VOICE_MUTE_KEY = "VoiceMuted";
    private const string FULLSCREEN_KEY = "IsFullscreen";
    private const string BRIGHTNESS_KEY = "Brightness";
    private const string GEMINI_API_KEY = "GeminiApiKey";
    private const string MURF_API_KEY = "MurfApiKey";

    void Start()
    {
        SetupUI();
        LoadSettings();
        UpdateUI();
        SetupAudioSources();
    }

    void SetupAudioSources()
    {
        if (backgroundMusicSource == null)
        {
            SimpleBackgroundMusic bgMusic = FindObjectOfType<SimpleBackgroundMusic>();
            if (bgMusic != null)
            {
                backgroundMusicSource = bgMusic.GetComponent<AudioSource>();
                Debug.Log("Auto-found background music source");
            }
        }

        if (voiceAudioSource == null)
        {
            GameObject voiceObj = GameObject.Find("VoiceAudio");
            if (voiceObj != null)
            {
                voiceAudioSource = voiceObj.GetComponent<AudioSource>();
            }
        }

        if (audioMixer != null)
        {
            if (backgroundMusicSource != null)
            {
                AudioMixerGroup[] musicGroups = audioMixer.FindMatchingGroups("Music");
                if (musicGroups.Length > 0)
                {
                    backgroundMusicSource.outputAudioMixerGroup = musicGroups[0];
                }
            }

            if (voiceAudioSource != null)
            {
                AudioMixerGroup[] voiceGroups = audioMixer.FindMatchingGroups("Voice");
                if (voiceGroups.Length > 0)
                {
                    voiceAudioSource.outputAudioMixerGroup = voiceGroups[0];
                }
            }
        }
    }

    void SetupUI()
    {
        // Basic button listeners
        settingsButton?.onClick.AddListener(OpenSettings);
        closeButton?.onClick.AddListener(CloseSettings);
        gameEndQuitButton?.onClick.AddListener(QuitToDesktop);

        // Mute button listeners
        masterMuteButton?.onClick.AddListener(ToggleMasterMute);
        musicMuteButton?.onClick.AddListener(ToggleMusicMute);
        voiceMuteButton?.onClick.AddListener(ToggleVoiceMute);

        // Display controls
        fullscreenToggleButton?.onClick.AddListener(ToggleFullscreen);

        // Volume sliders
        masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
        voiceVolumeSlider?.onValueChanged.AddListener(OnVoiceVolumeChanged);
        brightnessSlider?.onValueChanged.AddListener(OnBrightnessChanged);

        // API validation buttons
        validateGeminiButton?.onClick.AddListener(ValidateGeminiApiKey);
        validateMurfButton?.onClick.AddListener(ValidateMurfApiKey);

        // FIXED: Clear button listeners
        clearGeminiButton?.onClick.AddListener(ClearGeminiKey);
        clearMurfButton?.onClick.AddListener(ClearMurfKey);

        // Setup sliders
        if (masterVolumeSlider != null) { masterVolumeSlider.minValue = 0f; masterVolumeSlider.maxValue = 1f; }
        if (musicVolumeSlider != null) { musicVolumeSlider.minValue = 0f; musicVolumeSlider.maxValue = 1f; }
        if (voiceVolumeSlider != null) { voiceVolumeSlider.minValue = 0f; voiceVolumeSlider.maxValue = 1f; }
        if (brightnessSlider != null) { brightnessSlider.minValue = 0.1f; brightnessSlider.maxValue = 1f; }

        settingsPanel?.SetActive(false);
        InitializeStatusTexts();
    }

    void InitializeStatusTexts()
    {
        if (geminiStatusText != null)
        {
            geminiStatusText.text = "";
            geminiStatusText.color = Color.white;
        }

        if (murfStatusText != null)
        {
            murfStatusText.text = "";
            murfStatusText.color = Color.white;
        }
    }

    void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        voiceVolume = PlayerPrefs.GetFloat(VOICE_VOLUME_KEY, 1f);
        isMasterMuted = PlayerPrefs.GetInt(MASTER_MUTE_KEY, 0) == 1;
        isMusicMuted = PlayerPrefs.GetInt(MUSIC_MUTE_KEY, 0) == 1;
        isVoiceMuted = PlayerPrefs.GetInt(VOICE_MUTE_KEY, 0) == 1;
        isFullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;
        brightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, 0.5f);
        geminiApiKey = PlayerPrefs.GetString(GEMINI_API_KEY, "");
        murfApiKey = PlayerPrefs.GetString(MURF_API_KEY, "");

        ApplySettings();
        UpdateValidationStatus();
    }

    void UpdateValidationStatus()
    {
        if (!string.IsNullOrEmpty(geminiApiKey))
        {
            UpdateGeminiStatus("SAVED GEMINI KEY (VALIDATED)", Color.green);
        }
        else
        {
            UpdateGeminiStatus("NO GEMINI KEY SET", Color.gray);
        }

        if (!string.IsNullOrEmpty(murfApiKey))
        {
            UpdateMurfStatus("SAVED MURF KEY (VALIDATED)", Color.green);
        }
        else
        {
            UpdateMurfStatus("NO MURF KEY SET", Color.gray);
        }
    }

    void ApplySettings()
    {
        SetMasterVolume(isMasterMuted ? 0f : masterVolume);
        SetMusicVolume(isMusicMuted ? 0f : musicVolume);
        SetVoiceVolume(isVoiceMuted ? 0f : voiceVolume);
        Screen.fullScreen = isFullscreen;
        ApplyBrightness(brightness);
    }

    void UpdateUI()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (voiceVolumeSlider != null) voiceVolumeSlider.value = voiceVolume;
        if (brightnessSlider != null) brightnessSlider.value = brightness;

        if (geminiApiKeyInput != null) geminiApiKeyInput.text = geminiApiKey;
        if (murfApiKeyInput != null) murfApiKeyInput.text = murfApiKey;

        UpdateAllButtonSprites();
    }

    // Volume Controls
    public void OnMasterVolumeChanged(float value)
    {
        masterVolume = value;
        if (isMasterMuted && value > 0f)
        {
            isMasterMuted = false;
            UpdateMasterMuteButtonSprite();
        }
        SetMasterVolume(isMasterMuted ? 0f : value);
        SaveSettings();
    }

    public void OnMusicVolumeChanged(float value)
    {
        musicVolume = value;
        if (isMusicMuted && value > 0f)
        {
            isMusicMuted = false;
            UpdateMusicMuteButtonSprite();
        }
        SetMusicVolume(isMusicMuted ? 0f : value);
        SaveSettings();
    }

    public void OnVoiceVolumeChanged(float value)
    {
        voiceVolume = value;
        if (isVoiceMuted && value > 0f)
        {
            isVoiceMuted = false;
            UpdateVoiceMuteButtonSprite();
        }
        SetVoiceVolume(isVoiceMuted ? 0f : value);
        SaveSettings();
    }

    void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = volume > 0f ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat("MasterVolume", dbValue);
        }
        AudioListener.volume = volume;
    }

    void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = volume > 0f ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat("MusicVolume", dbValue);
        }

        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = volume;
        }
    }

    void SetVoiceVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = volume > 0f ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat("VoiceVolume", dbValue);
        }

        if (voiceAudioSource != null)
        {
            voiceAudioSource.volume = volume;
        }
    }

    // Mute Controls
    public void ToggleMasterMute()
    {
        isMasterMuted = !isMasterMuted;
        if (!isMasterMuted)
        {
            masterVolume = 1f;
            SetMasterVolume(1f);
            if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        }
        else
        {
            SetMasterVolume(0f);
        }
        UpdateMasterMuteButtonSprite();
        SaveSettings();
    }

    public void ToggleMusicMute()
    {
        isMusicMuted = !isMusicMuted;
        if (!isMusicMuted)
        {
            musicVolume = 1f;
            SetMusicVolume(1f);
            if (musicVolumeSlider != null) musicVolumeSlider.value = 1f;
        }
        else
        {
            SetMusicVolume(0f);
        }
        UpdateMusicMuteButtonSprite();
        SaveSettings();
    }

    public void ToggleVoiceMute()
    {
        isVoiceMuted = !isVoiceMuted;
        if (!isVoiceMuted)
        {
            voiceVolume = 1f;
            SetVoiceVolume(1f);
            if (voiceVolumeSlider != null) voiceVolumeSlider.value = 1f;
        }
        else
        {
            SetVoiceVolume(0f);
        }
        UpdateVoiceMuteButtonSprite();
        SaveSettings();
    }

    // Display Controls
    public void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        Screen.fullScreen = isFullscreen;
        UpdateFullscreenButtonSprite();
        SaveSettings();
        Debug.Log($"Fullscreen: {isFullscreen}");
    }

    public void OnBrightnessChanged(float value)
    {
        brightness = value;
        ApplyBrightness(value);
        SaveSettings();
    }

    void ApplyBrightness(float value)
    {
        RenderSettings.ambientIntensity = value;

        if (RenderSettings.fog)
        {
            RenderSettings.fogDensity = 0.01f * (1f - value);
        }
    }

    // Button Sprite Updates
    void UpdateMasterMuteButtonSprite()
    {
        if (masterMuteButton != null)
        {
            Image buttonImage = masterMuteButton.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.sprite = isMasterMuted ? unmuteButtonSprite : muteButtonSprite;
        }
    }

    void UpdateMusicMuteButtonSprite()
    {
        if (musicMuteButton != null)
        {
            Image buttonImage = musicMuteButton.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.sprite = isMusicMuted ? unmuteButtonSprite : muteButtonSprite;
        }
    }

    void UpdateVoiceMuteButtonSprite()
    {
        if (voiceMuteButton != null)
        {
            Image buttonImage = voiceMuteButton.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.sprite = isVoiceMuted ? unmuteButtonSprite : muteButtonSprite;
        }
    }

    void UpdateFullscreenButtonSprite()
    {
        if (fullscreenToggleButton != null)
        {
            Image buttonImage = fullscreenToggleButton.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.sprite = isFullscreen ? windowedSprite : fullscreenSprite;
        }
    }

    void UpdateAllButtonSprites()
    {
        UpdateMasterMuteButtonSprite();
        UpdateMusicMuteButtonSprite();
        UpdateVoiceMuteButtonSprite();
        UpdateFullscreenButtonSprite();
    }

    // API Key Validation
    public void ValidateGeminiApiKey()
    {
        if (geminiApiKeyInput != null)
        {
            string apiKey = geminiApiKeyInput.text.Trim();
            if (!string.IsNullOrEmpty(apiKey))
            {
                StartCoroutine(ValidateGeminiKey(apiKey));
            }
            else
            {
                UpdateGeminiStatus("PLEASE ENTER AN API KEY", Color.red);
            }
        }
    }

    public void ValidateMurfApiKey()
    {
        if (murfApiKeyInput != null)
        {
            string apiKey = murfApiKeyInput.text.Trim();
            if (!string.IsNullOrEmpty(apiKey))
            {
                StartCoroutine(ValidateMurfKey(apiKey));
            }
            else
            {
                UpdateMurfStatus("PLEASE ENTER AN API KEY", Color.red);
            }
        }
    }

    IEnumerator ValidateGeminiKey(string apiKey)
    {
        DisableBackButton(true);
        UpdateGeminiStatus("VALIDATING GEMINI KEY...", Color.yellow);

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var requestData = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = "This is a test message for API validation." }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 50,
                temperature = 0.1f
            }
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                geminiApiKey = apiKey;
                PlayerPrefs.SetString(GEMINI_API_KEY, geminiApiKey);
                PlayerPrefs.Save();

                UpdateGeminiStatus("VALIDATED & SAVED GEMINI KEY", Color.green);
                Debug.Log("Gemini API Key validated and saved successfully");

                OnAPIKeyUpdated();
            }
            else
            {
                UpdateGeminiStatus("INVALID GEMINI KEY - NOT SAVED", Color.red);
                Debug.LogError($"Gemini API validation failed: {request.error}");
            }
        }

        DisableBackButton(false);
    }

    IEnumerator ValidateMurfKey(string apiKey)
    {
        DisableBackButton(true);
        UpdateMurfStatus("VALIDATING MURF KEY...", Color.yellow);

        string url = "https://api.murf.ai/v1/speech/generate-with-key";

        var requestData = new
        {
            voiceId = "pt-BR-benício",
            text = "This is a test message for API validation.",
            format = "MP3",
            encodeAsBase64 = true
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("api-key", apiKey);
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                murfApiKey = apiKey;
                PlayerPrefs.SetString(MURF_API_KEY, murfApiKey);
                PlayerPrefs.Save();

                UpdateMurfStatus("VALIDATED & SAVED MURF KEY", Color.green);
                Debug.Log("Murf API Key validated and saved successfully");

                OnAPIKeyUpdated();
            }
            else
            {
                UpdateMurfStatus("INVALID MURF KEY - NOT SAVED", Color.red);
                Debug.LogError($"Murf API validation failed: {request.error}");
            }
        }

        DisableBackButton(false);
    }

    void UpdateGeminiStatus(string message, Color color)
    {
        if (geminiStatusText != null)
        {
            geminiStatusText.text = message;
            geminiStatusText.color = color;
        }
    }

    void UpdateMurfStatus(string message, Color color)
    {
        if (murfStatusText != null)
        {
            murfStatusText.text = message;
            murfStatusText.color = color;
        }
    }

    void DisableBackButton(bool disable)
    {
        if (closeButton != null)
            closeButton.interactable = !disable;
    }

    // FIXED: Clear key methods
    public void ClearGeminiKey()
    {
        geminiApiKey = "";
        PlayerPrefs.SetString(GEMINI_API_KEY, "");
        PlayerPrefs.Save();

        if (geminiApiKeyInput != null) geminiApiKeyInput.text = "";
        UpdateGeminiStatus("GEMINI KEY CLEARED", Color.gray);

        OnAPIKeyUpdated();
    }

    public void ClearMurfKey()
    {
        murfApiKey = "";
        PlayerPrefs.SetString(MURF_API_KEY, "");
        PlayerPrefs.Save();

        if (murfApiKeyInput != null) murfApiKeyInput.text = "";
        UpdateMurfStatus("MURF KEY CLEARED", Color.gray);

        OnAPIKeyUpdated();
    }

    // UI Methods
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            OnSettingsOpened?.Invoke();
            Debug.Log("Settings panel opened");
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            SaveSettings();
            OnSettingsClosed?.Invoke();
            Debug.Log("Settings panel closed and saved");
        }
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
        PlayerPrefs.SetFloat(VOICE_VOLUME_KEY, voiceVolume);
        PlayerPrefs.SetInt(MASTER_MUTE_KEY, isMasterMuted ? 1 : 0);
        PlayerPrefs.SetInt(MUSIC_MUTE_KEY, isMusicMuted ? 1 : 0);
        PlayerPrefs.SetInt(VOICE_MUTE_KEY, isVoiceMuted ? 1 : 0);
        PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
        PlayerPrefs.SetFloat(BRIGHTNESS_KEY, brightness);
        PlayerPrefs.SetString(GEMINI_API_KEY, geminiApiKey);
        PlayerPrefs.SetString(MURF_API_KEY, murfApiKey);
        PlayerPrefs.Save();

        Debug.Log("All settings saved successfully");
    }

    // FIXED: Integration with GoogleSheetsKeyManager
    public void OnAPIKeyUpdated()
    {
        if (GoogleSheetsKeyManager.Instance != null)
        {
            GoogleSheetsKeyManager.Instance.RefreshAPIKeys();
            Debug.Log("GoogleSheetsKeyManager refreshed after API key update");
        }
    }

    // Public getters
    public string GetGeminiApiKey() => geminiApiKey;
    public string GetMurfApiKey() => murfApiKey;
    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetVoiceVolume() => voiceVolume;
    public bool IsMasterMuted() => isMasterMuted;
    public bool IsMusicMuted() => isMusicMuted;
    public bool IsVoiceMuted() => isVoiceMuted;
    public bool IsFullscreen() => isFullscreen;
    public float GetBrightness() => brightness;

    public bool IsSettingsPanelOpen()
    {
        return settingsPanel != null && settingsPanel.activeInHierarchy;
    }

    public bool HasValidAPIKeys()
    {
        return !string.IsNullOrEmpty(geminiApiKey) && !string.IsNullOrEmpty(murfApiKey);
    }

    public bool HasUserSetKeys()
    {
        return !string.IsNullOrEmpty(geminiApiKey) || !string.IsNullOrEmpty(murfApiKey);
    }

    public void QuitToDesktop()
    {
        Debug.Log("Quitting game to desktop");
        SaveSettings();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("Quit in Editor - Stopping play mode");
#else
        Application.Quit();
        Debug.Log("Quit standalone application");
#endif
    }
}
