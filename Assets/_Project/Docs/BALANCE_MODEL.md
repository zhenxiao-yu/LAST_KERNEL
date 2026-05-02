# BALANCE_MODEL — LAST KERNEL
**Version:** 1.1  
**Maintainer:** Game Designer / Systems Lead  
**Last Updated:** 2026-04-30  
**Status:** Living document — update whenever values change

---

## Design North Star

> "Build the system. Trust the system. Watch it fail beautifully."

LAST_KERNEL is a **cyberpunk underground colony survival** game. The player manages a fragile autonomous machine that must survive nightly intrusions from corrupted processes.

**Tone:** Dark, functional, dry humor. Numbers should feel earned, not arbitrary.

---

## Part 1 — Audit: Current State (2026-04-30)

### Content Inventory

| Type | Count | Notes |
|------|-------|-------|
| Cards | 144 | 12 categories |
| Recipes | 113 | 5 subtypes (Base, Exploration, Growth, Research, Travel) |
| Packs | 11 | Price range: 0–24 coins |
| Quests | 96 | 10 tracks |
| Enemies | 3 | BasicCrawler, FastGlitch, HeavyBug |
| Waves | 3 | Night1–Night3 |
| Defenders | 3 | FirewallNode, KernelGuard, SignalPinger |

### Known Imbalances (Ranked by Severity)

#### CRITICAL
- **Mage (Netrunner) DPS ≈ 0.76** vs Warrior ≈ 2.80. The Mage is 3.7× weaker than the next lowest unit. Any combat role for the Mage is non-functional.
- **Crafting durations are inverted:** A sword (15s) forges faster than a bowl of soup (40s). Building a house (30s) takes less time than cooking a steak (60s). The time tier system is broken.
- **All 3 enemies share identical combat parameters** (`attackSpeed=80, accuracy=90, critChance=5, critMultiplier=150`). FastGlitch is mechanically identical to BasicCrawler except for HP and moveSpeed.

#### HIGH
- **All 3 waves have identical morale/salvage/fatigue** (`+5/-10, salvage=1, fatigue=2`). There is no night-to-night risk/reward scaling.
- **Material cards have `attackSpeed=1`** (Wood, Stone). These are non-combat cards — default Unity value leaked into data.

#### MEDIUM
- Pack cost vs quest-unlock ratio is non-linear (Pack_04 and Pack_02 both require 18 quests but cost 10 vs 12).
- After quest 32 (Pack_08 unlock), 64+ quests remain with no pack reward target.
- All recipe `randomWeight = 1` — no craft-priority differentiation on multi-recipe stacks.
- `Card_RawMeat` has `nutrition=0` — correct design intent (must cook) but the card needs a description that says so.

#### LOW
- FastGlitch `moveSpeed=3.5` is correctly differentiated, but `attackSpeed=80` is identical to BasicCrawler.
- Wave `flavorText` fields appear empty on all 3 waves.
- Wood/Stone/Plank/Brick etc. have 2–4 recipe variants with no weight differentiation.

---

## Part 2 — Core Loop Model

### Day Phase (Build & Sustain)

```
Gather → Process → Craft → Feed → Compress → Upgrade → Prepare
```

- **Gather:** Area cards + worker cards + mob cards produce raw resources passively.
- **Process:** Recipes convert raw → refined (Wood→Plank, Ore→Ingot, Meat→Cooked).
- **Craft:** Refined materials → equipment, structures, consumables.
- **Feed:** Consumables consumed to maintain villager morale/function.
- **Compress:** Sell excess via Market; consolidate stacks.
- **Upgrade:** Unlock new packs with accumulated coins.
- **Prepare:** Assign defenders for night phase.

### Night Phase (Survive)

```
Deploy → Auto-Combat → Resolve → Reward
```

- **Deploy:** Commit defenders to lanes. Fatigue cost per defender.
- **Auto-Combat:** Enemies advance; defenders engage; core takes damage if enemies reach base.
- **Resolve:** Morale adjusted by win/loss. Salvage earned per kill.
- **Reward:** Salvage converts to coins for next-day spending.

---

## Part 3 — Mathematical Models

### 3.1 Food / Survival System

```
DailyFoodDemand  = LivingVillagers × FoodPerVillager
DailyFoodSupply  = Σ(ExpectedFoodProducedPerDay per source)
FoodSafetyRatio  = DailyFoodSupply / DailyFoodDemand
```

**Targets:**
- `FoodPerVillager = 2` nutrition/day (1 Synth Apple = 2 nutrition = 1 day of food)
- `FoodSafetyRatio` target: **1.25–1.5** (25–50% buffer)
- Below 1.0 = starvation risk (morale loss, villager penalty)
- Above 2.0 = economy waste (oversupply should be sellable)

