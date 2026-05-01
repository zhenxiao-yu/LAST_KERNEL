# Theme Update Report — LAST KERNEL

**Date:** 2026-04-30  
**Scope:** Full visual theme lock — palette-based materials, USS token alignment  
**Gameplay changed:** None

---

## 1. Palette Reference

| Token | Hex | Use |
|---|---|---|
| Cyan bright | `#00DCFF` | Accent, Resource/Area cards |
| Cyan mid | `#007A8C` | Border, accent-dim |
| Steel Blue | `#6A86B6` | Equipment, Material, Structure, Character |
| Magenta | `#A03291` | Mob cards |
| Red | `#CC2E2E` | Mob_Aggressive, danger status |
| Green | `#3FAF6F` | Consumable, Recipe, success status |
| Amber | `#FFA028` | Currency, Valuable, warning status |
| Navy panel | `#141A28` | Board surfaces |
| Navy dark | `#0A0C16` | Scene background |

---

## 2. Material Updates (17 / 24 materials)

All card materials use `Card.shadergraph`. Properties updated: `_OverlayTint`, `_Color`, `_EmissionColor`, `_HueShift` (forced to 0).  
Board/zone materials use the SimpleLit variant shadergraph. Property updated: `_Color`.

### Card Category Materials

| Material | Family | `_OverlayTint` | `_Color` | Emission |
|---|---|---|---|---|
| Resource | Cyan | (0.04, 0.22, 0.28) | (0.05, 0.18, 0.22) | black |
| Area | Cyan | (0.04, 0.22, 0.28) | (0.04, 0.18, 0.23) | black |
| Consumable | Green | (0.07, 0.24, 0.15) | (0.06, 0.20, 0.13) | black |
| Recipe | Green | (0.06, 0.23, 0.14) | (0.05, 0.19, 0.12) | black |
| Equipment | Steel Blue | (0.11, 0.16, 0.26) | (0.09, 0.14, 0.22) | black |
| Material | Steel Blue | (0.12, 0.17, 0.26) | (0.10, 0.14, 0.22) | black |
| Structure | Steel Blue | (0.14, 0.18, 0.26) | (0.11, 0.15, 0.22) | black |
| Character | Steel Blue | (0.09, 0.14, 0.24) | (0.08, 0.12, 0.20) | black |
| Mob | Magenta | (0.22, 0.06, 0.22) | (0.18, 0.05, 0.18) | black |
| Mob_Aggressive | Red | (0.28, 0.05, 0.05) | (0.24, 0.04, 0.04) | (0.05, 0, 0) |
| Currency | Amber | (0.28, 0.16, 0.03) | (0.24, 0.14, 0.02) | black |
| Valuable | Amber | (0.30, 0.18, 0.04) | (0.26, 0.16, 0.03) | black |

### Board / Environment Materials

| Material | `_Color` | Note |
|---|---|---|
| Body_01 | (0.06, 0.08, 0.14) | Main board surface — dark navy |
| Body_02 | (0.06, 0.08, 0.14) | Secondary board surface |
| Header_01 | (0.03, 0.06, 0.12) | Board header — darker accent |
| Header_02 | (0.02, 0.05, 0.10) | Board header variant |
| TradeZone | (0.04, 0.10, 0.16) | Trade zone — dark cyan-navy |

### Unchanged Materials (7)

| Material | Reason |
|---|---|
| Pack.mat | Intentionally white/neutral (mystery pack) |
| Grass.mat | Built-in sprite shader — background dressing |
| Water.mat | Background shader — independent palette |
| CurrencyIcon.mat | UI overlay — no tint properties |
| EquipmentPanel.mat | Dedicated EquipmentPanel shader |
| Puff.mat | Particle effect |
| CustomPostProcess.mat | Post-process effect |

---

## 3. USS Token Updates (`theme.uss`)

| Variable | Before | After | Reason |
|---|---|---|---|
| `--lk-panel` | `rgb(18, 24, 40)` | `rgb(20, 26, 40)` | Match `#141A28` board target |
| `--lk-accent-dim` | `rgb(0, 140, 170)` | `rgb(0, 122, 140)` | Match `#007A8C` cyan-mid |
| `--lk-border` | `rgb(0, 175, 205)` | `rgb(0, 122, 140)` | Consistent with accent-dim |
| `--lk-border-dim` | `rgb(25, 42, 60)` | `rgb(42, 51, 71)` | Matches `#2A3347` separator tone |
| `--lk-danger` | `rgb(215, 55, 35)` | `rgb(204, 46, 46)` | Pure red aligned to `#CC2E2E` |
| `--lk-success` | `rgb(55, 195, 95)` | `rgb(63, 175, 111)` | Cooler green aligned to `#3FAF6F` |

Variables already correct (no change needed):
- `--lk-accent` `rgb(0, 220, 255)` — matches `#00DCFF` ✓
- `--lk-warning` `rgb(255, 160, 40)` — matches `#FFA028` ✓
- `--lk-accent-secondary` `rgb(160, 50, 145)` — matches `#A03291` ✓

---

## 4. Shader Consistency

**`_HueShift`** — forced to `0` on all 12 card materials (was already 0, now explicitly locked).

**`_Brightness` / `_Saturation`** — runtime-only via `MaterialPropertyBlock` in `CardFeelPresenter.cs`. No material-level change needed:
- Hover max: `1.0 + 0.06 = 1.06` (within 1.1 ceiling) ✓
- Drag max: `1.0 + 0.10 = 1.10` (at ceiling) ✓

---

## 5. Unity Editor Steps Required

1. Unity should auto-refresh material previews. If not: `Assets → Refresh`.
2. Open any Game scene and check card visuals in Play Mode.
3. Verify board surface color against `#141A28` reference.
4. Verify `--lk-border` narrowing is visible in UI panels (should be slightly darker/tighter cyan).
