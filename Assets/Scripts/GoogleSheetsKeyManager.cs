using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class GoogleSheetsKeyManager : MonoBehaviour
{
    [Header("Google Sheets Configuration")]
    public string spreadsheetId = "your-spreadsheet-id-here";
    public string googleSheetsApiKey = "your-google-sheets-api-key";

    private string geminiApiKey = "";
    private string murfApiKey = "";
    private bool keysLoaded = false;

    // Singleton pattern
    public static GoogleSheetsKeyManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeAPIKeys());
    }

    public IEnumerator InitializeAPIKeys()
    {
        Debug.Log("Initializing API keys...");

        // Check if user has manually set keys in settings first
        string userGeminiKey = GetUserSetGeminiKey();
        string userMurfKey = GetUserSetMurfKey();

        // Only fetch from Google Sheets if both keys are empty
        if (string.IsNullOrEmpty(userGeminiKey) && string.IsNullOrEmpty(userMurfKey))
        {
            Debug.Log("No user-set API keys found. Fetching from Google Sheets...");
            yield return StartCoroutine(FetchAPIKeysFromSheets());
        }
        else
        {
            Debug.Log("Using user-set API keys from settings");
            geminiApiKey = userGeminiKey;
            murfApiKey = userMurfKey;
            keysLoaded = true;
            OnKeysLoaded?.Invoke();
        }
    }

    private string GetUserSetGeminiKey()
    {
        if (PlayerPrefs.HasKey("GeminiApiKey"))
        {
            string key = PlayerPrefs.GetString("GeminiApiKey", "");
            return string.IsNullOrEmpty(key) ? "" : key;
        }
        return "";
    }

    private string GetUserSetMurfKey()
    {
        if (PlayerPrefs.HasKey("MurfApiKey"))
        {
            string key = PlayerPrefs.GetString("MurfApiKey", "");
            return string.IsNullOrEmpty(key) ? "" : key;
        }
        return "";
    }

    public IEnumerator FetchAPIKeysFromSheets()
    {
        Debug.Log("Fetching API keys from Google Sheets...");

        string url = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/A1:A2?key={googleSheetsApiKey}";
        Debug.Log($"Request URL: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"Raw Google Sheets Response: {responseText}");

                    if (string.IsNullOrEmpty(responseText))
                    {
                        Debug.LogError("Empty response from Google Sheets API");
                        UseFallbackKeys();
                        yield break;
                    }

                    // FIXED: Use regex to extract all quoted strings, then filter
                    string fetchedGeminiKey = "";
                    string fetchedMurfKey = "";

                    // Pattern to match quoted strings
                    string pattern = @"""([^""]+)""";
                    MatchCollection matches = Regex.Matches(responseText, pattern);

                    List<string> extractedValues = new List<string>();

                    // Extract all quoted values
                    foreach (Match match in matches)
                    {
                        string value = match.Groups[1].Value;
                        extractedValues.Add(value);
                    }

                    Debug.Log($"All extracted values: {string.Join(", ", extractedValues)}");

                    // Filter out known metadata fields and keep only API keys
                    List<string> apiKeys = new List<string>();
                    foreach (string value in extractedValues)
                    {
                        // Skip known metadata fields
                        if (value != "range" && value != "majorDimension" &&
                            !value.StartsWith("Sheet1!") && value != "ROWS" && value != "values")
                        {
                            apiKeys.Add(value);
                        }
                    }

                    Debug.Log($"Filtered API keys count: {apiKeys.Count}");

                    if (apiKeys.Count >= 2)
                    {
                        fetchedGeminiKey = apiKeys[0];
                        fetchedMurfKey = apiKeys[1];

                        Debug.Log($"Found Gemini key: {fetchedGeminiKey.Substring(0, 15)}...");
                        Debug.Log($"Found Murf key: {fetchedMurfKey.Substring(0, 15)}...");
                    }
                    else
                    {
                        Debug.LogError($"Not enough API keys found. Expected 2, got {apiKeys.Count}");
                        foreach (var key in apiKeys)
                        {
                            Debug.Log($"Found key: {key}");
                        }
                    }

                    if (!string.IsNullOrEmpty(fetchedGeminiKey) && !string.IsNullOrEmpty(fetchedMurfKey))
                    {
                        geminiApiKey = fetchedGeminiKey.Trim();
                        murfApiKey = fetchedMurfKey.Trim();

                        keysLoaded = true;
                        Debug.Log("SUCCESS: API keys loaded successfully from Google Sheets!");
                        Debug.Log($"SUCCESS: Gemini key loaded: {geminiApiKey.Length} characters");
                        Debug.Log($"SUCCESS: Murf key loaded: {murfApiKey.Length} characters");

                        OnKeysLoaded?.Invoke();
                    }
                    else
                    {
                        Debug.LogError($"Failed to extract API keys! Gemini: '{fetchedGeminiKey}', Murf: '{fetchedMurfKey}'");
                        UseFallbackKeys();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error parsing Google Sheets response: " + e.Message);
                    Debug.LogError($"Stack trace: {e.StackTrace}");
                    Debug.LogError($"Response was: {request.downloadHandler.text}");
                    UseFallbackKeys();
                }
            }
            else
            {
                Debug.LogError("Failed to fetch API keys: " + request.error);
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response Text: {request.downloadHandler.text}");
                UseFallbackKeys();
            }
        }
    }

    private void UseFallbackKeys()
    {
        Debug.LogWarning("Using fallback - no API keys available");
        keysLoaded = true;
        OnKeysLoaded?.Invoke();
    }

    public void RefreshAPIKeys()
    {
        keysLoaded = false;
        StartCoroutine(InitializeAPIKeys());
    }

    public bool ShouldFetchFromSheets()
    {
        string userGeminiKey = GetUserSetGeminiKey();
        string userMurfKey = GetUserSetMurfKey();
        return string.IsNullOrEmpty(userGeminiKey) && string.IsNullOrEmpty(userMurfKey);
    }

    public string GetGeminiApiKey()
    {
        if (!keysLoaded)
        {
            Debug.LogWarning("API keys not loaded yet!");
            return "";
        }

        string userKey = GetUserSetGeminiKey();
        return !string.IsNullOrEmpty(userKey) ? userKey : geminiApiKey;
    }

    public string GetMurfApiKey()
    {
        if (!keysLoaded)
        {
            Debug.LogWarning("API keys not loaded yet!");
            return "";
        }

        string userKey = GetUserSetMurfKey();
        return !string.IsNullOrEmpty(userKey) ? userKey : murfApiKey;
    }

    public bool AreKeysLoaded()
    {
        return keysLoaded;
    }

    public bool HasValidKeys()
    {
        return !string.IsNullOrEmpty(GetGeminiApiKey()) && !string.IsNullOrEmpty(GetMurfApiKey());
    }

    public System.Action OnKeysLoaded;
}

[System.Serializable]
public class SheetsResponse
{
    public string range;
    public string majorDimension;
    public string[][] values;
}