**Nutrition Value Ladder:**

| Card (Internal ID) | Display Name | Nutrition | Sell Price | Notes |
|--------------------|--------------|-----------|------------|-------|
| Card_Acorn | Compressed Node | 1 | 1 | Forage fallback |
| Card_Apple | Synth Apple | 2 | 2 | Baseline food unit |
| Card_Berry | Signal Berry | 2 | 2 | Alternative baseline |
| Card_RoastedAcorn | Roasted Node | 2 | 2 | Cooked forage |
| Card_Turnip | Ferrite Bulb | 2 | 1 | Farmable staple |
| Card_Potato | Starch Chip | 2 | 1 | Farmable staple |
| Card_Egg | Protein Cell | 2 | 3 | Animal produce |
| Card_Milk | Core Fluid | 2 | 3 | Animal produce |
| Card_BakedPotato | Baked Starch | 3 | 3 | Tier 2 food |
| Card_FruitSalad | Mixed Signal | 3 | 4 | Tier 2 food |
| Card_Soup | District Broth | 3 | 3 | Tier 2 food |
| Card_Steak | Searpack | 3 | 4 | Tier 2 food |
| Card_Milkshake | Fluid Composite | 4 | 5 | Tier 3 food |
| Card_Omelette | Protein Matrix | 4 | 5 | Tier 3 food |
| Card_Stew | Deep Packet | 4 | 6 | Tier 3 food |
| Card_Coconut | Sealed Canteen | 3 | 4 | Island food |

**Rule:** Processed food should always have nutrition ≥ raw ingredients combined, and sell price ≥ sum of ingredient sell prices.

**RawMeat Rule:** `Card_RawMeat` (`nutrition=0`) is intentional — unprocessed protein cannot sustain units. Its card description must say: *"Must be cooked before consumption. Raw protein has no nutritional value."*

---

### 3.2 Combat DPS Model

```
DPS = Attack × (AttackSpeed / 100) × (Accuracy / 100) × CritModifier
CritModifier = 1 + (CritChance / 100) × ((CritMultiplier / 100) - 1)
```

**Example — Warrior (Enforcer) at current values:**
```
DPS = 3 × (90/100) × (95/100) × [1 + 0.15 × (1.60 - 1)]
    = 3 × 0.90 × 0.95 × 1.09
    = 2.80
```

**Current Unit DPS (Calculated):**

| Unit | Atk | Spd | Acc | Crit% | CritMul | DPS | Rating |
|------|-----|-----|-----|-------|---------|-----|--------|
| Villager (Recruit) | 2 | 130 | 95 | 5 | 150 | 2.54 | OK |
| Warrior (Enforcer) | 3 | 90 | 95 | 15 | 160 | 2.80 | OK |
| Ranger (Scavenger) | 2 | 120 | 98 | 15 | 140 | 2.50 | OK |
| Mage (Netrunner) | 1 | 90 | 80 | 10 | 150 | **0.76** | BROKEN |

**Target DPS Bands:**
- Utility unit (Villager): 2.0–2.5 DPS
- Specialist unit (Warrior/Ranger): 2.5–3.5 DPS
- Support/AoE unit (Mage): 1.5–2.5 DPS base (compensated by multi-target or debuff)

**Mage Fix Target:**
```
Required DPS ≥ 1.8
Current attack=1 is the core problem.
Fix: attack=3, attackSpeed=80, accuracy=85, critChance=15, critMultiplier=200
DPS = 3 × 0.80 × 0.85 × [1 + 0.15 × 1.00] = 3 × 0.80 × 0.85 × 1.15 = 2.35
```

**Effective HP:**
```
EffectiveHP = MaxHP / (1 - min(Dodge / 100, 0.80))
```

| Unit | MaxHP | Dodge | EHP |
|------|-------|-------|-----|
| Villager | 15 | 5 | 15.8 |
| Warrior | 20 | 5 | 21.1 |
| Ranger | 18 | 10 | 20.0 |
| Mage | 16 | 5 | 16.8 |

**Enemy DPS (Current):**

| Enemy | Atk | Spd | Acc | DPS | EHP | Reward |
|-------|-----|-----|-----|-----|-----|--------|
| BasicCrawler | 2 | 80 | 90 | 1.48 | 8 | 1 |
| FastGlitch | 1 | 80 | 90 | 0.74 | 5 | 2 |
| HeavyBug | 4 | 80 | 90 | 2.95 | ~30 | 4 |

