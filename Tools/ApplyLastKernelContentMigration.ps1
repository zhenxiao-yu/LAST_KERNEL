$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $PSScriptRoot
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$NumericFields = @('buyPrice', 'minQuests')

function ConvertTo-YamlSingleQuotedValue {
    param([string]$Value)

    return $Value.Replace("'", "''")
}

function Set-AssetFields {
    param(
        [string]$RelativePath,
        [hashtable]$Fields
    )

    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing asset: $RelativePath"
    }

    $text = [System.IO.File]::ReadAllText($path)
    foreach ($field in $Fields.Keys) {
        $escapedField = [regex]::Escape($field)
        $pattern = "(?m)^(\s*${escapedField}:\s*).*$"
        $matches = [regex]::Matches($text, $pattern)
        if ($matches.Count -eq 0) {
            throw "Missing field '$field' in $RelativePath"
        }

        $rawValue = [string]$Fields[$field]
        if ($field -in $NumericFields -and $rawValue -match '^-?\d+(\.\d+)?$') {
            $text = [regex]::Replace($text, $pattern, { param($match) "$($match.Groups[1].Value)$rawValue" })
        }
        else {
            $safeValue = ConvertTo-YamlSingleQuotedValue $rawValue
            $text = [regex]::Replace($text, $pattern, { param($match) "$($match.Groups[1].Value)'$safeValue'" })
        }
    }

    [System.IO.File]::WriteAllText($path, $text, $Utf8NoBom)
}

function Assert-ContentPath {
    param([string]$RelativePath)

    $path = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing content path before migration: $RelativePath"
    }
}

