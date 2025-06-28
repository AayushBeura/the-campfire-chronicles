using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCreationPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject characterPanel;
    public TMP_InputField nameInputField;
    public Button submitButton;
    
    [Header("Characteristic Buttons")]
    public Button[] personalityButtons;
    public Button[] backgroundButtons;
    public Button[] skillsButtons;
    public Button[] goalsButtons;
    
    [Header("Button Sprites")]
    public Sprite normalButtonSprite;   // Your normal button sprite
    public Sprite selectedButtonSprite; // Your selected button sprite
    
    [Header("API Integration")]
    public StoryGenerator storyGenerator;
    
    private GameCharacterData characterData;
    private List<string> selectedPersonalities = new List<string>();
    private List<string> selectedBackgrounds = new List<string>();
    private List<string> selectedSkills = new List<string>();
    private List<string> selectedGoals = new List<string>();
    
    void Start()
    {
        if (characterPanel != null)
            characterPanel.SetActive(false);
        
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitCharacter);
        
        InitializeButtons();
        SetupButtonSprites(personalityButtons);
        SetupButtonSprites(backgroundButtons);
        SetupButtonSprites(skillsButtons);
        SetupButtonSprites(goalsButtons);
    }
    
    void SetupButtonSprites(Button[] buttons)
    {
        foreach (Button button in buttons)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null && normalButtonSprite != null)
            {
                buttonImage.sprite = normalButtonSprite;
                buttonImage.color = Color.white; // Make sure it's visible
            }
            
            // Keep text visible
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.color = Color.white; // Keep text visible
            }
        }
    }
    
    void InitializeButtons()
    {
        // Using your optimized 4x4 grid
        string[] personalities = {"Brave", "Cunning", "Kind", "Mystic"};
        SetupButtonCategory(personalityButtons, personalities, TogglePersonalitySelection);
        
        string[] backgrounds = {"Noble", "Outlaw", "Scholar", "Warrior"};
        SetupButtonCategory(backgroundButtons, backgrounds, ToggleBackgroundSelection);
        
        string[] skills = {"Magic", "Fight", "Stealth", "Heal"};
        SetupButtonCategory(skillsButtons, skills, ToggleSkillsSelection);
        
        string[] goals = {"Adventure", "Truth", "Protect", "Power"};
        SetupButtonCategory(goalsButtons, goals, ToggleGoalsSelection);
    }
    
    void SetupButtonCategory(Button[] buttons, string[] options, System.Action<string, Button> onToggle)
    {
        for (int i = 0; i < buttons.Length && i < options.Length; i++)
        {
            int index = i;
            Button currentButton = buttons[i];
            
            // Set button text (if using text)
            TextMeshProUGUI buttonText = currentButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = options[i];
            
            // Add click listener for toggle behavior
            currentButton.onClick.AddListener(() => {
                onToggle(options[index], currentButton);
            });
        }
    }
    
    void TogglePersonalitySelection(string personality, Button button)
    {
        ToggleSelection(personality, button, selectedPersonalities);
    }
    
    void ToggleBackgroundSelection(string background, Button button)
    {
        ToggleSelection(background, button, selectedBackgrounds);
    }
    
    void ToggleSkillsSelection(string skills, Button button)
    {
        ToggleSelection(skills, button, selectedSkills);
    }
    
    void ToggleGoalsSelection(string goals, Button button)
    {
        ToggleSelection(goals, button, selectedGoals);
    }
    
    void ToggleSelection(string value, Button button, List<string> selectionList)
    {
        Image buttonImage = button.GetComponent<Image>();
        
        if (selectionList.Contains(value))
        {
            // Deselect: Remove from list and change to normal sprite
            selectionList.Remove(value);
            if (buttonImage != null && normalButtonSprite != null)
            {
                buttonImage.sprite = normalButtonSprite;
                buttonImage.color = Color.white; // Make visible
            }
            Debug.Log($"Deselected: {value}");
        }
        else
        {
            // Select: Add to list and change to selected sprite
            selectionList.Add(value);
            if (buttonImage != null && selectedButtonSprite != null)
            {
                buttonImage.sprite = selectedButtonSprite;
                buttonImage.color = Color.white; // Make visible
            }
            Debug.Log($"Selected: {value}");
        }
        
        // Debug current selections
        Debug.Log($"Current selections in category: {string.Join(", ", selectionList)}");
    }
    
    public void ShowPanel()
    {
        if (characterPanel != null)
            characterPanel.SetActive(true);
    }
    
    public void HidePanel()
    {
        if (characterPanel != null)
            characterPanel.SetActive(false);
    }
    
    void OnSubmitCharacter()
    {
        // Validate that at least one option is selected in each category
        if (string.IsNullOrEmpty(nameInputField.text) ||
            selectedPersonalities.Count == 0 ||
            selectedBackgrounds.Count == 0 ||
            selectedSkills.Count == 0 ||
            selectedGoals.Count == 0)
        {
            Debug.LogWarning("Please complete all character details! Select at least one option from each category.");
            return;
        }
        
        // Create character data with multiple selections
        characterData = new GameCharacterData
        {
            name = nameInputField.text,
            personality = string.Join(", ", selectedPersonalities),
            background = string.Join(", ", selectedBackgrounds),
            skills = string.Join(", ", selectedSkills),
            goals = string.Join(", ", selectedGoals)
        };
        
        Debug.Log($"Character Created: {characterData.name}");
        Debug.Log($"Personalities: {characterData.personality}");
        Debug.Log($"Backgrounds: {characterData.background}");
        Debug.Log($"Skills: {characterData.skills}");
        Debug.Log($"Goals: {characterData.goals}");
        
        // Hide panel and start story generation
        HidePanel();
        
        // THIS IS THE MISSING PART - Call StoryGenerator
    Debug.Log("About to call StoryGenerator");
    if (storyGenerator != null)
    {
        Debug.Log("StoryGenerator found, calling GenerateStory");
        storyGenerator.GenerateStory(characterData);
    }
    else
    {
        Debug.LogError("StoryGenerator reference is NULL! Please assign it in the Inspector.");
    }
    }
}
