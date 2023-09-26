using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public class CatalystAPIManager : MonoBehaviour
{
    private string _catalystApi = "https://instant-play.qa-conductive.ai/catalyst/webview/player/notification/";
    private string _catalystApiButtonUpdate = "https://instant-play.qa-conductive.ai/catalyst/webview/player/openWebView/";
    public CatalystApiData fetchedCatalystData { get; private set; }
    public GameObject catalystButton;
    public GameObject countdownBadge;
    public Text countdownText;
    public GameObject rewardBadge;
    public Image rewardImage;
    private TimeSpan countdownTimer;
    public CatalystSDK catalystSDK;
    private string buttonScene;

    // ButtonPulse animation controls
    private float pulseScale = 1.2f; // the amount the badge grows, 20% scale increase
    private float pulseTime = 3f; // the duration of the pulse animation, 1 seconds

    void Start()
    {
        if (!string.IsNullOrEmpty(catalystSDK._distinctHash))
        {
            StartCoroutine(FetchData());
        }
        else
        {
            Debug.Log("The _distinctHash needs to be generated before the button is shown.");
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
    {
        if (!string.IsNullOrEmpty(catalystSDK.sceneToShowButton))
        {
            if (scene.name == catalystSDK.sceneToShowButton)
            {
                catalystButton.SetActive(true);
            }
            else
            {
                catalystButton.SetActive(false);   
            }
        }
        else
        {
            Debug.LogWarning("Please input the name of the scene where you would like to show the button in the ConductiveSDK prefab.");
        }
    }

    public IEnumerator FetchData()
    {
        if (_catalystApi == null)
        {
            Debug.LogWarning("The Catalyst API url is missing");
            yield break;   
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("There is no internet connection");
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequest.Get(_catalystApi))
        {
            www.SetRequestHeader("Authorization", catalystSDK._distinctHash);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Error: " + www.error);
            }
            else
            {
                try
                {
                    CatalystApi response = JsonConvert.DeserializeObject<CatalystApi>(www.downloadHandler.text);
                    
                    if (!response.error)
                    {
                        fetchedCatalystData = response.data;
                        
                        Debug.Log(response.data.current_contest_ends_at);
                        Debug.Log(response.data.next_contest_starts_at);
                        Debug.Log(response.data.reward_token_logo);
                        Debug.Log(response.data.reward_seen);


                        if (!string.IsNullOrEmpty(response.data.current_contest_ends_at))
                        {
                            Debug.Log($"Current Contest Ends At: {response.data.current_contest_ends_at}");
                            countdownBadge.SetActive(true);
                            countdownTimer = DateTime.Parse(response.data.current_contest_ends_at) - DateTime.UtcNow;
                        }
                        else
                        {
                            Debug.LogWarning("The contest time IsNullOrEmpty");
                        }

                        if (!response.data.reward_seen)
                        {
                            Debug.Log($"Reward Seen: {response.data.reward_seen}");
                            rewardBadge.SetActive(true);
                            StartCoroutine(LoadRewardImage(response.data.reward_token_logo));
                            ButtonPulse(rewardBadge);
                        }                        
                    }
                    else
                    {
                        Debug.LogWarning("There was an error with the Catalyst API. Please check your API key.");
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogWarning($"There was an error parsing the JSON file: {ex.Message}");
                }
            }
        }
    }

    public IEnumerator PostRewardSeen()
    {
        if (string.IsNullOrEmpty(catalystSDK._distinctHash))
        {
            Debug.LogWarning("The _distinctHash is null");
            yield break;
        }

        if (_catalystApiButtonUpdate == null)
        {
            Debug.LogWarning("The Catalyst API POST url is missing");
            yield break;   
        }

        CatalystApiData data = new CatalystApiData();
        data.reward_seen = true;

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(_catalystApiButtonUpdate, "POST"))
        {
            request.SetRequestHeader("Authorization", catalystSDK._distinctHash);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                Debug.Log("Response: " + request.downloadHandler.text);
            }
        }
    }

    private IEnumerator LoadRewardImage(string imageUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            rewardImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogWarning("Unable to fetch reward image");
        }
    }

    string FormatTime(TimeSpan time)
    {
        if (time.TotalDays >= 1)
        {
            return string.Format("{0}d {1}hrs", time.Days, time.Hours);
        }
        else if (time.TotalHours >= 1)
        {
            return string.Format("{0}hrs", time.Hours);
        }
        else
        {
            return string.Format("{0}min", time.Minutes);
        }
    }

    public void ButtonPulse(GameObject obj)
    {
        StartCoroutine(Pulse(obj));
    }

    private IEnumerator Pulse(GameObject obj)
    {
        Vector3 originalScale = obj.transform.localScale;
        Vector3 targetScale = originalScale * pulseScale;

        while (true) // Infinite loop for Yoyo behavior
        {
            // Scale up
            float time = 0;
            while (time <= pulseTime / 2)
            {
                obj.transform.localScale = Vector3.Lerp(originalScale, targetScale, time / (pulseTime / 2));
                time += Time.deltaTime;
                yield return null;
            }

            // Scale down
            time = 0;
            while (time <= pulseTime / 2)
            {
                obj.transform.localScale = Vector3.Lerp(targetScale, originalScale, time / (pulseTime / 2));
                time += Time.deltaTime;
                yield return null;
            }
        }
    }

    void Update()
    {
        if (countdownTimer != null & countdownText != null)
        {
            countdownTimer = countdownTimer.Subtract(TimeSpan.FromSeconds(Time.deltaTime));

            countdownText.text = FormatTime(countdownTimer);
            
            if (countdownTimer.TotalSeconds <= 0)
            {
                //add notification later
            }
        }
    }
}

[Serializable]
public class CatalystApi
{
    public string message;
    public CatalystApiData data;
    public bool error;
}

[Serializable]
public class CatalystApiData
{
    public string next_contest_starts_at;
    public string current_contest_ends_at;
    public string reward_token_logo;
    public bool reward_seen;
}