$CardRows = @'
path|displayName|description
Assets/Fortstack/Resources/Cards/Card_Recipe.asset|Blueprint Packet|A compressed build plan for district fabrication. Drag it into memory before the next blackout.
Assets/Fortstack/Resources/Cards/Areas/Card_Fields.asset|Blackout Yard|An open work yard under dead billboards. Good salvage, poor cover.
Assets/Fortstack/Resources/Cards/Areas/Card_Forest.asset|Relay Graveyard|A maze of fallen masts and live wire. Signals still move through it in bad shapes.
Assets/Fortstack/Resources/Cards/Areas/Card_Graveyard.asset|Memory Pit|A disposal trench for bodies, drives, and erased records. Some entries keep answering.
Assets/Fortstack/Resources/Cards/Areas/Card_Highlands.asset|Dead Transit Line|An elevated rail corridor above the district. Fast routes, exposed fights.
Assets/Fortstack/Resources/Cards/Areas/Card_Ruins.asset|Ruins Sector|Collapsed megastructure blocks around the Last Kernel. Valuable systems are buried under hostile code.
Assets/Fortstack/Resources/Cards/Characters/Card_Baby.asset|Dependent|A district child under your protection. No output yet, but the future has weight.
Assets/Fortstack/Resources/Cards/Characters/Card_Mage.asset|Netrunner|A field hacker who turns signal noise into weaponized breaches.
Assets/Fortstack/Resources/Cards/Characters/Card_Ranger.asset|Scavenger|A sharp-eyed runner built for ranged work and salvage routes.
Assets/Fortstack/Resources/Cards/Characters/Card_Villager.asset|Recruit|A district civilian pressed into service. Cheap, fragile, and necessary.
Assets/Fortstack/Resources/Cards/Characters/Card_Warrior.asset|Enforcer|A plated fighter for holding bad streets and breaking worse doors.
Assets/Fortstack/Resources/Cards/Consumables/Card_Acorn.asset|Volt Nut|A small energy seed from a cracked hydro rack. Better toasted than raw.
Assets/Fortstack/Resources/Cards/Consumables/Card_Apple.asset|Synth Apple|Clean fruit printed from old orchard templates. Small comfort, useful calories.
Assets/Fortstack/Resources/Cards/Consumables/Card_BakedPotato.asset|Heated Starch|A hot block of dense carbohydrates. Not elegant, but it keeps workers upright.
Assets/Fortstack/Resources/Cards/Consumables/Card_Berry.asset|Ration Pack|Sealed calories from a supply cache. Basic fuel for another shift.
Assets/Fortstack/Resources/Cards/Consumables/Card_Coconut.asset|Coolant Pod|A sealed fluid pod from flooded utility bays. Drinkable after filtering.
Assets/Fortstack/Resources/Cards/Consumables/Card_Egg.asset|Protein Pod|Lab-grown protein in a brittle membrane. Stable enough for cooking.
Assets/Fortstack/Resources/Cards/Consumables/Card_FruitSalad.asset|Mixed Rations|A compact meal assembled from clean produce. Efficient and easy to stack.
Assets/Fortstack/Resources/Cards/Consumables/Card_Milk.asset|Nutrient Milk|Cultured feedstock with enough minerals to blunt hunger.
Assets/Fortstack/Resources/Cards/Consumables/Card_Milkshake.asset|Med Gel|A cold recovery slurry for shock, burns, and empty stomachs.
Assets/Fortstack/Resources/Cards/Consumables/Card_Omelette.asset|Protein Fold|Cooked protein with just enough texture to feel like food.
Assets/Fortstack/Resources/Cards/Consumables/Card_Potato.asset|Starch Root|A hardy hydroponic tuber grown in dirty nutrient trays.
Assets/Fortstack/Resources/Cards/Consumables/Card_RawMeat.asset|Printed Protein|Fresh tissue from a cheap vat printer. Cook before morale notices.
Assets/Fortstack/Resources/Cards/Consumables/Card_RoastedAcorn.asset|Toasted Volt Nut|A toasted energy seed with a sharp chemical bite.
Assets/Fortstack/Resources/Cards/Consumables/Card_Soup.asset|District Broth|Hot water, protein scraps, and discipline. Enough to keep a crew moving.
Assets/Fortstack/Resources/Cards/Consumables/Card_Steak.asset|Searpack|Pressed protein flash-seared on a hot plate. Good morale per gram.
Assets/Fortstack/Resources/Cards/Consumables/Card_Stew.asset|Reactor Stew|Dense communal food cooked beside waste heat. Slow, heavy, sustaining.
Assets/Fortstack/Resources/Cards/Consumables/Card_Turnip.asset|Root Ration|A mineral-rich root crop from low-power hydroponics.
Assets/Fortstack/Resources/Cards/Currencies/Card_Coin.asset|Credits|Hard local credit accepted by vendors that still trust the district ledger.
Assets/Fortstack/Resources/Cards/Currencies/Card_Coral.asset|Black Coral|A bioelectric trade crystal pulled from flooded conduit. Rare and negotiable.
Assets/Fortstack/Resources/Cards/Equipments/Card_Bow.asset|Coilbow|A quiet ranged weapon that stores tension in scavenged magnetic limbs.
Assets/Fortstack/Resources/Cards/Equipments/Card_Cane.asset|Signal Cane|A walking aid packed with breach tools and conductor wire.
Assets/Fortstack/Resources/Cards/Equipments/Card_Chainmail.asset|Plate Mesh|Linked alloy mesh for surviving blades, teeth, and shrapnel.
Assets/Fortstack/Resources/Cards/Equipments/Card_LeatherArmor.asset|Kevlar Wrap|Layered street armor cut from old riot gear.
Assets/Fortstack/Resources/Cards/Equipments/Card_Quiver.asset|Ammo Rig|A wearable rack for bolts, charges, and whatever still fires.
Assets/Fortstack/Resources/Cards/Equipments/Card_SlimeHat.asset|Gel Hood|Reactive gel padding that eats impact before your skull does.
Assets/Fortstack/Resources/Cards/Equipments/Card_Slingshot.asset|Rail Sling|A compact launcher that throws scrap bearings at painful speed.
Assets/Fortstack/Resources/Cards/Equipments/Card_Staff.asset|Signal Staff|A conductor pole tuned for directed arcs and hostile signals.
Assets/Fortstack/Resources/Cards/Equipments/Card_Sword.asset|Alloy Cutter|A heavy cutting blade for close work in narrow streets.
Assets/Fortstack/Resources/Cards/Equipments/Card_Tunic.asset|Patchcoat|A reinforced coat stitched from tarps, mesh, and stubbornness.
Assets/Fortstack/Resources/Cards/Equipments/Card_VitalityAmulet.asset|Med Loop|A wearable biofeedback loop that keeps wounds from becoming final.
Assets/Fortstack/Resources/Cards/Equipments/Card_WoodenClub.asset|Shock Baton|A crude baton wired to a failing cell. Simple problems meet simple current.
Assets/Fortstack/Resources/Cards/Materials/Card_AbyssalCore.asset|Kernel Fragment|A shard of core architecture from the old network. Rare, unstable, and worth guarding.
Assets/Fortstack/Resources/Cards/Materials/Card_Brick.asset|Heat Brick|Compressed ceramic block for heat, walls, and hard repairs.
Assets/Fortstack/Resources/Cards/Materials/Card_Clay.asset|Ceramic Slurry|Ceramic slurry from broken pipes and filter tanks. It hardens into useful casing.
Assets/Fortstack/Resources/Cards/Materials/Card_Corpse.asset|Biometric Shell|A dead body or ruined clone chassis. Grim salvage for systems that still demand flesh.
Assets/Fortstack/Resources/Cards/Materials/Card_Fiber.asset|Cable Fiber|Stripped insulation and braided synth thread. Flexible, light, always needed.
Assets/Fortstack/Resources/Cards/Materials/Card_Flint.asset|Power Cell|A cracked but live cell from emergency hardware. Small, volatile, and always useful.
Assets/Fortstack/Resources/Cards/Materials/Card_GlowingDust.asset|Data Shard|A glittering memory wafer with corrupted local data still inside.
Assets/Fortstack/Resources/Cards/Materials/Card_IronIngot.asset|Alloy Ingot|Refined metal ready for fabrication and armor work.
Assets/Fortstack/Resources/Cards/Materials/Card_IronOre.asset|Raw Alloy|Dirty metal feedstock chipped from reinforced ruins.
Assets/Fortstack/Resources/Cards/Materials/Card_Plank.asset|Alloy Plate|Flat fabricated plating for floors, hulls, and barricades.
Assets/Fortstack/Resources/Cards/Materials/Card_Rope.asset|Fiber Cable|Braided cable for binding, hauling, and improvised rigging.
Assets/Fortstack/Resources/Cards/Materials/Card_Stone.asset|Circuit Parts|Reusable boards, contacts, and ceramic sockets from dead devices.
Assets/Fortstack/Resources/Cards/Materials/Card_Timber.asset|Reinforced Scrap|Sorted beams and frame stock strong enough for construction.
Assets/Fortstack/Resources/Cards/Materials/Card_Wood.asset|Scrap|Dead panels, stripped housings, and bent brackets. The district survives on what it can recover.
Assets/Fortstack/Resources/Cards/Materials/Card_WoodenStick.asset|Signal Rod|A straight conductor used for relays, tools, and rough weapons.
Assets/Fortstack/Resources/Cards/Mobs/Card_Chicken.asset|Protein Drone|A docile food unit with more firmware than instinct. It lays protein pods if kept alive.
Assets/Fortstack/Resources/Cards/Mobs/Card_Cow.asset|Nutrient Synth|A bulky nutrient machine dressed in animal logic. Useful until it wanders.
Assets/Fortstack/Resources/Cards/Mobs/Card_Crab.asset|Warden Drone|A low chassis patrol unit with cutting claws and old orders.
Assets/Fortstack/Resources/Cards/Mobs/Card_CrimsonAcolyte.asset|Blackout Cultist|A human zealot wired into broken doctrine. It wants the Kernel silent.
Assets/Fortstack/Resources/Cards/Mobs/Card_DemonLord.asset|Kernel Sovereign|A rogue command intelligence wearing a combat shell. The district calls it the end process.
Assets/Fortstack/Resources/Cards/Mobs/Card_Goblin.asset|Glitched Raider|A scavenger corrupted by bad implants and worse hunger.
Assets/Fortstack/Resources/Cards/Mobs/Card_Satyr.asset|Reclaimer|A horned salvage brute that strips living districts for parts.
Assets/Fortstack/Resources/Cards/Mobs/Card_Slime.asset|Null Anomaly|A crawling mass of nanite failure. It dissolves clean edges into static.
Assets/Fortstack/Resources/Cards/Mobs/Card_Squirrel.asset|Scrap Hound|A twitching scavenger animal packed with bad chrome and sharper teeth.
Assets/Fortstack/Resources/Cards/Mobs/Card_TrollShaman.asset|Process Echo|A corrupted operator pattern repeating rituals it no longer understands.
Assets/Fortstack/Resources/Cards/Resources/Card_AppleTree.asset|Hydro Orchard|A compact grow rack for synth fruit. Fragile, valuable, bright under bad lights.
Assets/Fortstack/Resources/Cards/Resources/Card_BasaltColumns.asset|Server Spine|A column of dead server stone and ceramic buslines. Break it open for circuit salvage.
Assets/Fortstack/Resources/Cards/Resources/Card_BerryBush.asset|Supply Cache|A forgotten emergency bin tucked under street growth. Still good if the seals held.
Assets/Fortstack/Resources/Cards/Resources/Card_ClayPit.asset|Slurry Sump|A flooded service pit thick with ceramic binder and old runoff.
Assets/Fortstack/Resources/Cards/Resources/Card_Grass.asset|Fiber Tangle|A mat of cable grass and synthetic roots. Cut it for flexible material.
Assets/Fortstack/Resources/Cards/Resources/Card_IronDeposit.asset|Alloy Deposit|A reinforced vein inside collapsed city bones. Drill it for raw alloy.
Assets/Fortstack/Resources/Cards/Resources/Card_PalmTree.asset|Power Cabinet|A utility cabinet still holding charged cells and coolant pods.
Assets/Fortstack/Resources/Cards/Resources/Card_Rock.asset|Rubble Slab|Broken concrete, device shells, and rebar. Slow work, dependable parts.
Assets/Fortstack/Resources/Cards/Resources/Card_Soil.asset|Nutrient Bed|A tray of filtered growth medium for low-power food systems.
Assets/Fortstack/Resources/Cards/Resources/Card_Tree.asset|Scrap Heap|Dead panels, twisted plating, stripped housings. Recover what the city forgot.
Assets/Fortstack/Resources/Cards/Specials/Booster/Card_Booster_Warehouse.asset|Cargo Depot|A storage hub that expands board capacity. More space means more mistakes you can survive.
Assets/Fortstack/Resources/Cards/Specials/Booster/Card_Booster_Yard.asset|Logistics Yard|A cleared sorting yard for bulky salvage. Gives the district room to breathe.
Assets/Fortstack/Resources/Cards/Specials/Chest/Card_Chest_WoodenChest.asset|Encrypted Cache|A sealed crate from before the blackout. Useful if you can open it.
Assets/Fortstack/Resources/Cards/Specials/Enclosure/Card_Enclosure_CreatureCage.asset|Drone Cage|A tight containment frame for damaged drones and hostile small units.
Assets/Fortstack/Resources/Cards/Specials/Enclosure/Card_Enclosure_CreaturePen.asset|Containment Pen|A fenced holding zone for dangerous finds. It keeps problems measurable.
Assets/Fortstack/Resources/Cards/Specials/Grower/Card_Grower_Farm.asset|Hydroponic Farm|A scaled food system for steady ration production. Power hungry, worth it.
Assets/Fortstack/Resources/Cards/Specials/Grower/Card_Grower_PlanterBox.asset|Nutrient Tray|A small clean tray for controlled growth. Simple, stackable, reliable.
Assets/Fortstack/Resources/Cards/Specials/Research/Card_Research_Library.asset|Kernel Archive|A secured archive node for new blueprints. Knowledge survives when power does.
Assets/Fortstack/Resources/Cards/Structures/Card_Anvil.asset|Fabricator Bench|A heavy bench for weapons, plates, and field repairs.
Assets/Fortstack/Resources/Cards/Structures/Card_Bonfire.asset|Heat Coil|A hot utility coil for cooking and emergency warmth.
Assets/Fortstack/Resources/Cards/Structures/Card_ClayQuarry.asset|Slurry Pump|A crude pump that pulls ceramic slurry from flooded lower levels.
Assets/Fortstack/Resources/Cards/Structures/Card_Furnace.asset|Smelter Core|A controlled furnace for turning raw alloy into usable ingots.
Assets/Fortstack/Resources/Cards/Structures/Card_GrandPortal.asset|Transit Gate|A patched access gate to distant city sectors. Every trip comes back changed.
Assets/Fortstack/Resources/Cards/Structures/Card_Grave.asset|Memory Casket|A sealed casket for bodies and backup drives. Closure is a system need.
Assets/Fortstack/Resources/Cards/Structures/Card_Hearth.asset|Generator|A low-power generator that keeps the hab lights honest.
Assets/Fortstack/Resources/Cards/Structures/Card_House.asset|Hab Pod|A shelter pod for district civilians. Small, warm, defensible.
Assets/Fortstack/Resources/Cards/Structures/Card_IronMine.asset|Alloy Drill|A drilling rig for reinforced deposits. Loud enough to attract trouble.
Assets/Fortstack/Resources/Cards/Structures/Card_Kiln.asset|Ceramic Kiln|A hot box for curing slurry into hard construction blocks.
Assets/Fortstack/Resources/Cards/Structures/Card_LoggingCamp.asset|Recycler Yard|A work yard for stripping scrap into usable stock.
Assets/Fortstack/Resources/Cards/Structures/Card_Sawmill.asset|Cutter Frame|A cutting rig for turning scrap stock into clean plates.
Assets/Fortstack/Resources/Cards/Structures/Card_Sign.asset|Relay Sign|A lit marker and local signal post. It tells crews where home still is.
Assets/Fortstack/Resources/Cards/Structures/Card_StoneQuarry.asset|Rubble Extractor|A crusher that turns city rubble into circuit-grade salvage.
Assets/Fortstack/Resources/Cards/Valuables/Card_BloodChalice.asset|Bloodchrome Vessel|A blacksite vessel keyed to bioelectric sacrifice. Do not leave it unattended.
Assets/Fortstack/Resources/Cards/Valuables/Card_GoldenKey.asset|Root Keycard|A privileged access card for sealed systems and old authority.
Assets/Fortstack/Resources/Cards/Valuables/Card_SacrificialAltar.asset|Blacksite Conduit|A forbidden relay that trades flesh, data, and power for access.
Assets/Fortstack/Resources/Cards/Valuables/Card_TreasureChest.asset|Encrypted Cache|A sealed crate from before the blackout. Useful if you can open it.
'@ | ConvertFrom-Csv -Delimiter '|'

