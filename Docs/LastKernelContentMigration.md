# Last Kernel Content Migration

This pass moved the player-facing content toward a dark cyberpunk fortress survival identity while preserving asset GUIDs, recipe references, quest references, and pack references.

## Audit Summary

The content was mechanically useful but thematically split across several layers:

- A partial cyberpunk rename layer in Chinese.
- Fantasy residue around bosses, rituals, gear, enemies, and treasure.
- Nature and farming chains that read like a survival fantasy economy.
- Packs with old progression roles such as farmstead, blacksmith, adventure, and revelations.
- Quests that taught the loop but used generic or old-theme objective language.

## Disposition

| Content cluster | Decision | Reason |
| --- | --- | --- |
| Core mechanics and asset IDs | Keep | Existing references are stable and useful. |
| Display names and descriptions | Strong rewrite | This is the highest-value identity surface with the lowest reference risk. |
| Food and growth chains | Strong rewrite | Preserved hunger economy while reframing it as ration, hydroponic, and recovery logistics. |
| Fantasy bosses and rituals | Strong rewrite | Converted into Kernel, blackout, blacksite, and rogue-system language. |
| Packs | Strong rewrite | Reframed packs by economic role and progression gate. |
| Quests | Strong rewrite | Reframed objectives as district survival, control, expansion, and defense milestones. |
| Encounters | Light rewrite | Preserved timing and spawn references while replacing old notification language. |
| UI localization | Light rewrite | Replaced colony/settlement residue with district, salvage, fabrication, and containment terms. |
| Asset file names and `m_Name` values | Postpone | Safe to leave for now because they are internal and reference-sensitive. |
| New GUID-level cards | Postpone | Existing assets could cover the missing economy roles without breaking pack or recipe references. |

## Major Migration Map

| Existing content | Last Kernel direction | Gameplay role | Disposition | W/R/B |
| --- | --- | --- | --- | --- |
| Villager, Baby, Warrior, Ranger, Mage | Recruit, Dependent, Enforcer, Scavenger, Netrunner | Personnel and class progression | Strong rewrite | High |
| Wood, Timber, Stone, Flint, Glowing Dust, Abyssal Core | Scrap, Reinforced Scrap, Circuit Parts, Power Cell, Data Shard, Kernel Fragment | Core resources and chase materials | Strong rewrite | High |
| Berry, Potato, Turnip, Meat, Milk, Egg | Ration Pack, Starch Root, Root Ration, Printed Protein, Nutrient Milk, Protein Pod | Hunger and recovery economy | Strong rewrite | High |
| Tree, Rock, Berry Bush, Palm Tree, Basalt Columns | Scrap Heap, Rubble Slab, Supply Cache, Power Cabinet, Server Spine | Nodes and source cards | Strong rewrite | High |
| House, Hearth, Anvil, Sawmill, Logging Camp | Hab Pod, Generator, Fabricator Bench, Cutter Frame, Recycler Yard | District infrastructure | Strong rewrite | High |
| Bonfire, Farm, Planter Box, Library | Heat Coil, Hydroponic Farm, Nutrient Tray, Kernel Archive | Production and research | Strong rewrite | High |
| Goblin, Slime, Troll Shaman, Demon Lord | Glitched Raider, Null Anomaly, Process Echo, Kernel Sovereign | Threat escalation | Strong rewrite | High |
| Chicken, Cow, Crab, Satyr, Squirrel | Protein Drone, Nutrient Synth, Warden Drone, Reclaimer, Scrap Hound | Livestock/producers and enemies | Strong rewrite | Medium |
| Treasure Chest, Golden Key, Blood Chalice, Sacrificial Altar | Encrypted Cache, Root Keycard, Bloodchrome Vessel, Blacksite Conduit | Specials, access, and endgame ritual path | Strong rewrite | High |
| Forest, Fields, Graveyard, Highlands, Ruins | Relay Graveyard, Blackout Yard, Memory Pit, Dead Transit Line, Ruins Sector | Exploration areas | Strong rewrite | High |

W/R/B means the change improves worldbuilding, gameplay readability, and board-management feel.

## Pack Migration

