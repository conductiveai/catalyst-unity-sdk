# Conductive Unity SDK

If you want to integrate **ConductiveUnitySDK** into your Unity game development project, this step-by-step guide will help you install the package into Unity.

## Prerequisites

Before we begin, make sure you have the following:

- Unity 2018 or later installed on your computer
- Internet connection
- GitHub account

## Installation

1. In Unity, go to **Window > Package Manager**  
2. Using the GitHub:
    1. In the ➕ button, go to **Add package from git URL**
    2. And paste <https://github.com/conductiveai/conductive-unity-sdk.git> and click **Add**
    3. 🎉 Package installed
3. Using the ZIP file:
    1. Go to <https://github.com/conductiveai/conductive-unity-sdk.git> and download zip
    2. In the ➕ button, go to **Add package from disk**
    3. Select the zip file, that you download from GitHub.
    4. 🎉 Package installed
4. In your scene add a **GameObject**
5. Go to, **Add Component**, and search for **ConductiveUnitySDK**, and select it.
6. Now fill the *Api Key* field, with your project api key.
7. 🎉 Package integrated

## Troubleshooting

### Mac M1

### ************************************************Building for iOS / XCode************************************************

- If you’re receiving a CocoaPods error e.g. `...ruby/2.6.0/gems/ffi-1.15.5/lib/ffi_c.bundle' (mach-o file, but is an incompatible architecture (have 'arm64', need 'x86_64')),`
- This means you have `ffi` built for `arm64` but not `x86_64` architecture
- Make sure you have Developer Mode enabled on your iOS device, Settings → Privacy → Developer mode → on
- Do the following:

```python
# install llvm
arch -arm64 brew install llvm
export LDFLAGS="-L/opt/homebrew/opt/llvm/lib"
export CPPFLAGS="-I/opt/homebrew/opt/llvm/include"

# uninstall ffi
sudo gem uninstall ffi

# if prompted, uninstall ALL versions

# install ffi gem for x86_64
sudo arch -x86_64 gem install ffi
```

- Reference: <https://stackoverflow.com/questions/66644365/cocoapods-on-m1-apple-silicon-fails-with-ffi-wrong-architecture>

### Trouble building for iOS

- Be sure to disable bitcode
- Be sure to remove Quoted Include in Framework Header to no

[Changelog](CHANGELOG.md)
