
The Conductive Catalyst SDK is primarily provided to game developers as a Unity SDK. We also support native Android (Kotlin) and iOS (Swift) SDKs upon request.

We’ve made it extremely easy to get started with the SDK. Simply follow the steps below to get started.

## SDK Installation

### Installation Guide

You can follow our [quick installation guide in our public Github repository](https://github.com/conductiveai/catalyst-unity-sdk), or continue following the instructions below.

### Requirements

- Unity 2020 or later installed on your computer
- An internet connection
- A GitHub account

### API Key

You will first want to acquire an API key by visiting the dashboard <https://app.conductive.ai> and selecting the settings icon, followed by “Settings”

![](https://github.com/conductiveai/catalyst-unity-sdk/blob/main/.github/settings.png?raw=true)

This should take you to the project settings below. Copy your API key provided for your project.

![](https://github.com/conductiveai/catalyst-unity-sdk/blob/main/.github/settings2.png?raw=true)

### Installing the Unity SDK

1. In Unity, go to **Window > Package Manager**. You can install the SDK using either the GitHub URL or the ZIP file.
2. Using GitHub:
    - Click the ➕ button, then go to **Add package from git URL**.
    - Paste <https://github.com/conductiveai/catalyst-unity-sdk.git> and click **Add**.
3. Using the ZIP file:
    - Go to [https://github.com/conductiveai/catalyst-unity-sdk](https://github.com/conductiveai/catalyst-unity-sdk.git) and [download the zip file](https://github.com/conductiveai/catalyst-unity-sdk/archive/refs/heads/main.zip)
    - Unzip the file
    - Click the ➕ button, then go to **Add package from disk**.
    - Select the folder that you unzipped select the package.json file.

![](https://github.com/conductiveai/catalyst-unity-sdk/blob/main/.github/step1.png?raw=true)

![](https://github.com/conductiveai/catalyst-unity-sdk/blob/main/.github/step2.png?raw=true)

![](https://github.com/conductiveai/catalyst-unity-sdk/blob/main/.github/step3.png?raw=true)

### Integration in Unity

1. In the packages list, go to **Packages > CatalystSDK > Prefab**.

2. Drag the *CatalystSDK.prefab* to your project's loading scene or first scene

    ![](https://github.com/conductiveai/catalyst-unity-sdk/blob/main/.github/add-game-object.png?raw=true)

3. Fill in the `API_KEY` field in the *CatalystSDK.prefab* using the API key you acquired earlier.

4. Fill in the `SceneToShowButton` field in the *CatalystSDK.prefab* with the name of the Unity scene where you want the button to be shown. Pressing this button takes a user to the rewards interface where they can see contest information and their leaderboard rank.

 ![](https://github.com/conductiveai/catalyst-docs/blob/main/.github/unity-prefab.png?raw=true)

5. Configure the `Canvas` and `Canvas Scaler` on the *CatalystSDK.prefab* to fit your game's UI. The button and webview for the rewards interface is rendered on this Canvas.

 ![](https://github.com/conductiveai/catalyst-docs/blob/main/.github/unity-prefab-canvas.png?raw=true)

6. Open the `CatalystSDK.cs` script located in the `CatalystSDK` folder. In this script, you'll find a line of code designated for identifying players who will utilize Catalyst services.

```csharp
//Set your game's user id here to synchronize data with Catalyst services
SetExternalId("USER_ID");
```

It's crucial to modify this line by replacing the placeholder "USER_ID" with your specific method for retrieving player IDs. This step ensures that player identification is properly integrated with the Catalyst services.

### That’s it! 🚀

The Catalyst SDK will automatically capture user login events automatically.

Placing the prefab in the game's first loaded scene or Main Menu ensures user logins are captured when the game starts.

### Building for specific platforms 📱

iOS:

- Please include `Security.Framework` to your project in Xcode before you build. This is requirement to use Keychain services. For more information please check [Apple's documentation](https://developer.apple.com/documentation/security)

Android:

- Uniwebiew includes Android libraries that may have a duplicate class with other Android plugins. If you see errors refering to a duplicate class please check [Uniwebview's documentation](https://docs.uniwebview.com/guide/trouble-shooting.html#android)
