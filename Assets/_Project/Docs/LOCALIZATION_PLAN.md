# Localization Plan

## Supported Languages

- English is the primary authoring language.
- Chinese support should include Simplified Chinese first unless a feature explicitly requires Traditional Chinese.
- Keep language-specific authoring notes under `Assets/_Project/Localization/English/` and `Assets/_Project/Localization/Chinese/`.
- Keep Unity table assets under `Assets/_Project/Localization/Tables/` or the existing table layout until a safe migration is planned.

## String Table Naming

- Use stable table names by feature area, not by scene.
- Recommended tables:
  - `GameText` for shared UI and gameplay text.
  - `Cards` for card names, descriptions, and flavor text.
  - `Recipes` for recipe names and descriptions.
  - `Quests` for quest titles, objectives, and completion text.
  - `Packs` for pack names, descriptions, and store/vendor text.

## Key Naming Rules

- Use lowercase dot-separated keys.
- Do not include scene names unless the text is truly scene-specific.
- Do not rename keys after content ships; add a replacement key and keep compatibility until references are migrated.

## Card Keys

- Name: `card.<card_id>.name`
- Description: `card.<card_id>.description`
- Flavor: `card.<card_id>.flavor`

## Recipe Keys

- Name: `recipe.<recipe_id>.name`
- Description: `recipe.<recipe_id>.description`
- Requirement hint: `recipe.<recipe_id>.hint`

## Quest Keys

- Title: `quest.<quest_id>.title`
- Objective: `quest.<quest_id>.objective`
- Completion: `quest.<quest_id>.complete`

## Pack Keys

- Name: `pack.<pack_id>.name`
- Description: `pack.<pack_id>.description`
- Vendor text: `pack.<pack_id>.vendor`

## Adding Localized Content Safely

1. Add the data asset first and give it a stable ID.
2. Add localization keys using the stable ID.
3. Add English text before wiring the content into gameplay.
4. Add Chinese text or an explicit temporary fallback before release-facing builds.
5. Reference keys from ScriptableObjects or UI components; avoid hardcoded visible strings.
6. Test language switching in `Boot`, `MainMenu`, and `Game`.
7. Do not rename serialized fields or existing localization keys during content cleanup.
