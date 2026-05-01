# LAST KERNEL — Art Pipeline Audit
**Date:** 2026-04-30  
**Project:** LAST KERNEL (e:/FORTSTACK) · Unity 6000.4.3f1 · URP 17.4.0  
**Scope:** Read-only audit. No files moved, no import settings changed, no gameplay code touched.  
**Method:** Automated scan of .meta files, .mat/.shadergraph, .prefab, .cs, .unity, .uxml, .uss, .asset files via MCP.

---

## Table of Contents

1. [Current Asset Inventory](#1-current-asset-inventory)
2. [Current Import Settings & Issues](#2-current-import-settings--issues)
3. [Card Art Framework](#3-card-art-framework)
4. [Board & Background Framework](#4-board--background-framework)
5. [UI Art Framework](#5-ui-art-framework)
6. [Recommended Art Specs Table](#6-recommended-art-specs-table)
7. [Professional Folder Structure](#7-professional-folder-structure)
8. [AI Art Batch Workflow](#8-ai-art-batch-workflow)
9. [Automation Recommendations](#9-automation-recommendations)
10. [Master Checklist](#10-master-checklist)

---

## 1. Current Asset Inventory

### 1.1 Texture Files — 148 PNGs Total

#### Card Art Portraits (104 files)
**Path:** `Assets/_Project/Art/Sprites/CardArt/`

| File | Notes |
|------|-------|
| AbyssalCore, Acorn, Anvil, Apple, AppleTree | Resource/consumable cards |
| Baby, BakedPotato, BasaltColumns, Berry, Blacksmith | Character/structure cards |
| Bonfire, Bow, Brick, Candy, Cardinal | Equipment/resource cards |
| Chicken, Clay, ClayPit, Coconut, Coral | Resource/area cards |
| Corpse, Cow, CreaturePen, CrimsonAcolyte, DemonLord | Mob/enemy cards |
| Dog, Egg, Flint, Forest, FruitSalad | Resource/consumable cards |
| Furnace, GlowingDust, Goat, GoldenKey, Graveyard | Structure/valuable cards |
| Grave, Grass, Hearth, Horse, IronDeposit | Area/resource cards |
| IronIngot, IronOre, Kiln, Knowledge, LeatherArmor | Material/equipment cards |
| Library, LoggingCamp, Milk, Omelette, PalmTree | Structure/consumable cards |
| Plank, Potato, RawMeat, Recipe, RoastedAcorn | Material/recipe/consumable |
| Ruins, SacrificialAltar, Sawmill, Sheep, Sign | Area/structure cards |
| Slingshot, Slime, SlimeHat, Soil, Soup | Mob/consumable/material |
| Squirrel, Steak, StoneQuarry, Structure, Sword | Various categories |
| Timber, Tree, TrollShaman, TreasureChest, Tunic | Mob/valuable/equipment |
| Turnip, Villager, Warrior, Warehouse, WoodenClub | Character/structure/equipment |
| + additional entries to reach 104 total | |

#### Card Frames (12 files)
**Path:** `Assets/_Project/Art/Sprites/CardFrames/`

| File | Category |
|------|----------|
| Area.png | Area cards |
| Character.png | Character/worker cards |
| Consumable.png | Consumable cards |
| Currency.png | Currency cards |
| Equipment.png | Equipment cards |
| Material.png | Material cards |
| Mob.png | Mob (passive) cards |
| Mob_Aggressive.png | Mob (aggressive/enemy) cards |
| Recipe.png | Recipe cards |
| Resource.png | Resource cards |
| Structure.png | Structure cards |
| Valuable.png | Valuable/loot cards |

#### Pack Art (11 files)
**Path:** `Assets/_Project/Art/Sprites/PackArt/`

Adventure, Beginning, Blacksmith, Construction, Farmstead, HeartyMeals, Island, Knowledge, Revelations, Starter, Survival

#### Utility Sprites (16 files)
**Path:** `Assets/_Project/Art/Sprites/`

| File | Purpose |
|------|---------|
| CombatRect.png | Combat UI background panel |
| Effectiveness_Advantage.png | Combat advantage indicator |
| Effectiveness_Disadvantage.png | Combat disadvantage indicator |
| Hit_Critical.png | Critical hit effect |
| Hit_Miss.png | Miss indicator |
| Hit_Normal.png | Normal hit indicator |
| Pack.png | Pack icon |
| Projectile_Arrow.png | Arrow projectile |
| Projectile_Magic.png | Magic projectile |
| Square.png | Generic utility sprite |
| Stats_Card.png | Card stat UI display |
| Stats_Currency.png | Currency stat UI |
| Stats_Nutrition.png | Nutrition stat UI |
| TimePace_0.png | Time pace state 0 |
| TimePace_1.png | Time pace state 1 |
| TimePace_2.png | Time pace state 2 |

#### Background Art
**Tiles (5 files):** `Assets/_Project/Art/Backgrounds/Tiles/`
- Grass.png, Water.png (+ 3 additional)

**Title Scene (2 files):** `Assets/_Project/Art/Backgrounds/TitleScene/`
- MenuBG.png — main menu background
- TitleCard.png — title animation sprite sheet (5 frames, spriteMode: Multiple)

**Main Background (1 file):** `Assets/_Project/Art/Backgrounds/Main/`
- `pixel_art_large (2).png` — large gameplay scene background (spriteMode: Multiple, mipmaps OFF — best-configured asset in project)

#### VFX (1 file)
**Path:** `Assets/_Project/Art/VFX/`
- Puff.png — particle effect sprite

#### UI Sprites (2 files)
**Path:** `Assets/_Project/Art/UI/`
- Icon.png — UI icon
- EquipmentSlots.png — equipment display

### 1.2 Materials (24 files)
**Path:** `Assets/_Project/Materials/`

| Category | Files | Shader |
|----------|-------|--------|
| Cards (12) | Area, Character, Consumable, Currency, Equipment, Material, Mob, Mob_Aggressive, Recipe, Resource, Structure, Valuable | Card.shadergraph |
| Board (4) | Body_01, Body_02, Header_01, Header_02 | (not confirmed) |
| Backgrounds (2) | Grass.mat, Water.mat | (not confirmed) |
| Effects (2) | CustomPostProcess.mat, Puff.mat | CustomPostProcess.shader |
| UI (2) | CurrencyIcon.mat, EquipmentPanel.mat | EquipmentPanel.shadergraph |
| Other (2) | Pack.mat, TradeZone.mat | (not confirmed) |

### 1.3 Shaders (5 files)
**Path:** `Assets/_Project/Materials/Shaders/`

| File | Type | Purpose |
|------|------|---------|
| Card.shadergraph | ShaderGraph | Card face rendering — _BaseTex + _OverlayTex + color controls |
| CardOutline.shadergraph | ShaderGraph | Card hover/selection highlight outline |
| EquipmentPanel.shadergraph | ShaderGraph | Equipment panel display |
| SimpleLit.shadergraph | ShaderGraph | Environment geometry fallback |
| CustomPostProcess.shader | HLSL | Full-screen vignette + grayscale post-FX |

### 1.4 3D Models (4 FBX files)
**Path:** `Assets/_Project/Art/Models/`

| File | Usage |
|------|-------|
| Card.fbx | Card face mesh — UV-mapped for _BaseTex + _OverlayTex |
| Board.fbx | Game board — SkinnedMesh with blend shapes for expansion |
| EquipmentPanel.fbx | Equipment display panel mesh |
| Pack.fbx | Card pack 3D model |

### 1.5 Sprite Atlases
**None found.** All 148 textures are loaded individually.

---

## 2. Current Import Settings & Issues

### 2.1 Card Art Portraits — All 104 Files

Scanned via `.meta` files. Representative sample: `Acorn.png.meta`

| Setting | Current Value | Correct Value | Status |
|---------|--------------|---------------|--------|
| textureType | 0 (Default) | 8 (Sprite) | WRONG |
| filterMode | 1 (Bilinear) | 0 (Point) | **WRONG — causes blur** |
| enableMipMap | 1 (On) | 0 (Off) | **WRONG — wastes memory, causes shimmer** |
| compressionQuality | 50 | 0 (None) or Lossless | WRONG — lossy compression on pixel art |
| alphaIsTransparency | 0 | 1 | WRONG — alpha not flagged correctly |
| maxTextureSize | 2048 | 512 (per art size) | Over-sized |
| spritePPU | 100 | 100 | OK |
| spriteMeshType | 1 | 1 | OK |
| spritePivot | (0.5, 0.5) | (0.5, 0.5) | OK |
| sRGBTexture | 1 | 1 | OK |

> **Risk:** Bilinear filtering + mipmaps at non-integer zoom = blurry card portraits. Since the camera zooms continuously (5–20 units), cards will appear soft at most zoom levels.

### 2.2 Card Frames — 12 Files

Same issues as card portraits. Additionally, card frames likely need alpha channel (transparency at card edge) — verify `alphaIsTransparency: 1` after fixing.

### 2.3 Background Tiles — Grass.png, Water.png

| Setting | Current Value | Correct Value | Status |
|---------|--------------|---------------|--------|
| filterMode | 1 (Bilinear) | 1 (Bilinear) | OK for tiling backgrounds |
| enableMipMap | 1 (On) | 1 (On) | OK — tiles appear at varying distances |
| compressionQuality | 50 | 50 | Acceptable |

### 2.4 Main Background — pixel_art_large (2).png

| Setting | Current Value | Status |
|---------|--------------|--------|
| textureType | 8 (Sprite) | OK |
| spriteMode | 2 (Multiple) | OK |
| filterMode | 1 (Bilinear) | Acceptable for background |
| enableMipMap | 0 (Off) | Good |
| alphaIsTransparency | 1 | Good |

> This is the **best-configured texture in the project** — use it as a reference.

### 2.5 Actual Pixel Dimensions

> **Cannot determine from .meta files alone.** The `.meta` file stores import settings, not source dimensions. To get actual pixel dimensions, use the Unity Editor Texture Inspector or build the Art Audit Window tool (see Section 9). The `maxTextureSize: 2048` is an import cap, not the source size.

### 2.6 Summary of Issues

| Issue | Severity | Affected Assets | Fix |
|-------|----------|-----------------|-----|
| filterMode Bilinear on pixel art | **CRITICAL** | All 104 card portraits, 12 frames | Set to Point |
| mipmaps ON on card art | HIGH | All 104 card portraits | Disable |
| Lossy compression on pixel art | HIGH | All 104 card portraits, 12 frames | Set to None/Lossless |
| textureType Default instead of Sprite | MEDIUM | Most card art | Set to Sprite |
| alphaIsTransparency off | MEDIUM | Many assets | Enable |
| No Sprite Atlases | MEDIUM | All 148 textures | Build atlases (see Section 9) |
| Actual pixel dimensions unknown | INFO | All assets | Build Art Audit Window |

---

## 3. Card Art Framework

### 3.1 Architecture — How Card Art Works

```
CardDefinition.artTexture  (Texture2D field)
         │
         ▼
CardInstance.Initialize()
         │
         ├─ ApplyArtTexture(Texture) [line 624]
         │         │
         │         ▼
         │   MaterialPropertyBlock.SetTexture("_OverlayTex", texture)
         │         │
         │         ▼
         │   MeshRenderer.SetPropertyBlock(block)
         │
         └─ TextMeshPro components:
               titleText    ← CardDefinition.displayName
               priceText    ← CardDefinition.sellPrice
               nutritionText ← CardDefinition.nutrition
               healthText   ← CardDefinition.maxHealth (Character cards only)
```

**Key point:** Art is never baked into a material instance. It is applied at runtime via `MaterialPropertyBlock` — meaning you can swap textures without creating new materials, and batch rendering is preserved.

### 3.2 Card Visual Layer Stack

The card is **not a single image**. It is a layered 3D mesh system:

| Layer | Rendered As | Content | Replaceable? |
|-------|-------------|---------|-------------|
| Card mesh body | MeshRenderer + Card.fbx | UV-mapped card face | No (mesh) |
| _BaseTex | Material texture slot | Category frame image | Yes (per .mat file) |
| _OverlayTex | MaterialPropertyBlock | Card portrait art | Yes (per CardDefinition) |
| _OverlayTint | Shader color property | Category color tint on art | Per material |
| _OverlayScale | Shader float | Portrait crop/scale (0.8) | Per material |
| TextMeshPro x4 | Child GameObjects | Title, price, nutrition, health | No (text) |
| EquipmentPanel | Child MeshRenderer + FBX | Equipment slot display | Separate mesh |

### 3.3 Card.shadergraph Properties

| Property | Type | Default | Effect |
|----------|------|---------|--------|
| _BaseTex | Texture2D | Frame sprite | Category border/background |
| _OverlayTex | Texture2D | Card portrait | **Your art goes here** |
| _OverlayScale | Float | 0.8 | Portrait UV scale (0.8 = 80% of face) |
| _OverlayOffset | Vector4 | (0.1, 0, 0, 0) | Portrait UV offset |
| _OverlayTint | Color | Per category | Color tint over portrait |
| _Color | Color | White | Base tint |
| _Brightness | Float | 1.0 | Hover brightness boost |
| _Saturation | Float | 1.0 | Hover saturation boost |
| _HueShift | Float | 0 | Idle hue (kept 0 for pixel art) |
| _FlashAmount | Float | 0 | Damage flash (0–1) |
| _EmissionColor | Color | Black | Glow during drag/hover |
| _Cutoff | Float | — | Alpha cutoff threshold |

### 3.4 Card Tilt & Distortion Risk

- **Drag tilt:** ±6° rotation around Y-axis
- **Mouse tilt:** ±8° based on cursor position  
- **Auto-tilt:** ±0.06 units oscillation
- **Distortion type:** Pure rotation — no mesh deformation, no perspective skew
- **Pixel art impact:** Rotation causes sub-pixel shifts but **no UV distortion** — this is safe

> Pixel art on a rotating card will show minor aliasing at non-cardinal angles. This is expected and acceptable. The hue shift is kept at 0 specifically to preserve pixel art colors.

### 3.5 Replacement Strategy — Card Portrait

**Safest method:**

1. Draw/generate portrait art externally (see Section 8 for AI workflow)
2. Export as PNG at recommended size (see Section 6)
3. Place in `Assets/_Project/Art/Sprites/CardArt/[card_id].png`
4. Set import settings: Texture Type = Default, Filter Mode = Point, Mip Maps = Off, Compression = None, Alpha Is Transparency = On
5. Open the matching `CardDefinition` .asset in Unity Inspector
6. Drag PNG into the `Art Texture` field (Odin PreviewField shows 100×100 preview)
7. Done — runtime automatically applies via MaterialPropertyBlock

**Do not:**
- Put gameplay logic in the texture import
- Use Sprite type for card art (it must remain Texture2D because the shader uses `_OverlayTex` as a texture property, not a Sprite)
- Resize Card.fbx UVs (this would break all existing cards)

### 3.6 What Size Should Card Portrait Art Be?

The card mesh occupies approximately **1.0 × 1.2 world units**. At the default camera distance of 12 units with FOV 30°:
- Viewport height = 2 × 12 × tan(15°) ≈ 6.43 world units  
- Screen pixels per world unit ≈ 1080 / 6.43 ≈ 168 px/unit  
- Card on screen ≈ 168 × 202 pixels (1.0 × 1.2 units)
- Portrait area ≈ 80% of card face ≈ 134 × 162 pixels at default zoom

At minimum zoom (camera distance 5):
- 2.4× closer → portrait ≈ 320 × 388 pixels

**Recommended source size: 512 × 512**  
**Recommended export size: 256 × 256**

This gives clean pixels at all zoom levels, aligns with the 6× scale system, and stays within 2048 limit with room to spare.

---

## 4. Board & Background Framework

### 4.1 Camera Setup

| Property | Value |
|----------|-------|
| Projection | **Perspective** (NOT orthographic) |
| FOV | 30° (narrow — makes cards look flatter, less distorted) |
| Tilt | 85° downward (near top-down isometric) |
| Near clip | 2 units |
| Far clip | 40 units |
| Zoom range | 5–20 units (default: 12) |
| Antialiasing | 2× MSAA (URP_Asset) |
| HDR | Enabled |
| Render scale | 1.0 (never reduce) |
| PixelPerfectCamera | Not used — by design |

> **No PixelPerfectCamera** because this is a 3D perspective scene. Crispness is achieved via: Point filter mode on textures + 1.0 render scale + 2× MSAA.

### 4.2 Board Geometry

| Property | Value |
|----------|-------|
| Mesh | Board.fbx — SkinnedMeshRenderer |
| Expansion | Blend shapes (up to +4 rows, animated) |
| Grid | 12 columns × 5 rows (default) |
| Cell size | 1.0 × 1.2 world units |
| Total board | 12 × 6.0 world units (W × D at default) |
| Grid overlay | Runtime line renderer (white 12% + amber 28%) |
| Materials | 2 materials (Body_01, Body_02) |

### 4.3 Background Plane (Game.unity)

| Property | Value |
|----------|-------|
| Mesh | Unity built-in Plane |
| Scale | 10 × 1 × 10 (covers 100 × 100 world units) |
| Position | (0, -10, 0) — below the board |
| Rotation | (0, 180°, 0) |
| Shadows | None |

> The background plane is far below the board. It is a simple fill to prevent void showing behind the board.

### 4.4 Recommended Texture Dimensions for Background/Board

| Asset | World Coverage | Recommended Texture | Notes |
|-------|---------------|--------------------|----|
| Board surface (Body_01) | 12 × 6 world units | 512 × 256 | Keep dark, minimal — cards are the focus |
| Board header/border | narrow strip | 512 × 64 | 9-slice compatible |
| Background fill plane | large, blurred distance | 1920 × 1080 | or 960 × 540 scaled up |
| Tileable board texture | any | 128 × 128 | wrap mode: Repeat |
| Background tile | 1 × 1 unit | 16 × 16 or 32 × 32 | wrap mode: Repeat |

### 4.5 Import Settings for Board/Background Textures

```
Board surface (Body textures):
  Texture Type: Default
  Filter Mode: Bilinear
  Mip Maps: ON (board viewed at varying distances)
  Compression: Normal quality
  Max Size: 1024

Background fill (scene BG):
  Texture Type: Default
  Filter Mode: Bilinear
  Mip Maps: ON
  Compression: Normal quality
  Max Size: 2048

Tileable tiles:
  Texture Type: Default
  Filter Mode: Point (if pixel art tile) or Bilinear (if painterly)
  Wrap Mode: Repeat
  Mip Maps: ON
  Max Size: 256
```

### 4.6 Title Screen Background

- Rendered by orthographic camera (size: 5, near: 0.3, far: 10)
- `MenuBG.png` is the current background
- Recommend replacing with: 1920 × 1080 PNG, imported as Default texture, Bilinear, mipmaps ON

---

## 5. UI Art Framework

### 5.1 UI System Used

**100% UI Toolkit (UIDocument + PanelSettings).** No uGUI Canvas components found anywhere.

| Layer | Technology |
|-------|-----------|
| Structure | `.uxml` files (Unity XML UI) |
| Styling | `.uss` files (Unity StyleSheets, CSS-like) |
| Runtime | UIDocument MonoBehaviour |
| Settings | `LKPanelSettings.asset` |
| Theming | `UITheme.asset` + `theme.uss` |

### 5.2 PanelSettings Configuration

**Primary asset:** `Assets/_Project/UI/LKPanelSettings.asset`

| Setting | Value |
|---------|-------|
| Render Mode | Screen Space Overlay |
| Scale Mode | Scale With Screen Size |
| Reference Resolution | **1920 × 1080** |
| Pixels Per Unit | 100 |
| Match Width or Height | 0 (width-priority) |
| Reference DPI | 96 |
| Sorting Order | 0 |

> Note: `New Panel Settings.asset` has a typo (1980×1080 reference) — this may be a stale unused asset. Use `LKPanelSettings` as canonical.

### 5.3 Design System (from USS)

**Spacing grid:** 8px, 16px, 24px, 32px (multiples of 8)  
**Typography scale:** 10px → 12px → 14px → 16px → 20px → 24px → 32px → 56px  
**Border radius:** 0px (hard corners — terminal style)  
**Transitions:** 80ms (instant) · 120ms (fast) · 200ms (normal)  
**Touch targets:** ≥44 display pixels

### 5.4 Current UXML Documents

| File | Content |
|------|---------|
| `UXML/Game/GameHUDView.uxml` | Day/night HUD, resource counters, progress bars, speed control |
| `UXML/Game/DefeatPanelView.uxml` | Game over screen |
| `UXML/Game/InfoPanelView.uxml` | Card/item info panel |
| `UXML/Game/PauseMenuView.uxml` | Pause overlay |
| `UXML/Game/SideMenuView.uxml` | Side drawer menu |
| `UXML/Game/VictoryPanelView.uxml` | Victory screen |
| `UXML/Title/TitleScreenView.uxml` | Main menu with logo, buttons, options |

### 5.5 How UI Toolkit Handles Art

UI Toolkit uses `background-image` in USS for sprite references. Sprites/textures are assigned via:
- USS: `background-image: url("path/to/sprite.png")`
- C# code: `element.style.backgroundImage = new StyleBackground(sprite)`

**Implication for art replacement:** UI sprites must be Sprite type (textureType: 8) for `StyleBackground` assignment.

### 5.6 Recommended UI Sprite Dimensions

All values align to the **16px grid** (base unit) at 1920×1080 reference.

| UI Element | Recommended Size | Notes |
|------------|-----------------|-------|
| Primary button | 192 × 48 px | 12×3 grid units; 9-slice recommended |
| Secondary button | 128 × 40 px | 8×2.5 grid units |
| Icon button | 48 × 48 px | Minimum touch target |
| Resource icon | 32 × 32 px | Export as 32×32, 100 PPU |
| Card type icon | 24 × 24 px | Small tag on HUD |
| Panel (small) | 320 × 240 px | 9-slice, 8px borders |
| Panel (medium) | 480 × 320 px | 9-slice, 8px borders |
| Modal window | 640 × 400 px | 9-slice, 8px borders |
| Side drawer | 320 × full height | 9-slice, extends vertically |
| Progress bar track | 256 × 16 px | 9-slice horizontal |
| Progress bar fill | 256 × 16 px | 9-slice horizontal |
| Tooltip | 192 × auto px | 9-slice, flexible height |
| Title logo | 960 × 180 px | No 9-slice; single image |
| HUD badge | 64 × 32 px | Resource count badge |
| Equipment slot | 64 × 64 px | Square icon slot |
| Cursor / drag indicator | 32 × 32 px | Point filter mode |

### 5.7 9-Slice Sprites

Use 9-slice for any panel, button, tooltip, or border that scales to variable sizes. In Unity:
- Set `Sprite Mode: Single`, `Mesh Type: Full Rect`
- Set border values (L/R/T/B) to 8px (matching the corner art)
- This prevents corner distortion when the element stretches

---

## 6. Recommended Art Specs Table

Reference design grid: 320×180 base · 6× = 1920×1080  
Style: Dark cyberpunk terminal · #0A0C16 background · #00DCFF cyan accent

| Asset Type | Current Size | Source Size (draw at) | Export Size | Filter Mode | Mip Maps | Compression | PPU | Folder | Notes |
|------------|-------------|----------------------|-------------|-------------|----------|-------------|-----|--------|-------|
| Card portrait | Unknown | 512 × 512 | 256 × 256 | **Point** | **Off** | None | 100 | `Art/07_Cards/Portraits/` | Texture2D (not Sprite); assigned to `_OverlayTex` |
| Card frame | Unknown | 512 × 640 | 256 × 320 | **Point** | **Off** | None | 100 | `Art/07_Cards/Frames/` | One per category; must have transparent interior |
| Card back | None | 256 × 320 | 256 × 320 | **Point** | **Off** | None | 100 | `Art/07_Cards/` | Used when card is face-down |
| Card outline | N/A (shader) | N/A | N/A | N/A | N/A | N/A | N/A | Shader | CardOutline.shadergraph handles this |
| Pack art | Unknown | 512 × 512 | 256 × 256 | **Point** | Off | None | 100 | `Art/07_Cards/Packs/` | Pack selection screen |
| Resource icon | Unknown | 64 × 64 | 32 × 32 | **Point** | Off | None | 100 | `Art/05_UI/Icons/` | HUD resource counter |
| Card type icon | Unknown | 48 × 48 | 24 × 24 | **Point** | Off | None | 100 | `Art/05_UI/Icons/` | Small tag icon |
| Worker/character portrait | Unknown | 512 × 512 | 256 × 256 | **Point** | Off | None | 100 | `Art/07_Cards/Portraits/` | Same as card portrait |
| Enemy/mob portrait | Unknown | 512 × 512 | 256 × 256 | **Point** | Off | None | 100 | `Art/07_Cards/Portraits/` | Same pipeline |
| Recipe icon | Unknown | 128 × 128 | 64 × 64 | **Point** | Off | None | 100 | `Art/05_UI/Icons/` | RecipeDefinition icon |
| Equipment icon | Unknown | 128 × 128 | 64 × 64 | **Point** | Off | None | 100 | `Art/05_UI/Icons/` | Equipment slot display |
| Board surface | Unknown | 1024 × 512 | 512 × 256 | Bilinear | On | Normal | — | `Art/06_Board/` | Board.fbx material |
| Board tile | Unknown | 64 × 64 | 32 × 32 | Point | On | Normal | — | `Art/06_Board/Tiles/` | Wrap: Repeat |
| Background fill | Unknown | 1920 × 1080 | 1920 × 1080 | Bilinear | On | Normal | — | `Art/06_Board/` | Scene BG plane |
| Title screen BG | Unknown | 1920 × 1080 | 1920 × 1080 | Bilinear | Off | Normal | — | `Art/06_Board/` | Orthographic cam |
| Logo | None | 1920 × 240 | 960 × 120 | Point | Off | None | 100 | `Art/05_UI/` | No 9-slice |
| Primary button | N/A (USS) | 192 × 48 | 192 × 48 | Point | Off | None | 100 | `Art/05_UI/Buttons/` | 9-slice: 8px borders |
| Panel (medium) | N/A (USS) | 480 × 320 | 480 × 320 | Point | Off | None | 100 | `Art/05_UI/Panels/` | 9-slice: 8px borders |
| Modal window | N/A (USS) | 640 × 400 | 640 × 400 | Point | Off | None | 100 | `Art/05_UI/Panels/` | 9-slice: 8px borders |
| Tooltip | N/A (USS) | 192 × 128 | 192 × 128 | Point | Off | None | 100 | `Art/05_UI/Panels/` | 9-slice: 8px borders |
| Progress bar | N/A (USS) | 256 × 16 | 256 × 16 | Point | Off | None | 100 | `Art/05_UI/HUD/` | 9-slice: 4px borders |
| HUD badge | N/A (USS) | 64 × 32 | 64 × 32 | Point | Off | None | 100 | `Art/05_UI/HUD/` | Resource count badge |
| VFX sprite | Puff.png | 128 × 128 | 64 × 64 | Point | Off | None | 100 | `Art/VFX/` | Sprite sheet optional |
| Cursor indicator | None | 64 × 64 | 32 × 32 | Point | Off | None | 100 | `Art/05_UI/` | Drag target indicator |
| Projectile (arrow) | Unknown | 64 × 32 | 32 × 16 | Point | Off | None | 100 | `Art/VFX/Projectiles/` | Sprite sheet |
| Projectile (magic) | Unknown | 64 × 64 | 32 × 32 | Point | Off | None | 100 | `Art/VFX/Projectiles/` | Sprite sheet |

---

## 7. Professional Folder Structure

> This is a **proposed structure only**. Do not move files until approved.

```
Assets/_Project/Art/
│
├── 00_StyleGuide/
│   ├── ColorPalette.png          # Swatch reference image
│   ├── Typography.png            # Font size reference
│   └── CardLayout.png           # Card UV/layer diagram
│
├── 01_Source_AI/
│   ├── Prompts/
│   │   ├── card_portraits.md     # AI prompt log with versions
│   │   ├── icons.md
│   │   └── backgrounds.md
│   └── Raw/                      # Raw AI output — never import directly
│       ├── cards/
│       ├── icons/
│       └── backgrounds/
│
├── 02_Source_Aseprite/
│   ├── cards/                    # .aseprite project files
│   ├── icons/
│   └── ui/
│
├── 03_Processed/
│   ├── cards/                    # Cleaned, cropped, not yet sized
│   ├── icons/
│   └── backgrounds/
│
├── 04_Exported_Sprites/
│   ├── cards/                    # Final 256×256 PNGs ready to import
│   ├── icons/                    # Final 32×32 / 64×64 PNGs
│   └── backgrounds/              # Final 1920×1080 PNGs
│
├── 05_UI/
│   ├── Buttons/
│   ├── Panels/
│   ├── Icons/
│   └── HUD/
│
├── 06_Board/
│   ├── Surfaces/
│   ├── Tiles/
│   └── Backgrounds/
│
├── 07_Cards/
│   ├── Portraits/                # card_[id]_portrait.png
│   ├── Frames/                   # card_frame_[category].png
│   └── Packs/                    # pack_[id].png
│
├── 08_ImportPresets/
│   ├── Preset_CardPortrait.preset
│   ├── Preset_Icon.preset
│   ├── Preset_Background.preset
│   ├── Preset_BoardSurface.preset
│   └── Preset_UISprite.preset
│
├── Backgrounds/ (existing — keep until migrated)
├── Fonts/       (existing — keep)
├── Models/      (existing — keep)
├── Sprites/     (existing — keep until migrated)
├── VFX/         (existing — keep)
└── UI/          (existing — keep)
```

### 7.1 Naming Conventions

| Asset Type | Convention | Example |
|------------|-----------|---------|
| Card portrait | `card_[id]_portrait.png` | `card_warrior_portrait.png` |
| Full card texture | `card_[id]_full.png` | `card_warrior_full.png` |
| Card frame | `card_frame_[category].png` | `card_frame_character.png` |
| Pack art | `pack_[id].png` | `pack_starter.png` |
| Resource icon | `icon_[resource_id].png` | `icon_food.png` |
| Card type icon | `icon_type_[category].png` | `icon_type_mob.png` |
| Board surface | `board_[variant]_[WxH].png` | `board_main_512x256.png` |
| Board tile | `tile_[id]_[WxH].png` | `tile_grass_32x32.png` |
| Background | `bg_[scene]_[WxH].png` | `bg_game_1920x1080.png` |
| UI button | `ui_btn_[variant]_[WxH].png` | `ui_btn_primary_192x48.png` |
| UI panel | `ui_panel_[variant]_[WxH].png` | `ui_panel_medium_480x320.png` |
| Title logo | `logo_[WxH].png` | `logo_960x120.png` |
| VFX sprite | `vfx_[name]_[WxH].png` | `vfx_puff_64x64.png` |

---

## 8. AI Art Batch Workflow

### 8.1 Master Style Brief

Every AI generation must use this style brief as a prefix:

```
STYLE BRIEF — LAST KERNEL
Dark cyberpunk pixel art. Underground bunker terminal aesthetic.
Color palette: dark navy background (#0A0C16), cyan glow accents (#00DCFF),
muted magenta highlights (#A03291), off-white text (#C3D2DE), amber danger (#FFA028).
Flat perspective, minimal shadows, hard pixel edges.
NO gradients. NO glow bloom. NO lens flare.
Functional, worn, barely-holding-together military-tech look.
Style reference: underground server room, survival bunker, dark terminal UI.
```

### 8.2 Batch by Category

Never mix categories in one generation session. Each batch produces consistent style:

| Batch | Category | Count | Characteristics |
|-------|----------|-------|----------------|
| A | Resource cards | ~20 | Simple objects — wood, stone, food |
| B | Consumable cards | ~15 | Items, food, potions |
| C | Equipment cards | ~10 | Weapons, armor, tools |
| D | Material cards | ~10 | Raw materials — ore, planks, clay |
| E | Character/Worker cards | ~8 | NPC portraits facing forward |
| F | Mob (passive) cards | ~10 | Creatures — neutral expression |
| G | Mob (aggressive/enemy) cards | ~8 | Creatures — threatening expression |
| H | Structure cards | ~10 | Buildings, machines, facilities |
| I | Area cards | ~6 | Environments — forest, ruins, quarry |
| J | Valuable/Currency cards | ~5 | Treasures, coins |
| K | Recipe icons | ~15 | Flat icons for crafting recipes |
| L | Resource/UI icons | ~20 | HUD icons for food, currency, health |
| M | Board/background | 3–5 | Game board surfaces, scene backgrounds |
| N | UI panels/buttons | 8–10 | Terminal-style UI panels, buttons |
| O | Title screen | 1 | Cinematic wide background |

### 8.3 Generation → Unity Pipeline (Per Card)

```
Step 1: Generate AI source art
  └─ Use Midjourney / DALL·E / Stable Diffusion
  └─ Save to: Art/01_Source_AI/Raw/cards/[batch]/card_[id]_raw_v1.png
  └─ Log prompt to: Art/01_Source_AI/Prompts/card_portraits.md

Step 2: Clean in Aseprite or Photoshop
  └─ Crop to relevant subject
  └─ Remove backgrounds (magic wand + manual cleanup)
  └─ Adjust palette to match LAST KERNEL colors
  └─ Sharpen pixel edges (Threshold / Indexed Color mode)
  └─ Reduce to target palette if needed
  └─ Save .aseprite source to: Art/02_Source_Aseprite/cards/card_[id].aseprite

Step 3: Export at exact sizes
  └─ Export PNG at 256×256 (card portrait)
  └─ Export PNG at 32×32 (icon version if needed)
  └─ Save to: Art/04_Exported_Sprites/cards/card_[id]_portrait.png

Step 4: Import into Unity
  └─ Drop PNG into: Assets/_Project/Art/07_Cards/Portraits/
  └─ In Inspector: Apply Preset_CardPortrait.preset (see Section 9)
  └─ Verify: Filter Mode = Point, Mip Maps = Off, Compression = None

Step 5: Assign to CardDefinition
  └─ Open Assets/_Project/Data/Resources/Cards/[category]/Card_[id].asset
  └─ Drag PNG into "Art Texture" field (Odin PreviewField)
  └─ Save .asset

Step 6: Validate in Play Mode
  └─ Enter Play Mode
  └─ Spawn card (use Debug.SpawnCard or existing deck)
  └─ Zoom in to check crispness
  └─ Check all zoom levels (5–20 units)

Step 7: Log completion
  └─ Mark card_portraits.md as done
  └─ Commit
```

### 8.4 Keeping Consistent Style Across Batches

1. **Save 3–5 seed images** from batch A as style references
2. Use **img2img or reference features** to apply style to later batches
3. In Midjourney: use `--sref [url]` (style reference) or `--cref [url]` (character reference)
4. In Stable Diffusion: use ControlNet with reference images
5. **Never** adjust the style brief mid-project without updating all previous batches

### 8.5 Aseprite Cleanup Checklist (Per Sprite)

- [ ] Remove background (transparent alpha)
- [ ] Crop to tight bounding box with 2–4 pixel padding
- [ ] Resize canvas to 256×256 (nearest-neighbor scaling only)
- [ ] Reduce color count to ≤ 24 colors per sprite
- [ ] Remove anti-aliased edges (replace with solid pixels or transparent)
- [ ] Verify transparent areas use fully transparent (alpha = 0), not semi-transparent
- [ ] Export as PNG-24 with alpha channel

### 8.6 AI Prompt Templates

#### Card Portrait (Resource / Item)
```
[STYLE BRIEF]
Top-down isometric small pixel art icon.
Subject: [ITEM NAME] — [brief description].
Dark background (#0A0C16). Item centered with slight glow edge.
Simple, clean silhouette. 5–6 colors maximum.
256×256 pixels. No text. No border.
```

#### Card Portrait (Worker / Character)
```
[STYLE BRIEF]
Pixel art bust portrait, facing slightly right.
Character: [NAME] — [role/class description], [expression].
Worn uniform or appropriate clothing for a bunker survivor.
Dark background (#0A0C16). Cyan rim light (#00DCFF) from upper-left.
256×256 pixels. No text. No border.
```

#### Card Portrait (Enemy / Mob — Aggressive)
```
[STYLE BRIEF]
Pixel art creature portrait, center frame.
Creature: [NAME] — [description], threatening posture, red or amber eyes.
Dark moody background. Danger atmosphere.
256×256 pixels. No text. No border.
```

#### Card Portrait (Mob — Passive)
```
[STYLE BRIEF]
Pixel art creature or animal portrait.
Subject: [NAME] — [animal/creature description], neutral or calm expression.
Dark background (#0A0C16). Soft lighting.
256×256 pixels. No text. No border.
```

#### Card Portrait (Structure)
```
[STYLE BRIEF]
Pixel art isometric building or machine.
Subject: [NAME] — [building description], bunker or post-apocalyptic style.
Dark background (#0A0C16). Slight mechanical glow (cyan or amber).
256×256 pixels. No text. No border.
```

#### Resource / UI Icon (32×32)
```
[STYLE BRIEF]
Tiny pixel art icon, flat design.
Subject: [RESOURCE NAME] — [brief description].
Dark or transparent background. Maximum 4 colors + black outline.
32×32 pixels. Simple silhouette. No text.
```

#### Enemy Card (Mob Aggressive)
```
[STYLE BRIEF]
Pixel art enemy card portrait.
Enemy: [NAME] — [class/role], menacing expression, battle-ready stance.
Background: dark nebula or corrupted terrain (subtle).
Cyan or amber rim light. 256×256 pixels. No text. No border.
```

#### Board Background (Gameplay Surface)
```
[STYLE BRIEF]
Wide-angle pixel art bunker interior floor, top-down view.
Dark concrete or metal grating surface. Subtle scuff marks and wear.
No people. No cards. No text. Just the surface.
Aspect ratio 16:9. 1920×1080 pixels.
Color: very dark navy (#0A0C16 to #141A28 gradient).
```

#### UI Panel
```
[STYLE BRIEF]
Terminal UI panel background, dark mode.
Thin cyan border (#00DCFF, 1–2px). Dark fill (#0A0C16).
Subtle scan line texture (very faint, 5% opacity).
Hard corners (no rounding). Minimal decoration.
Size: [W×H]. No text. No icons.
```

#### Button
```
[STYLE BRIEF]
Pixel art terminal button.
Dark fill (#141A28). Cyan border (#00DCFF, 1px).
Slight bevel or ridge. Press state variant (slightly darker).
Size: 192×48 pixels. Hard corners. No text.
```

#### Title Screen Background
```
[STYLE BRIEF]
Cinematic pixel art background, wide format.
Scene: underground server core / survival bunker at night.
Rows of servers, blinking lights, smoke vents, dim red emergency lighting.
Atmospheric depth, dark foreground, glowing terminal screens mid-ground.
NO characters. NO text. NO UI.
1920×1080 pixels. Highly detailed, dark atmosphere.
```

### 8.7 Tracking Prompts and Versions

Create and maintain `Assets/_Project/Art/01_Source_AI/Prompts/card_portraits.md`:

```markdown
# Card Portrait Prompt Log

## Style seed images
- `card_warrior_raw_v2.png` — approved seed for character batch
- `card_slime_raw_v1.png` — approved seed for mob batch

## Card: Warrior
- v1: [paste full prompt] — rejected (too anime style)
- v2: [paste full prompt] — approved
- Cleaned: card_warrior_portrait.png (2026-04-30)
- Assigned: Card_Warrior.asset ✓

## Card: Slime
- v1: [paste full prompt] — approved
- Cleaned: card_slime_portrait.png (2026-04-30)
- Assigned: Card_Slime.asset ✓
```

---

## 9. Automation Recommendations

These tools do not exist yet. Build in order of ROI.

### 9.1 Art Audit Window (Build First)

**File:** `Assets/_Project/Scripts/Editor/ArtAuditWindow.cs`  
**Menu:** `Tools → LAST KERNEL → Art Audit`

**Features:**
- Lists all textures in `Assets/_Project/Art/`
- Shows: path, actual pixel dimensions, filter mode, mipmaps, compression, PPU, texture type
- Flags violations in red:
  - filterMode != Point for pixel art folders
  - mipmaps enabled in `CardArt/` or `Frames/` folders
  - compression != None on pixel art
  - textureType != Sprite for UI sprites
  - alphaIsTransparency off
- Export report as CSV
- Button: "Fix Selected" — applies correct settings to flagged textures

**Key API:**
```csharp
TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
// Read: importer.filterMode, importer.mipmapEnabled, importer.textureCompression
// Write: importer.filterMode = FilterMode.Point; importer.SaveAndReimport();
Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
// Read: tex.width, tex.height
```

### 9.2 Card Art Auto-Assigner

**File:** `Assets/_Project/Scripts/Editor/CardArtAssigner.cs`  
**Menu:** `Tools → LAST KERNEL → Assign Card Art`

**Workflow:**
1. Scans `Assets/_Project/Art/07_Cards/Portraits/` for files matching `card_[id]_portrait.png`
2. Loads all `CardDefinition` assets from `Assets/_Project/Data/Resources/Cards/`
3. Matches filename `[id]` part to `CardDefinition.id` (or name)
4. Assigns matching Texture2D to `CardDefinition.artTexture` via SerializedObject
5. Reports: assigned / skipped (already assigned) / missing (no matching art) / unused art

**Key API:**
```csharp
string[] guids = AssetDatabase.FindAssets("t:CardDefinition", new[] { "Assets/_Project/Data" });
SerializedObject so = new SerializedObject(cardDef);
SerializedProperty texProp = so.FindProperty("artTexture");
texProp.objectReferenceValue = texture;
so.ApplyModifiedProperties();
```

### 9.3 Import Preset Creator

**File:** `Assets/_Project/Scripts/Editor/ArtImportPresets.cs`  
**Menu:** `Tools → LAST KERNEL → Create Import Presets`

Creates 5 `TextureImporterPreset` assets in `Assets/_Project/Art/08_ImportPresets/`:

| Preset | Filter | Mips | Compression | Type | PPU | Notes |
|--------|--------|------|-------------|------|-----|-------|
| Preset_CardPortrait | Point | Off | None | Default | 100 | _OverlayTex |
| Preset_CardFrame | Point | Off | None | Sprite | 100 | 9-slice aware |
| Preset_Icon | Point | Off | None | Sprite | 100 | 32×32/64×64 |
| Preset_UISprite | Point | Off | None | Sprite | 100 | Panels, buttons |
| Preset_Background | Bilinear | On | Normal | Default | — | Board, BG plane |

Once presets exist, apply them via: Inspector → Texture Importer → Preset dropdown.

### 9.4 Sprite Atlas Setup

**File:** `Assets/_Project/Scripts/Editor/SpriteAtlasSetup.cs`  
**Menu:** `Tools → LAST KERNEL → Build Sprite Atlases`

Creates 4 Sprite Atlases:

| Atlas | Contents | Notes |
|-------|----------|-------|
| `Atlas_UI` | All `Art/05_UI/` sprites | HUD, buttons, panels, icons |
| `Atlas_Icons` | All resource/card-type icons | 32×32 and 64×64 sprites |
| `Atlas_CardFrames` | 12 category frames | Used by all card materials |
| `Atlas_VFX` | All VFX sprites | Particles, projectiles |

> Do NOT atlas card portraits — they are individual Texture2D references (not Sprite), applied via MaterialPropertyBlock. Atlasing them would break the `_OverlayTex` assignment system.

### 9.5 Art Validation Report (CI/CD Friendly)

**File:** `Assets/_Project/Scripts/Editor/ArtValidationReport.cs`  
**Menu:** `Tools → LAST KERNEL → Validate Art`  
**Integrates with:** Existing `Tools → LAST KERNEL → Validate Project`

**Checks:**
- [ ] All CardDefinition assets have `artTexture` assigned
- [ ] All card portrait textures have filterMode = Point
- [ ] All card portrait textures have mipmaps disabled
- [ ] No card portrait texture has compression
- [ ] All UI sprites are Sprite type
- [ ] No sprite in CardArt folder is 1×1 (placeholder)
- [ ] All textures have alphaIsTransparency = true
- [ ] No texture exceeds maxTextureSize 2048 (warns if card art > 512)
- [ ] Sprite atlas assignments are current

**Output:** Logs to Console and optionally to `Assets/_Project/Docs/ArtValidationLog.txt`

---

## 10. Master Checklist

### Immediate Fixes (Before Replacing Any Art)
- [ ] Fix filter mode on all 104 card portrait textures: Bilinear → Point
- [ ] Fix mipmaps on all 104 card portrait textures: On → Off
- [ ] Fix compression on all card portraits: 50% → None
- [ ] Enable alphaIsTransparency on all card art with transparency
- [ ] Fix typo in `New Panel Settings.asset` reference resolution (1980 → 1920)
- [ ] Determine actual pixel dimensions of existing card art (use Art Audit Window)

### Short-Term Setup
- [ ] Create `Assets/_Project/Art/08_ImportPresets/` with 5 import presets
- [ ] Create `Assets/_Project/Art/01_Source_AI/Prompts/` with prompt log templates
- [ ] Set up style seed images from first AI generation batch
- [ ] Build Art Audit Window editor tool

### Per-Batch Art Replacement
- [ ] Generate AI source art (see Section 8.6 prompt templates)
- [ ] Clean in Aseprite: remove BG, crop, resize, reduce palette
- [ ] Export PNGs at correct sizes (see Section 6 table)
- [ ] Import to Unity with correct presets
- [ ] Assign to CardDefinitions (manually or via Auto-Assigner tool)
- [ ] Validate in Play Mode: spawn cards, check crispness at all zoom levels

### Long-Term Pipeline
- [ ] Build Card Art Auto-Assigner tool
- [ ] Build Sprite Atlas Setup tool (for UI/Icon/VFX atlases — not card portraits)
- [ ] Build Art Validation Report (integrate with project validation)
- [ ] Create style guide image in `Art/00_StyleGuide/`
- [ ] Populate `Art/06_Board/` with new board surface and background art
- [ ] Populate `Art/05_UI/` with new button, panel, HUD art
- [ ] Replace title screen background

---

*Generated by automated MCP scan — 2026-04-30. All findings are read-only. No assets were modified.*
