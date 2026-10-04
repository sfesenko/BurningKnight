# Burning Knight — Android port

Sideloaded arm64 APK, Release only. Hardware gamepad required (no touch input).

## Prerequisites

- JDK 21 (`JAVA_HOME`).
- .NET 10 SDK with the android workload (`dotnet workload install android`).
- Android SDK:
  ```sh
  export ANDROID_HOME=<sdk>  # any directory
  dotnet build Android/Android.csproj -c Release -t:InstallAndroidDependencies \
    -p:AndroidSdkDirectory=$ANDROID_HOME -p:AcceptAndroidSDKLicenses=true
  ```
- `ffmpeg` on PATH (without it the packer ships PCM and the APK balloons).
- `adb` for install.

## Build

```sh
JAVA_HOME=<jdk-21> ANDROID_HOME=<sdk> \
  dotnet build Android/Android.csproj -c Release -f net10.0-android
```

APK: `Android/bin/Release/net10.0-android/com.burningknight.android-Signed.apk`.
Content packs automatically; Debug fails (no ImGui natives).

## Install

```sh
adb -s <serial> install -r Android/bin/Release/net10.0-android/com.burningknight.android-Signed.apk
```

## Logs

`/data/data/com.burningknight.android/files/`: `burning_log.txt`, `crash_log.txt` —
root only; without root use `adb logcat -d`.
