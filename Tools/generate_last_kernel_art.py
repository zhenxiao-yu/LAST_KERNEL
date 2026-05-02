"""
LAST KERNEL Card + Pack Art Generator
Uses dall-e-3. Transparent background is requested via prompt
(dall-e-3 does not have a native transparent-output API parameter).

Install:
    pip install openai

Run:
    Windows PowerShell:
        $env:OPENAI_API_KEY="sk-..."
        python generate_last_kernel_art.py

    macOS / Linux:
        export OPENAI_API_KEY="sk-..."
        python generate_last_kernel_art.py
"""

import os
import time
import urllib.request
from typing import Dict, List, Tuple

import openai


# ============================================================
# CONFIG
# ============================================================

API_KEY = os.getenv("OPENAI_API_KEY", "")

CARD_ART_DIR = "CardArt"
PACK_ART_DIR = "PackArt"

MODEL         = "dall-e-3"   # Universally available; use gpt-image-1 if your project has access
SIZE          = "1024x1024"
QUALITY       = "standard"   # standard (~$0.04) or hd (~$0.08)
DELAY_SECONDS = 13           # Stay under 5 images/min rate limit


# ============================================================
# STYLE
#
# Target: 64x64 pixel art card sprites WITH category backgrounds.
# Each card art = subject sprite + atmospheric background in one image.
# Background color is defined by category, pulled from theme.uss palette.
# Style reference: bold chunky GBA/SNES RPG card sprites.
#
# LAST KERNEL palette (from theme.uss):
#   Dark bg:   #080A12   Panel:     #161E30
#   Cyan:      #00DCFF   Cyan dim:  #007A8C
#   Magenta:   #A03291   Amber:     #FFA028
#   Red:       #CC2E2E   Green:     #3FAF6F
#   Disabled:  #3C4858   Border:    #008CA5
# ============================================================

MASTER_STYLE = (
    # ── Hard bans — stated FIRST so the model never ignores them ─────────────
    # Previous outputs showed: color swatch panels, hex labels, fake UI chrome.
    # All caused by hex codes + "4-6 colors" reading as "make a design doc".
    "NO text of any kind, NO letters, NO numbers, NO words anywhere in the image, "
    "NO color swatch panel, NO color palette grid, NO reference sheet layout, "
    "NO sprite sheet, NO design document format, NO side panels, NO sidebars, "
    "NO fake UI, NO health bars, NO stat boxes, NO HUD chrome, NO inventory panels, "
    "NO card frame baked into the art, NO decorative border, NO corner ornaments, "
    "NO drop shadow, NO glow halo behind subject, NO background rectangle, "

    # ── Background ────────────────────────────────────────────────────────────
    # dall-e-3 cannot output true alpha; use white so edges are easy to key.
    "pure solid white background, subject floating on flat white and nothing else, "
    "no scenery, no floor, no wall, no atmosphere behind the subject, "

    # ── Pixel art style ───────────────────────────────────────────────────────
    "retro pixel art style, chunky blocky pixels clearly visible, "
    "classic 16-bit RPG card illustration, "
    "style similar to GBA Fire Emblem or Slay the Spire card art, "
    "bold dark outline around the entire subject, "
    "flat cel-shading with dithered pixel shading, "
    "hard pixel edges, no smooth gradients, no anti-aliasing, "

    # ── Palette — names only, NO hex codes (hex codes trigger palette panels) ─
    "cyberpunk color palette: dark navy outlines, cyan accent highlights, "
    "muted magenta secondary color, warm amber for warm highlights, "
    "off-white for brightest lit pixels, "

    # ── Composition ───────────────────────────────────────────────────────────
    "subject centered in frame, fills roughly two-thirds of the canvas height, "
    "equal white margin on all sides, "
    "strong readable silhouette at small display sizes, "
    "one memorable iconic design detail that makes this card instantly recognizable, "

    # ── Quality ───────────────────────────────────────────────────────────────
    "every pixel intentional, no stray isolated pixel specks, "
    "professional polished pixel art quality, "
    "dark cyberpunk post-apocalyptic bunker survival world aesthetic"
)

