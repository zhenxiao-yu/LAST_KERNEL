# Font System Audit — LAST KERNEL
Generated: 2026-05-01

## Summary

The project uses **TextMesh Pro (TMP)** for all 3D/world-space and Canvas/uGUI text. UI Toolkit (UIDocument) text uses Unity's `PanelTextSettings` pipeline. There is currently a single default font (LiberationSans) applied globally to all TMP components at runtime by `TMPThemeController`. No role-based font assignment exists yet.

---

## TMP Usage Inventory

### 3D / World-Space Text (TextMeshPro)
| File | Fields | Notes |
|---|---|---|
| `Cards/CardInstance.cs` | titleText, priceText, nutritionText, healthText | 4× TextMeshPro |
| `Cards/CardView.cs` | 4 TextMeshPro fields | Mirrors CardInstance |
| `Trading/BoardExpansionVendor.cs` | 3× TextMeshPro | Creates labels dynamically |
| `Trading/PackVendor.cs` | 3× TextMeshPro | Pack UI labels |

### Canvas / uGUI Text (TextMeshProUGUI)
| File | Fields | Role |
|---|---|---|
| `Combat/UI/HitUI.cs` | damageLabel | Damage float labels |
| `UI/CardStatsUI.cs` | nutrition, currency, card counters | HUD stats |
| `UI/DayTimeUI.cs` | dayText | Phase day counter |
| `UI/DayHUD.cs` | phaseLabel | Day phase label |
| `UI/DefeatPanel.cs` | messageLabel | Defeat screen body |
| `UI/GameOptionsUI.cs` | labelSFX, labelBGM | Volume option labels |
| `UI/ModalWindow.cs` | titleText, dialogText | Modal header + body |
| `UI/NightHUD.cs` | phase, wave, HP, enemy count, speed | Night HUD panel |
| `UI/TextButton.cs` | text label | Button text w/ underline |
| `UI/VictoryPanel.cs` | title, description, reward | Victory screen |
| `UI/Menu/MenuToggle.cs` | labelText | Toggle label |
| `Night/UI/CombatLaneView.cs` | name, HP labels | Lane/unit UI (dynamic) |
| `Night/UI/NightDeploymentView.cs` | 5× labels | Deployment UI (dynamic) |

### Total TMP Components: ~50 across 13 files

---

## Existing Font Assets

| Asset | Path | Type | Status |
|---|---|---|---|
| LiberationSans SDF | `TextMesh Pro/Resources/Fonts & Materials/` | TMP default | Active (global default) |
| LiberationSans SDF - Fallback | Same folder | TMP fallback | In TMP_Settings |
| TMP_SmileySans_Display | `Art/Fonts/SmileySans/TMP/` | TMP display | Exists, usage TBD |
| TMP_NotoSansSC_Fallback | `Art/Fonts/NatoSans/TMP/` | TMP fallback | Exists, wired as CN fallback |
| FA_NotoSansSC | `Art/Fonts/NatoSans/` | TextCore (UIToolkit) | Wired to LKPanelSettings |

---

## UI Toolkit / USS Typography

- **No `font-family` or `-unity-font` in USS** — fonts are applied entirely at runtime via C#
- 7 font-size scale tokens in `theme.uss` (xs=15px → logo=84px)
- 4 responsive scale tiers: xsmall (0.67×), small (0.83×), large (1.20×), xlarge (1.40×)
- 18 bold-style class definitions in `components.uss`
- Text-shadow applied to: `.lk-label--logo`, `.lk-label--title`, `.lk-panel__title`, `.lk-label--header`, `.lk-label--value`, `.lk-phase-label`, `.lk-modal__title`
- UIToolkit default font: `FA_NotoSansSC` via `LKTextSettings` → `LKPanelSettings`

---

## Font Management Architecture

### Runtime Flow
```
[BeforeSceneLoad] TMPChineseFontBootstrap.RegisterChineseFallback()
    → enables multi-atlas on TMP_Settings.defaultFontAsset
    → wires TMP_Settings.fallbackFontAssets into defaultFont.fallbackFontAssetTable

[BeforeSceneLoad] TMPThemeController.Bootstrap()
    → creates singleton MonoBehaviour (DontDestroyOnLoad)
    → on Awake: initializes localization + subscribes to events
    → RefreshAllText(): blasts ALL TMP_Text with the same defaultFont
```

### Current Limitation
`TMPThemeController.RefreshAllText()` applies one single font to every `TMP_Text` in the scene. There is no role-based font assignment — everything gets LiberationSans.

---

## Gaps to Address

| Gap | Target |
|---|---|
| No MiSans Global — primary readable UI font | Install + TMP_UI_MiSans_*.asset |
| No Sarasa Mono — technical/terminal voice | Install + TMP_Mono_Sarasa_*.asset |
| No Oxanium — EN cyberpunk accent labels | Install + TMP_Accent_Oxanium_*.asset |
| No role-based font assignment | `GameTypographyProfile` + `GameTypographyApplier` |
| No `GameTypographyProfile` ScriptableObject | Create after font installs |
| Noto used as primary (LKTextSettings) | Demote to emergency fallback only |
| LiberationSans SDF as TMP default | Replace with MiSans once installed |

---

## UXML Structure
All 9 UXML files delegate 100% of styling to USS `.lk-*` classes. No inline font properties exist. This is clean — font changes via USS or C# will propagate automatically.

---

## Next Steps
See `FontInstallInstructions.md` for the full setup workflow.
