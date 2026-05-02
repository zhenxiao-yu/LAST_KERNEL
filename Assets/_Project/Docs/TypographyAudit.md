# Typography Audit — LAST KERNEL
**Date:** 2026-05-01  
**Audited:** theme.uss, components.uss, GameHUDController, TitleScreenController, UIFonts

---

## Scale Before → After

| Token | Old | New | Rationale |
| --- | --- | --- | --- |
| `--lk-font-size-meta` | *(none)* | **12px** | Watermark/version was 18px — wildly oversized for secondary metadata |
| `--lk-font-size-xs` | 15px | **14px** | Aligns with 14px industry standard for fine print |
| `--lk-font-size-sm` | 18px | **16px** | 18px was too heavy for secondary labels; 16px is the minimum readable body |
| `--lk-font-size-md` | 21px | **18px** | 21px is non-standard; 18px is the comfortable body floor per spec |
| `--lk-font-size-card` | *(none)* | **20px** | New token fills the gap between body (18px) and panel headers (24px) |
| `--lk-font-size-lg` | 27px | **24px** | 27px is an awkward odd size; 24px is the standard panel-header value |
| `--lk-font-size-xl` | 36px | **32px** | Section/phase headers at 36px competed with title banners |
| `--lk-font-size-title` | 48px | **40px** | 48px victory/defeat titles were too dominant relative to panel hierarchy |
| `--lk-font-size-logo` | 84px | **72px** | 84px is excessive for a 1080p canvas; 72px matches common game logo sizing |

---

## Component Changes

| Class | Property | Old | New | Reason |
| --- | --- | --- | --- | --- |
| `.lk-panel__title` | `text-transform` | uppercase | *(removed)* | Uppercase breaks German compound words and French diacritics |
| `.lk-modal__title` | `text-transform` | uppercase | *(removed)* | Same — modal titles are localized strings |
| `.lk-label--header` | `text-transform` | uppercase | *(removed)* | Section headers are localized — uppercase is harmful for CJK/DE/FR |
| `.lk-watermark` | `font-size` | `var(--lk-font-size-sm)` → 18px | `var(--lk-font-size-meta)` → **12px** | Watermark should be invisible metadata, not a readable label |
| `.lk-label--dim` | `font-size` | `var(--lk-font-size-sm)` → 18px | `var(--lk-font-size-xs)` → **14px** | Dim labels are secondary — they should step back, not compete with body |
| `.lk-resource-counter__value` | `font-size` | `var(--lk-font-size-md)` → 21px | `var(--lk-font-size-lg)` → **24px** | Resource counters are gameplay-critical — must be larger than body text |
| `.lk-phase-label` | `font-size` | `var(--lk-font-size-lg)` → 27px | `var(--lk-font-size-xl)` → **32px** | Phase badge (DAY/NIGHT) is the primary HUD orientation cue |
| `.lk-info-panel__header` | `font-size` | `var(--lk-font-size-lg)` → 27px | `var(--lk-font-size-card)` → **20px** | Info panel headers are context labels inside a side panel, not primary headers |
| `.lk-info-badge` | `font-size` | `var(--lk-font-size-sm)` → 18px | `var(--lk-font-size-xs)` → **14px** | Type badges (CHARACTER, RESOURCE) are metadata chips, not readable body |
| `.lk-info-panel__stats` | `font-size` | `var(--lk-font-size-sm)` → 18px | `var(--lk-font-size-xs)` → **14px** | Stat block is secondary detail — xs is readable and appropriate |
| `.lk-label--body` | `line-height` | 1.4 | **1.5** | 1.5 is the minimum for CJK and European language readability |
| `.lk-hint` | `line-height` | 1.4 | **1.5** | Multi-line hints need consistent line height for CJK overflow |
| `.lk-info-panel__body` | `line-height` | 1.4 | **1.5** | Card descriptions often wrap to 3+ lines — 1.5 prevents cramping |
| `.lk-info-panel__stats` | `line-height` | 1.4 | **1.5** | Consistent with body |

---

## Font Role Changes (UIToolkit)