| Asset | New pack | Role | Gate |
| --- | --- | --- | --- |
| `00_Pack_Starter` | Survivor Intake Crate | Free cold-start worker, food, and salvage | 0 Credits / 0 quests |
| `01_Pack_Beginning` | District Utility Crate | Early stabilization and broad utility | 3 Credits / 3 quests |
| `11_Pack_Survival` | Emergency Survival Crate | Cheap recovery for unstable runs | 3 Credits / 3 quests |
| `10_Pack_Island` | Floodline Salvage Crate | Remote starter cache with coolant and flooded-sector salvage | 0 Credits / 0 quests |
| `03_Pack_Farmstead` | Hydroponics Crate | Long-term ration production | 10 Credits / 14 quests |
| `04_Pack_HeartyMeals` | Recovery Crate | Prepared food and margin against hunger | 10 Credits / 18 quests |
| `02_Pack_Revelations` | Kernel Research Crate | Volatile access, fragments, and high-value research | 12 Credits / 18 quests |
| `05_Pack_Knowledge` | Signal Crate | Blueprints, archive hardware, and relay support | 15 Credits / 20 quests |
| `06_Pack_Blacksmith` | Armory Crate | Weapons, armor, fabrication, and combat pressure | 16 Credits / 24 quests |
| `07_Pack_Adventure` | Blackout Expedition Crate | Risk-heavy exploration and better salvage | 20 Credits / 28 quests |
| `08_Pack_Construction` | Infrastructure Crate | Late district expansion and capacity support | 24 Credits / 32 quests |

Pack contents were reclassified through the cards they already reference, so the economy role changes without GUID churn. This is safer than replacing serialized card references before a playtest pass.

## Quest Migration

| Quest group | New identity | Example objectives |
| --- | --- | --- |
| Introduction | District bootstrapping | Open Survivor Intake, Recover Rations, Build a Hab Pod |
| Ascension | Kernel endgame crisis | Build the Conduit, Start the Root Rite, Kill the Sovereign |
| Training | Street defense | Train an Enforcer, Contain a Null Anomaly |
| Advancement | Equipment and class growth | Train a Scavenger, Build Fabrication, Train a Netrunner |
| Cooking | Heat and ration management | Power the Heat Coil, Cook Protein Fold, Make District Broth |
| Exploration | Sector control | Survey Relay Graveyard, Enter Ruins Sector, Search Memory Pit |
| Hoarder | Stockpile pressure | Stockpile Scrap, Credit Reserve, Storage Buffer |
| Construction | District logistics | Build Recycler Yard, Build Slurry Pump, Build Hydroponic Farm |
| Survive | Time pressure | Hold Until Day 8, Hold Until Day 64 |
| The Basics | Floodline onboarding | Open Floodline Crate, Secure Coolant, Mine Server Spine |

## Encounter Migration

| Asset | New notification | Role |
| --- | --- | --- |
| `Encounter_Weekly_Goblin` | A Glitched Raider crew is closing on the outer barricades. | Recurring hostile pressure |
| `Encounter_Weekly_Slime` | A Null Anomaly leaks through the outer grid. | Recurring anomaly pressure |
| `Encounter_Villager` | A lost Recruit signals from the district edge. | Friendly survivor intake |

## UI Language

The footer, day-cycle messages, trade expansion copy, and recipe category labels now lean into the district-survival economy:

- Colony and settlement labels became district labels.
- Gathering became Salvage.
- Forging became Fabrication.
- Husbandry became Containment.
- Credit-chip wording now uses Credits.

## Recipe Migration

Recipes were rewritten as visible actions instead of generic crafting labels:

- Build actions: `Build Hab Pod`, `Build Recycler Yard`, `Build Kernel Archive`.
- Processing actions: `Sort Scrap`, `Strip Circuit Parts`, `Smelt Alloy Ingot`.
- Food actions: `Cook District Broth`, `Mix Med Gel`, `Toast Volt Nut`.
- Exploration actions: `Sweep Blackout Yard`, `Sweep Relay Graveyard`, `Route to Floodline Sector`.
- Endgame actions: `Build Blacksite Conduit`, `Wake Kernel Sovereign`.

## Added Economy Roles

No new card GUIDs were introduced in this pass. Instead, existing referenced assets were converted into missing Last Kernel economy roles:

- `Power Cell`
- `Data Shard`
- `Kernel Fragment`
- `Power Cabinet`
- `Server Spine`
- `Kernel Archive`
- `Floodline Salvage Crate`
- `Blackout Expedition Crate`
- `Emergency Survival Crate`

This keeps existing packs, recipes, quests, and scene references intact while making the economy read as salvage, signal, infrastructure, ration, and Kernel-tech systems.

## Still To Address

- Internal asset filenames still expose old terms such as `Card_DemonLord` and `Recipe_Wood_1`. These are stable internal names for now.
- Art assets still need a visual migration pass so icons match the new fiction.
- Pack card-reference reshuffling and weight tuning should be done after a playtest or economy simulation pass.
- Quest groups appear to be scene/prefab serialized, so adding brand-new quest assets should be paired with an editor wiring pass.