# Per-category prefix + background.
# Prefix goes FIRST in the prompt to lock pose and composition.
# Background description defines the atmospheric color fill behind the subject,
# derived from the LAST KERNEL theme.uss palette per category.
CATEGORY_PREFIX: Dict[str, str] = {

    "Character": (
        "pixel art trading card game illustration, white background, "
        "single character, full body, facing slightly right, "
        "neutral combat-ready stance, arms slightly away from body, "
        "head and feet both fully in frame, "
        "slightly large head for sprite readability, "
    ),
    "Mob": (
        "pixel art trading card game illustration, white background, "
        "single creature, full body, facing front, "
        "threatening wide stance, limbs spread, aggressive posture, "
        "exaggerated menacing proportions, entire body in frame, "
    ),
    "Material": (
        "pixel art trading card game illustration, white background, "
        "single raw material object floating centered, "
        "slight isometric tilt to show depth, "
        "bold chunky pixel shapes, no fine detail, "
    ),
    "Consumable": (
        "pixel art trading card game illustration, white background, "
        "single food or container object floating centered, "
        "slight isometric tilt, bold readable pixel shapes, "
    ),
    "Equipment": (
        "pixel art trading card game illustration, white background, "
        "single weapon or armor piece floating centered, "
        "45-degree diagonal angle with tip toward upper-right, "
        "bold chunky pixel shapes, "
    ),
    "Structure": (
        "pixel art trading card game illustration, white background, "
        "single building or machine, compact isometric 3/4 view, "
        "front face and rooftop both visible, centered with equal margins, "
        "chunky readable architecture, "
    ),
    "Resource": (
        "pixel art trading card game illustration, white background, "
        "single natural object or plant floating centered, "
        "front-facing or slight 3/4 angle, bold chunky silhouette, "
    ),
    "Area": (
        "pixel art trading card game illustration, white background, "
        "compact isometric scene with two or three landmark silhouettes, "
        "bold readable shapes, minimal detail, "
    ),
    "Currency": (
        "pixel art trading card game illustration, white background, "
        "single coin or credit chip floating centered, "
        "slight isometric tilt, glinting highlight on top face, "
    ),
    "Valuable": (
        "pixel art trading card game illustration, white background, "
        "single rare artifact floating centered, slight isometric tilt, "
        "striking color contrast to imply rarity, "
    ),
    "Recipe": (
        "pixel art trading card game illustration, white background, "
        "single rolled or folded schematic scroll floating centered, "
        "curled edges clearly visible, front-facing, "
    ),
    "Other": (
        "pixel art trading card game illustration, white background, "
        "single object floating centered, slight isometric tilt, "
        "bold chunky shapes, "
    ),
    "Pack": (
        "pixel art trading card game illustration, white background, "
        "single sealed supply pack or crate floating centered, "
        "slight isometric tilt, bold chunky silhouette, "
    ),
}


# ============================================================
# CARD DATA  —  (filename, category, subject description)
# ============================================================