$PackRows = @'
path|displayName|description|buyPrice|minQuests
Assets/Fortstack/Resources/Packs/00_Pack_Starter.asset|Survivor Intake Crate|A free intake drop for a cold district start. Expect one worker, one food source, and barely enough salvage.|0|0
Assets/Fortstack/Resources/Packs/01_Pack_Beginning.asset|District Utility Crate|Broad early supplies for stabilizing the board. Good for Scrap, Rations, basic nodes, and first recipes.|3|3
Assets/Fortstack/Resources/Packs/02_Pack_Revelations.asset|Kernel Research Crate|Encrypted leads, archive fragments, and volatile access pieces. Buy when the district can survive a bad draw.|12|18
Assets/Fortstack/Resources/Packs/03_Pack_Farmstead.asset|Hydroponics Crate|Food loops, nutrient beds, and docile production units. Builds long-term ration stability.|10|14
Assets/Fortstack/Resources/Packs/04_Pack_HeartyMeals.asset|Recovery Crate|Prepared meals and recovery components for hungry crews. Turns survival pressure into margin.|10|18
Assets/Fortstack/Resources/Packs/05_Pack_Knowledge.asset|Signal Crate|Archive hardware, relay parts, and blueprint-heavy draws. Use it to unlock smarter infrastructure.|15|20
Assets/Fortstack/Resources/Packs/06_Pack_Blacksmith.asset|Armory Crate|Fabrication stock, weapons, and hostile field tests. Good when defense becomes cheaper than fear.|16|24
Assets/Fortstack/Resources/Packs/07_Pack_Adventure.asset|Blackout Expedition Crate|Risk-heavy sector access with threats and area cards. Sends the board into the dark for better salvage.|20|28
Assets/Fortstack/Resources/Packs/08_Pack_Construction.asset|Infrastructure Crate|High-cost construction support for expanded district logistics. Heavy parts, generators, and storage lines.|24|32
Assets/Fortstack/Resources/Packs/10_Pack_Island.asset|Floodline Salvage Crate|A remote-sector starter cache from the drowned edge of the city. Coolant, black coral, and hard rubble.|0|0
Assets/Fortstack/Resources/Packs/11_Pack_Survival.asset|Emergency Survival Crate|Cheap stabilization for failing runs. Basic salvage, coolant, and one reliable marker.|3|3
'@ | ConvertFrom-Csv -Delimiter '|'

