using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ConductiveUnitySDK : MonoBehaviour {

    private static ConductiveUnitySDK s_instance;
    public static ConductiveUnitySDK Instance {
        get {
            if (s_instance == null) {
                return new GameObject("ConductiveUnitySDK").AddComponent<ConductiveUnitySDK>();
            } else {
                return s_instance;
            }
        }
    }

    [SerializeField] public string apiKey = null;
    private string _apiUrl = "https://frame.conductive.ai";
    private HttpClient _httpClient;

    private string _distinctId;

    private float _syncInterval = 60f; // Sync interval in seconds
    private List<string> _eventCache = new List<string>();
    private Dictionary<string, object> _userPropertiesCache = new Dictionary<string, object>();

    private bool _automaticUserIdentification = true;
    private bool internetDisconnected = false;

    private void Awake() {
        if (s_instance != null) {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        _httpClient = new HttpClient();
    }

    private void OnEnable() {
        if (s_instance == null) {
            s_instance = this;
        }
    }

    private void OnDestroy() {
        if (s_instance != this) {
            return;
        }
        s_instance = null;
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

        if (Application.internetReachability == NetworkReachability.NotReachable) {
            Debug.Log("No internet connection. Caching event <Capture>");
            _eventCache.Add(payload);
            internetDisconnected = true;
        } else {
            await SendEvent(payload);
            internetDisconnected = false;
        }
    }

    public async Task Identify(string distinctId, object properties = null) {
        _distinctId = distinctId;

        string payload = GeneratePayload("identify", "$identify", properties);
        
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            Debug.Log("No internet connection. Caching event <Identify>");
            _userPropertiesCache[distinctId] = payload;
        } else {
            await SendEvent(payload);
        }
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
            payload.Add("distinct_id", string.IsNullOrEmpty(apiKey) ? GenerateUserFingerprint() : _distinctId);
        }
        
        string jsonPayload = JsonConvert.SerializeObject(payload, Formatting.Indented);
        
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
        if(_eventCache.Count > 0 || _userPropertiesCache.Count > 0) {
            Debug.Log("Syncing cache with API");
            foreach (string payload in _eventCache) {
                Debug.Log($"Syncing event with API <Capture>");
                await PostEventAsync(payload);
            }
            _eventCache.Clear();

            foreach (var userProperties in _userPropertiesCache.Values) {
                Debug.Log($"Syncing user properties with API <Identify>");
                await PostEventAsync(userProperties.ToString());
            }
            _userPropertiesCache.Clear();            
        }
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
        } else if (Application.internetReachability != NetworkReachability.NotReachable) {
            // has internet connection
            AsyncStart();
        }
    }

    private void Update() {
        if(internetDisconnected && Application.internetReachability != NetworkReachability.NotReachable && _eventCache.Count > 0) {
            Debug.Log("Internet connection restored, syncing cached events");
            internetDisconnected = false;
            SyncCacheWithApi();            
        }
    }
}