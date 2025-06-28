using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NarratorEntry : MonoBehaviour
{
    [Header("Narrator Sprites")]
    public Sprite sprite1; // s1
    public Sprite sprite2; // s2
    public Image narratorImage;
    
    [Header("Animation Settings")]
    public float movementDuration = 2f;
    public float spriteToggleSpeed = 0.2f;  // How fast sprites toggle
    public float delayBeforeStart = 1f;
    
    [Header("Cloud Settings")]
    public GameObject speechCloud; // The cloud GameObject to enable
    public float cloudDelay = 0.5f; // Delay after reaching position before showing cloud
    
    [Header("Prologue Settings")]
    public PrologueMediaPlayer prologuePlayer; // Reference to the media player
    public float videoStartDelay = 0.2f; // Minimal delay before first video starts
    
    private bool isSequenceActive = false;
    private bool narratorHasEntered = false; // Prevent re-triggering narrator movement
    
    // Canvas coordinates
    private Vector3 initialPosition = new Vector3(1090.46f, -275.9f, 0f);  // Off-canvas
    private Vector3 finalPosition = new Vector3(639.7f, -275.9f, 0f);     // On-canvas
    
    void Start()
    {
        // Position narrator at initial off-canvas position
        narratorImage.transform.localPosition = initialPosition;
        narratorImage.sprite = sprite1; // Start with s1
        
        // Hide the cloud initially
        if (speechCloud != null)
            speechCloud.SetActive(false);
    }
    
    void Update()
    {
        // Check if prologue is active before processing any input
        bool prologueActive = prologuePlayer != null && prologuePlayer.IsPrologueActive();
        
        // Only allow narrator entry if it hasn't happened yet AND prologue isn't active
        if (!prologueActive && Input.GetKeyDown(KeyCode.Return) && !isSequenceActive && !narratorHasEntered)
        {
            StartCoroutine(DelayedNarratorEntry());
        }
    }
    
    System.Collections.IEnumerator DelayedNarratorEntry()
    {
        isSequenceActive = true;
        narratorHasEntered = true; // Prevent re-triggering
        
        // Wait for initial delay
        yield return new WaitForSeconds(delayBeforeStart);
        
        // Start movement and sprite toggle animation
        yield return StartCoroutine(AnimateNarratorEntry());
        
        // Wait for cloud delay
        yield return new WaitForSeconds(cloudDelay);
        
        // Enable the cloud GameObject
        if (speechCloud != null)
            speechCloud.SetActive(true);
        
        // Wait minimal delay before starting videos
        yield return new WaitForSeconds(videoStartDelay);
        
        // Start prologue videos (they will handle their own Enter key logic)
        if (prologuePlayer != null)
            prologuePlayer.StartPrologue();
        
        isSequenceActive = false;
        
        // Disable this script after sequence completes to prevent any further input
        this.enabled = false;
    }
    
    System.Collections.IEnumerator AnimateNarratorEntry()
    {
        float elapsedTime = 0f;
        float nextToggleTime = 0f;
        bool useSprite1 = true; // Start with sprite1
        
        while (elapsedTime < movementDuration)
        {
            // Toggle sprites continuously
            if (elapsedTime >= nextToggleTime)
            {
                narratorImage.sprite = useSprite1 ? sprite1 : sprite2;
                useSprite1 = !useSprite1; // Toggle for next time
                nextToggleTime += spriteToggleSpeed;
            }
            
            // Handle movement from off-canvas to on-canvas
            float t = elapsedTime / movementDuration;
            narratorImage.transform.localPosition = Vector3.Lerp(initialPosition, finalPosition, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final position and sprite
        narratorImage.transform.localPosition = finalPosition;
        narratorImage.sprite = sprite1; // Always end with sprite1
    }
}