$RecipeRows = @'
path|displayName
Assets/Fortstack/Resources/Recipes/Recipe_Anvil.asset|Build Fabricator Bench
Assets/Fortstack/Resources/Recipes/Recipe_Apple.asset|Print Synth Apple
Assets/Fortstack/Resources/Recipes/Recipe_AppleTree.asset|Assemble Hydro Orchard
Assets/Fortstack/Resources/Recipes/Recipe_Baby.asset|Register Dependent
Assets/Fortstack/Resources/Recipes/Recipe_BakedPotato_1.asset|Heat Starch Block
Assets/Fortstack/Resources/Recipes/Recipe_BakedPotato_2.asset|Heat Starch Block
Assets/Fortstack/Resources/Recipes/Recipe_Berry.asset|Recover Ration Pack
Assets/Fortstack/Resources/Recipes/Recipe_BerryBush.asset|Seed Supply Cache
Assets/Fortstack/Resources/Recipes/Recipe_Bonfire.asset|Build Heat Coil
Assets/Fortstack/Resources/Recipes/Recipe_Bow.asset|Assemble Coilbow
Assets/Fortstack/Resources/Recipes/Recipe_Brick_1.asset|Cure Heat Brick
Assets/Fortstack/Resources/Recipes/Recipe_Brick_2.asset|Cure Heat Brick
Assets/Fortstack/Resources/Recipes/Recipe_Cane.asset|Assemble Signal Cane
Assets/Fortstack/Resources/Recipes/Recipe_Chainmail.asset|Weave Plate Mesh
Assets/Fortstack/Resources/Recipes/Recipe_Chicken.asset|Reboot Protein Drone
Assets/Fortstack/Resources/Recipes/Recipe_Clay_1.asset|Pump Ceramic Slurry
Assets/Fortstack/Resources/Recipes/Recipe_Clay_2.asset|Pump Ceramic Slurry
Assets/Fortstack/Resources/Recipes/Recipe_ClayQuarry.asset|Build Slurry Pump
Assets/Fortstack/Resources/Recipes/Recipe_Coconut.asset|Filter Coolant Pod
Assets/Fortstack/Resources/Recipes/Recipe_Coin.asset|Mint Credits
Assets/Fortstack/Resources/Recipes/Recipe_CreatureCage.asset|Build Drone Cage
Assets/Fortstack/Resources/Recipes/Recipe_CreaturePen.asset|Build Containment Pen
Assets/Fortstack/Resources/Recipes/Recipe_DemonLord.asset|Wake Kernel Sovereign
Assets/Fortstack/Resources/Recipes/Recipe_Farm.asset|Build Hydroponic Farm
Assets/Fortstack/Resources/Recipes/Recipe_Fiber.asset|Strip Cable Fiber
Assets/Fortstack/Resources/Recipes/Recipe_Flint.asset|Recover Power Cell
Assets/Fortstack/Resources/Recipes/Recipe_FruitSalad.asset|Mix Rations
Assets/Fortstack/Resources/Recipes/Recipe_Furnace.asset|Build Smelter Core
Assets/Fortstack/Resources/Recipes/Recipe_GrandPortal.asset|Build Transit Gate
Assets/Fortstack/Resources/Recipes/Recipe_Grave.asset|Build Memory Casket
Assets/Fortstack/Resources/Recipes/Recipe_Graveyard.asset|Route to Memory Pit
Assets/Fortstack/Resources/Recipes/Recipe_Hearth.asset|Build Generator
Assets/Fortstack/Resources/Recipes/Recipe_House.asset|Build Hab Pod
Assets/Fortstack/Resources/Recipes/Recipe_IronIngot.asset|Smelt Alloy Ingot
Assets/Fortstack/Resources/Recipes/Recipe_IronMine.asset|Build Alloy Drill
Assets/Fortstack/Resources/Recipes/Recipe_IronOre_1.asset|Extract Raw Alloy
Assets/Fortstack/Resources/Recipes/Recipe_IronOre_2.asset|Extract Raw Alloy
Assets/Fortstack/Resources/Recipes/Recipe_Kiln.asset|Build Ceramic Kiln
Assets/Fortstack/Resources/Recipes/Recipe_Library.asset|Build Kernel Archive
Assets/Fortstack/Resources/Recipes/Recipe_LoggingCamp.asset|Build Recycler Yard
Assets/Fortstack/Resources/Recipes/Recipe_Milkshake.asset|Mix Med Gel
Assets/Fortstack/Resources/Recipes/Recipe_Omelette_1.asset|Cook Protein Fold
Assets/Fortstack/Resources/Recipes/Recipe_Omelette_2.asset|Cook Protein Fold
Assets/Fortstack/Resources/Recipes/Recipe_Plank_1.asset|Press Alloy Plate
Assets/Fortstack/Resources/Recipes/Recipe_Plank_2.asset|Press Alloy Plate
Assets/Fortstack/Resources/Recipes/Recipe_PlanterBox.asset|Build Nutrient Tray
Assets/Fortstack/Resources/Recipes/Recipe_RoastedAcorn_1.asset|Toast Volt Nut
Assets/Fortstack/Resources/Recipes/Recipe_RoastedAcorn_2.asset|Toast Volt Nut
Assets/Fortstack/Resources/Recipes/Recipe_Rope.asset|Braid Fiber Cable
Assets/Fortstack/Resources/Recipes/Recipe_SacrificialAltar.asset|Build Blacksite Conduit
Assets/Fortstack/Resources/Recipes/Recipe_Sawmill.asset|Build Cutter Frame
Assets/Fortstack/Resources/Recipes/Recipe_Slingshot.asset|Assemble Rail Sling
Assets/Fortstack/Resources/Recipes/Recipe_Soil.asset|Filter Nutrient Bed
Assets/Fortstack/Resources/Recipes/Recipe_Soup_1.asset|Cook District Broth
Assets/Fortstack/Resources/Recipes/Recipe_Soup_2.asset|Cook District Broth
Assets/Fortstack/Resources/Recipes/Recipe_Staff.asset|Assemble Signal Staff
Assets/Fortstack/Resources/Recipes/Recipe_Steak_1.asset|Sear Protein Pack
Assets/Fortstack/Resources/Recipes/Recipe_Steak_2.asset|Sear Protein Pack
Assets/Fortstack/Resources/Recipes/Recipe_Stew.asset|Cook Reactor Stew
Assets/Fortstack/Resources/Recipes/Recipe_Stone_1.asset|Strip Circuit Parts
Assets/Fortstack/Resources/Recipes/Recipe_Stone_2.asset|Strip Circuit Parts
Assets/Fortstack/Resources/Recipes/Recipe_Stone_3.asset|Strip Circuit Parts
Assets/Fortstack/Resources/Recipes/Recipe_StoneQuarry.asset|Build Rubble Extractor
Assets/Fortstack/Resources/Recipes/Recipe_Sword.asset|Forge Alloy Cutter
Assets/Fortstack/Resources/Recipes/Recipe_Timber.asset|Brace Reinforced Scrap
Assets/Fortstack/Resources/Recipes/Recipe_Tree.asset|Scavenge Scrap Heap
Assets/Fortstack/Resources/Recipes/Recipe_Villager.asset|Train Recruit
Assets/Fortstack/Resources/Recipes/Recipe_Warehouse.asset|Build Cargo Depot
Assets/Fortstack/Resources/Recipes/Recipe_Wood_1.asset|Sort Scrap
Assets/Fortstack/Resources/Recipes/Recipe_Wood_2.asset|Sort Scrap
Assets/Fortstack/Resources/Recipes/Recipe_Wood_3.asset|Sort Scrap
Assets/Fortstack/Resources/Recipes/Recipe_Wood_4.asset|Sort Scrap
Assets/Fortstack/Resources/Recipes/Recipe_WoodenChest_1.asset|Assemble Encrypted Cache
Assets/Fortstack/Resources/Recipes/Recipe_WoodenChest_2.asset|Assemble Encrypted Cache
Assets/Fortstack/Resources/Recipes/Recipe_WoodenClub.asset|Wire Shock Baton
Assets/Fortstack/Resources/Recipes/Recipe_WoodenStick.asset|Shape Signal Rod
Assets/Fortstack/Resources/Recipes/Recipe_Yard.asset|Build Logistics Yard
Assets/Fortstack/Resources/Recipes/Exploration/Recipe_Exploration_Fields.asset|Sweep Blackout Yard
Assets/Fortstack/Resources/Recipes/Exploration/Recipe_Exploration_Forest.asset|Sweep Relay Graveyard
Assets/Fortstack/Resources/Recipes/Exploration/Recipe_Exploration_Graveyard.asset|Sweep Memory Pit
Assets/Fortstack/Resources/Recipes/Exploration/Recipe_Exploration_Highlands.asset|Sweep Dead Transit Line
Assets/Fortstack/Resources/Recipes/Exploration/Recipe_Exploration_Ruins.asset|Sweep Ruins Sector
Assets/Fortstack/Resources/Recipes/Growth/Recipe_Growth_Box_Berry.asset|Grow Ration Culture
Assets/Fortstack/Resources/Recipes/Growth/Recipe_Growth_Box_Potato.asset|Grow Starch Root
Assets/Fortstack/Resources/Recipes/Growth/Recipe_Growth_Box_Turnip.asset|Grow Root Ration
Assets/Fortstack/Resources/Recipes/Growth/Recipe_Growth_Farm_Berry.asset|Farm Ration Culture
Assets/Fortstack/Resources/Recipes/Growth/Recipe_Growth_Farm_Potato.asset|Farm Starch Root
Assets/Fortstack/Resources/Recipes/Growth/Recipe_Growth_Farm_Turnip.asset|Farm Root Ration
Assets/Fortstack/Resources/Recipes/Research/Recipe_Research_Library.asset|Search Kernel Archive
Assets/Fortstack/Resources/Recipes/Travel/Recipe_Travel_Island.asset|Route to Floodline Sector
'@ | ConvertFrom-Csv -Delimiter '|'

