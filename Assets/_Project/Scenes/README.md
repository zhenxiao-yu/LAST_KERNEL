# Scene Roles

Production Build Settings use this order:

0. `Boot.unity` - Persistent managers, save/bootstrap setup, localization/input/audio initialization, and first-flow routing.
1. `MainMenu.unity` - Title screen, save slot selection, options, and menu flow.
2. `Game.unity` - Main gameplay scene.

Development and prototype scenes stay out of production Build Settings:

- `Scenes/Test/` - Development-only scene work.
- `Main.unity`, `Island.unity`, and `Title.unity` - Legacy/prototype scenes retained for reference until they are reviewed in Unity.

Do not rename or delete scenes without first checking Build Settings, scene-loading code, saved-game scene names, Addressables, and prefab references.
