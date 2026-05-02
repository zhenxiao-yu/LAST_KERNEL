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

## Type Scale

All sizes target 1920×1080 reference. UIToolkit scales via `UIScaleManager`; the base (medium) tier needs no class.

| Token | Size | Usage |
| --- | --- | --- |
| `--lk-font-size-meta` | 12px | Watermark, version string, micro metadata |
| `--lk-font-size-xs` | 14px | Hints, dim labels, tabs, badges, stats |
| `--lk-font-size-sm` | 16px | Secondary body, list items, save labels |
| `--lk-font-size-md` | 18px | Comfortable body, buttons, toggles (default) |
| `--lk-font-size-card` | 20px | Card titles, info-panel headers |
| `--lk-font-size-lg` | 24px | Resource values, nav buttons, value labels |
| `--lk-font-size-xl` | 32px | Phase headers (DAY/NIGHT), panel/modal titles |
| `--lk-font-size-title` | 40px | Victory/defeat banners, screen titles |
| `--lk-font-size-logo` | 72px | Game logo, brand splash |

Responsive tiers (HUD only): xsmall ×0.67 · small ×0.83 · **medium (base)** · large ×1.20 · xlarge ×1.40

---

## USS / UIToolkit Text

UIToolkit text (UIDocument panels) uses the TextCore pipeline, separate from TMP:
- Default font: `FA_MiSans_Regular` → wired via `LKTextSettings` → `LKPanelSettings`
- Font-size tokens defined in `theme.uss` (all `--lk-font-size-*` vars)
- Bold is achieved via `-unity-font-style: bold` in USS (TextCore synthetic bold — no separate Bold asset needed)
- Role-based font overrides for UIToolkit via `UIFonts` static class:
  - `UIFonts.AccentSemibold(el)` — Oxanium SemiBold — phase badges, wave label, pace button
  - `UIFonts.AccentBold(el)` — Oxanium Bold — NIGHT badge, speed toggle
  - `UIFonts.DisplayHeavy(el)` — paid display font (MiSans Heavy fallback) — logo label
  - `UIFonts.TerminalRegular(el)` — Sarasa Gothic SC — HUD numeric counters (food/gold/cards/HP/enemies)

---

## Localization Safety Rules

1. **Never use Oxanium (Accent) for translatable strings.** Phase names are translatable; in CJK locales the string will render in MiSans automatically (Oxanium lacks CJK glyphs — it silently falls through).
2. **Never use `text-transform: uppercase` on panel titles or modal titles** — all-caps breaks German compound words, French diacritics, and CJK scripts. Uppercase is acceptable only for short, hard-coded EN labels (logo, phase badge, button, tab).
3. **Leave 30–40% horizontal space for CJK and 20–25% for German/French/Spanish** — these scripts are wider or have longer words than English.
4. **Line height 1.5** for all wrappable body text (enforced in `components.uss`).
5. Font coverage summary:
   - **MiSans Global**: EN + CJK (Simplified, Traditional, Japanese Kana, Korean Hangul)
   - **Sarasa Gothic SC**: EN + Simplified Chinese + Japanese + Korean
   - **Oxanium**: Latin + extended Latin only
   - **Noto Sans SC**: emergency CJK fallback — do not use as primary

Languages to test before any layout freeze: EN · ZH-Hans · ZH-Hant · JA · KO · FR · DE · ES

---

## File Locations

### TMP (TextMesh Pro — uGUI / world-space)

| Asset | Path |
|---|---|
| GameTypographyProfile | `Assets/_Project/Resources/Typography/GameTypographyProfile.asset` |
| MiSans TTF | `Assets/_Project/Art/Fonts/MiSans/Source/` |
| MiSans TMP assets | `Assets/_Project/Art/Fonts/MiSans/TMP/` (5 weights) |
| Sarasa TTF | `Assets/_Project/Art/Fonts/Sarasa/Source/` |
| Sarasa TMP assets | `Assets/_Project/Art/Fonts/Sarasa/TMP/` (Regular, SemiBold) |
| Oxanium TTF | `Assets/_Project/Art/Fonts/Oxanium/Source/` |
| Oxanium TMP assets | `Assets/_Project/Art/Fonts/Oxanium/TMP/` (5 weights) |
| Noto SC TMP fallback | `Assets/_Project/Art/Fonts/NatoSans/TMP/` |

### TextCore (UIToolkit)

| Asset | Path | Used for |
| --- | --- | --- |
| FA_MiSans_Regular | `Assets/_Project/Art/Fonts/MiSans/TMP/FA_MiSans_Regular.asset` | Default UIToolkit font via LKTextSettings |
| FA_Oxanium_SemiBold | `Assets/_Project/Resources/Typography/` | Phase badge, wave label, pace button |
| FA_Oxanium_Bold | `Assets/_Project/Resources/Typography/` | NIGHT badge, speed toggle |
| FA_MiSans_Heavy | `Assets/_Project/Resources/Typography/` | Logo (fallback when display font absent) |
| FA_Sarasa_Regular | `Assets/_Project/Resources/Typography/` | HUD numeric counters |
| FA_Display_Regular | `Assets/_Project/Resources/Typography/` | Logo (paid font slot — drop in to activate) |

---

## C# System Files

| File | Purpose |
|---|---|
| `Scripts/Runtime/UI/Typography/GameTextRole.cs` | Enum: UI, Terminal, Accent, Display |
| `Scripts/Runtime/UI/Typography/GameFontWeight.cs` | Enum: Regular, Medium, Semibold, Bold, Heavy |
| `Scripts/Runtime/UI/Typography/GameTypographyProfile.cs` | ScriptableObject — maps roles+weights to TMP assets |
| `Scripts/Runtime/UI/Typography/GameTypographyApplier.cs` | Component — tags a TMP_Text with role+weight |
| `Scripts/Runtime/UI/Typography/UIFonts.cs` | Static — applies TextCore fonts to UIToolkit elements |
| `Scripts/Runtime/Core/TMPThemeController.cs` | Runtime singleton — loads profile, applies TMP fonts on scene load/language change |

---

## Paid Display Font Slot

The `Display` role is a reserved slot. When a paid display font is licensed:
1. Drop the TTF into `Assets/_Project/Art/Fonts/DisplayFont/Source/`
2. Create a **TextCore** FontAsset named `FA_Display_Regular` → save to `Assets/_Project/Resources/Typography/`
3. Create a **TMP** FontAsset named `TMP_Display_Regular` → assign to `GameTypographyProfile.display.regular`
4. Both pipelines update automatically — no code changes needed.
