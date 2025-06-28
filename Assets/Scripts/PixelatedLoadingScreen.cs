using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PixelatedLoadingScreen : MonoBehaviour
{
    [Header("Loading Screen UI")]
    public GameObject loadingPanel;
    public Image panelImage;
    public TextMeshProUGUI loadingText;
    public Scrollbar progressScrollbar;
    
    [Header("Network Error")]
    public TextMeshProUGUI networkErrorText;
    
    [Header("Cleanup References")]
    public GameObject characterCreationPanel;
    
    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    
    [Header("Progress Animation")]
    public float progressAnimationSpeed = 2f;
    
    [Header("Loading Messages")]
    public string[] loadingMessages = {
        "Gathering story elements...",
        "Consulting the ancient texts...",
        "Weaving your character's tale...",
        "Preparing the narrator's voice...",
        "Almost ready..."
    };
    
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private float currentDisplayProgress = 0f;
    private Coroutine progressCoroutine;
    
    void Start()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        if (networkErrorText != null)
            networkErrorText.gameObject.SetActive(false);
    }
    
    public void ShowLoadingScreen()
    {
        StartCoroutine(ShowLoadingScreenSmooth());
    }
    
    public IEnumerator ShowLoadingScreenSmooth()
    {
        targetProgress = 0f;
        currentDisplayProgress = 0f;
        
        // Hide text and progress bar initially
        if (loadingText != null)
            loadingText.gameObject.SetActive(false);
        
        if (progressScrollbar != null)
        {
            progressScrollbar.gameObject.SetActive(false);
            progressScrollbar.size = 0f;
            progressScrollbar.value = 0f;
        }
        
        if (networkErrorText != null)
            networkErrorText.gameObject.SetActive(false);
        
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        
        if (panelImage != null)
        {
            panelImage.color = new Color(0f, 0f, 0f, 0f);
        }
        
        yield return StartCoroutine(FadeInPanel());
        
        // Enable text and progress bar after fade to black is complete
        if (loadingText != null)
            loadingText.gameObject.SetActive(true);
        
        if (progressScrollbar != null)
            progressScrollbar.gameObject.SetActive(true);
        
        OnBlackScreenComplete();
        
        currentProgress = 0f;
        UpdateLoadingDisplay();
    }
    
    IEnumerator FadeInPanel()
    {
        if (panelImage == null) yield break;
        
        float elapsed = 0f;
        Color startColor = new Color(0f, 0f, 0f, 0f);
        Color endColor = new Color(0f, 0f, 0f, 1f);
        
        panelImage.color = startColor;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            
            panelImage.color = Color.Lerp(startColor, endColor, progress);
            
            yield return null;
        }
        
        panelImage.color = endColor;
    }
    
    void OnBlackScreenComplete()
    {
        if (characterCreationPanel != null)
        {
            Debug.Log("Destroying Character Creation Panel");
            Destroy(characterCreationPanel);
            characterCreationPanel = null;
        }
    }
    
    public void UpdateProgress(float progress, string message = "")
    {
        targetProgress = Mathf.Clamp01(progress);
        
        if (!string.IsNullOrEmpty(message))
        {
            if (loadingText != null)
                loadingText.text = message;
        }
        else
        {
            int messageIndex = Mathf.FloorToInt(progress * (loadingMessages.Length - 1));
            if (loadingText != null && messageIndex < loadingMessages.Length)
                loadingText.text = loadingMessages[messageIndex];
        }
        
        if (progressCoroutine != null)
            StopCoroutine(progressCoroutine);
        
        progressCoroutine = StartCoroutine(AnimateProgressScrollbar());
    }
    
    IEnumerator AnimateProgressScrollbar()
    {
        while (Mathf.Abs(currentDisplayProgress - targetProgress) > 0.01f)
        {
            currentDisplayProgress = Mathf.MoveTowards(
                currentDisplayProgress, 
                targetProgress, 
                progressAnimationSpeed * Time.deltaTime
            );
            
            if (progressScrollbar != null)
            {
                progressScrollbar.size = currentDisplayProgress;
                progressScrollbar.value = 0f;
            }
            
            yield return null;
        }
        
        currentDisplayProgress = targetProgress;
        if (progressScrollbar != null)
        {
            progressScrollbar.size = currentDisplayProgress;
            progressScrollbar.value = 0f;
        }
    }
    
    public void ShowNetworkError(string message)
    {
        if (networkErrorText != null)
        {
            networkErrorText.text = message;
            networkErrorText.gameObject.SetActive(true);
        }
    }
    
    public void HideNetworkError()
    {
        if (networkErrorText != null)
        {
            networkErrorText.gameObject.SetActive(false);
        }
    }
    
    public void HideLoadingScreen()
    {
        StartCoroutine(FadeOutAndHide());
    }
    
    IEnumerator FadeOutAndHide()
    {
        // Hide text and progress bar before fading out
        if (loadingText != null)
            loadingText.gameObject.SetActive(false);
        
        if (progressScrollbar != null)
            progressScrollbar.gameObject.SetActive(false);
        
        if (networkErrorText != null)
            networkErrorText.gameObject.SetActive(false);
        
        if (panelImage != null)
        {
            float elapsed = 0f;
            Color startColor = panelImage.color;
            Color endColor = new Color(0f, 0f, 0f, 0f);
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                panelImage.color = Color.Lerp(startColor, endColor, progress);
                yield return null;
            }
        }
        
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
    
    void UpdateLoadingDisplay()
    {
        UpdateProgress(currentProgress);
    }
}