CARDS: List[Tuple[str, str, str]] = [
    # ── Characters ───────────────────────────────────────────
    ("Villager",       "Character", "a colonist survivor in patched bunker clothes, no helmet"),
    ("Warrior",        "Character", "an armored enforcer holding a salvaged blade, heavy shoulder plates"),
    ("Mage",           "Character", "a netrunner with a glowing visor and data-cable arms"),
    ("Ranger",         "Character", "a scout in lightweight armor holding a composite bow"),
    ("Baby",           "Character", "a small infant wrapped in thermal foil, eyes wide open"),

    # ── Mobs ─────────────────────────────────────────────────
    ("Slime",          "Mob", "a blobby acidic creature with glowing core, dripping"),
    ("Goblin",         "Mob", "a wiry scavenger humanoid with large ears and clawed hands"),
    ("Satyr",          "Mob", "a goat-legged mutant with cracked bio-mechanical hind legs"),
    ("TrollShaman",    "Mob", "a massive hunched troll holding a totem staff with circuit glyphs"),
    ("CrimsonAcolyte", "Mob", "a robed cultist with glowing red sigils on the chest"),
    ("DemonLord",      "Mob", "a towering horned entity with crackling dark energy in both fists"),
    ("Squirrel",       "Mob", "a feral mutant squirrel with enlarged claws"),
    ("Chicken",        "Mob", "a bio-bred mutant chicken, slightly oversized, beady eyes"),
    ("Cow",            "Mob", "a stocky bio-bred cow with metal ear tag"),
    ("Corpse",         "Mob", "a fallen colonist body lying flat, hazard X marks nearby"),

    # ── Materials ─────────────────────────────────────────────
    ("Wood",           "Material", "a rough-cut timber log with visible grain"),
    ("Stone",          "Material", "a jagged rock chunk with flat facets"),
    ("Clay",           "Material", "a block of raw reddish clay, fingerprint mark on surface"),
    ("IronOre",        "Material", "a rock with bright metallic iron veins running through it"),
    ("IronIngot",      "Material", "a rectangular smelted iron bar with stamped surface"),
    ("Plank",          "Material", "two stacked flat wooden boards, nailed at corner"),
    ("Brick",          "Material", "a single fired clay brick, worn edges"),
    ("Timber",         "Material", "a long structural beam with rough-hewn ends"),
    ("Flint",          "Material", "a sharp angular flint shard with knapped edges"),
    ("Fiber",          "Material", "a bundle of twisted plant or synthetic fibers tied with string"),
    ("Rope",           "Material", "a coiled rope loop with knotted end"),
    ("Soil",           "Material", "a rounded mound of dark nutrient soil"),

    # ── Consumables ───────────────────────────────────────────
    ("Apple",          "Consumable", "a round apple with leaf, slightly oversized stem"),
    ("Berry",          "Consumable", "a small cluster of three round berries on a twig"),
    ("Potato",         "Consumable", "a lumpy oval potato with a few sprout nubs"),
    ("BakedPotato",    "Consumable", "a split baked potato wrapped in foil, steam rising"),
    ("Egg",            "Consumable", "a single egg in a small wire containment tray"),
    ("RawMeat",        "Consumable", "a raw protein slab on a flat surface, vacuum-sealed edge"),
    ("Steak",          "Consumable", "a thick cooked steak on a metal mess tray"),
    ("Milk",           "Consumable", "a sealed cylindrical flask with drop icon on label"),
    ("Milkshake",      "Consumable", "a sealed canister with straw poking from sealed top"),
    ("Soup",           "Consumable", "a sealed thermal pouch with steam vent nozzle"),
    ("FruitSalad",     "Consumable", "a sealed ration cup with fruit pieces visible through clear lid"),
    ("Omelette",       "Consumable", "a folded omelette on a flat metal camp tray"),
    ("Coconut",        "Consumable", "a coconut cracked in half, liquid visible inside"),
    ("Acorn",          "Consumable", "a single acorn with cap, compact and round"),
    ("RoastedAcorn",   "Consumable", "three small roasted acorns in a small tin bowl"),
    ("Turnip",         "Consumable", "a round turnip with leafy top, roots at bottom"),

    # ── Equipment ─────────────────────────────────────────────
    ("Sword",          "Equipment", "a straight single-edged salvaged blade with wrapped grip"),
    ("Bow",            "Equipment", "a recurve composite bow with tech-wrapped limbs"),
    ("WoodenClub",     "Equipment", "a thick wooden club with a heavy rounded end"),
    ("WoodenStick",    "Equipment", "a sharpened straight wooden staff, pointed tip"),
    ("Slingshot",      "Equipment", "a Y-shaped slingshot with elastic band"),
    ("Staff",          "Equipment", "a tall staff with a glowing circuit-node at the top"),
    ("Quiver",         "Equipment", "a cylindrical arrow quiver with three arrow flights visible"),
    ("Tunic",          "Equipment", "a sleeveless vest with patch repairs and buckled straps"),
    ("LeatherArmor",   "Equipment", "a chest plate of layered leather with shoulder rivets"),
    ("Chainmail",      "Equipment", "a folded mesh shirt of interlocked metal rings"),
    ("SlimeHat",       "Equipment", "a rounded helmet dripping with green slime residue"),
    ("VitalityAmulet", "Equipment", "a round amulet with a circuit pattern etched into the face"),

    # ── Structures ────────────────────────────────────────────
    ("Sawmill",        "Structure", "a compact sawmill with a large circular blade and log feed"),
    ("Furnace",        "Structure", "a squat box furnace with glowing orange front vent"),
    ("Kiln",           "Structure", "a domed kiln with chimney vents and visible heat shimmer"),
    ("Farm",           "Structure", "a hydroponic grow tray unit with rows of seedlings under a UV strip light"),
    ("LoggingCamp",    "Structure", "a felled-log processing station with chainsaw arm"),
    ("StoneQuarry",    "Structure", "a drill rig on a rock shelf with piles of cut stone"),
    ("ClayPit",        "Structure", "a shallow excavation pit with clay walls and shovel"),
    ("ClayQuarry",     "Structure", "a deeper mechanized clay dig with conveyor arm"),
    ("IronMine",       "Structure", "a mine shaft entrance with timber supports and cart rail"),
    ("IronDeposit",    "Structure", "a rock face with exposed shiny iron veins"),
    ("CreaturePen",    "Structure", "a reinforced pen with metal fence posts and gate latch"),
    ("CreatureCage",   "Structure", "a portable metal cage with heavy padlock"),
    ("Warehouse",      "Structure", "a flat-roofed storage depot with large sliding door"),
    ("Library",        "Structure", "a data terminal kiosk with stacked drive shelves and blinking lights"),
    ("House",          "Structure", "a small prefab shelter module with bolted panel walls and a hatch door"),
    ("Hearth",         "Structure", "a compact heating unit with glowing coil element and fan vents"),
    ("Bonfire",        "Structure", "a metal burn barrel with flames licking the top rim"),
    ("Anvil",          "Structure", "a heavy flat-topped iron anvil on a low block base"),
    ("Yard",           "Structure", "a fenced compound outline with a gate and perimeter posts"),

    # ── Resources / Areas ─────────────────────────────────────
    ("Forest",         "Area",     "a cluster of three twisted mutant trees with glowing roots"),
    ("Grass",          "Resource", "a patch of bioluminescent moss on cracked concrete"),
    ("Highlands",      "Area",     "a rocky plateau silhouette with jagged peaks"),
    ("Ruins",          "Area",     "crumbled concrete pillars with rebar exposed and rubble pile"),
    ("Fields",         "Area",     "a grid of cracked dry furrows, abandoned cropland"),
    ("Graveyard",      "Area",     "three grave mounds with simple cross markers and a hazard flag"),
    ("Coral",          "Resource", "a branching mutant coral formation"),
    ("BasaltColumns",  "Resource", "a tight cluster of hexagonal basalt pillars"),
    ("AppleTree",      "Resource", "a slender apple tree column in a grow tube with two apples"),
    ("BerryBush",      "Resource", "a compact shrub with small round berries on branch tips"),
    ("Tree",           "Resource", "a single bioluminescent tree with glowing vein pattern on trunk"),
    ("PalmTree",       "Resource", "a tall thin palm tree with splayed fronds at the top"),

    # ── Valuables ─────────────────────────────────────────────
    ("GoldenKey",      "Valuable", "an ornate key with circuit-board etchings on the bow"),
    ("TreasureChest",  "Valuable", "a military-style supply crate with heavy clasps and a glowing seal"),
    ("WoodenChest",    "Valuable", "a small wooden box with a metal latch and hinged lid"),
    ("GlowingDust",    "Valuable", "a sealed vial of glowing bioluminescent particle dust"),
    ("BloodChalice",   "Valuable", "a goblet with dark liquid, glowing faintly at the rim"),
    ("AbyssalCore",    "Valuable", "a jagged crystal shard pulsing with dark energy from within"),
    ("SacrificialAltar","Valuable","a flat stone block altar with power conduit cables attached"),
    ("GrandPortal",    "Valuable", "a tall arched gateway frame crackling with energy between the posts"),

    # ── Currency / Other ──────────────────────────────────────
    ("Coin",           "Currency", "a round coin with a circuit-board hexagon stamped on the face"),
    ("Sign",           "Other",    "a rectangular rusted metal sign on a post, blank face"),
    ("Grave",          "Other",    "a single grave mound with a crude flat marker stone"),
    ("Recipe",         "Recipe",   "a curled schematic scroll with gear and arrow diagrams"),
    ("Rock",           "Material", "a small rounded loose rock"),
]

