using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ConductiveUnitySDK : MonoBehaviour {

    public static ConductiveUnitySDK Instance { get; private set;}

    [SerializeField] public string apiKey = null;
    private string _apiUrl = "https://frame.conductive.ai";
    private HttpClient _httpClient;

    private string _distinctId;

    [SerializeField] private float _syncInterval = 60f; // Sync interval in seconds
    private List<string> _eventCache = new List<string>();
    private Dictionary<string, object> _userPropertiesCache = new Dictionary<string, object>();

    private bool _automaticUserIdentification = true;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _httpClient = new HttpClient();
        } else {
            Destroy(gameObject);
        }        
    }

    public void SetAutomaticUserIdentification(bool enabled) {
        _automaticUserIdentification = enabled;
    }

    public async Task Alias(string distinctId, string alias) {
        string payload = GeneratePayload("alias", "$create_alias", new Dictionary<string, object>{
            { "distinct_id", distinctId },
            { "alias", alias }
        });
        await SendEvent(payload);
    }

    public async Task Capture(string eventName, object properties = null) {
        string payload = GeneratePayload(eventName, "$event", properties);
        await SendEvent(payload);
    }

    private void CaptureCached(string eventName, object properties = null) {
        string payload = GeneratePayload(eventName, "$event", properties);
        _eventCache.Add(payload);
    }

    public async Task Identify(string distinctId, object properties = null) {
        _distinctId = distinctId;

        string payload = GeneratePayload("identify", "$identify", properties);
        await SendEvent(payload);
    }

    public void IdentifyUserCached(string userId, object properties = null) {
        string payload = GeneratePayload("identify", userId, properties);
        _userPropertiesCache[userId] = payload;
    }

    public async Task ScreenView(string screenName, object properties = null) {
        string payload = GeneratePayload(screenName, "$screen", properties);
        await SendEvent(payload);
    }

    public void OpenUrl(string url) {
        Application.OpenURL(url);
    }

    public string GenerateUserFingerprint() {
        return SystemInfo.deviceUniqueIdentifier;
    }

    private async Task SendEvent(string payload) {
        Debug.Log($"Sending event: {payload}");
        await PostEventAsync(payload);
    }

    private async Task PostEventAsync(string payload) {
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        try {
            HttpResponseMessage response = await _httpClient.PostAsync($"{_apiUrl}/capture", content);

            if (!response.IsSuccessStatusCode) {
                Debug.Log($"API Error: {response.StatusCode}");
            }

            Debug.Log($"API Response: {response.StatusCode}");
        } catch (Exception e) {
            Debug.Log($"API Exception: {e.Message}");
        }
    }

    // Custom event tracking
    public async Task TrackCustomEvent(string eventName, object properties = null) {
        await Capture(eventName, properties);
    }

    // Automatic event tracking
    private async void OnApplicationFocus(bool focus) {
        if (string.IsNullOrEmpty(apiKey)) {
            Debug.LogWarning("The API key is not set in the ConductiveSDK. Please input the API key in the ConductiveSDK game object");
        } else if (focus) {
            await TrackSessionStart();
        } else {
            await TrackSessionEnd();
        }
    }

    private async Task TrackSessionStart() {
        await Capture("Session Start", new Dictionary<string, object>{
            { "session_start", DateTime.UtcNow }
        });
    }

    private async Task TrackSessionEnd() {
        await Capture("Session End", new Dictionary<string, object>{
            { "session_end", DateTime.UtcNow }
        });
    }

    private string GeneratePayload(string eventType, string eventName, object properties) {
        var payload = new Dictionary<string, object>{
            { "api_key", apiKey },
            { "properties", AddPlatformSpecificProperties(properties as Dictionary<string, object>) },
            { "event", eventName }
        };

        // set generated distinct id for all events except alias
        if (eventType != "alias") {

            // set new distinct_id if event is identify
            // otherwise use the generated distinct_id based on the device
            payload.Add("distinct_id", eventType == "identify" ? _distinctId: GenerateUserFingerprint());
        }        
        
        string jsonPayload = JsonConvert.SerializeObject(payload, Formatting.Indented);

        Debug.Log($"Payload for {eventType}: {payload}");
        
        return jsonPayload;
    }

    public async Task UpdateUserProperties(object properties = null) {
        string userId = _automaticUserIdentification ? GenerateUserFingerprint() : null;
        await Identify(userId, properties);
    }

    private object AddPlatformSpecificProperties(Dictionary<string, object> properties) {
        if (properties == null) {
            properties = new Dictionary<string, object>();
        }

        properties["device_model"] = SystemInfo.deviceModel;
        properties["os_version"] = SystemInfo.operatingSystem;
        properties["screen_resolution"]= $"{Screen.width}x{Screen.height}";

        return properties;
    }

    private async Task SyncCacheWithApi() {
        foreach (string payload in _eventCache) {
            await PostEventAsync(payload);
        }
        _eventCache.Clear();

        foreach (var userProperties in _userPropertiesCache.Values) {
            await PostEventAsync(userProperties.ToString());
        }
        _userPropertiesCache.Clear();
    }

    private async Task SyncCacheCoroutine() {
        while (true) {
            await Task.Delay(TimeSpan.FromSeconds(_syncInterval));                  
            await SyncCacheWithApi();
        }
    }

    async void AsyncStart() {
        await SyncCacheCoroutine();
    }

    private void Start() {
        if (string.IsNullOrEmpty(apiKey)) {
            Debug.LogWarning("The API key is not set in the ConductiveSDK. Please input the API key in the ConductiveSDK game object");
        } else {
            AsyncStart();
        }
    }
}