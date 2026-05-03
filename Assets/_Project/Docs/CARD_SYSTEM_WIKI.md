# LAST KERNEL — Card System Wiki

> Developer reference for card creation, pack creation, visual reskinning, and UI integration.

---

## Table of Contents

1. [How Cards Are Defined](#1-how-cards-are-defined)
2. [How Cards Are Spawned & Assigned](#2-how-cards-are-spawned--assigned)
3. [How Packs Work](#3-how-packs-work)
4. [Stacking Rules](#4-stacking-rules)
5. [Reskinning Cards — Art Pipeline](#5-reskinning-cards--art-pipeline)
6. [Reskinning Packs — Art Pipeline](#6-reskinning-packs--art-pipeline)
7. [Card Feel & Shader Customization](#7-card-feel--shader-customization)
8. [Card UI Layout](#8-card-ui-layout)
9. [Localization — Naming Cards & Packs](#9-localization--naming-cards--packs)
10. [Creating a New Card (Step-by-Step)](#10-creating-a-new-card-step-by-step)
11. [Creating a New Pack (Step-by-Step)](#11-creating-a-new-pack-step-by-step)
12. [Save & Restore System](#12-save--restore-system)
13. [Quick Reference — Enums & Constants](#13-quick-reference--enums--constants)

---

## 1. How Cards Are Defined

All cards are **ScriptableObjects** that extend `CardDefinition`.

**Base type:** `CardDefinition`
**File:** `Assets/_Project/Scripts/Runtime/Cards/Definitions/CardDefinition.cs`
**Create in Unity:** Right-click → `Last Kernel / Card`
**Save location:** `Assets/_Project/Data/Resources/Cards/<Category>/`

### Core Fields

| Field | Type | Purpose |
|---|---|---|
| `id` | string (GUID) | Auto-generated. Unique key for save/load lookup. **Never edit manually.** |
| `displayName` | string | Localization-driven display name (see §9) |
| `description` | string | Card lore/effect text. Localization-driven. |
| `artTexture` | Texture2D | Card face image. See §5 for import rules. |
| `category` | `CardCategory` | Drives prefab selection, stacking, and save behaviour |
| `faction` | `CardFaction` | Neutral / Player / Mob |
| `combatType` | `CombatType` | None / Melee / Ranged / Magic |

### Combat Stat Fields (used when card can fight)

| Field | Range | Notes |
|---|---|---|
| `maxHealth` | int | Starting HP |
| `attack` | int | Base damage |
| `defense` | int | Damage reduction |
| `attackSpeed` | 1–300 | Percentage. 100 = normal. |
| `accuracy` | 0–100 | Hit chance % |
| `dodge` | 0–100 | Evade chance % |
| `criticalChance` | 0–100 | Crit chance % |
| `criticalMultiplier` | 100–500 | Percentage. 150 = 1.5× default. |

### Economy Fields

| Field | Type | Notes |
|---|---|---|
| `isSellable` | bool | Whether player can sell this card |
| `sellPrice` | int (≥0) | Coin value when sold |
| `hasDurability` | bool | Card is consumed after `uses` |
| `uses` | int (≥1) | How many interactions before consumed |
| `nutrition` | int | Food value provided to characters |

### Mob-Specific Fields

| Field | Type | Notes |
|---|---|---|
| `isAggressive` | bool | Triggers `aggressiveMobPrefab` instead of category prefab |
| `aggroRadius` | float | Detection range |
| `attackRadius` | float | Melee/ranged engagement range |
| `produceCard` | CardDefinition | Card dropped passively (non-aggressive mobs) |
| `produceInterval` | float | Seconds between productions |

### Equipment Fields (Category == Equipment)

| Field | Type | Notes |
|---|---|---|
| `equipmentSlot` | `EquipmentSlot` | Weapon / Armor / Accessory |
| `statModifiers` | `List<StatModifier>` | Additive or multiplicative stat boosts |
| `classChangeResult` | CardDefinition | When equipped, transforms character to this definition |

### Loot Fields

| Field | Type | Notes |
|---|---|---|
| `loot` | `List<LootEntry>` | Weighted drop table. Evaluated on card death/harvest. |

### Specialized Subtypes

When `category` alone isn't enough, the SO asset uses a specialized definition subclass:

| Subtype | Create Menu | Extra Fields |
|---|---|---|
| `ChestDefinition` | `Last Kernel / Card` + set category Chest | `capacity` — max coins stored |
| `EnclosureDefinition` | — | `capacity` — max passive mobs allowed inside |
| `GrowerDefinition` | — | Passive production accelerator |
| `ResearchDefinition` | — | Tech / recipe tree node |
| `LimitBoosterDefinition` | — | Raises global card limit |
| `MarketDefinition` | — | Enables trade zone interaction |
| `PackDefinition` | `Last Kernel / Pack` | See §3 |

---

## 2. How Cards Are Spawned & Assigned

### The Spawn Pipeline

```
Request (CardManager.CreateCardInstance)
  └─► CardSpawnService.SpawnCard(definition, position)
        ├─ Aggressive mob?  → aggressiveMobPrefab
        ├─ Category match?  → CategoryEntry[category].Prefab
        └─ Fallback         → generic fallback prefab
        
        Instantiate prefab at position
        CardInstance.Initialize(definition, cardSettings, stackToIgnore)
        
        Attach logic components (conditional):
          ├─ EnclosureLogic   if ChestDefinition
          ├─ ChestLogic       if EnclosureDefinition
          ├─ MarketLogic      if MarketDefinition
          └─ CardAI + VillagerLockToggle   if Character
          
        Auto-attach to nearby stack within spawnAttachRadius
        CardManager.ResolveOverlaps()
```

### CardInstance — Runtime State

`CardInstance` is the live entity on the board. It holds:

| Property | What it is |
|---|---|
| `Definition` | Reference back to the immutable SO asset |
| `Stack` | Which `CardStack` group this card belongs to |
| `Stats` | Runtime `CombatStats` (may include equipment modifiers) |
| `UsesLeft` | Remaining durability charges |
| `CurrentHealth` | Live HP |
| `CurrentNutrition` | Live food value remaining |

### CardManager Events

Subscribe to these to react to card lifecycle changes:

```csharp
CardManager.Instance.OnCardCreated  += (CardInstance card) => { };
CardManager.Instance.OnCardKilled   += (CardInstance card) => { };
CardManager.Instance.OnCardEquipped += (CardDefinition def) => { };
CardManager.Instance.OnStatsChanged += (StatsSnapshot snap) => { };
```

### Category → Prefab Mapping

Set in `CardManager` inspector under `cardPrefabs (List<CategoryEntry>)`:

| Category | Default Prefab |
|---|---|
| Area | `Card_Area.prefab` |
| Character | `Card_Character.prefab` |
| Consumable | `Card_Consumable.prefab` |
| Currency | `Card_Currency.prefab` |
| Equipment | `Card_Equipment.prefab` |
| Material | `Card_Material.prefab` |
| Mob | `Card_Mob.prefab` |
| Mob (Aggressive) | `Card_Mob_Aggressive.prefab` ← separate slot |
| Recipe | `Card_Recipe.prefab` |
| Resource | `Card_Resource.prefab` |
| Structure | `Card_Structure.prefab` |
| Valuable | `Card_Valuable.prefab` |

All prefabs live in: `Assets/_Project/Prefabs/Cards/`

---

## 3. How Packs Work

### PackDefinition

`PackDefinition` extends `CardDefinition` (category = None).

**Create in Unity:** Right-click → `Last Kernel / Pack`
**Save location:** `Assets/_Project/Data/Resources/Packs/`

| Field | Type | Notes |
|---|---|---|
| `buyPrice` | int (≥0) | Cost to purchase the pack |
| `minQuests` | int | Quest completion requirement to unlock |
| `slots` | `List<PackSlot>` | Each slot is one card-draw when opening |

### PackSlot — Draw Logic

Each `PackSlot` contains:

| Field | Purpose |
|---|---|
| `Entries` | Weighted list of `PackEntry` (Card + Weight) |
| `PossibleRecipes` | Optional recipe cards that can appear |
| `RecipeChance` | Float 0–1. Probability recipe replaces normal draw |

**Draw algorithm per slot:**
1. Roll `RecipeChance`. If recipe wins AND there are undiscovered recipes in `PossibleRecipes` → draw a random undiscovered recipe.
2. Otherwise: weighted random selection from `Entries` using `PackEntry.Weight`.

Higher weight = more likely. Example:
```
Wood (weight 10) + IronOre (weight 3) + GoldenKey (weight 1)
→ Wood appears ~71% of the time in that slot
```

### Pack Opening Flow

```
Player clicks PackInstance
  └─► PackInstance.PullFromNextSlot()
        └─► slot.GetRandomCard() → CardDefinition
              └─► CardSpawnService.SpawnCard(def, adjacentPosition)
        UsesLeft--
        If UsesLeft == 0:
          TradeManager.NotifyPackOpened(this)
          Destroy pack
```

---

## 4. Stacking Rules

`StackingRulesMatrix` SO (`Last Kernel / Stacking Rules Matrix`) defines which categories can stack on top of each other as a 2D enum matrix.

**Rule values:**

| Rule | Meaning |
|---|---|
| `None` | Cannot stack |
| `CategoryWide` | Any cards sharing that category can stack together |
| `SameDefinition` | Only identical card definitions stack |

Check rules at runtime:
```csharp
StackingRule rule = matrix.GetRule(bottomCard.category, topCard.category);
```

---

## 5. Reskinning Cards — Art Pipeline

### Where Art Goes

```
Assets/_Project/Art/Sprites/CardArt/<Name>.png
```

One PNG per card. File name **must match** either:
- The `CardDefinition` asset name (e.g., `Card_Warrior.asset` → `Warrior.png`)
- Or the card's `displayName` field

### Required Import Settings

| Setting | Value | Why |
|---|---|---|
| Filter Mode | **Point** | Pixel-crisp, no blur |
| Compression | **None** | Prevents dithering artifacts |
| Generate Mipmaps | **Off** | Pixel art should not mipmap |
| Max Texture Size | **1024** | Enforced cap |
| Sprite Mode | Single | Each file = one sprite |
| Pixels Per Unit | Match project PPU | Consistent scaling |

### Auto-Import System

`CardArtAutoImporter` (Editor script) detects PNG drops into `CardArt/` or `PackArt/` and auto-applies all the above settings, then attempts to wire the texture to the matching `CardDefinition.artTexture` field.

**Auto-wire priority:**
1. SO already has `artTexture` reference → skip (don't overwrite)
2. Asset name match (case-insensitive, strips `Card_` prefix)
3. `displayName` match

If auto-wire fails, assign the texture manually in the `CardDefinition` inspector.

### Changing an Existing Card's Art

1. Drop the new PNG into `Assets/_Project/Art/Sprites/CardArt/`
2. Import settings are auto-applied.
3. Open the `CardDefinition` SO and assign the texture to `artTexture`.
4. No prefab changes needed — `CardView` reads the texture from the definition at runtime.

### Card Prefab Visual Structure

Each category prefab contains:
- **MeshRenderer** — Renders the card face quad. The card art texture drives the main material.
- **BoxCollider** — Interaction bounds. Size it to the card face.
- **TextMeshPro** elements — Title, sell price, nutrition value, health value.
- **CardView** component — Bridges definition data → visual elements at runtime.

To **replace the card frame** (border, background), modify the Material on the MeshRenderer in the category prefab. All cards of that category use the same prefab, so one material change affects all cards in that category.

---

## 6. Reskinning Packs — Art Pipeline

### Where Pack Art Goes

```
Assets/_Project/Art/Sprites/PackArt/<Name>.png
```

Existing packs: Adventure, Beginning, Blacksmith, Construction, Farmstead, HeartyMeals, Island, Knowledge, Revelations, Starter, Survival.

Import settings are identical to CardArt (Point filtering, no mipmaps, no compression).

### Assigning Pack Art

Pack art is assigned via `PackDefinition.artTexture` (inherited from `CardDefinition`). The pack visual uses the same `CardView` presenter as regular cards — the texture flows through the same pipeline.

To reskin a pack:
1. Drop the new PNG into `PackArt/`.
2. Set `PackDefinition.artTexture` to the new texture.

---

## 7. Card Feel & Shader Customization

### CardFeelProfile

`CardFeelProfile` (`Last Kernel / Card Feel Profile`) is the single asset that controls all card animation and shader feedback. One profile is shared via `CardSettings.feelProfile`.

**Sections you can tune:**

| Section | Controls |
|---|---|
| Hover Presets | Scale, duration, easing, flash brightness, glow |
| Drag Presets | Pickup punch, hold scale, drag glow |
| Drop Presets | Squish, settle, spawn animation, merge punch |
| Damage Presets | Shake intensity, flash colour, recovery duration |
| Shader Globals | Idle hue shift amount/frequency, glow colour |
| Snap Settings | Snap duration, easing, overshoot |

### Shader Properties (MaterialPropertyBlock — no material instance created)

`CardFeelPresenter` writes these per-card without instantiating materials:

| Property | Effect |
|---|---|
| `_FlashAmount` | White/colour flash on hit or highlight |
| `_OverlayOffset` | Tiling pattern shift (scanline / grid feel) |
| `_Brightness` | Overall brightness modifier |
| `_Saturation` | Saturation boost when hovered/dragged |
| `_HueShift` | Idle rainbow cycle; set to 0 to disable |
| `_EmissionColor` | Glow color (outline/emission) |

To change the **glow colour** globally: edit `CardFeelProfile → Shader Globals → glow color`.
To change glow for **one card type only**: you'd need a custom `CardFeelProfile` instance and assign it per-card — not currently supported in the base architecture, so keep feels uniform or extend `CardFeelPresenter`.

### Outline / Highlight

The selection outline uses `CardSettings.outlineMaterial`. Replace this material to change highlight style across all cards.

---

## 8. Card UI Layout

### CardView Component

Attached to card prefabs. Exposes:

```csharp
cardView.SetTitle(string text);
cardView.SetArt(Texture texture);
cardView.SetArt(Sprite sprite);
cardView.SetStats(int price, int nutrition, int health);
cardView.SetHighlighted(bool highlighted);
```

`CardInstance.Initialize()` calls these automatically from the `CardDefinition` data. You should not drive `CardView` directly from game logic — mutate the `CardInstance` state and let `CardView` react.

### Text Hierarchy on Cards

| Element | Font Size | Notes |
|---|---|---|
| Card title | Compact, single line preferred | TMP component; driven by localization |
| Stats (price / nutrition / health) | Compact, high contrast | Icon + number pairs |
| Description (detail panels) | Small, word-wrap | Shown in detail panel popup, not on card face |

Per `ART_DIRECTION.md`:
- All text is **localization-ready** — never hardcode strings on cards.
- Allow **30–40% text expansion** for CN/other languages.
- Minimum touch target: **44 px** on any interactive element.

### HUD / Panel UI

Card stat panels and the trading UI live outside the card prefabs:
- Colony report, trade zone, and card limit HUD are separate panel prefabs.
- Card icons in menus reuse the same `artTexture` from the `CardDefinition`.

---

## 9. Localization — Naming Cards & Packs

### Key Format

```
{category}.{asset_name_normalized}.{field}
```

| Asset | Field | Generated Key |
|---|---|---|
| `Card_Warrior.asset` | name | `card.card_warrior.name` |
| `Card_Warrior.asset` | description | `card.card_warrior.description` |
| `Pack_Starter.asset` | name | `pack.pack_starter.name` |
| `Pack_Starter.asset` | description | `pack.pack_starter.description` |

**Normalization rules:** lowercase, strip non-alphanumeric (keep underscores), e.g. `"Card Warrior"` → `"card_warrior"`.

### Adding Translations

Locale files live at:
```
Assets/_Project/Localization/Locales/
  ├── en.json       (English)
  ├── zh-Hans.json  (Simplified Chinese)
  ├── fr.json
  ├── de.json
  ├── ja.json
  └── ko.json
```

For each new card, add entries in at least `en.json` and `zh-Hans.json`:
```json
"card.card_newcard.name": "New Card",
"card.card_newcard.description": "Does something interesting."
```

Recipe cards are generated at runtime and use a `"recipe:"` prefix key — the discovery service manages these dynamically.

---

## 10. Creating a New Card (Step-by-Step)

### Step 1 — Create the ScriptableObject

1. Navigate to `Assets/_Project/Data/Resources/Cards/<Category>/`
2. Right-click → **Last Kernel → Card**
3. Name it `Card_<YourName>` (e.g., `Card_IronShield`)

### Step 2 — Fill in the Definition

In the Inspector:
- Set **category** first — this determines prefab and stacking behaviour.
- Set **faction** (Neutral for most resources/structures, Player for characters, Mob for enemies).
- Set **combatType** if the card fights.
- Fill in stats appropriate to the category.
- Set `isSellable` and `sellPrice` if applicable.
- Set `hasDurability` + `uses` if the card is consumed.

### Step 3 — Add Art

1. Create or commission pixel art at the correct resolution (see §5).
2. Drop the PNG into `Assets/_Project/Art/Sprites/CardArt/`
3. Auto-importer applies correct settings.
4. Assign the texture to `artTexture` in the SO inspector if not auto-wired.

### Step 4 — Add Localization Keys

In `Assets/_Project/Localization/Locales/en.json`:
```json
"card.card_ironshield.name": "Iron Shield",
"card.card_ironshield.description": "A heavy shield forged from refined iron."
```
Add the same keys in `zh-Hans.json` with the Chinese translation.

### Step 5 — Validate

Run `Tools → LAST KERNEL → Validate Project`. Fix any missing reference or duplicate ID warnings before proceeding.

### Step 6 — Optionally Add to Packs

Open any `PackDefinition` in `Assets/_Project/Data/Resources/Packs/`.
Add a `PackEntry` in the relevant `PackSlot.Entries` with your new card and a weight.

---

## 11. Creating a New Pack (Step-by-Step)

### Step 1 — Create the ScriptableObject

1. Navigate to `Assets/_Project/Data/Resources/Packs/`
2. Right-click → **Last Kernel → Pack**
3. Name it `Pack_<Theme>` (e.g., `Pack_Dungeon`)

### Step 2 — Configure Pack Fields

| Field | Guidance |
|---|---|
| `artTexture` | Pack cover art (see §6) |
| `displayName` | Will be localized — set key in localization files |
| `buyPrice` | Coin cost. Tune against `BALANCE_MODEL.md` |
| `minQuests` | How far into the game before this unlocks (0 = always) |
| `slots` | Each slot = one card drawn when opened |

### Step 3 — Define Slots

Each `PackSlot`:
1. Add `PackEntry` items to `Entries`. Each entry needs a `Card` reference and a `Weight`.
2. Optionally add `PossibleRecipes` + set `RecipeChance` (0.0–1.0).

**Design guidelines:**
- 3–6 slots per pack is typical.
- Spread weights so common resources appear frequently and rare cards appear seldom.
- Use `RecipeChance` 0.05–0.15 for packs with a crafting theme.

### Step 4 — Add Art

Drop a PNG into `Assets/_Project/Art/Sprites/PackArt/` and assign to `artTexture`.

### Step 5 — Add Localization Keys

```json
"pack.pack_dungeon.name": "Dungeon Pack",
"pack.pack_dungeon.description": "Venture into the dark. Monsters and riches await."
```

### Step 6 — Register in Shop / TradeZone

Packs become available in-game via the `TradeZone` or pack-slot UI. Wire the new `PackDefinition` to the appropriate `TradeManager` or market configuration asset.

---

## 12. Save & Restore System

Cards are serialized as `CardData` (in `GameData.cs`):

```csharp
public class CardData
{
    public string Id;           // CardDefinition.id (GUID)
    public int UsesLeft;
    public int CurrentHealth;
    public int CurrentNutrition;
    public int StoredCoins;     // Chest state
    public bool IsAILocked;
    public string OriginalId;   // Pre-class-change definition (characters only)
    public List<CardData> EquippedItems;
}
```

- **Only runtime state** is saved — definitions are always reloaded from the catalog.
- Class-changed characters preserve their pre-change `OriginalId` for reversion.
- Equipped items are recursively serialized inside `EquippedItems`.

If you add a new persistent field to a card type, add the corresponding property to `CardData` and update `CardSaveRestoreService` to read/write it.

---

## 13. Quick Reference — Enums & Constants

### CardCategory
```
None, Resource, Character, Consumable, Material, Equipment,
Structure, Currency, Recipe, Mob, Area, Valuable
```

### CardFaction
```
Neutral, Player, Mob
```

### CombatType
```
None, Melee, Ranged, Magic
```

### EquipmentSlot
```
Weapon, Armor, Accessory
```

### StackingRule
```
None, CategoryWide, SameDefinition
```

### StatType (used in StatModifier)
```
MaxHealth, Attack, Defense, AttackSpeed, Accuracy, Dodge,
CriticalChance, CriticalMultiplier
```

### ModifierType
```
Additive, Multiplicative
```

### Key Paths

| What | Path |
|---|---|
| Card SOs | `Assets/_Project/Data/Resources/Cards/<Category>/` |
| Pack SOs | `Assets/_Project/Data/Resources/Packs/` |
| Card prefabs | `Assets/_Project/Prefabs/Cards/` |
| Card art PNGs | `Assets/_Project/Art/Sprites/CardArt/` |
| Pack art PNGs | `Assets/_Project/Art/Sprites/PackArt/` |
| Localization | `Assets/_Project/Localization/Locales/` |
| CardDefinition script | `Assets/_Project/Scripts/Runtime/Cards/Definitions/CardDefinition.cs` |
| PackDefinition script | `Assets/_Project/Scripts/Runtime/Packs/PackDefinition.cs` |
| CardSpawnService | `Assets/_Project/Scripts/Runtime/Cards/CardSpawnService.cs` |
| CardManager | `Assets/_Project/Scripts/Runtime/Cards/CardManager.cs` |
| CardFeelProfile | `Assets/_Project/Scripts/Runtime/Cards/Presentation/CardFeelProfile.cs` |
| CardArt auto-importer | `Assets/_Project/Scripts/Editor/CardArtAutoImporter.cs` |
| Stacking rules SO | Any asset created via `Last Kernel / Stacking Rules Matrix` |
