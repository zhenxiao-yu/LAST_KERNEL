# Project Settings

## Unity

- Unity version: `6000.4.3f1`
- Product name: `Last Kernel`
- Company: `Markyu`
- Bundle version: `1.0`

## Rendering

- Render pipeline package: Universal Render Pipeline `17.4.0`
- URP assets: `Assets/_Project/Settings/URP/`
- Active quality pipeline: `URP_Asset.asset`
- Graphics pipeline global settings: `UniversalRenderPipelineGlobalSettings.asset`

## Input

- Input package: `com.unity.inputsystem` `1.19.0`
- Active input handling: Input System package
- Main actions asset: `Assets/InputSystem_Actions.inputactions`

## Localization

- Localization package: `com.unity.localization` `1.5.11`
- Localization settings are registered in `ProjectSettings/EditorBuildSettings.asset`.
- Project localization assets live under `Assets/_Project/Localization/`.
- Runtime bridge code lives under `Assets/_Project/Scripts/Runtime/Localization/` and `Assets/_Project/Scripts/Runtime/Core/`.

## Target Platforms

- Current configured identifiers include Standalone/Desktop.
- Android and iOS settings are present in `ProjectSettings/ProjectSettings.asset`, but should be verified before release builds.
- Treat WebGL/mobile as unverified until platform-specific quality, input, and UI checks are completed.

## Build Scenes

Production Build Settings:

0. `Assets/_Project/Scenes/Boot.unity`
1. `Assets/_Project/Scenes/MainMenu.unity`
2. `Assets/_Project/Scenes/Game.unity`

Test and prototype scenes are intentionally excluded from production Build Settings.

## Pixel-Perfect And Crispness Setup

- This project uses URP for a 3D/isometric card game, not the 2D Pixel Perfect Camera package.
- Crisp visuals come from texture import settings, camera configuration, URP anti-aliasing, and UI Canvas Scaler setup.
- See `Assets/_Project/Docs/PixelPerfectSetup.md` for detailed guidance.

## Important Project Settings

- Keep packages under `Packages/`; do not move local UPM packages into `Assets/`.
- Keep imported assets under `Assets/ThirdParty/`.
- Keep project-owned gameplay code under `Assets/_Project/Scripts/Runtime/`.
- Keep editor tools under `Assets/_Project/Scripts/Editor/`.
- Preserve `.meta` files when moving assets so Unity GUID references remain stable.