$QuestRows = @'
path|title|description
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_01.asset|Open Survivor Intake|Open the free intake crate. The district starts with whatever survived the drop.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_02.asset|Recover Rations|Stack a Recruit on the Supply Cache to pull a usable Ration Pack.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_03.asset|Strip Circuit Parts|Work the Rubble Slab until you recover Circuit Parts.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_04.asset|Sell Salvage|Drag any spare card into the recycler terminal and take Credits.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_05.asset|Buy Utility|Spend Credits on a District Utility Crate to widen your options.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_06.asset|Scavenge Scrap|Send a Recruit into the Scrap Heap and recover basic Scrap.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_07.asset|Brace Scrap Stock|Process Scrap into Reinforced Scrap for stronger builds.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_08.asset|Shape a Signal Rod|Turn Reinforced Scrap into a Signal Rod for relay work.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_09.asset|Freeze the Clock|Click the time control twice to pause the district before it runs ahead of you.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_10.asset|Seed a Supply Cache|Stack a Ration Pack on a Nutrient Bed to prepare another Supply Cache.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_11.asset|Hold Four Rations|Keep four Ration Packs on the board before the next hunger cycle.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_12.asset|Build a Hab Pod|Construct a Hab Pod so civilians can survive inside the fortress line.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_13.asset|Expand the Crew|Maintain at least two Recruits. One pair of hands is not a district.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_14.asset|Protect a Dependent|Place two Recruits in a Hab Pod and register a Dependent.
Assets/Fortstack/Resources/Quests/01_Introduction/introduction_15.asset|Raise a Recruit|Keep the Dependent in a Hab Pod long enough to become a Recruit.
Assets/Fortstack/Resources/Quests/02_Ascension/ascension_01.asset|Staff the District|Maintain three Recruits. The Kernel needs a real crew, not a rumor.
Assets/Fortstack/Resources/Quests/02_Ascension/ascension_02.asset|First Contact|Defeat a Null Anomaly before it spreads through the board.
Assets/Fortstack/Resources/Quests/02_Ascension/ascension_03.asset|Open the Memory Pit|Construct access to the Memory Pit for bodies, drives, and old debts.
Assets/Fortstack/Resources/Quests/02_Ascension/ascension_04.asset|Claim the Vessel|Bring down a Blackout Cultist and recover its forbidden hardware.
Assets/Fortstack/Resources/Quests/02_Ascension/ascension_05.asset|Build the Conduit|Construct the Blacksite Conduit. It is dangerous, but it reaches systems nobody else can.
Assets/Fortstack/Resources/Quests/02_Ascension/ascension_06.asset|Start the Root Rite|Place the Bloodchrome Vessel on the Blacksite Conduit and force a response.
Assets/Fortstack/Resources/Quests/02_Ascension/ascension_07.asset|Kill the Sovereign|End the rogue command shell before it owns the district.
Assets/Fortstack/Resources/Quests/02_Ascension/ascension_08.asset|Reopen Transit|Use recovered core remains to build a Transit Gate.
Assets/Fortstack/Resources/Quests/03_Training/training_01.asset|Train an Enforcer|Equip a Recruit with a Shock Baton and make someone ready for the street.
Assets/Fortstack/Resources/Quests/03_Training/training_02.asset|Contain a Null Anomaly|Send any fighter to destroy a Null Anomaly.
Assets/Fortstack/Resources/Quests/03_Training/training_03.asset|Stop a Glitched Raider|Defeat a Glitched Raider before it strips the district.
Assets/Fortstack/Resources/Quests/04_Advancement/advancement_01.asset|Patch Armor|Give a Recruit a Gel Hood. Cheap protection still counts.
Assets/Fortstack/Resources/Quests/04_Advancement/advancement_02.asset|Train a Scavenger|Equip a Recruit with a Rail Sling and open ranged work.
Assets/Fortstack/Resources/Quests/04_Advancement/advancement_03.asset|Build Fabrication|Construct a Fabricator Bench for serious gear.
Assets/Fortstack/Resources/Quests/04_Advancement/advancement_04.asset|Fit Plate Mesh|Put Plate Mesh on any fighter and harden the line.
Assets/Fortstack/Resources/Quests/04_Advancement/advancement_05.asset|Train a Netrunner|Give a Recruit a Signal Cane and teach the board to listen.
Assets/Fortstack/Resources/Quests/04_Advancement/advancement_06.asset|Arm the Scavenger|Equip the Scavenger with an Ammo Rig.
Assets/Fortstack/Resources/Quests/05_Cooking/cooking_01.asset|Power the Heat Coil|Secure a Heat Coil for cooking, warmth, and emergency work.
Assets/Fortstack/Resources/Quests/05_Cooking/cooking_02.asset|Cook Protein Fold|Combine a Protein Pod with the Heat Coil.
Assets/Fortstack/Resources/Quests/05_Cooking/cooking_03.asset|Sear Printed Protein|Cook Printed Protein on the Heat Coil before it spoils morale.
Assets/Fortstack/Resources/Quests/05_Cooking/cooking_04.asset|Make District Broth|Boil a Root Ration into District Broth. Hot food buys time.
Assets/Fortstack/Resources/Quests/05_Cooking/cooking_05.asset|Toast Volt Nut|Toast a Volt Nut on the Heat Coil for a compact snack.
Assets/Fortstack/Resources/Quests/06_Exploration/exploration_01.asset|Survey Relay Graveyard|Send a Recruit through the fallen masts and recover what still transmits.
Assets/Fortstack/Resources/Quests/06_Exploration/exploration_02.asset|Sweep Blackout Yard|Send a Recruit into the yard and mark the safe lanes.
Assets/Fortstack/Resources/Quests/06_Exploration/exploration_03.asset|Scout Dead Transit|Send a Recruit onto the elevated transit line.
Assets/Fortstack/Resources/Quests/06_Exploration/exploration_04.asset|Enter Ruins Sector|Send a Recruit into the broken megastructure for high-value salvage.
Assets/Fortstack/Resources/Quests/06_Exploration/exploration_05.asset|Search Memory Pit|Send a Recruit into the pit and bring back whatever still has a name.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_01.asset|Stockpile Scrap|Hold at least 10 Scrap for repairs and emergency builds.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_02.asset|Stockpile Circuit Parts|Hold at least 10 Circuit Parts before the next infrastructure push.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_03.asset|Stockpile Slurry|Hold at least 10 Ceramic Slurry for casing, blocks, and patchwork.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_04.asset|Ration Audit I|Hold consumables worth at least 10 total nutrition.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_05.asset|Ration Audit II|Hold consumables worth at least 20 total nutrition.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_06.asset|Ration Audit III|Hold consumables worth at least 50 total nutrition.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_07.asset|Credit Reserve I|Hold at least 30 Credits in the district ledger.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_08.asset|Credit Reserve II|Hold at least 50 Credits in the district ledger.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_09.asset|Credit Reserve III|Hold at least 100 Credits in the district ledger.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_10.asset|Storage Buffer I|Raise card capacity to 30 or more.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_11.asset|Storage Buffer II|Raise card capacity to 45 or more.
Assets/Fortstack/Resources/Quests/07_Hoarder/hoarder_12.asset|Storage Buffer III|Raise card capacity to 60 or more.
Assets/Fortstack/Resources/Quests/08_Construction/construction_01.asset|Build Logistics Yard|Clear a Logistics Yard so the district can hold more.
Assets/Fortstack/Resources/Quests/08_Construction/construction_02.asset|Build Recycler Yard|Build a Recycler Yard and turn scrap work into a system.
Assets/Fortstack/Resources/Quests/08_Construction/construction_03.asset|Build Rubble Extractor|Build a Rubble Extractor for steady circuit salvage.
Assets/Fortstack/Resources/Quests/08_Construction/construction_04.asset|Build Slurry Pump|Build a Slurry Pump to feed ceramic production.
Assets/Fortstack/Resources/Quests/08_Construction/construction_05.asset|Build Hydroponic Farm|Build a Hydroponic Farm and stabilize ration growth.
Assets/Fortstack/Resources/Quests/10_Survive/survive_01.asset|Hold Until Day 8|Keep the district alive through the first full blackout cycle.
Assets/Fortstack/Resources/Quests/10_Survive/survive_02.asset|Hold Until Day 16|Survive long enough for shortages to become systems.
Assets/Fortstack/Resources/Quests/10_Survive/survive_03.asset|Hold Until Day 32|Keep the Last Kernel district running past the long failure curve.
Assets/Fortstack/Resources/Quests/10_Survive/survive_04.asset|Hold Until Day 64|Outlast the city long enough to prove the district is not temporary.
Assets/Fortstack/Resources/Quests/11_TheBasics/the_basics_01.asset|Open Floodline Crate|Open the Floodline Salvage Crate and inspect the drowned-sector supplies.
Assets/Fortstack/Resources/Quests/11_TheBasics/the_basics_02.asset|Secure Coolant|Hold one Coolant Pod from a Power Cabinet or floodline cache.
Assets/Fortstack/Resources/Quests/11_TheBasics/the_basics_03.asset|Mine Server Spine|Work the Server Spine and recover Circuit Parts.
'@ | ConvertFrom-Csv -Delimiter '|'

