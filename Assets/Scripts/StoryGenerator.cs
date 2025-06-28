using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class StoryGenerator : MonoBehaviour
{
    [Header("Loading Screen")]
    public PixelatedLoadingScreen loadingScreen;

    [Header("Video System")]
    public UnskippableVideoPlayer unskippableVideo;
    public GameObject speechCloud;

    [Header("Chapter System")]
    public ChapterTransition chapterTransition;

    [Header("Story Settings")]
    public int maxWordsPerSegment = 40;
    public int segmentsPerChapter = 6;

    [Header("Story Display")]
    public StoryDisplayManager storyDisplay;

    [Header("API Delays")]
    public float geminiDelay = 3f;      // 3 second delay before Gemini call
    public float murfDelay = 2f;        // 2 second delay between Murf calls

    // FIXED: API key management
    private string currentGeminiApiKey = "";
    private string currentMurfApiKey = "";
    private bool apiKeysReady = false;

    private List<string> storySegments = new List<string>();
    private List<List<AudioClip>> chapterAudioSegments = new List<List<AudioClip>>();
    private GameCharacterData currentCharacter;
    private string coreStory;

    // Cooldown management
    private static float lastMurfAPICall = 0f;
    private const float MURF_API_COOLDOWN = 5f;

    void Start()
    {
        LoadCoreStory();
        InitializeChapterAudio();
        EnsureStreamingAssetsDirectories();

        // FIXED: Wait for API keys before starting story generation
        StartCoroutine(InitializeWithAPIKeys());
    }

    IEnumerator InitializeWithAPIKeys()
    {
        Debug.Log("Waiting for API keys to be ready...");

        // Wait for GoogleSheetsKeyManager to load keys
        if (GoogleSheetsKeyManager.Instance != null)
        {
            // Wait until keys are loaded
            while (!GoogleSheetsKeyManager.Instance.AreKeysLoaded())
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Get the keys
            UpdateAPIKeys();

            // Check if we have valid keys
            if (HasValidAPIKeys())
            {
                apiKeysReady = true;
                Debug.Log("API keys ready - Story generation can begin");
            }
            else
            {
                Debug.LogWarning("No valid API keys available! Will use fallback story generation.");
                apiKeysReady = false;
            }
        }
        else
        {
            Debug.LogError("GoogleSheetsKeyManager not found!");
            apiKeysReady = false;
        }
    }

    // FIXED: Method to update API keys from GoogleSheetsKeyManager
    void UpdateAPIKeys()
    {
        if (GoogleSheetsKeyManager.Instance != null)
        {
            currentGeminiApiKey = GoogleSheetsKeyManager.Instance.GetGeminiApiKey();
            currentMurfApiKey = GoogleSheetsKeyManager.Instance.GetMurfApiKey();

            Debug.Log($"Updated API keys - Gemini: {(!string.IsNullOrEmpty(currentGeminiApiKey) ? "✓" : "✗")}, Murf: {(!string.IsNullOrEmpty(currentMurfApiKey) ? "✓" : "✗")}");
        }
    }

    // FIXED: Check if we have valid API keys
    bool HasValidAPIKeys()
    {
        return !string.IsNullOrEmpty(currentGeminiApiKey) && !string.IsNullOrEmpty(currentMurfApiKey);
    }

    // FIXED: Subscribe to key updates
    void OnEnable()
    {
        if (GoogleSheetsKeyManager.Instance != null)
        {
            GoogleSheetsKeyManager.Instance.OnKeysLoaded += OnAPIKeysUpdated;
        }
    }

    void OnDisable()
    {
        if (GoogleSheetsKeyManager.Instance != null)
        {
            GoogleSheetsKeyManager.Instance.OnKeysLoaded -= OnAPIKeysUpdated;
        }
    }

    // FIXED: Called when API keys are updated (from settings or Google Sheets)
    void OnAPIKeysUpdated()
    {
        Debug.Log("API keys updated - refreshing StoryGenerator keys");
        UpdateAPIKeys();

        if (HasValidAPIKeys())
        {
            apiKeysReady = true;
            Debug.Log("StoryGenerator now has valid API keys");
        }
        else
        {
            apiKeysReady = false;
            Debug.LogWarning("StoryGenerator lost valid API keys");
        }
    }

    void EnsureStreamingAssetsDirectories()
    {
#if UNITY_EDITOR
        string streamingAssetsPath = Application.streamingAssetsPath;
        if (!System.IO.Directory.Exists(streamingAssetsPath))
        {
            System.IO.Directory.CreateDirectory(streamingAssetsPath);
        }

        string generatedAudioPath = System.IO.Path.Combine(streamingAssetsPath, "GeneratedAudio");
        if (!System.IO.Directory.Exists(generatedAudioPath))
        {
            System.IO.Directory.CreateDirectory(generatedAudioPath);
        }

        string base64TextPath = System.IO.Path.Combine(streamingAssetsPath, "Base64Text");
        if (!System.IO.Directory.Exists(base64TextPath))
        {
            System.IO.Directory.CreateDirectory(base64TextPath);
        }
#endif
    }

    void LoadCoreStory()
    {
        TextAsset storyAsset = Resources.Load<TextAsset>("CoreStory");
        if (storyAsset != null)
        {
            coreStory = storyAsset.text;
            Debug.Log("Core story loaded successfully");
        }
        else
        {
            Debug.LogError("Could not load CoreStory.txt from Resources folder");
        }
    }

    void InitializeChapterAudio()
    {
        for (int i = 0; i < 5; i++)
        {
            chapterAudioSegments.Add(new List<AudioClip>());
        }
    }

    public void GenerateStory(GameCharacterData character)
    {
        Debug.Log("Start button pressed - Beginning story generation");
        currentCharacter = character;
        StartCoroutine(CompleteStoryFlow());
    }

    IEnumerator CompleteStoryFlow()
    {
        if (loadingScreen != null)
        {
            loadingScreen.ShowLoadingScreen();
            yield return new WaitForSeconds(loadingScreen.fadeDuration + 0.1f);
        }

        // FIXED: Check API keys before proceeding
        if (!apiKeysReady)
        {
            Debug.LogWarning("API keys not ready, waiting...");
            yield return StartCoroutine(InitializeWithAPIKeys());
        }

        // Add delay before calling Gemini
        loadingScreen?.UpdateProgress(0.1f, "Preparing story generation...");
        yield return new WaitForSeconds(geminiDelay);

        // FIXED: Only call Gemini if we have valid keys, otherwise use fallback
        if (HasValidAPIKeys())
        {
            yield return StartCoroutine(CallGeminiAPIWithRetry());
        }
        else
        {
            Debug.LogWarning("No valid API keys - using complete fallback story");
            CreateCompleteFallbackStory();
        }

        float loadingStartTime = Time.time;
        while (Time.time - loadingStartTime < 10f)
        {
            float progress = (Time.time - loadingStartTime) / 10f;
            loadingScreen?.UpdateProgress(progress * 0.8f, "Weaving your tale...");
            yield return null;
        }

        loadingScreen?.UpdateProgress(1.0f, "Story ready!");
        yield return new WaitForSeconds(1f);

        if (loadingScreen != null)
            loadingScreen.HideLoadingScreen();

        yield return StartCoroutine(PostLoadingSequence());
    }

    IEnumerator CallGeminiAPIWithRetry()
    {
        Debug.Log("Calling Gemini 2.5 Flash API with retry capability...");
        bool success = false;
        int retryCount = 0;
        const int maxRetries = 3;

        while (!success && retryCount < maxRetries)
        {
            if (retryCount > 0)
            {
                Debug.Log($"Retry attempt {retryCount}/{maxRetries}");
                yield return new WaitForSeconds(5f);
            }

            string prompt = CreateEnhancedStoryPrompt(currentCharacter);

            // FIXED: Use current API key from GoogleSheetsKeyManager
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={currentGeminiApiKey}";

            var requestData = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 4000,
                    temperature = 0.6f,
                    topP = 0.9f,
                    topK = 40,
                    candidateCount = 1
                },
                safetySettings = new[]
                {
                    new
                    {
                        category = "HARM_CATEGORY_HARASSMENT",
                        threshold = "BLOCK_MEDIUM_AND_ABOVE"
                    },
                    new
                    {
                        category = "HARM_CATEGORY_HATE_SPEECH",
                        threshold = "BLOCK_MEDIUM_AND_ABOVE"
                    },
                    new
                    {
                        category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                        threshold = "BLOCK_MEDIUM_AND_ABOVE"
                    },
                    new
                    {
                        category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                        threshold = "BLOCK_MEDIUM_AND_ABOVE"
                    }
                }
            };

            string jsonData = JsonConvert.SerializeObject(requestData);
            Debug.Log($"Sending Gemini 2.5 Flash request (attempt {retryCount + 1})");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 120;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Gemini 2.5 Flash API Success");
                    ProcessGeminiResponse(request.downloadHandler.text);
                    Debug.Log("Gemini response received - Starting Murf processing with delay");

                    // FIXED: Only start Murf processing if we have Murf API key
                    if (!string.IsNullOrEmpty(currentMurfApiKey))
                    {
                        yield return new WaitForSeconds(murfDelay);
                        StartCoroutine(ProcessMurfForChapter(1));
                    }
                    else
                    {
                        Debug.LogWarning("No Murf API key - story will play without audio");
                    }

                    success = true;

                    if (loadingScreen != null)
                        loadingScreen.HideNetworkError();
                }
                else
                {
                    retryCount++;
                    Debug.LogError($"Gemini 2.5 Flash API Error (attempt {retryCount}): {request.error}");
                    Debug.LogError($"Response Code: {request.responseCode}");
                    Debug.LogError($"Response: {request.downloadHandler.text}");

                    if (retryCount >= maxRetries)
                    {
                        Debug.LogWarning("Gemini API failed - using complete fallback story");
                        CreateCompleteFallbackStory();
                        success = true; // Continue with fallback
                    }
                }
            }
        }
    }

    string CreateEnhancedStoryPrompt(GameCharacterData character)
    {
        int totalWords = segmentsPerChapter * 5 * maxWordsPerSegment;
        int totalSegments = segmentsPerChapter * 5;

        return $"**PERSONA**: You are Gemini 2.5 Flash, an advanced reasoning AI and master storyteller.\n\n" +
               $"**TASK**: Create a personalized narrative adaptation using advanced reasoning and storytelling capabilities.\n\n" +
               $"**CONTEXT**: \n" +
               $"Core Story: {coreStory}\n\n" +
               $"Character Profile:\n" +
               $"- Name: {character.name}\n" +
               $"- Personality: {character.personality}\n" +
               $"- Background: {character.background}\n" +
               $"- Skills: {character.skills}\n" +
               $"- Goals: {character.goals}\n\n" +
               $"**FORMAT**: Write exactly {totalWords} words in {totalSegments} segments of precisely {maxWordsPerSegment} words each.\n\n" +
               $"**REASONING INSTRUCTIONS**:\n" +
               $"1. Think deeply about the emotional journey across five life stages\n" +
               $"2. Plan the gradual revelation of the narrator's identity\n" +
               $"3. Consider how each segment builds narrative tension\n" +
               $"4. Ensure each segment ends with complete thoughts\n" +
               $"5. Connect the character's traits to universal life lessons\n" +
               $"6. Don't show texts like PART ONE, or CHAPTER ONE, directly start with its content\n\n" +
               $"**STORY STRUCTURE** (5 chapters, {segmentsPerChapter} segments each):\n" +
               $"- **Chapter 1**: Childhood fears and first courage (use 'Elya Fen')\n" +
               $"- **Chapter 2**: Youth's doubt and finding hope (use 'Elya Fen')\n" +
               $"- **Chapter 3**: Adult hardening and rediscovering compassion (use 'Elya Fen')\n" +
               $"- **Chapter 4**: Mature wisdom and integration (use 'Elya Fen')\n" +
               $"- **Chapter 5**: Narrator reveals identity as original Elya Fen, connects to {character.name}'s journey\n\n" +
               $"**CRITICAL REQUIREMENTS**:\n" +
               $"- Each segment must be exactly {maxWordsPerSegment} words\n" +
               $"- No abrupt endings like 'They flew a' or 'He ran after The'\n" +
               $"- No filler phrases like 'time moves forward' or 'journey continues'\n" +
               $"- Focus on specific moments of growth and decision-making\n" +
               $"- Maintain mystical atmosphere while being psychologically grounded\n" +
               $"- Use wise, reflective narrative voice throughout\n" +
               $"- Only reveal narrator is Elya Fen in Chapter 5 using first person\n\n" +
               $"**OUTPUT**: Write {totalSegments} consecutive paragraphs, each exactly {maxWordsPerSegment} words, with no headers or additional text or segment headers like STAGE ONE : THE FRIGHTENED CHILD or so.";
    }

    void ProcessGeminiResponse(string response)
    {
        try
        {
            Debug.Log($"Processing Gemini 2.5 Flash response (length: {response.Length})");

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("Empty response from Gemini 2.5 Flash API");
                CreateCompleteFallbackStory();
                return;
            }

            var responseData = JsonConvert.DeserializeObject<GeminiResponse>(response);

            if (responseData?.candidates == null || responseData.candidates.Length == 0)
            {
                Debug.LogError("No candidates in Gemini 2.5 Flash response");
                Debug.LogError($"Raw response: {response}");
                CreateCompleteFallbackStory();
                return;
            }

            if (responseData.candidates[0]?.content?.parts == null || responseData.candidates[0].content.parts.Length == 0)
            {
                Debug.LogError("No content parts in Gemini 2.5 Flash response");
                CreateCompleteFallbackStory();
                return;
            }

            string storyText = responseData.candidates[0].content.parts[0].text;

            if (string.IsNullOrEmpty(storyText))
            {
                Debug.LogError("Empty story text from Gemini 2.5 Flash");
                CreateCompleteFallbackStory();
                return;
            }

            Debug.Log($"Story text received from Gemini 2.5 Flash: {storyText.Substring(0, Mathf.Min(200, storyText.Length))}...");

            storySegments = SplitStoryIntoSegments(storyText);
            Debug.Log($"Story processed into {storySegments.Count} segments using Gemini 2.5 Flash");
        }
        catch (JsonException jsonEx)
        {
            Debug.LogError($"JSON parsing error with Gemini 2.5 Flash response: {jsonEx.Message}");
            Debug.LogError($"Response: {response}");
            CreateCompleteFallbackStory();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing Gemini 2.5 Flash response: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            CreateCompleteFallbackStory();
        }
    }

    // FIXED: Complete fallback story that tells the full narrative
    void CreateCompleteFallbackStory()
    {
        Debug.LogWarning("Creating complete fallback story based on CoreStory.txt");

        storySegments = new List<string>();

        // Chapter 1: The Frightened Child
        storySegments.AddRange(GetFallbackTemplate(1));

        // Chapter 2: The Doubting Youth  
        storySegments.AddRange(GetFallbackTemplate(2));

        // Chapter 3: The Hardened Warrior
        storySegments.AddRange(GetFallbackTemplate(3));

        // Chapter 4: The Wise Adult
        storySegments.AddRange(GetFallbackTemplate(4));

        // Chapter 5: The Eternal Teacher
        storySegments.AddRange(GetFallbackTemplate(5));

        Debug.Log($"Complete fallback story created with {storySegments.Count} segments");
    }

    // FIXED: Complete fallback template that tells the full story
    private List<string> GetFallbackTemplate(int chapterNumber)
    {
        List<string> segments = new List<string>();

        switch (chapterNumber)
        {
            case 1: // THE FRIGHTENED CHILD
                segments.Add("I have watched this story unfold countless times. A city dies. A child carries light. The cycle repeats. But this time feels different. This bearer burns brighter than the others who came before.");
                segments.Add("In Varrowind, when darkness comes, someone must carry the Fifth Lantern. They always have the same face. The same eyes. The same trembling hands that refuse to let go of hope when all seems lost.");
                segments.Add("The first trial comes in childhood. When the world demands courage you don't possess. When shadows whisper that you are too small, too weak, too afraid to matter in the grand design of fate.");
                segments.Add($"The child {currentCharacter.name} faced the dying of the First Lantern. Faced the mist that devours everything. The cold that freezes souls. The darkness that makes heroes into forgotten names carved on broken stones.");
                segments.Add("But instead of running, he whispered to the light: 'You're warm.' That simple truth, spoken in terror, became the foundation of everything that would follow. A moment of acceptance over fear.");
                segments.Add("That moment of connection, of choosing warmth over cold, light over shadow, hope over despair - that is how a soul begins to grow. How courage is born from the ashes of terror.");
                break;

            case 2: // THE DOUBTING YOUTH
                segments.Add("The second trial comes with prophecy. When you can see all possible futures and they all end in failure. When knowledge becomes a burden heavier than ignorance ever was.");
                segments.Add($"{currentCharacter.name} descended into the Clockwell, where time fractures into regrets. Where every choice shows its consequences. Where the weight of destiny presses down like the ocean depths.");
                segments.Add("He saw himself drowning in visions of defeat. Saw every path leading to ruin. Saw the city burning, the lanterns extinguished, the people he loved becoming shadows in the endless night.");
                segments.Add("The weight of prophecy pressed down like stone. Every vision showed the same ending - darkness consuming all. Hope became a luxury he could no longer afford to believe in.");
                segments.Add("But instead of choosing despair, he chose to walk forward anyway. Not because he knew he would succeed, but because standing still was not living. Movement was defiance against fate.");
                segments.Add("That moment of faith despite doubt, of action despite certainty of failure - that is how a soul learns hope. How destiny bends to will rather than will bending to destiny.");
                break;

            case 3: // THE HARDENED WARRIOR
                segments.Add("The third trial comes with victory. When you win by becoming something cold and unbreakable. When strength becomes a prison built from the bones of your former self.");
                segments.Add($"The warrior {currentCharacter.name} climbed the Saltspire, where hope becomes mere obligation. Where duty replaces joy. Where efficiency matters more than the heart that drives the hand.");
                segments.Add("He found himself armored in salt and silence, holding the line but forgetting why it mattered. Victory after victory, each one carving away another piece of who he used to be.");
                segments.Add("The lantern's light grew steady but cold. Efficient. Merciless. A tool of war rather than a beacon of hope. Power without purpose, strength without soul, victory without meaning.");
                segments.Add("But instead of accepting that hollow victory, he remembered his name. Remembered the child who whispered to light. Remembered that strength without compassion is just another kind of failure.");
                segments.Add("That moment of reclaiming softness in hardness, of finding humanity in efficiency, of choosing love over mere duty - that is how a soul finds balance between power and purpose.");
                break;

            case 4: // THE WISE ADULT
                segments.Add("The fourth trial comes with understanding. When you gather all the broken pieces of who you were. When wisdom means accepting every failure as a necessary step toward truth.");
                segments.Add($"{currentCharacter.name} walked to the Hollow Crown carrying not just light, but all the versions of himself that had failed before. The frightened child, the despairing youth, the hollow warrior.");
                segments.Add("Each failure whispered its lessons. Each scar told its story. Each moment of weakness became a foundation for strength. Integration meant accepting all parts of the journey as sacred.");
                segments.Add("Instead of being crushed by their weight, he embraced them. Made their failures into wisdom. Their pain into strength. Their regrets into the very foundation of his compassion.");
                segments.Add("The Fifth Lantern blazed not with magic, but with memory. Not with power, but with understanding. Not with perfection, but with the beautiful acceptance of imperfection made whole.");
                segments.Add("That moment of integration, of wholeness from brokenness, of wisdom from folly - that is how a soul becomes complete. How all the scattered pieces finally find their home.");
                break;

            case 5: // THE ETERNAL TEACHER
                segments.Add("The final trial is not for the bearer, but for the one who has watched it all. When I step forward from the shadows, the teacher revealed at last.");
                segments.Add($"When {currentCharacter.name} reached the heart of darkness, he was not alone. I was there, as I have always been. The first Elya Fen, the original bearer, the eternal witness.");
                segments.Add("'Every story was preparation for this moment,' I told him. 'Every failure taught courage. Every despair taught hope. Every hollow victory taught the value of love over mere power.'");
                segments.Add("The cycle finally breaks when someone succeeds where all others failed. Not through perfection, but through the courage to be imperfect. Not through strength, but through the wisdom of vulnerability.");
                segments.Add("The Fifth Lantern burns not with magic, but with memory. And memory, when finally understood, becomes wisdom. When wisdom is shared, it becomes love. When love is given freely, it becomes eternal.");
                segments.Add($"This is the story of how one soul, {currentCharacter.name}, finally learned to be whole. How the cycle of suffering became the cycle of growth. How the end became the beginning.");
                break;
        }

        return segments;
    }

    List<string> SplitStoryIntoSegments(string storyText)
    {
        List<string> segments = new List<string>();

        string[] paragraphs = storyText.Split(new string[] { "\n\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        if (paragraphs.Length >= 30)
        {
            for (int i = 0; i < 30 && i < paragraphs.Length; i++)
            {
                string cleanParagraph = paragraphs[i].Trim();
                segments.Add(ValidateSegmentWordCount(cleanParagraph));
            }
        }
        else
        {
            string[] sentences = storyText.Split(new char[] { '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);

            string currentSegment = "";
            int currentWordCount = 0;

            foreach (string sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence)) continue;

                string cleanSentence = sentence.Trim() + ".";
                int sentenceWordCount = CountWords(cleanSentence);

                if (currentWordCount + sentenceWordCount > maxWordsPerSegment && !string.IsNullOrEmpty(currentSegment))
                {
                    segments.Add(ValidateSegmentWordCount(currentSegment.Trim()));
                    currentSegment = cleanSentence + " ";
                    currentWordCount = sentenceWordCount;
                }
                else
                {
                    currentSegment += cleanSentence + " ";
                    currentWordCount += sentenceWordCount;
                }

                if (segments.Count >= 30) break;
            }

            if (!string.IsNullOrEmpty(currentSegment) && segments.Count < 30)
            {
                segments.Add(ValidateSegmentWordCount(currentSegment.Trim()));
            }
        }

        while (segments.Count < 30)
        {
            segments.Add(GetFallbackTemplate((segments.Count / segmentsPerChapter) + 1)[segments.Count % segmentsPerChapter]);
        }

        if (segments.Count > 30)
        {
            segments = segments.GetRange(0, 30);
        }

        LogSegmentWordCounts(segments);

        return segments;
    }

    string FixAbruptEnding(string segment)
    {
        segment = segment.Trim();

        if (string.IsNullOrEmpty(segment)) return segment;

        HashSet<string> abruptWords = new HashSet<string>
        {
            "a", "an", "the", "and", "but", "or", "so", "because", "if", "then",
            "when", "while", "as", "at", "by", "for", "in", "of", "on", "to",
            "up", "with", "after", "before", "during", "since", "until", "into",
            "onto", "upon", "within", "without", "through", "across", "behind",
            "beneath", "beside", "between", "beyond", "near", "under", "over"
        };

        string[] words = segment.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0) return segment;

        string lastWord = words[words.Length - 1].ToLower();
        lastWord = System.Text.RegularExpressions.Regex.Replace(lastWord, @"[^\w]", "");

        if (abruptWords.Contains(lastWord))
        {
            List<string> wordList = new List<string>(words);
            wordList.RemoveAt(wordList.Count - 1);

            if (wordList.Count > 0)
            {
                segment = string.Join(" ", wordList);
            }
            else
            {
                segment = "The story continues";
            }
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(segment, @"[.!?]$"))
        {
            segment += ".";
        }

        return FixCommonGrammarIssues(segment);
    }

    string FixCommonGrammarIssues(string text)
    {
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        text = text.Replace(" ,", ",");
        text = text.Replace(" .", ".");
        text = text.Replace(" !", "!");
        text = text.Replace(" ?", "?");

        text = System.Text.RegularExpressions.Regex.Replace(text, @"^(\w)", m => m.Value.ToUpper());
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(\. )(\w)", m => m.Groups[1].Value + m.Groups[2].Value.ToUpper());

        text = System.Text.RegularExpressions.Regex.Replace(text, @"\bi\b", "I");

        return text.Trim();
    }

    int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        string[] words = text.Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    string ValidateSegmentWordCount(string segment)
    {
        segment = FixAbruptEnding(segment);

        int wordCount = CountWords(segment);

        if (wordCount > maxWordsPerSegment)
        {
            string[] words = segment.Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            string trimmedSegment = string.Join(" ", words, 0, maxWordsPerSegment);

            trimmedSegment = FixAbruptEnding(trimmedSegment);

            return trimmedSegment;
        }
        else if (wordCount < maxWordsPerSegment - 8)
        {
            string[] words = segment.Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            List<string> wordList = new List<string>(words);

            string[] fillerPhrases = {
                "and the journey continued",
                "as destiny would have it",
                "with purpose in every step",
                "through the mystical realm",
                "guided by ancient wisdom",
                "toward an uncertain future"
            };

            int fillerIndex = 0;

            while (wordList.Count < maxWordsPerSegment - 2 && fillerIndex < fillerPhrases.Length)
            {
                string[] fillerWords = fillerPhrases[fillerIndex].Split(' ');
                foreach (string word in fillerWords)
                {
                    if (wordList.Count < maxWordsPerSegment - 1)
                        wordList.Add(word);
                }
                fillerIndex++;
            }

            string result = string.Join(" ", wordList);
            return FixAbruptEnding(result);
        }

        return segment;
    }

    void LogSegmentWordCounts(List<string> segments)
    {
        Debug.Log($"Generated {segments.Count} segments with word counts:");
        for (int i = 0; i < segments.Count; i++)
        {
            int wordCount = CountWords(segments[i]);
            Debug.Log($"Segment {i + 1}: {wordCount} words - {segments[i].Substring(0, Mathf.Min(50, segments[i].Length))}...");
        }

        int totalWords = 0;
        foreach (string segment in segments)
        {
            totalWords += CountWords(segment);
        }
        Debug.Log($"Total story word count: {totalWords} words (Target: {segmentsPerChapter * 5 * maxWordsPerSegment})");
    }

    IEnumerator ProcessMurfForChapter(int chapterNumber)
    {
        // FIXED: Check if we have Murf API key before processing
        if (string.IsNullOrEmpty(currentMurfApiKey))
        {
            Debug.LogWarning($"No Murf API key available - Chapter {chapterNumber} will play without audio");
            yield break;
        }

        Debug.Log($"Starting Murf processing for Chapter {chapterNumber} with delays");

        int startIndex = (chapterNumber - 1) * segmentsPerChapter;
        int endIndex = Mathf.Min(startIndex + segmentsPerChapter, storySegments.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            yield return StartCoroutine(GenerateTTSForSegment(storySegments[i], i, chapterNumber));

            if (i < endIndex - 1)
            {
                Debug.Log($"Waiting {murfDelay} seconds before next Murf API call...");
                yield return new WaitForSeconds(murfDelay);
            }
        }

        Debug.Log($"Murf processing complete for Chapter {chapterNumber}");
    }

    IEnumerator GenerateTTSForSegment(string text, int segmentIndex, int chapterNumber)
    {
        yield return StartCoroutine(CallMurfAPI(text, segmentIndex, chapterNumber));
    }

    IEnumerator CallMurfAPI(string text, int segmentIndex, int chapterNumber)
    {
        // FIXED: Check if we have Murf API key
        if (string.IsNullOrEmpty(currentMurfApiKey))
        {
            Debug.LogWarning($"No Murf API key - creating placeholder audio for segment {segmentIndex}");
            CreatePlaceholderAudio(segmentIndex, chapterNumber);
            yield break;
        }

        float timeSinceLastCall = Time.time - lastMurfAPICall;
        if (timeSinceLastCall < MURF_API_COOLDOWN)
        {
            float waitTime = MURF_API_COOLDOWN - timeSinceLastCall;
            Debug.Log($"Murf API cooldown active. Waiting {waitTime:F1} seconds...");
            yield return new WaitForSeconds(waitTime);
        }

        lastMurfAPICall = Time.time;

        string url = "https://api.murf.ai/v1/speech/generate-with-key";

        var requestData = new
        {
            voiceId = "pt-BR-benício",
            style = "Conversational",
            pitch = -14,
            rate = -14,
            text = text,
            format = "MP3",
            sampleRate = 44100,
            encodeAsBase64 = true,
            modelVersion = "GEN2",
            multiNativeLocale = "en-US"
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        UnityWebRequest request = null;
        bool success = false;
        string responseText = "";

        request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("api-key", currentMurfApiKey); // FIXED: Use current API key
        request.SetRequestHeader("Accept", "application/json");
        request.timeout = 60;

        Debug.Log($"Calling Murf API for segment {segmentIndex}: {text.Substring(0, Mathf.Min(50, text.Length))}...");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            responseText = request.downloadHandler.text;
            Debug.Log($"Raw Murf Response for segment {segmentIndex}: {responseText.Substring(0, Mathf.Min(200, responseText.Length))}...");

            var response = JsonConvert.DeserializeObject<MurfResponse>(responseText);

            if (response != null)
            {
                if (!string.IsNullOrEmpty(response.encodedAudio))
                {
                    success = true;
                    Debug.Log($"Valid Base64 audio received for segment {segmentIndex}");

                    yield return StartCoroutine(SaveAudioFiles(response.encodedAudio, segmentIndex));
                    yield return StartCoroutine(ProcessBase64Audio(response.encodedAudio, segmentIndex, chapterNumber));
                }
                else if (!string.IsNullOrEmpty(response.audioFile))
                {
                    success = true;
                    Debug.Log($"Valid audio URL received for segment {segmentIndex}");
                    yield return StartCoroutine(DownloadAudioFromUrl(response.audioFile, segmentIndex, chapterNumber));
                }
                else
                {
                    Debug.LogError($"Murf response missing audio data for segment {segmentIndex}");
                    Debug.LogError($"Response structure: audioFile={response.audioFile != null}, encodedAudio={response.encodedAudio != null}");
                }
            }
            else
            {
                Debug.LogError($"Failed to deserialize Murf response for segment {segmentIndex}");
            }
        }
        else
        {
            Debug.LogError($"Murf API Error for segment {segmentIndex}: {request.error}");
            Debug.LogError($"Response Code: {request.responseCode}");
            Debug.LogError($"Response Text: {request.downloadHandler.text}");
        }

        if (request != null)
            request.Dispose();

        if (!success)
        {
            Debug.LogWarning($"Creating placeholder audio for segment {segmentIndex}");
            CreatePlaceholderAudio(segmentIndex, chapterNumber);
        }
    }

    IEnumerator SaveAudioFiles(string base64Audio, int segmentIndex)
    {
        string persistentAudioPath = System.IO.Path.Combine(Application.persistentDataPath, "GeneratedAudio");
        string persistentBase64Path = System.IO.Path.Combine(Application.persistentDataPath, "Base64Text");

        if (!System.IO.Directory.Exists(persistentAudioPath))
            System.IO.Directory.CreateDirectory(persistentAudioPath);

        if (!System.IO.Directory.Exists(persistentBase64Path))
            System.IO.Directory.CreateDirectory(persistentBase64Path);

        try
        {
            string base64FilePath = System.IO.Path.Combine(persistentBase64Path, $"Base64Audio_Segment_{segmentIndex}.txt");
            System.IO.File.WriteAllText(base64FilePath, base64Audio);
            Debug.Log($"Base64 text saved to: {base64FilePath}");

            byte[] audioBytes = System.Convert.FromBase64String(base64Audio);
            string audioFilePath = System.IO.Path.Combine(persistentAudioPath, $"segment_{segmentIndex}.mp3");
            System.IO.File.WriteAllBytes(audioFilePath, audioBytes);
            Debug.Log($"Audio file saved to: {audioFilePath}");

#if UNITY_EDITOR
            string streamingAudioPath = System.IO.Path.Combine(Application.streamingAssetsPath, "GeneratedAudio");
            string streamingBase64Path = System.IO.Path.Combine(Application.streamingAssetsPath, "Base64Text");

            if (!System.IO.Directory.Exists(streamingAudioPath))
                System.IO.Directory.CreateDirectory(streamingAudioPath);

            if (!System.IO.Directory.Exists(streamingBase64Path))
                System.IO.Directory.CreateDirectory(streamingBase64Path);

            string streamingBase64File = System.IO.Path.Combine(streamingBase64Path, $"Base64Audio_Segment_{segmentIndex}.txt");
            string streamingAudioFile = System.IO.Path.Combine(streamingAudioPath, $"segment_{segmentIndex}.mp3");

            System.IO.File.WriteAllText(streamingBase64File, base64Audio);
            System.IO.File.WriteAllBytes(streamingAudioFile, audioBytes);

            Debug.Log($"Editor: Also saved to StreamingAssets - Base64: {streamingBase64File}, Audio: {streamingAudioFile}");
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving audio files for segment {segmentIndex}: {ex.Message}");
        }

        yield return null;
    }

    IEnumerator ProcessBase64Audio(string base64Audio, int segmentIndex, int chapterNumber)
    {
        if (string.IsNullOrEmpty(base64Audio))
        {
            Debug.LogError($"Empty Base64 audio data for segment {segmentIndex}");
            CreatePlaceholderAudio(segmentIndex, chapterNumber);
            yield break;
        }

        byte[] audioBytes = null;
        string tempPath = "";
        bool conversionSuccess = false;

        try
        {
            audioBytes = System.Convert.FromBase64String(base64Audio);
            tempPath = System.IO.Path.Combine(Application.temporaryCachePath, $"temp_audio_{segmentIndex}.mp3");
            System.IO.File.WriteAllBytes(tempPath, audioBytes);
            conversionSuccess = true;
            Debug.Log($"Decoded {audioBytes.Length} bytes for segment {segmentIndex}");
            Debug.Log($"Wrote temp audio file to: {tempPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error converting Base64 to audio file for segment {segmentIndex}: {ex.Message}");
            CreatePlaceholderAudio(segmentIndex, chapterNumber);
            yield break;
        }

        if (!conversionSuccess)
        {
            CreatePlaceholderAudio(segmentIndex, chapterNumber);
            yield break;
        }

        string fileUrl = "file://" + tempPath;
        UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.MPEG);

        audioRequest.downloadHandler = new DownloadHandlerAudioClip(fileUrl, AudioType.MPEG);
        ((DownloadHandlerAudioClip)audioRequest.downloadHandler).streamAudio = false;
        audioRequest.timeout = 30;

        yield return audioRequest.SendWebRequest();

        bool audioSuccess = false;
        if (audioRequest.result == UnityWebRequest.Result.Success)
        {
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(audioRequest);
            if (audioClip != null && audioClip.length > 0)
            {
                Debug.Log($"AudioClip created: Length={audioClip.length}s, Samples={audioClip.samples}, Channels={audioClip.channels}, Frequency={audioClip.frequency}");

                audioClip.name = $"MurfSegment_{segmentIndex}";
                StoreAudioClip(audioClip, segmentIndex, chapterNumber);
                Debug.Log($"Successfully processed Base64 audio for segment {segmentIndex} (Duration: {audioClip.length}s)");
                audioSuccess = true;
            }
            else
            {
                Debug.LogError($"Invalid AudioClip created for segment {segmentIndex}: Length={audioClip?.length}, Samples={audioClip?.samples}");
            }
        }
        else
        {
            Debug.LogError($"Failed to load Base64 audio for segment {segmentIndex}: {audioRequest.error}");
        }

        audioRequest.Dispose();

        if (!audioSuccess)
        {
            CreatePlaceholderAudio(segmentIndex, chapterNumber);
        }

        yield return new WaitForSeconds(1f);

        if (System.IO.File.Exists(tempPath))
        {
            try
            {
                System.IO.File.Delete(tempPath);
                Debug.Log($"Cleaned up temp file: {tempPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not delete temp file {tempPath}: {ex.Message}");
            }
        }
    }

    IEnumerator DownloadAudioFromUrl(string audioUrl, int segmentIndex, int chapterNumber)
    {
        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
        {
            yield return audioRequest.SendWebRequest();

            if (audioRequest.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(audioRequest);
                audioClip.name = $"MurfSegment_{segmentIndex}";

                StoreAudioClip(audioClip, segmentIndex, chapterNumber);
                Debug.Log($"Murf TTS downloaded for Chapter {chapterNumber}, Segment {segmentIndex}");
            }
            else
            {
                Debug.LogError($"Failed to download audio for segment {segmentIndex}: {audioRequest.error}");
                CreatePlaceholderAudio(segmentIndex, chapterNumber);
            }
        }
    }

    void CreatePlaceholderAudio(int segmentIndex, int chapterNumber)
    {
        AudioClip placeholderClip = AudioClip.Create($"Chapter{chapterNumber}_Segment_{segmentIndex}", 44100 * 3, 1, 44100, false);
        StoreAudioClip(placeholderClip, segmentIndex, chapterNumber);
    }

    void StoreAudioClip(AudioClip audioClip, int segmentIndex, int chapterNumber)
    {
        int chapterIndex = chapterNumber - 1;
        int segmentInChapter = segmentIndex - (chapterIndex * segmentsPerChapter);

        while (chapterAudioSegments[chapterIndex].Count <= segmentInChapter)
        {
            chapterAudioSegments[chapterIndex].Add(null);
        }

        chapterAudioSegments[chapterIndex][segmentInChapter] = audioClip;
    }

    IEnumerator PostLoadingSequence()
    {
        Debug.Log("Starting post-loading sequence");

        if (speechCloud != null)
        {
            speechCloud.SetActive(true);
            Debug.Log("Speech cloud activated immediately");
        }

        if (unskippableVideo != null)
        {
            yield return StartCoroutine(PlayVideoDirectly());
        }

        yield return StartCoroutine(StartChapter(1));
    }

    IEnumerator PlayVideoDirectly()
    {
        Debug.Log("Playing video directly over speech cloud");

        if (storyDisplay != null)
        {
            storyDisplay.SetVideoPlayingState(true);
        }

        yield return StartCoroutine(unskippableVideo.PlayVideoOnly());
        yield return StartCoroutine(unskippableVideo.ShowContinuePromptOnly());
        yield return StartCoroutine(unskippableVideo.WaitForContinueInputOnly());

        unskippableVideo.HideContinuePrompt();

        if (storyDisplay != null)
        {
            storyDisplay.SetVideoPlayingState(false);
        }

        Debug.Log("Video sequence completed - ready for chapter");
    }

    IEnumerator StartChapter(int chapterNumber)
    {
        string[] chapterTitles = {
        "CHAPTER 1: THE FRIGHTENED CHILD",
        "CHAPTER 2: THE DOUBTING YOUTH",
        "CHAPTER 3: THE HARDENED WARRIOR",
        "CHAPTER 4: THE WISE ADULT",
        "CHAPTER 5: THE ETERNAL TEACHER"
    };

        if (storyDisplay != null)
        {
            storyDisplay.ClearStoryText();
            Debug.Log($"Cleared story text before Chapter {chapterNumber}");
        }

        if (chapterNumber > 1)
        {
            chapterTransition.OnChapterComplete();
        }

        yield return StartCoroutine(chapterTransition.ShowChapterTitle(chapterTitles[chapterNumber - 1]));

        if (chapterNumber < 5)
        {
            StartCoroutine(ProcessMurfForChapter(chapterNumber + 1));
        }

        yield return StartCoroutine(PlayChapterSegmentsInCloud(chapterNumber));

        if (chapterNumber < 5)
        {
            yield return StartCoroutine(StartChapter(chapterNumber + 1));
        }
        else
        {
            // FIXED: Chapter 5 completed - trigger final sequence
            chapterTransition.OnChapterComplete();
            Debug.Log("All chapters complete - waiting for final continue");

            // Wait for final continue input
            yield return StartCoroutine(WaitForFinalContinue());

            // Start final sequence
            yield return StartCoroutine(storyDisplay.StartFinalSequence());
        }
    }

    IEnumerator PlayChapterSegmentsInCloud(int chapterNumber)
    {
        int chapterIndex = chapterNumber - 1;
        int startSegmentIndex = chapterIndex * segmentsPerChapter;

        for (int i = 0; i < segmentsPerChapter && startSegmentIndex + i < storySegments.Count; i++)
        {
            string segmentText = storySegments[startSegmentIndex + i];
            AudioClip segmentAudio = null;

            if (chapterIndex < chapterAudioSegments.Count && i < chapterAudioSegments[chapterIndex].Count)
            {
                segmentAudio = chapterAudioSegments[chapterIndex][i];
            }

            yield return StartCoroutine(storyDisplay.PlayStorySegmentInCloud(segmentText, segmentAudio));
        }
    }

    // FIXED: Method to check if story generation is ready
    public bool IsStoryGenerationReady()
    {
        return apiKeysReady || !HasValidAPIKeys(); // Ready if we have keys OR if we'll use fallback
    }

    // FIXED: Add method to wait for final continue input
    IEnumerator WaitForFinalContinue()
    {
        Debug.Log("*** WAITING FOR FINAL CONTINUE INPUT ***");

        // Show final continue prompt
        if (storyDisplay.continuePrompt != null)
        {
            storyDisplay.continuePrompt.SetActive(true);
        }

        // Wait for input
        bool continuePressed = false;
        while (!continuePressed)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0))
            {
                continuePressed = true;
                Debug.Log("*** FINAL CONTINUE INPUT RECEIVED! ***");
            }
            yield return null;
        }

        // Hide continue prompt
        if (storyDisplay.continuePrompt != null)
        {
            storyDisplay.continuePrompt.SetActive(false);
        }

        // Clear input
        yield return new WaitForSeconds(0.2f);
    }
}

// Keep your existing response classes
[System.Serializable]
public class GeminiResponse
{
    public GeminiCandidate[] candidates;
}

[System.Serializable]
public class GeminiCandidate
{
    public GeminiContent content;
}

[System.Serializable]
public class GeminiContent
{
    public GeminiPart[] parts;
}

[System.Serializable]
public class GeminiPart
{
    public string text;
}

[System.Serializable]
public class MurfResponse
{
    public string audioFile;
    public string encodedAudio;
    public string audioContent;
    public string audioUrl;
    public float audioLengthInSeconds;
    public int consumedCharacterCount;
    public int remainingCharacterCount;
    public string status;
    public string message;
}