**FastGlitch Inconsistency:** Has lower DPS than BasicCrawler despite costing more reward (2 vs 1). Its threat is purely speed-based (moveSpeed=3.5), not damage. This is an underspecified design intent — fix by either:
- Raising FastGlitch `attackSpeed` to 140 (it's "fast" after all), or
- Lowering its reward to 1 (it's a weak rusher, not worth 2)

---

### 3.3 Crafting Time Tier Model

Crafting time should reflect **cognitive complexity + material rarity**, not arbitrary seconds.

```
Tier 0 — Sort/Split     :  5–10s   (basic resource separation)
Tier 1 — Process        : 10–20s   (basic refinement, gathering)
Tier 2 — Cook/Assemble  : 30–60s   (food, simple tools)
Tier 3 — Forge/Build    : 60–120s  (weapons, basic structures)
Tier 4 — Construct      : 120–240s (buildings, advanced equipment)
Tier 5 — Engineer       : 240–480s (training, grand structures)
```

**Current vs Recommended:**

| Recipe | Current | Target Tier | Recommended | Issue |
|--------|---------|-------------|-------------|-------|
| Recipe_Wood_1 (Sort Scrap) | 10s | 0 | 5s | Acceptable |
| Recipe_Stone_1 (Strip Parts) | 10s | 0 | 5s | Acceptable |
| Recipe_WoodenStick | 10s | 1 | 10s | OK |
| Recipe_IronIngot | 10s | 1 | 15s | Too fast for smelting |
| Recipe_Rope | 10s | 1 | 12s | OK |
| Recipe_Flint | 10s | 1 | 8s | OK |
| Recipe_Plank | 10s | 1 | 12s | OK |
| Recipe_Soup | 40s | 2 | 35s | Slightly OK |
| Recipe_Steak | 60s | 2 | 45s | Steak ≠ harder than soup |
| Recipe_BakedPotato | — | 2 | 30s | |
| Recipe_WoodenClub | — | 2 | 25s | Primitive weapon |
| Recipe_Slingshot | — | 2 | 35s | Simple ranged |
| Recipe_Sword | 15s | 3 | **75s** | BROKEN: swords require forging |
| Recipe_Bow | — | 3 | 60s | |
| Recipe_Anvil | 30s | 3 | **60s** | BROKEN: prerequisite structure |
| Recipe_Chainmail | — | 3 | 90s | |
| Recipe_Bonfire | — | 2 | 20s | Basic fire |
| Recipe_Hearth | — | 3 | 60s | Permanent structure |
| Recipe_Furnace | — | 3 | 90s | Enables Tier 3+ |
| Recipe_House | 30s | 4 | **150s** | BROKEN: housing = major project |
| Recipe_Kiln | — | 3 | 75s | |
| Recipe_Sawmill | — | 4 | 120s | |
| Recipe_IronMine | — | 4 | 120s | |
| Recipe_Villager | 180s | 5 | 300s | Training should be costly |
| Recipe_GrandPortal | — | 5 | 480s | Late-game anchor |

**The Inversion Rule:** No weapon should craft faster than the structure required to craft it. No food should take longer than a building.

---

### 3.4 Economy / Pack Model

**Currency Sources:**
- Sell cards via Market (Apple=2, Steak=4, IronIngot=5 coins)
- Night salvage: `salvagePerKill × killCount` per night
- Quest rewards (assumed coin or card drops)

**Currency Sinks:**
- Pack purchases (3–24 coins)
- Board expansion (BoardExpansionVendor)

**Target Economy by Day:**

| Day | Expected Coins Earned | Pack Affordable | Gate |
|-----|----------------------|-----------------|------|
| 1 | 2–5 (selling surplus food) | Starter (free) | — |
| 3 | 8–15 (gathering + Night1) | Pack_01/11 (3 coins) | 3 quests |
| 6 | 20–35 | Pack_03 Farmstead (10) | 14 quests |
| 8 | 35–55 | Pack_02 Revelations (12) | 18 quests |
| 10 | 50–80 | Pack_05 Knowledge (15) | 20 quests |
| 12 | 70–110 | Pack_06 Blacksmith (16) | 24 quests |
| 15 | 100–160 | Pack_07 Adventure (20) | 28 quests |
| 18 | 140–220 | Pack_08 Construction (24) | 32 quests |

**Pack Value Formula:**
```
PerceivedValue = SlotCount × AvgCardValue × (1 + RecipeChance × RecipeValue)
CostEfficiency = PerceivedValue / BuyPrice
Target CostEfficiency: 1.5–2.5× (packs should feel worth buying)
```

**Pack Pricing Review:**

| Pack | Price | Min Quests | Uses | Note |
|------|-------|-----------|------|------|
| 00_Starter | 0 | 0 | 1 | Tutorial gate |
| 01_Beginning | 3 | 3 | 3 | Good (3 opens for 3 coins) |
| 11_Survival | 3 | 3 | 1 | Weaker than Pack_01 for same price |
| 03_Farmstead | 10 | 14 | 1 | OK |
| 04_HeartyMeals | 10 | 18 | 1 | Same price as Farmstead, higher gate |
| 02_Revelations | 12 | 18 | 1 | Same gate as HeartyMeals, more expensive |
| 05_Knowledge | 15 | 20 | 1 | OK |
| 06_Blacksmith | 16 | 24 | 1 | OK |
| 07_Adventure | 20 | 28 | 1 | OK |
| 08_Construction | 24 | 32 | 1 | OK |
| 10_Island | 0 | 0 | 1 | Scene travel trigger |

**Issues:**
- Pack_11 is strictly worse than Pack_01 for the same quest gate and same price — either raise Pack_11's uses to 3 or make it a different product.
- HeartyMeals (10 coins, 18 quests) and Revelations (12 coins, 18 quests) compete at the same gate. Either stagger their unlock thresholds or justify the price difference with content value.

---

### 3.5 Night Wave Scaling Model

```
ThreatBudget(Night N) = BaseThreat × ScaleFactor^(N-1)
BaseThreat = Σ(EnemyEHP × EnemyDPS) per wave
RewardBudget(Night N) = BaseReward × ScaleFactor^(N-1)

Target: Player's defender DPS should exceed threat by 1.2–1.5× on a "prepared" run.
```

**Wave Progression Targets:**

| Night | Threat Level | Enemies | Salvage/Kill | Victory Morale | Defeat Morale | Fatigue/Defender |
|-------|-------------|---------|-------------|----------------|---------------|-----------------|
| 1 | Tutorial | 2–3 Crawlers | 1 | +8 | -5 | 1 |
| 2 | Easy | 3 Crawlers + 2 Glitch | 1 | +6 | -8 | 2 |
| 3 | Medium | 3 Crawlers + 3 Glitch + 1 Heavy | 2 | +5 | -12 | 2 |
| 4 | Hard | 4 Crawlers + 4 Glitch + 2 Heavy | 2 | +4 | -15 | 3 |
| 5 | Brutal | 5+ mixed + elite | 3 | +3 | -20 | 3 |

**Current Wave Issues:**
- All 3 waves have identical morale/reward/fatigue — the "risk" of losing Night3 should be far greater than losing Night1.
- Victory morale should **decrease** per wave (each survival is less surprising).
- Defeat morale should **increase** in magnitude per wave (losing a harder wave is more catastrophic).
- Salvage per kill should increase per wave (harder enemies = richer rewards).

**Current Values vs Recommended:**

| | Night1 Current | Night1 Target | Night2 Current | Night2 Target | Night3 Current | Night3 Target |
|-|---|---|---|---|---|---|
| `victoryMoraleDelta` | +5 | +8 | +5 | +6 | +5 | +5 |
| `defeatMoraleDelta` | -10 | -5 | -10 | -8 | -10 | -15 |
| `salvagePerKill` | 1 | 1 | 1 | 1 | 1 | 2 |
| `fatigueCostPerDefender` | 2 | 1 | 2 | 2 | 2 | 2 |

---

### 3.6 Enemy Differentiation Model

Each enemy archetype should have a **distinct mechanical role**:

| Enemy | Role | Differentiator | Threat Source |
|-------|------|----------------|--------------|
| BasicCrawler | Frontline | HP tank, steady damage | Sustained DPS |
| FastGlitch | Rusher | High moveSpeed, low HP | Bypasses slow defenders |
| HeavyBug | Elite | High HP+ATK+DEF, high reward | Requires dedicated counter |

**Target Stats (After Fix):**

| Stat | BasicCrawler | FastGlitch | HeavyBug |
|------|-------------|-----------|---------|
| maxHP | 8 | 5 | 25 |
| attack | 2 | 1 | 4 |
| defense | 0 | 0 | 2 |
| attackSpeed | **80** | **140** | **60** |
| accuracy | 85 | 80 | 90 |
| dodge | 0 | **15** | 0 |
| critChance | 5 | 5 | **10** |
| critMultiplier | 150 | 150 | **175** |
| moveSpeed | 2.0 | 3.5 | 0.9 |
| damageToBase | 1 | 1 | 3 |
| rewardAmount | 1 | **1** | 4 |

Changes:
- FastGlitch `attackSpeed`: 80 → **140** (it's fast — it attacks more often)
- FastGlitch `dodge`: 0 → **15** (agile, hard to hit)
- FastGlitch `rewardAmount`: 2 → **1** (low-value rusher, threat is speed not power)
- HeavyBug `attackSpeed`: 80 → **60** (slow but powerful)
- HeavyBug `critChance`: 5 → **10** (tanky units sometimes land crushing hits)

---

### 3.7 Character DPS Target Model

Mage (Netrunner) fix — assumes Mage has future AoE or debuff potential, but needs baseline DPS viability:

**Target Values:**

| Stat | Villager | Warrior | Ranger | Mage (fixed) |
|------|----------|---------|--------|--------------|
| maxHealth | 15 | 20 | 18 | **18** |
| attack | 2 | 3 | 2 | **3** |
| defense | 1 | 2 | 1 | **0** |
| attackSpeed | 130 | 90 | 120 | **80** |
| accuracy | 95 | 95 | 98 | **85** |
| dodge | 5 | 5 | 10 | **10** |
| critChance | 5 | 15 | 15 | **20** |
| critMultiplier | 150 | 160 | 140 | **200** |

**Resulting DPS:**
- Mage: 3 × 0.80 × 0.85 × [1 + 0.20 × 1.00] = **2.45** (up from 0.76)
- Design flavor: Glass-cannon scholar — low defense, high volatility crits, misses sometimes.

---

### 3.8 Material Card Cleanup

Material cards should have `attackSpeed = 0` (or the field should simply not apply to non-combatants).

**Cards requiring `attackSpeed` = 0:**
- Card_Wood (Scrap)
- Card_Stone (Circuit Parts)
- Card_Fiber, Card_Flint, Card_Plank, Card_Rope, Card_Brick, Card_Clay
- Card_IronOre, Card_Timber
- All Consumable cards

This is likely a Unity default value (1) that was never cleared on non-combat cards.

---

## Part 4 — Pack Content Design Rules

1. **Free packs (price=0)** are story/tutorial triggers, not economy. Never add free packs to the main progression track.
2. **Multi-use packs** (`uses > 1`) should cost proportionally more per use than single-use packs.
3. **Recipe chance per slot** should scale with pack price: early packs rarely include recipes (10%), late packs are predominantly recipes (60–80%).
4. **Pack unlock gates** should use quest count as a proxy for progression depth. No two packs should share the same `minQuests` value if they have different prices and content profiles.

---

## Part 5 — Progression Model

### Quest Track → Pack Gate Map

| Quest Count | Pack Unlocked | Pack Type |
|-------------|--------------|-----------|
| 0 | 00_Starter, 10_Island | Tutorial/Story |
| 3 | 01_Beginning (×3), 11_Survival (×1, 1 coin) | Survival basics |
| 8 | 03_Farmstead | Food production |
| 12 | 04_HeartyMeals | Advanced food |
| 18 | 02_Revelations | Research/discovery |
| 20 | 05_Knowledge | Signal/intel |
| 24 | 06_Blacksmith | Combat/forging |
| 28 | 07_Adventure | Exploration |
| 32 | 08_Construction | Infrastructure |
| 40 | 09_SignalCore | Advanced processing (28 coins) |
| 55 | 12_Overseer | Colony control (35 coins) |
| 70 | 13_LastKernel | Final directive (44 coins) |

---

## Part 6 — Localization / Description Standards

Every card MUST have a localized description that communicates:
1. **What it does** (mechanically, in-game terms)
2. **How to get more** (if applicable)
3. **Any hidden rules** (e.g., RawMeat: "must be cooked")

**Description tone:** Functional, dry, cyberpunk. Avoid fantasy language. Use "system," "kernel," "data," "signal," "core," "packet" over "magic," "ancient," "mystical."

**Priority for description completion:**
1. All Consumables (food items, nutrition rules)
2. All Characters (combat role + how to produce)
3. All Equipment (what stat modifiers apply)
4. All Structures (what recipes they enable)
5. All Mobs (aggression behavior, loot)

---

## Part 7 — Known Gaps (Not Yet Designed)

- [ ] Defender card integration with CardDefinition system (currently separate DefenderData)
- [x] Post-quest-32 pack content — 09_SignalCore (Q40), 12_Overseer (Q55), 13_LastKernel (Q70) added
- [ ] Endgame pack art textures (artTexture: {fileID: 0} on all three new packs)
- [ ] Elite enemy variants beyond Night 3
- [ ] Boss/event wave definitions (NightWaveDefinition.flavorText unused)
- [ ] Economy sink beyond packs (board upgrades, trader events)
- [x] Wave flavor text — populated on all 3 waves (2026-04-30)
- [ ] New food card art (artTexture: {fileID: 0} on AlgaeWafer, SignalJerky, MycoChip, CoreRation, VatBroth)
- [ ] Weapon type triangle (combatType enum exists, triangle logic unimplemented)

---

---

## Part 8 — Stacklands Reference Comparison

Stacklands (Sokpop, 2022) is the closest genre reference. Using it as a calibration baseline.

### Economy & Survival

| Metric | Stacklands | LAST KERNEL (v1.1) | Gap / Notes |
|--------|-----------|---------------------|-------------|
| Food per villager / day | 2 | 2 | Aligned |
| Day cycle (Normal) | 120s | Not timed (card-driven) | Different system |
| Apple / basic food nutrition | 2 | 2 | Identical |
| High-value food (carrot/Core Ration) | 4 | 4 | Aligned |
| Cooked food nutrition | 3 (omelette) | 3–4 (steak/broth) | LAST_KERNEL slightly higher |
| Basic food sell price | 2–5 | 1–6 | Comparable range |
| Starter pack cost | 3 coins / 3 cards | 3 coins / 3 cards | Identical |
| Mid-tier pack cost | 10 coins / 3–4 cards | 10 coins / 1 card | LAST_KERNEL packs are thinner |
| Late pack cost | 25 coins / 4 cards | 24 coins / 1 card | LAST_KERNEL should add more slots |
| Market sell multiplier | 2× base sell price | Direct sell price | Stacklands uses a market doubler |

### Combat

| Metric | Stacklands | LAST KERNEL (v1.1) | Notes |
|--------|-----------|---------------------|-------|
| Villager base HP | 15 | 15 | Identical |
| Defense formula | Blocks 1 per 2 DEF (50% bypass) | Flat reduction (verify CombatRules.cs) | Different model |
| Crit system | +1 damage on 50% of hits | critChance × critMultiplier | LAST_KERNEL more granular |
| Combat tiers | 6 discrete tiers | Continuous numeric stats | LAST_KERNEL more precise |
| Weapon type triangle | Melee > Magic > Ranged > Melee | combatType enum (not yet enforced) | Gap: weapon triangle unimplemented |

### Crafting

| Recipe | Stacklands | LAST KERNEL (v1.1) | Notes |
|--------|-----------|---------------------|-------|
| Stove cook time | 27s (2 eggs → omelette) | 35–45s (soups/steaks) | LAST_KERNEL slightly longer, reasonable |
| Building construction | Ingredient-only (no timer) | 60–150s | LAST_KERNEL adds time cost |
| Weapon crafting | Ingredient-only | 75s (sword) | LAST_KERNEL adds time cost |

### Key Lessons from Stacklands

1. **Pack density:** Stacklands gives 3–4 cards per pack at 3–25 coins. LAST_KERNEL packs often give 1 card at high prices — increase slot counts or add bonus slot chances.
2. **Market as economic valve:** Stacklands doubles sell prices through the Market card. Consider mirroring this: base sellPrice low, Market gives a multiplier.
3. **Weapon triangle:** Melee/Magic/Ranged creates counterplay. The `combatType` enum exists in LAST_KERNEL but the triangle isn't enforced — high-value future work.
4. **Food variety is critical:** Stacklands has few food types (berry, apple, carrot, egg, milk) but each has a clear production chain. New LAST_KERNEL cards (Vat Plating, Myco Chip, Core Ration, Dried Protein Strip, Vat Broth) follow this model.

---

## Part 9 — Applied Fixes Log (2026-04-30)

All changes applied directly to .asset files.

### Enemies

| Asset | Field | Old | New | Reason |
|-------|-------|-----|-----|--------|
| Enemy_FastGlitch | attackSpeed | 80 | 140 | "Fast" enemy should attack fast |
| Enemy_FastGlitch | accuracy | 90 | 82 | Speed trades off with precision |
| Enemy_FastGlitch | dodge | 0 | 15 | Agile runner is hard to hit |
| Enemy_FastGlitch | rewardAmount | 2 | 1 | Rusher = low-value threat |
| Enemy_HeavyBug | attackSpeed | 80 | 60 | Tank enemy attacks slowly |
| Enemy_HeavyBug | accuracy | 90 | 85 | Armored but less precise |
| Enemy_HeavyBug | critChance | 5 | 10 | Occasional crushing hit |
| Enemy_HeavyBug | critMultiplier | 150 | 175 | Hits harder when it lands |

### Waves

| Asset | Change | Reason |
|-------|--------|--------|
| Wave_Night1 | 2 BasicCrawler + 1 HeavyBug → 3 BasicCrawler | Night1 had a heavy enemy; tutorial should be crawlers only |
| Wave_Night1 | victoryMoraleDelta 5 → 8 | Early victory should feel rewarding |
| Wave_Night1 | defeatMoraleDelta -10 → -5 | Early loss should sting less |
| Wave_Night1 | fatigueCostPerDefender 2 → 1 | Low-stakes night, low commitment cost |
| Wave_Night1 | flavorText (empty → set) | Added flavor text to all waves |
| Wave_Night2 | 2 HeavyBug + 2 FastGlitch → 2 BasicCrawler + 3 FastGlitch | Introduce rusher type before tank |
| Wave_Night2 | victoryMoraleDelta 5 → 6 | Slight downward trend per wave |
| Wave_Night2 | defeatMoraleDelta -10 → -8 | Mid-range stakes |
| Wave_Night3 | DUPLICATE 7×BasicCrawler → 2 BasicCrawler + 3 FastGlitch + 2 HeavyBug | Bug fix + proper escalation |
| Wave_Night3 | defeatMoraleDelta -10 → -15 | Late wave loss is serious |
| Wave_Night3 | salvagePerKill 1 → 2 | Harder wave = richer reward |

### Characters

| Asset | Field | Old | New | Reason |
|-------|-------|-----|-----|--------|
| Card_Mage (Netrunner) | maxHealth | 16 | 18 | Slight HP buff to improve survivability |
| Card_Mage (Netrunner) | attack | 1 | 3 | DPS was 0.76 — completely non-functional |
| Card_Mage (Netrunner) | defense | 1 | 0 | Glass cannon — high crit, no armor |
| Card_Mage (Netrunner) | attackSpeed | 90 | 80 | Deliberate, slow-but-powerful caster |
| Card_Mage (Netrunner) | accuracy | 80 | 85 | Less miss-prone after attack buff |
| Card_Mage (Netrunner) | dodge | 5 | 10 | Agile but fragile |
| Card_Mage (Netrunner) | criticalChance | 10 | 20 | High-variance identity |
| Card_Mage (Netrunner) | criticalMultiplier | 150 | 200 | Crits deal real damage |
| **Result** | DPS | 0.76 | **2.45** | Now competitive with Warrior (2.80) |

### Materials

| Asset | Field | Old | New | Reason |
|-------|-------|-----|-----|--------|
| Card_Wood (Scrap) | attackSpeed | 1 | 0 | Unity default leaked into non-combat card |
| Card_Stone (Circuit Parts) | attackSpeed | 1 | 0 | Same |

### Recipes

| Asset | Field | Old | New | Reason |
|-------|-------|-----|-----|--------|
| Recipe_Sword (Forge Alloy Cutter) | craftingDuration | 15s | 75s | Swords require actual forging time |
| Recipe_House (Build Hab Pod) | craftingDuration | 30s | 150s | A house took less time than soup — inverted |
| Recipe_Anvil (Build Fabricator Bench) | craftingDuration | 30s | 60s | Prerequisite structure needs weight |
| Recipe_IronIngot (Smelt Alloy Ingot) | craftingDuration | 10s | 20s | Smelting takes longer than sorting scrap |
| Recipe_Steak_1 (Sear Protein Pack) | craftingDuration | 60s | 45s | Steak was slower than soup — inverted |
| Recipe_Soup_1 (Cook District Broth) | craftingDuration | 40s | 35s | Slight reduction to match tier |

### New Food Cards (5 added)

| Card | Display Name | Nutrition | Sell Price | Description Summary |
|------|-------------|-----------|------------|---------------------|
| Card_AlgaeWafer | Vat Plating | 1 | 1 | Pressed algae — sub-baseline survival food |
| Card_SignalJerky | Dried Protein Strip | 2 | 3 | Preserved printed protein — shelf-stable |
| Card_CoreRation | Core Ration | 4 | 6 | Emergency paste — dense, flavorless, necessary |
| Card_MycoChip | Myco Chip | 2 | 2 | Fungal growth from ventilation shafts |
| Card_VatBroth | Vat Broth | 4 | 5 | Bulk rendered broth — premium food tier |

All cards need: art texture, localization keys, and recipe definitions before they are game-ready.

### New Late-Game Cards (9 added, 2026-05-01)

These cards form interlocking production chains available from packs 09–13 (quest gates 40–70). All use `Card_Anvil` (Fabricator Bench) as a non-consumed tool ingredient (`consumptionMode: 1`).

#### Card Catalog

| Card | Display Name | Category | GUID suffix | Role |
|------|-------------|----------|-------------|------|
| Card_DataShard | Data Shard | Material (4) | d1...a1 | Endgame crafting currency — "information ore" used in all new recipes |
| Card_KernelShard | Kernel Shard | Material (4) | d7...a7 | Rare endgame drop; sellPrice=25; required for Recipe_StasisPod |
| Card_Conscript | Emergency Conscript | Character (2) | d4...a4 | Budget emergency defender; atk=2, DPS≈1.96; faction=1 |
| Card_SurgeWeapon | Surge Weapon | Equipment (5) | d3...a3 | 3-use burst weapon: +5 atk, -15 attackSpeed |
| Card_CombatChip | Combat Implant | Equipment (5) | d9...a9 | 5-use neural enhancer: +2 atk, +10 speed, +10 critChance |
| Card_TapNode | Tap Node | Structure (6) | d2...a2 | Passive coin engine: 1 Coin every 90s |
| Card_OverseerCore | Overseer Core | Structure (6) | d5...a5 | Colony management hub; maxHP=40 |
| Card_SignalDrone | Signal Drone | Structure (6) | d6...a6 | Passive DataShard generator: 1 DataShard every 120s |
| Card_StasisPod | Stasis Pod | Structure (6) | d8...a8 | Emergency food synthesizer: 1 AlgaeWafer every 150s |

#### Production Chains

**Economy loop (coin sink → passive income):**
```
3×Coin → [Recipe_DataShard, 20s] → DataShard
DataShard + Construction + Anvil → [Recipe_TapNode, 90s] → TapNode → 1 Coin/90s [passive]
```

**Combat chain:**
```
2×Coin + Anvil → [Recipe_Conscript, 90s] → Conscript [DPS 1.96, cheap bulk defender]
DataShard + Anvil → [Recipe_CombatChip, 60s] → CombatChip [5-use all-round buff]
2×DataShard + Anvil → [Recipe_SurgeWeapon, 90s] → SurgeWeapon [3-use burst]
```

**Infrastructure chain:**
```
DataShard + Construction + Anvil → [Recipe_SignalDrone, 60s] → SignalDrone → 1 DataShard/120s [passive]
2×DataShard + Anvil → [Recipe_OverseerCore, 120s] → OverseerCore [HP=40 hub]
KernelShard + DataShard + Anvil → [Recipe_StasisPod, 180s] → StasisPod → 1 AlgaeWafer/150s [passive]
```

#### Recipe Summary (8 new recipes)

| Recipe GUID suffix | Display Name | Ingredients | Result | Duration | Category |
|--------------------|-------------|-------------|--------|----------|----------|
| e1...b1 | Forge Data Shard | 3×Coin | DataShard | 20s | 4 (Materials) |
| e2...b2 | Build Tap Node | DataShard + Construction + Anvil | TapNode | 90s | 6 (Structures) |
| e3...b3 | Forge Surge Weapon | 2×DataShard + Anvil | SurgeWeapon | 90s | 5 (Equipment) |
| e4...b4 | Recruit Conscript | 2×Coin + Anvil | Conscript | 90s | 2 (Characters) |
| e5...b5 | Build Overseer Core | 2×DataShard + Anvil | OverseerCore | 120s | 6 (Structures) |
| e6...b6 | Build Signal Drone | DataShard + Construction + Anvil | SignalDrone | 60s | 6 (Structures) |
| e7...b7 | Build Stasis Pod | KernelShard + DataShard + Anvil | StasisPod | 180s | 6 (Structures) |
| e8...b8 | Forge Combat Implant | DataShard + Anvil | CombatChip | 60s | 5 (Equipment) |

#### Balance Notes

- **Conscript DPS (1.96)** sits below Warrior (2.80) and Ranger (2.50) intentionally — it is the "floor" unit that prevents being defenseless, not a damage dealer.
- **TapNode output** (~40 coins/hour) is slower than active selling but requires no player attention. Net positive with 2+ TapNodes running.
- **SurgeWeapon trade-off:** +5 attack but -15 attackSpeed means lower sustained DPS — correct for a "save for the hard fight" item.
- **StasisPod food output** (1 AlgaeWafer every 150s = ~1 nutrition per 2.5 minutes) supplements but does not replace active food production — intended as emergency buffer.

All 9 cards need: art textures (artTexture: {fileID: 0} on all), localization keys, and description strings before shipping.

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-04-30 | Initial audit + balance model created | Claude (Systems) |
| 2026-04-30 | v1.1: Applied all fixes; added 5 food cards; added Stacklands comparison | Claude (Systems) |
| 2026-05-01 | v1.2: Created 3 endgame packs (09_SignalCore Q40/28c, 12_Overseer Q55/35c, 13_LastKernel Q70/44c); verified all prior fixes applied in assets; updated progression map with actual minQuests values | Claude (Systems) |
| 2026-05-01 | v1.3: Created 9 new late-game cards + 8 recipes; wired all 3 endgame packs to reference new card/recipe GUIDs; added new card catalog section with production chain map | Claude (Systems) |
