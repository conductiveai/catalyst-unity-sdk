using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

public class CatalystSDK : MonoBehaviour {

    private static CatalystSDK s_instance;
    public static CatalystSDK Instance {
        get {
            if (s_instance == null) {
                return new GameObject("CatalystSDK").AddComponent<CatalystSDK>();
            } else {
                return s_instance;
            }
        }
    }

    [SerializeField] public string _apiKey = null;
    public string _apiUrl = "https://frame.qa-conductive.ai";
    private string _catalystURL = "https://catalyst-web-client.vercel.app/contest/";
    private HttpClient _httpClient;
    public bool showToolbar = true;
    public CatalystAPIManager apiManager;
    private UniWebView webview;

    private string _distinctId = null;
    public string _externalId = null;
    public string _distinctHash = null;

    private float _syncInterval = 60f; // Sync interval in seconds
    private List<string> _eventCache = new List<string>();
    private Dictionary<string, object> _userPropertiesCache = new Dictionary<string, object>();

    private bool _automaticUserIdentification = true;
    private bool internetDisconnected = false;

    public RectTransform WebviewCanvas;
    
    private static readonly int[] Bytes = {
        14,  20, 11,   6, -48,  21, -51,  15,
        -1, -49, 17,  15,  -3,   5,   4, -50,
        17,  -3, -3, -51,   6,  20, -44,  -3,
        10,  -1,  3, -52, -46, -45,   0,  10,
    };

    private void Awake() {
        if (s_instance != null) {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        _httpClient = new HttpClient();
        SetExternalId(GenerateUserFingerprint());
        _distinctHash = Encode("{\"frame_api_token\":\"" + _apiKey + "\",\"fingerprint\":\"" + GenerateUserFingerprint() + "\",\"external_id\":\"" + _externalId + "\"}");
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
        string payload = GeneratePayload("$create_alias", "$create_alias", new Dictionary<string, object>{
            { "alias", alias }
        });

        await SendEvent(payload);
    }

    public async Task Capture(string eventName, object properties = null) {
        string payload = GeneratePayload("$event", eventName, properties);

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

        string payload = GeneratePayload("$identify", "$identify", properties);
        
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            Debug.Log("No internet connection. Caching event <Identify>");
            _userPropertiesCache[distinctId] = payload;
            internetDisconnected = true;
        } else {
            await SendEvent(payload);
            internetDisconnected = false;
        }
    }

    public async Task ScreenView(string screenName, object properties = null) {
        string payload = GeneratePayload("$screen", screenName, properties);

        await SendEvent(payload);
    }

    public async Task SetExternalId(string externalId) {

        _externalId = externalId;

        await Capture("Set External ID", new Dictionary<string, object>{
            { "external_id", externalId }
        });
    }

    public void OpenCatalyst() {
        StartCoroutine(apiManager.PostRewardSeen());
        apiManager.rewardBadge.SetActive(false);
        ShowWebview(_catalystURL+_distinctHash);        
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
        if (string.IsNullOrEmpty(_apiKey)) {
            Debug.LogWarning("The API key is not set in the Catalyst SDK. Please input the API key in the Catalyst SDK prefab.");
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
            { "api_key", _apiKey },
            { "distinct_id", string.IsNullOrEmpty(_distinctId) ? GenerateUserFingerprint() : _distinctId},
            { "properties", AddPlatformSpecificProperties(properties as Dictionary<string, object>) },
            { "event", eventName }
        };

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
        properties["fp_id"] = GenerateUserFingerprint();

        // add external id if set
        if (!string.IsNullOrEmpty(_externalId)) {
            properties["external_id"] = _externalId;
        }
       
        return properties;
    }

    private async Task SyncCacheWithApi() {
        if (_eventCache.Count > 0 || _userPropertiesCache.Count > 0) {
            Debug.Log("Syncing cache with API");
            foreach (string payload in _eventCache) {
                Debug.Log($"Syncing event with API <Capture>");
                await SendEvent(payload);
            }
            _eventCache.Clear();

            foreach (var userProperties in _userPropertiesCache.Values) {
                Debug.Log($"Syncing user properties with API <Identify>");
                await SendEvent(userProperties.ToString());
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
        if (string.IsNullOrEmpty(_apiKey)) {
            Debug.LogWarning("The API key is not set in the Catalyst SDK. Please input the API key in the Catalyst SDK prefab.");
        } else if (Application.internetReachability != NetworkReachability.NotReachable) {
            // has internet connection
            AsyncStart();            
        }
        InitializeWebview();
    }

    async void AsyncSyncCache() {
        await SyncCacheWithApi();
    }

    private static byte[] Xor(byte[] data) {
        for (var i = 0; i < data.Length; i++) {
            data[i] ^= (byte)(Bytes[i % Bytes.Length] + 100);
        }
        
        return data;
    }

    private static string Encode(string data) {
        return Convert.ToBase64String(Xor(Encoding.ASCII.GetBytes(data))).Replace("+", "-").Replace("/", "_");
    }

    private static string Decode(string data) {
        return Encoding.ASCII.GetString(Xor(Convert.FromBase64String(data.Replace("-", "+").Replace("_", "/"))));
    }

    private void InitializeWebview() {
        if (webview == null) {
            var webviewGO = new GameObject("webviewGO");
            webview = webviewGO.AddComponent<UniWebView>();
            webview.ReferenceRectTransform = WebviewCanvas;
            
            if (showToolbar) {
                webview.EmbeddedToolbar.Show();
                webview.EmbeddedToolbar.HideNavigationButtons();
                webview.EmbeddedToolbar.SetDoneButtonText("Close");
                webview.EmbeddedToolbar.SetBackgroundColor(Color.white);
                webview.EmbeddedToolbar.SetTitleText("Conductive.ai");
            }

            webview.Load(_catalystURL+_distinctHash);
            webview.OnPageFinished += (view, statusCode, url) => { 
                Debug.Log("Page Load Finished: " + url); 
            };

            webview.OnShouldClose += (view) => {
                HideWebview();
                return true;
            };

        }
    }

    public void ShowWebview(string url) {
        if (webview != null) {
            webview.Show(true, UniWebViewTransitionEdge.Bottom, 0.35f);
        } else {
            InitializeWebview();
            webview.Show(true, UniWebViewTransitionEdge.Bottom, 0.35f);
        }
    }

    public void HideWebview() {
        if (webview != null) {
            webview.Hide(true, UniWebViewTransitionEdge.Bottom, 0.35f);
        }
    }

    void CloseWebView() {
        HideWebview();
    }

    private void Update() {
        if (internetDisconnected && Application.internetReachability != NetworkReachability.NotReachable && (_eventCache.Count > 0 || _userPropertiesCache.Count > 0)) {
            Debug.Log("Internet connection restored, syncing cached events");
            internetDisconnected = false;
            AsyncSyncCache();            
        }
    }
}