PACKS: List[Tuple[str, str, str]] = [
    ("Starter",       "Pack", "a basic sealed survival kit pouch with a star emblem"),
    ("Knowledge",     "Pack", "a data chip slotted into an open book spine"),
    ("Farmstead",     "Pack", "a seed packet with a sprout icon on the front"),
    ("HeartyMeals",   "Pack", "a ration pack with a flame and bowl icon"),
    ("Blacksmith",    "Pack", "a sealed tool pack with an anvil emblem"),
    ("Revelations",   "Pack", "a glowing sealed envelope with broken wax seal"),
    ("Beginning",     "Pack", "a clean new supply crate with sunrise chevron emblem"),
    ("Adventure",     "Pack", "a scout kit roll with compass icon"),
    ("Survival",      "Pack", "an emergency supply pack with hazard stripe border"),
    ("Island",        "Pack", "a waterproof pack with wave and palm icon"),
    ("Construction",  "Pack", "a heavy materials pack with I-beam and hammer icon"),
]


# ============================================================
# GENERATION
# ============================================================

def ensure_dirs() -> None:
    os.makedirs(CARD_ART_DIR, exist_ok=True)
    os.makedirs(PACK_ART_DIR, exist_ok=True)


def build_prompt(category: str, subject: str) -> str:
    prefix = CATEGORY_PREFIX.get(category, CATEGORY_PREFIX["Other"])
    return (
        f"{prefix}"
        f"subject: {subject}, "
        f"post-apocalyptic survival bunker world, worn industrial technology, "
        f"not fantasy not medieval not painterly, "
        f"{MASTER_STYLE}"
    )