| Label | Old font | New font | Applied via |
| --- | --- | --- | --- |
| `lbl-phase` (DAY badge) | MiSans (default) | Oxanium SemiBold | `UIFonts.AccentSemibold()` |
| `lbl-night-phase` (NIGHT badge) | MiSans (default) | Oxanium Bold | `UIFonts.AccentBold()` |
| `lbl-wave` | MiSans (default) | Oxanium SemiBold | `UIFonts.AccentSemibold()` |
| `btn-speed` (1×/2×) | MiSans (default) | Oxanium Bold | `UIFonts.AccentBold()` |
| `btn-pace` | MiSans (default) | Oxanium SemiBold | `UIFonts.AccentSemibold()` |
| `lbl-logo` | MiSans (default) | MiSans Heavy / paid display | `UIFonts.DisplayHeavy()` |
| `lbl-nutrition`, `lbl-currency`, `lbl-cards` | MiSans (default) | **Sarasa Gothic SC** | `UIFonts.TerminalRegular()` |
| `lbl-base-hp`, `lbl-enemies` | MiSans (default) | **Sarasa Gothic SC** | `UIFonts.TerminalRegular()` |

Sarasa Gothic SC is used for all numeric HUD counters — its tabular figures, tighter spacing, and monospace CJK grid make digits read at a glance during combat.

---

## Responsive Scale Tier Restructuring

The xsmall/small/large/xlarge tiers were rebuilt to match the new base scale. Key structural fixes:

- `.lk-label--subheader` moved from the `lg` bucket to the `xs` bucket in all tiers (was incorrectly scaled at 18–38px; it is a 14px uppercase category label)
- `.lk-resource-counter__value` moved from the `md` bucket to the `lg` bucket (reflects the new `lg` role for gameplay-critical values)
- `.lk-list__header-label`, `.lk-list__item-label`, `.lk-info-panel__body` moved from `md` bucket to `sm` bucket
- `.lk-watermark` gets its own `meta` bucket (10–17px across tiers) in all four tiers
- `.lk-info-panel__header` gets its own `card` bucket in all four tiers
- All tier values recomputed from the new base with correct multipliers

---

## Issues Found But Not Changed

| Issue | Decision |
| --- | --- |
| `.lk-label--logo` has `text-transform: uppercase` | Kept — logo is always EN "LAST KERNEL" and benefits from the treatment |
| `.lk-label--title` has `text-transform: uppercase` | Kept — victory/defeat banners are short EN-dominant strings |
| `.lk-button` has `text-transform: uppercase` | Kept — short button labels (NEW GAME, OPTIONS) are fine; localized variants handled by C# `.ToUpper()` call |
| `.lk-tab` has `text-transform: uppercase` | Kept — tab labels are short and EN-biased |
| `.lk-button--text` at `lg` (24px) feels large | Intentional — title screen nav buttons are primary navigation, high prominence is correct |

---

## Language Readability Checklist

Test each language at medium (base) scale on 1080p before any layout freeze:

| Language | Min readable size | Wrap concern | Notes |
| --- | --- | --- | --- |
| EN | 14px | Low | Baseline |
| ZH-Hans | 16px | Medium | MiSans + Sarasa cover all glyphs; test at 18px body |
| ZH-Hant | 16px | Medium | Same coverage; traditional forms are slightly wider |
| JA | 16px | Medium | Mixed kana/kanji; Sarasa handles both scripts cleanly |
| KO | 16px | Medium | Hangul syllable blocks are tall — verify line-height 1.5 is enough |
| FR | 14px | Low-medium | Diacritics (é, à, ç) need correct Unicode — MiSans covers all |
| DE | 16px | High | Compound words are very long — never uppercase panel titles in DE |
| ES | 14px | Low | Similar to EN; watch inverted punctuation (¿ ¡) at small sizes |

---

## New Assets Created

| Asset | Type | Source TTF |
| --- | --- | --- |
| `FA_Sarasa_Regular.asset` | TextCore FontAsset (UIToolkit) | `SarasaGothicSC-Regular.ttf` |

---

## Files Modified

- `Assets/_Project/UI/USS/theme.uss` — scale tokens + all four responsive tiers
- `Assets/_Project/UI/USS/components.uss` — 13 targeted property changes
- `Assets/_Project/Scripts/Runtime/UI/Typography/UIFonts.cs` — added `TerminalRegular()`
- `Assets/_Project/UI/Controllers/Game/GameHUDController.cs` — applied `TerminalRegular` to 5 numeric labels
- `Assets/_Project/Docs/TypographyStyleGuide.md` — updated scale table, UIToolkit section, localization rules, file locations
