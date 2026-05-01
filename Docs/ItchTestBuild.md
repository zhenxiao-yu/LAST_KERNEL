# Itch.io Test Build

This project is set up for an itch.io WebGL test build.

## Build locally

```powershell
.\Tools\BuildItchWebGL.ps1
```

The script reads `ProjectSettings/ProjectVersion.txt`, finds the matching Unity editor, runs a batch-mode WebGL development build, and writes:

- `Build/ItchWebGL/`
- `Builds/itch/last-kernel-webgl-test.zip`
- `Logs/itch-webgl-build.log`

If Unity is installed somewhere custom:

```powershell
.\Tools\BuildItchWebGL.ps1 -UnityExe "C:\Path\To\Unity.exe"
```

## Upload with butler

Install and authenticate itch.io butler, then run:

```powershell
butler login
.\Tools\PushItchWebGL.ps1 -ItchTarget "your-itch-user/last-kernel:webgl-test"
```

Use the itch.io project page settings:

- Kind of project: HTML
- Upload: `last-kernel-webgl-test.zip`, or the `webgl-test` butler channel
- Embed: enabled

The build script disables WebGL compression for simple test hosting on itch.io.