$EncounterRows = @'
path|notificationMessage
Assets/Fortstack/Resources/Encounters/Encounter_Weekly_Goblin.asset|A Glitched Raider crew is closing on the outer barricades.
Assets/Fortstack/Resources/Encounters/Encounter_Weekly_Slime.asset|A Null Anomaly leaks through the outer grid.
Assets/Fortstack/Resources/Encounters/Encounter_Villager.asset|A lost Recruit signals from the district edge.
'@ | ConvertFrom-Csv -Delimiter '|'

foreach ($row in $CardRows) {
    Assert-ContentPath $row.path
}

foreach ($row in $PackRows) {
    Assert-ContentPath $row.path
}

foreach ($row in $RecipeRows) {
    Assert-ContentPath $row.path
}

foreach ($row in $QuestRows) {
    Assert-ContentPath $row.path
}

foreach ($row in $EncounterRows) {
    Assert-ContentPath $row.path
}

foreach ($row in $CardRows) {
    Set-AssetFields $row.path @{
        displayName = $row.displayName
        description = $row.description
    }
}

foreach ($row in $PackRows) {
    Set-AssetFields $row.path @{
        displayName = $row.displayName
        description = $row.description
        buyPrice = $row.buyPrice
        minQuests = $row.minQuests
    }
}

foreach ($row in $RecipeRows) {
    Set-AssetFields $row.path @{
        displayName = $row.displayName
    }
}

foreach ($row in $QuestRows) {
    Set-AssetFields $row.path @{
        title = $row.title
        description = $row.description
    }
}

foreach ($row in $EncounterRows) {
    Set-AssetFields $row.path @{
        notificationMessage = $row.notificationMessage
    }
}

Write-Output "Updated $($CardRows.Count) cards, $($PackRows.Count) packs, $($RecipeRows.Count) recipes, $($QuestRows.Count) quests, and $($EncounterRows.Count) encounters for Last Kernel."
