# Font Install Instructions — LAST KERNEL
## Phase 3 & 4: Install Fonts + Create TMP Assets

Follow these steps **in order** after downloading the font files.

---

## Step 1 — Download the Font Files

### MiSans Global (Primary UI Font — free)
- Download: https://hyperos.mi.com/font/en (MiSans Global, all weights)
- Required weights: Regular, Medium, Semibold, Bold, Heavy
- Drop the `.ttf` files into: `Assets/_Project/Art/Fonts/MiSans/Source/`

### Sarasa Mono (Terminal / Technical — free, open source)
- Download: https://github.com/be5invis/Sarasa-Gothic/releases
- Get: `sarasa-mono-sc-regular.ttf`, `sarasa-mono-sc-bold.ttf`
- Drop into: `Assets/_Project/Art/Fonts/Sarasa/Source/`

### Oxanium (EN Cyberpunk Accent — free, Google Fonts)
- Download: https://fonts.google.com/specimen/Oxanium
- Get: Regular, Medium, SemiBold, Bold, ExtraBold
- Drop into: `Assets/_Project/Art/Fonts/Oxanium/Source/`

### Emergency Fallback (Noto — already installed)
- `NotoSansSC-Regular.ttf` already exists at `Art/Fonts/NatoSans/Source/`
- `TMP_NotoSansSC_Fallback.asset` already exists at `Art/Fonts/NatoSans/TMP/`

---

## Step 2 — Create TMP Font Assets

For **each** TTF file, right-click it in the Project window → **Create → TextMeshPro → Font Asset**.

Use these exact names and save to the TMP/ subfolder:

### MiSans → `Art/Fonts/MiSans/TMP/`
| Asset Name | Source TTF | Settings |
|---|---|---|
| `TMP_UI_MiSans_Regular` | MiSans-Regular.ttf | Dynamic, multi-atlas |
| `TMP_UI_MiSans_Medium` | MiSans-Medium.ttf | Dynamic, multi-atlas |
| `TMP_UI_MiSans_Semibold` | MiSans-Semibold.ttf | Dynamic, multi-atlas |
| `TMP_UI_MiSans_Bold` | MiSans-Bold.ttf | Dynamic, multi-atlas |
| `TMP_UI_MiSans_Heavy` | MiSans-Heavy.ttf | Dynamic, multi-atlas |

### Sarasa → `Art/Fonts/Sarasa/TMP/`
| Asset Name | Source TTF |
|---|---|
| `TMP_Mono_Sarasa_Regular` | sarasa-mono-sc-regular.ttf |
| `TMP_Mono_Sarasa_Bold` | sarasa-mono-sc-bold.ttf |

### Oxanium → `Art/Fonts/Oxanium/TMP/`
| Asset Name | Source TTF |
|---|---|
| `TMP_Accent_Oxanium_Regular` | Oxanium-Regular.ttf |
| `TMP_Accent_Oxanium_Medium` | Oxanium-Medium.ttf |
| `TMP_Accent_Oxanium_SemiBold` | Oxanium-SemiBold.ttf |
| `TMP_Accent_Oxanium_Bold` | Oxanium-Bold.ttf |
| `TMP_Accent_Oxanium_ExtraBold` | Oxanium-ExtraBold.ttf |

**TMP Asset Settings for each:**
- Atlas Population Mode: **Dynamic**
- Enable Multi Atlas Textures: **✓**
- Atlas Width/Height: **1024 × 1024** (MiSans may need 2048 for CJK)
- Render Mode: **SDFAA**

---

## Step 3 — Set MiSans as TMP Default Font

1. Go to **Edit → Project Settings → TextMesh Pro**
2. Set **Default Font Asset** → `TMP_UI_MiSans_Regular`
3. Add to **Fallback Font Assets** list (in order):
   - `TMP_UI_MiSans_Bold`
   - `TMP_Mono_Sarasa_Regular`
   - `TMP_NotoSansSC_Fallback` (emergency CN fallback)

---

## Step 4 — Create GameTypographyProfile Asset

1. In the Project window, right-click `Assets/_Project/Art/Fonts/TextStyles/`
2. Select **Create → LastKernel/Typography/Profile**
3. Name it exactly: `GameTypographyProfile`
4. Move it to a **Resources** folder (create `Assets/_Project/Resources/Typography/` if it doesn't exist)
5. In the Inspector, assign:
   - **UI Font** → `TMP_UI_MiSans_Regular`
   - **Terminal Font** → `TMP_Mono_Sarasa_Regular`
   - **Accent Font** → `TMP_Accent_Oxanium_Regular`
   - **Display Font** → *(leave null for now — assign paid display font later)*
   - **Fallback Font** → `TMP_NotoSansSC_Fallback`

---

## Step 5 — Update UIToolkit Default Font

1. Find `Assets/_Project/UI/LKTextSettings.asset`
2. In the Inspector, change **Default Font Asset** to the TextCore version of MiSans:
   - Create a TextCore FontAsset from MiSans-Regular.ttf:
     Right-click TTF → **Create → TextCore → Font Asset**
     Name it: `FA_MiSans_Regular`
     Save to: `Assets/_Project/Art/Fonts/MiSans/TMP/`
   - Assign `FA_MiSans_Regular` to `LKTextSettings.defaultFontAsset`
3. This replaces NotoSansSC as the UIToolkit default (demoted to fallback)

---

## Step 6 — Tag Components with GameTypographyApplier

For any TMP_Text component that should NOT use MiSans (the default), add a `GameTypographyApplier` component and set its Role:

| Role | Font Used | When to apply |
|---|---|---|
| `UI` | MiSans Regular | Default — most labels, buttons, panels |
| `Terminal` | Sarasa Mono | System feed, data readouts, machine-voice text |
| `Accent` | Oxanium | Phase labels, speed buttons, HUD accent badges (EN only) |
| `Display` | Paid display font | Logo, main menu title, brand splash |

Example candidates for non-UI roles:
- **Terminal**: `DayHUD.phaseLabel` (machine system voice), any "SYSTEM:" text feeds
- **Accent**: `NightHUD` wave/speed labels, category badges, day counter
- **Display**: Logo label in TitleScreen (if using TMP)

---

## Verification Checklist

- [ ] All 5 MiSans TMP assets created, multi-atlas enabled
- [ ] `TMP_Settings.defaultFontAsset` = `TMP_UI_MiSans_Regular`
- [ ] `GameTypographyProfile` asset exists in `Resources/Typography/`
- [ ] All 5 font slots assigned in profile Inspector
- [ ] UIToolkit `LKTextSettings` uses `FA_MiSans_Regular`
- [ ] Play mode: open console, no missing font warnings
- [ ] Chinese characters render in both TMP and UIToolkit text
- [ ] Accent labels (phase/speed) show Oxanium style
