using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEvolutionManager : MonoBehaviour
{
    [Header("Character Evolution Images")]
    public Image childImage;      // Enabled after Chapter 1 ends
    public Image teenImage;       // Enabled after Chapter 2 ends
    public Image warriorImage;    // Enabled after Chapter 3 ends
    public Image grownManImage;   // Enabled after Chapter 4 ends

    [Header("Timing Settings")]
    public float enableDelay = 1f; // Delay after chapter title appears before enabling image

    // Chapter tracking
    private static CharacterEvolutionManager instance;
    private static int completedChapters = 0;

    // Singleton pattern
    public static CharacterEvolutionManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<CharacterEvolutionManager>();
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Disable all images at start
        DisableAllImages();
        completedChapters = 0;
        Debug.Log("CharacterEvolutionManager initialized - all images disabled");
    }

    void DisableAllImages()
    {
        if (childImage != null) childImage.gameObject.SetActive(false);
        if (teenImage != null) teenImage.gameObject.SetActive(false);
        if (warriorImage != null) warriorImage.gameObject.SetActive(false);
        if (grownManImage != null) grownManImage.gameObject.SetActive(false);

        Debug.Log("All character evolution images disabled");
    }

    // Static method to be called when a chapter ends and next chapter title is showing
    public static void OnChapterTransitionStarted(int completedChapterNumber)
    {
        if (Instance == null)
        {
            Debug.LogError("CharacterEvolutionManager instance not found!");
            return;
        }

        completedChapters = completedChapterNumber;
        Debug.Log($"Chapter {completedChapterNumber} completed - starting transition");

        // Start the image enabling process with delay
        Instance.StartCoroutine(Instance.EnableImageAfterDelay(completedChapterNumber));
    }

    IEnumerator EnableImageAfterDelay(int completedChapterNumber)
    {
        // Wait for the specified delay (during black screen/chapter title display)
        yield return new WaitForSeconds(enableDelay);

        // Enable the appropriate image based on completed chapter
        switch (completedChapterNumber)
        {
            case 1:
                EnableChildImage();
                break;
            case 2:
                EnableTeenImage();
                break;
            case 3:
                EnableWarriorImage();
                break;
            case 4:
                EnableGrownManImage();
                break;
            default:
                Debug.Log($"No image to enable for chapter {completedChapterNumber}");
                break;
        }
    }

    void EnableChildImage()
    {
        if (childImage != null)
        {
            childImage.gameObject.SetActive(true);
            Debug.Log("*** CHILD IMAGE ENABLED - Character evolved to Child ***");

            // Optional: Add fade-in effect
            StartCoroutine(FadeInImage(childImage));
        }
        else
        {
            Debug.LogError("Child image is not assigned!");
        }
    }

    void EnableTeenImage()
    {
        if (teenImage != null)
        {
            teenImage.gameObject.SetActive(true);
            Debug.Log("*** TEEN IMAGE ENABLED - Character evolved to Teen ***");

            // Optional: Add fade-in effect
            StartCoroutine(FadeInImage(teenImage));
        }
        else
        {
            Debug.LogError("Teen image is not assigned!");
        }
    }

    void EnableWarriorImage()
    {
        if (warriorImage != null)
        {
            warriorImage.gameObject.SetActive(true);
            Debug.Log("*** WARRIOR IMAGE ENABLED - Character evolved to Warrior ***");

            // Optional: Add fade-in effect
            StartCoroutine(FadeInImage(warriorImage));
        }
        else
        {
            Debug.LogError("Warrior image is not assigned!");
        }
    }

    void EnableGrownManImage()
    {
        if (grownManImage != null)
        {
            grownManImage.gameObject.SetActive(true);
            Debug.Log("*** GROWN MAN IMAGE ENABLED - Character evolved to Grown Man ***");

            // Optional: Add fade-in effect
            StartCoroutine(FadeInImage(grownManImage));
        }
        else
        {
            Debug.LogError("Grown man image is not assigned!");
        }
    }

    // Optional: Smooth fade-in effect for images
    IEnumerator FadeInImage(Image image)
    {
        if (image == null) yield break;

        // Start with transparent
        Color color = image.color;
        color.a = 0f;
        image.color = color;

        float fadeDuration = 0.5f;
        float elapsedTime = 0f;

        // Fade in over time
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);

            color.a = alpha;
            image.color = color;

            yield return null;
        }

        // Ensure final alpha is 1
        color.a = 1f;
        image.color = color;

        Debug.Log($"Image {image.name} fade-in complete");
    }

    // Public method to reset evolution (for new game)
    public static void ResetEvolution()
    {
        if (Instance != null)
        {
            Instance.DisableAllImages();
            completedChapters = 0;
            Debug.Log("Character evolution reset");
        }
    }

    // Public method to get current evolution state
    public static int GetCompletedChapters()
    {
        return completedChapters;
    }

    // Debug method to manually test image enabling
    [System.Obsolete("For testing only")]
    public void TestEnableImage(int chapterNumber)
    {
        OnChapterTransitionStarted(chapterNumber);
    }
}
