# Typography Style Guide — LAST KERNEL

## Font System Overview

LAST KERNEL uses a **four-role font hierarchy** designed for a dark cyberpunk bunker terminal aesthetic. Every text element belongs to exactly one role.

---

## Font Roles

| Role | Font | Purpose | CJK Support |
|---|---|---|---|
| `UI` | **MiSans Global** | All readable UI — menus, buttons, cards, panels, tooltips | Full (designed for CJK-first) |
| `Terminal` | **Sarasa Mono SC** | Machine voice — system feeds, data readouts, code-style displays | Full (SC variant) |
| `Accent` | **Oxanium** | EN-only cyberpunk accent — phase names, speed buttons, HUD badges | EN only — never use for CJK |
| `Display` | *(paid display font)* | Brand moments — main logo, title splash | EN only |

**Noto Sans SC** is an emergency fallback only. It must not be used as a primary font.

---

## Role Assignment

### Default Behavior
Any `TMP_Text` component **without** a `GameTypographyApplier` component receives `GameTextRole.UI` (MiSans).

### Overriding the Role
Add a `GameTypographyApplier` component to a GameObject and set the `Role` field.

```
GameObject (with TextMeshProUGUI)
  └── GameTypographyApplier { role = Terminal }
```

---

## Usage Rules by Role

### UI — MiSans Global
- Use for: menus, buttons, card titles, card descriptions, panel headers, resource values, tooltips, modal text, ALL localized UI strings
- Weight usage:
  - Regular → body, labels, secondary info
  - Medium → buttons, list items
  - Semibold → panel sub-headers, tab labels
  - Bold → panel titles, modal titles, emphasis
  - Heavy → victory/defeat screen titles
- **Always use MiSans for anything that may be translated.** Oxanium and Sarasa are NOT suitable for Chinese text.

### Terminal — Sarasa Mono SC
- Use for: system event feed, machine-generated messages, debug readouts, "KERNEL OUTPUT:" style displays
- Monospace grid gives a terminal / code aesthetic
- Full CJK support for Chinese system messages
- Keep color: `--lk-text-dim` or `--lk-accent-dim` — never primary white

### Accent — Oxanium
- Use for: PHASE label (DAY / DUSK / NIGHT), speed multiplier (×1 / ×2 / ×3), wave counter header, category badge text
- **EN text only.** For CJK languages, fall through to MiSans for these strings
- Implement language check: if `GameLocalization.CurrentLanguage` is CJK, apply MiSans role instead
- Keep ALL-CAPS or Title-Case — never sentence-case for accent labels
- Color: `--lk-accent` or `--lk-text-header`

### Display — Paid Display Font
- Reserve for: game logo, main menu "LAST KERNEL" wordmark, loading screen title
- Never use for body text, buttons, or any localized string
- This slot is empty until a paid display font is licensed and installed

---

## Font Fallback Chain (TMP)

```
[Component Role Font]
    ↓ if missing glyph
[MiSans Global]
    ↓ if missing glyph
[TMP_NotoSansSC_Fallback]
    ↓ if missing glyph
[LiberationSans SDF] (TMP built-in last resort)
```

---

## USS / UIToolkit Text

UIToolkit text (UIDocument panels) uses the TextCore pipeline, separate from TMP:
- Default font: `FA_MiSans_Regular` → wired via `LKTextSettings` → `LKPanelSettings`
- Font-size tokens defined in `theme.uss` (`--lk-font-size-xs` through `--lk-font-size-logo`)
- Bold is achieved via `-unity-font-style: bold` in USS — a separate Bold font asset is not needed for UIToolkit (TextCore handles synthetic bold)
- Role-based font override for UIToolkit is NOT implemented — all UIToolkit text uses MiSans

---

## Localization Safety Rules

1. **Never use Oxanium (Accent) for translatable strings.** Phase names are translatable but should always render MiSans in non-EN locales.
2. **MiSans supports**: Latin, Greek, Cyrillic, Japanese Kana (partial), Simplified Chinese, Traditional Chinese, Korean
3. **Sarasa Mono SC supports**: Full Simplified Chinese
4. **Oxanium supports**: Latin + extended Latin only
5. All card text, button labels, and descriptions → always `GameTextRole.UI`
6. Test with Chinese text before locking any layout — CJK characters are taller and wider

---

## File Locations

| Asset | Path |
|---|---|
| GameTypographyProfile | `Assets/_Project/Resources/Typography/GameTypographyProfile.asset` |
| MiSans TTF files | `Assets/_Project/Art/Fonts/MiSans/Source/` |
| MiSans TMP assets | `Assets/_Project/Art/Fonts/MiSans/TMP/` |
| Sarasa TTF files | `Assets/_Project/Art/Fonts/Sarasa/Source/` |
| Sarasa TMP assets | `Assets/_Project/Art/Fonts/Sarasa/TMP/` |
| Oxanium TTF files | `Assets/_Project/Art/Fonts/Oxanium/Source/` |
| Oxanium TMP assets | `Assets/_Project/Art/Fonts/Oxanium/TMP/` |
| Noto SC TTF | `Assets/_Project/Art/Fonts/NatoSans/Source/` |
| Noto SC TMP fallback | `Assets/_Project/Art/Fonts/NatoSans/TMP/` |
| UIToolkit TextCore font | `Assets/_Project/Art/Fonts/MiSans/TMP/FA_MiSans_Regular.asset` |
| LKTextSettings | `Assets/_Project/UI/LKTextSettings.asset` |

---

## C# System Files

| File | Purpose |
|---|---|
| `Scripts/Runtime/UI/Typography/GameTextRole.cs` | Enum: UI, Terminal, Accent, Display |
| `Scripts/Runtime/UI/Typography/GameTypographyProfile.cs` | ScriptableObject — maps roles to font assets |
| `Scripts/Runtime/UI/Typography/GameTypographyApplier.cs` | Component — tags a TMP_Text with a role |
| `Scripts/Runtime/Core/TMPThemeController.cs` | Runtime — loads profile, applies fonts per role |
| `Scripts/Runtime/Localization/TMPChineseFontBootstrap.cs` | Runtime — ensures CJK fallback at boot |

---

## Paid Display Font Slot

The `Display` role is a reserved slot. When a paid display font is licensed:
1. Drop the TTF into `Assets/_Project/Art/Fonts/DisplayFont/Source/`
2. Create TMP Font Asset named `TMP_Display_[FontName]_Regular`
3. Assign to `GameTypographyProfile.displayFont` in the Inspector
4. The logo and title components tagged `GameTextRole.Display` will update automatically