def generate_and_save(client: openai.OpenAI, prompt: str, filepath: str) -> None:
    result = client.images.generate(
        model=MODEL,
        prompt=prompt,
        size=SIZE,
        quality=QUALITY,
        n=1,
    )
    url = result.data[0].url
    urllib.request.urlretrieve(url, filepath)


def process_batch(
    client: openai.OpenAI,
    items: List[Tuple[str, str, str]],
    output_dir: str,
    start_index: int,
    total: int,
) -> Tuple[int, int, int]:
    generated = skipped = failed = 0

    for i, (name, category, subject) in enumerate(items, start=start_index):
        filepath = os.path.join(output_dir, f"{name}.png")

        if os.path.exists(filepath):
            print(f"  skip [{i}/{total}] {name}")
            skipped += 1
            continue

        print(f"  gen  [{i}/{total}] {name}...")
        try:
            generate_and_save(client, build_prompt(category, subject), filepath)
            print(f"       saved → {filepath}")
            generated += 1
        except Exception as e:
            print(f"       FAILED: {e}")
            failed += 1

        time.sleep(DELAY_SECONDS)

    return generated, skipped, failed


# ============================================================
# MAIN
# ============================================================

def main() -> None:
    if not API_KEY:
        print("No API key found.")
        print("Set OPENAI_API_KEY in your terminal, or paste it into API_KEY at the top of this file.")
        return

    client = openai.OpenAI(api_key=API_KEY)
    ensure_dirs()

    total = len(CARDS) + len(PACKS)

    print("=" * 48)
    print("  LAST KERNEL — Art Generator")
    print(f"  Model:   {MODEL}  |  Quality: {QUALITY}")
    print(f"  Cards:   {len(CARDS)}  |  Packs: {len(PACKS)}  |  Total: {total}")
    print("=" * 48)

    cg, cs, cf = process_batch(client, CARDS, CARD_ART_DIR, 1, total)
    pg, ps, pf = process_batch(client, PACKS, PACK_ART_DIR, len(CARDS) + 1, total)

    print("=" * 48)
    print(f"  Generated : {cg + pg}")
    print(f"  Skipped   : {cs + ps}")
    print(f"  Failed    : {cf + pf}")
    print("=" * 48)
    if cf + pf:
        print("  Rerun to retry failed items — existing files are skipped automatically.")


if __name__ == "__main__":
    main()
