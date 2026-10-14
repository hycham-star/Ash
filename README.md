# Ash: After the Fall

A portrait Android strategy prototype about human factions competing for land after society's collapse. Ash is your settlement; the Iron Pact holds a nearby water depot. All visuals are original procedural placeholders.

## Play in Unity

1. Install Unity **6000.0.23f1** using Unity Hub. Include **Android Build Support**, its SDK/NDK tools, and OpenJDK if you want Android builds. Unity requires an appropriate editor license.
2. Add this directory to Unity Hub and open it. Allow the UGUI package to resolve.
3. The editor initializer creates `Assets/Ash/Scenes/Settlement.unity`, registers it for builds, and sets portrait orientation. If you already have an unsaved scene, save it and choose **Ash > Prepare Project** or reopen the project before proceeding.
4. Open the Settlement scene. Set the Game view to a portrait aspect ratio (9:16), then press Play.

The settlement generates supplies and salvage every 20 seconds. Farms increase supplies; workshops increase salvage. Recruit fighters and build watchtowers to reach strength 7, then raid the water depot. Capturing it gives additional supplies each day. Buttons work with mouse or touch. Pause stops production. Each play session starts fresh.

## Android

In Unity's Build Profiles, switch to Android and build the Settlement scene. Alternatively, with a licensed editor and Android modules installed:

```sh
Unity -batchmode -quit -projectPath /workspace/Ash -executeMethod Ash.Editor.PrepareProject.BuildAndroid -logFile /tmp/ash-android-build.log
```

The build writes `Builds/Ash.apk`. The application identifier is `com.ash.afterthefall`; choose your own identifier before store publication. Signing and Play Store release configuration are not included.

## Validate the strategy rules

With .NET 8 installed, from this directory:

```sh
dotnet run --project Tests/Rules/Rules.csproj
```

This runs 11 checks covering production, building costs, recruitment, failed raids, successful capture, and recurring territorial income. It does not validate Unity rendering, UI input, or Android builds.

## Prototype scope

A single-player sandbox with a procedurally drawn settlement, one rival territory, and deterministic battle outcomes. It has no online services or credential requirements. Multiplayer, diplomacy, character art, animations, persistence, and a full campaign are future work.
