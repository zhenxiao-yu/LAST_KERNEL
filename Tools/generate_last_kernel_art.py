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

MODEL            = "dall-e-3"  # Universally available; use gpt-image-1 if your project has access
SIZE             = "1024x1024"
QUALITY          = "standard"  # standard (~$0.04) or hd (~$0.08)
DELAY_SECONDS    = 13          # Stay under 5 images/min rate limit (60s / 5 = 12s min)
MAX_RETRIES      = 1           # Automatic retries on transient errors
RETRY_DELAY      = 60          # Extra wait on rate-limit retry
DOWNLOAD_TIMEOUT = 60          # Seconds before download is considered hung
MAX_PROMPT_CHARS = 4000        # dall-e-3 hard limit


# ============================================================
# STYLE
#
# Each card = isolated subject on flat white background.
# Background tint/color is applied by the Unity shader (_OverlayTex slot)
# layered on top of each card's material _Color per category.
# Do NOT bake backgrounds into the PNG — it breaks the shader pipeline.
#
# Style: 16-bit SNES/GBA era pixel art, bold dark outlines, cel-shading.
# Palette reference (names only — hex codes are BANNED from prompt strings
# because they cause the model to generate color swatch reference panels):
#   outlines:   dark navy       panels:     dark charcoal
#   accent:     electric cyan   dim accent: muted teal
#   secondary:  muted magenta   warm:       amber
#   danger:     muted red       success:    muted green
# ============================================================

MASTER_STYLE = (
    # ══════════════════════════════════════════════════════════════════════════
    # HARD BANS — stated FIRST, before any positive instruction.
    #
    # Each ban targets a specific observed or anticipated hallucination:
    #
    #  [1]  Hex codes in prompt → model generates a color swatch reference panel.
    #       Fix: no hex codes anywhere; colors described by name only (see Palette).
    #  [2]  "64x64 pixel art sprite" language → model produces a sprite-sheet doc.
    #       Fix: removed from CATEGORY_PREFIX; now uses "pixel art game icon".
    #  [3]  "palette strictly N colors" + hex codes → design-reference-card format.
    #       Fix: removed count spec; colors described visually, never numerically.
    #  [4]  Game title references ("Fire Emblem", "Slay the Spire") → model renders
    #       a screenshot of that game complete with its HUD and battle UI.
    #       Fix: removed named titles; style described by visual attributes only.
    #  [5]  "card sprite" / "trading card" → model adds playing-card frame + border.
    #       Fix: prefix now says "pixel art game icon" not "card sprite".
    #  [6]  "schematic" / "blueprint" → model draws annotated technical diagrams
    #       with dimension lines, measurement labels, and leader text.
    #       Fix: Recipe prefix now says "rolled paper scroll", not "schematic".
    #  [7]  "glowing X" without qualifier → model blooms a full radial halo that
    #       floods the background instead of staying as bright pixels on the subject.
    #       Fix: explicit ban on radial bloom; glow limited to surface pixels only.
    #  [8]  "cyberpunk" alone → model adds neon-light atmosphere and fog behind subject.
    #       Fix: explicit ban on neon atmosphere + reinforced white-only background.
    #  [9]  "bunker/underground world" context → model renders a room as background.
    #       Fix: explicit ban on room walls, floor tiles, ceiling.
    # [10]  "transparent" anywhere in prompt text → model generates a checkerboard
    #       (the universal PNG-transparency indicator pattern).
    #       Fix: "transparent" word removed; background described as "flat white".
    # [11]  "isometric scene" for multi-element cards → full landscape painting.
    #       Fix: Area prefix now says "two or three bold shapes side by side".
    # [12]  "retro pixel art" alone → model regresses to Atari/NES 4-color extreme.
    #       Fix: anchored to "16-bit SNES/GBA era" and explicitly NOT Atari/NES.
    # [13]  "dithered shading" alone → pure black-and-white 1-bit dithering, no color.
    #       Fix: "colored dithered midtones, NOT black-and-white dithering".
    # [14]  "cel-shading" without "2D pixel art" qualifier → 3D Borderlands-style render.
    #       Fix: "2D pixel art cel-shading" and explicit "NOT a 3D render".
    # [15]  "circuit glyphs" / "sigils" / "runes" in subject description → model
    #       renders actual readable text-like symbols and letters.
    #       Fix: card descriptions reworded to "abstract glowing markings/patterns".
    # [16]  Named genre archetypes ("Mage", "Ranger", "Goblin") → model defaults to
    #       medieval-fantasy look, ignoring the cyberpunk world context.
    #       Fix: Character/Mob prefixes now explicitly say "NOT medieval fantasy".
    # [17]  Pack cards with "emblem" description → model adds product branding text.
    #       Fix: pack subjects reworded to avoid "emblem"; Pack prefix bans labels.
    # [18]  Area cards: "two or three landmark elements" → full diorama/world-map.
    #       Fix: reworded to "two or three bold isolated shapes on white".
    # [19]  Equipment with blade+grip → model renders inventory detail panel
    #       showing parts labeled separately.
    #       Fix: Equipment prefix says "single unified object", bans part labels.
    # [20]  Multiple "glowing" elements in one card → sci-fi scene with lighting FX.
    #       Fix: ban on scene lighting; glow must stay surface-pixel level only.
    # ══════════════════════════════════════════════════════════════════════════

    # ── Layout / document bans [1][2][3][4][5][6] ─────────────────────────────
    "NO text of any kind, NO letters, NO numbers, NO words anywhere in the image, "
    "NO color swatch panel, NO color palette grid, NO reference sheet, "
    "NO sprite sheet layout, NO design document, NO concept art layout, "
    "NO side panels, NO sidebars, NO split-panel layout, "
    "NO fake UI, NO health bars, NO stat boxes, NO HUD chrome, NO inventory panels, "
    "NO playing-card frame, NO card border baked into the art, NO corner ornaments, "
    "NO blueprint annotations, NO dimension lines, NO measurement labels, "
    "NO part labels, NO callout arrows, "

    # ── Background bans [7][8][9][10][11][20] ─────────────────────────────────
    "NO background scenery of any kind, NO floor, NO wall, NO ceiling, NO room interior, "
    "NO neon glow atmosphere, NO neon light trails, NO atmospheric fog or haze, "
    "NO radial glow bloom behind the subject, glow stays as bright surface pixels only, "
    "NO drop shadow cast onto the background, NO ambient occlusion shadow behind subject, "
    "NO checkerboard pattern, NO scene lighting effects, "

    # ── Style bans [12][13][14] ────────────────────────────────────────────────
    "NOT a 3D render, NOT photorealistic, NOT painterly, NOT watercolor, "
    "NOT a game screenshot, NOT an Atari or NES 4-color style, "

    # ── Subject bans [15][16][17] ──────────────────────────────────────────────
    "NO readable rune symbols, NO readable sigil text, NO readable circuit-pattern text, "
    "NO product branding, NO logo text, NO emblem labels, "
    "NOT a medieval fantasy aesthetic, NOT a magic-fantasy setting, "

    # ── Background ────────────────────────────────────────────────────────────
    # dall-e-3 cannot output true alpha; white is easiest to chroma-key later.
    # NOTE: no hex codes here — hex codes trigger color swatch panel generation.
    "pure flat white background, subject floating on solid white and nothing else, "
    "absolutely nothing behind the subject except white, "

    # ── Pixel art style [12][13][14] ──────────────────────────────────────────
    "16-bit era pixel art, chunky blocky pixels clearly visible, "
    "SNES and Game Boy Advance era color depth and detail level, "
    "bold dark two-pixel outline around the entire subject, "
    "flat 2D pixel art cel-shading with colored dithered midtones, "
    "NOT black-and-white dithering, NOT 3D cel-shading, "
    "hard pixel edges only, no smooth gradients, no anti-aliasing, no sub-pixel blending, "

    # ── Palette [1] — color names only, zero hex codes ────────────────────────
    "cyberpunk palette: dark navy for outlines, electric cyan for accent highlights, "
    "muted magenta for secondary details, warm amber for warm-lit surfaces, "
    "off-white for the brightest lit pixel highlights, "

    # ── Composition ───────────────────────────────────────────────────────────
    "subject centered in frame, fills about two-thirds of canvas height, "
    "equal white margin on all four sides, "
    "strong readable silhouette at small display sizes, "
    "one memorable iconic design detail for instant card recognition, "

    # ── Quality ───────────────────────────────────────────────────────────────
    "every pixel intentional, no stray isolated pixel specks, "
    "professional polished pixel art game icon quality, "
    "dark cyberpunk post-apocalyptic underground bunker survival world"
)

# Per-category prefix + background.
# Prefix goes FIRST in the prompt to lock pose and composition.
# Background description defines the atmospheric color fill behind the subject,
# derived from the LAST KERNEL theme.uss palette per category.
CATEGORY_PREFIX: Dict[str, str] = {

    # [16] "Mage"/"Ranger" → medieval fantasy: explicitly blocked per-category
    "Character": (
        "pixel art game icon, flat white background, "
        "single character, full body, facing slightly right, "
        "neutral combat-ready stance, arms slightly away from body, "
        "head and feet both fully in frame, slightly large head for readability, "
        "post-apocalyptic cyberpunk survivor look, NOT medieval fantasy, "
    ),
    # [16] same — Goblin/Satyr → fantasy monster by default
    "Mob": (
        "pixel art game icon, flat white background, "
        "single creature, full body, facing front, "
        "threatening wide stance, limbs spread, aggressive posture, "
        "exaggerated menacing proportions, entire body in frame, "
        "post-apocalyptic mutant creature look, NOT medieval fantasy monster, "
    ),
    "Material": (
        "pixel art game icon, flat white background, "
        "single raw material object floating centered, "
        "slight isometric tilt to show top and front face, "
        "bold chunky pixel shapes, no fine surface detail, "
    ),
    "Consumable": (
        "pixel art game icon, flat white background, "
        "single food or container object floating centered, "
        "slight isometric tilt, bold readable pixel shapes, "
    ),
    # [19] Equipment: ban part labels; treat as single unified object
    "Equipment": (
        "pixel art game icon, flat white background, "
        "single unified weapon or armor piece floating centered, "
        "45-degree diagonal angle with tip toward upper-right, "
        "treated as one solid object, NO labeled parts, NO component breakdown, "
    ),
    "Structure": (
        "pixel art game icon, flat white background, "
        "single compact building or machine in isometric 3/4 view, "
        "front face and rooftop both visible, centered with equal margins, "
        "chunky readable architecture, no surrounding ground or environment, "
    ),
    "Resource": (
        "pixel art game icon, flat white background, "
        "single natural object or plant floating centered, "
        "front-facing or slight 3/4 angle, bold chunky silhouette, "
    ),
    # [11][18] Area: was "isometric scene" → landscape painting
    # Now: two or three bold shapes side by side, NOT a scene/environment
    "Area": (
        "pixel art game icon, flat white background, "
        "two or three bold chunky landmark silhouette shapes arranged side by side, "
        "NOT a scene, NOT a landscape, NOT a diorama, isolated shapes on white only, "
    ),
    "Currency": (
        "pixel art game icon, flat white background, "
        "single coin or credit chip floating centered, "
        "slight isometric tilt, bright glint pixel on top face, "
    ),
    "Valuable": (
        "pixel art game icon, flat white background, "
        "single rare artifact floating centered, slight isometric tilt, "
        "striking color contrast to imply rarity, "
    ),
    # [6] Recipe: "schematic" → blueprint annotations; use "rolled paper" instead
    "Recipe": (
        "pixel art game icon, flat white background, "
        "single rolled paper scroll floating centered, "
        "scroll curled at both ends, front-facing, no annotations, no labels, "
    ),
    "Other": (
        "pixel art game icon, flat white background, "
        "single object floating centered, slight isometric tilt, bold chunky shapes, "
    ),
    # [17] Pack: "emblem" in descriptions → product branding text; prefix blocks it
    "Pack": (
        "pixel art game icon, flat white background, "
        "single sealed supply pack or crate floating centered, "
        "slight isometric tilt, bold chunky silhouette, "
        "NO text, NO brand label, NO product text on surface, "
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
    ("TrollShaman",    "Mob", "a massive hunched troll holding a totem staff with abstract glowing circuit markings"),
    ("CrimsonAcolyte", "Mob", "a robed cultist with glowing red abstract markings on the chest"),
    ("DemonLord",      "Mob", "a towering horned entity with crackling dark energy in both fists"),
    ("Squirrel",       "Mob", "a feral mutant squirrel with enlarged claws"),
    ("Chicken",        "Mob", "a bio-bred mutant chicken, slightly oversized, beady eyes"),
    ("Cow",            "Mob", "a stocky bio-bred cow with metal ear tag"),
    ("Corpse",         "Mob", "a fallen colonist body lying flat, hazard warning markers nearby"),

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
    ("Milk",           "Consumable", "a sealed cylindrical flask with a drop marking on the side"),
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
    ("Staff",          "Equipment", "a tall staff with a bright glowing tech orb at the top"),
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
    ("GoldenKey",      "Valuable", "an ornate key with abstract circuit etchings on the bow"),
    ("TreasureChest",  "Valuable", "a military-style supply crate with heavy clasps and a glowing seal"),
    ("WoodenChest",    "Valuable", "a small wooden box with a metal latch and hinged lid"),
    ("GlowingDust",    "Valuable", "a sealed vial of glowing bioluminescent particle dust"),
    ("BloodChalice",   "Valuable", "a goblet with dark liquid, glowing faintly at the rim"),
    ("AbyssalCore",    "Valuable", "a jagged crystal shard pulsing with dark energy from within"),
    ("SacrificialAltar","Valuable","a flat stone block altar with power conduit cables attached"),
    ("GrandPortal",    "Valuable", "a tall arched gateway frame crackling with energy between the posts"),

    # ── Currency / Other ──────────────────────────────────────
    ("Coin",           "Currency", "a round coin with an abstract hexagon circuit pattern on the face"),
    ("Sign",           "Other",    "a rectangular rusted metal sign on a post, blank face"),
    ("Grave",          "Other",    "a single grave mound with a crude flat marker stone"),
    ("Recipe",         "Recipe",   "a curled paper scroll with abstract gear and arrow shapes printed on it"),
    ("Rock",           "Material", "a small rounded loose rock"),
]

PACKS: List[Tuple[str, str, str]] = [
    ("Starter",       "Pack", "a basic sealed survival kit pouch with a star symbol stamped on it"),
    ("Knowledge",     "Pack", "a data chip slotted into an open book spine"),
    ("Farmstead",     "Pack", "a seed packet with a sprout shape on the front"),
    ("HeartyMeals",   "Pack", "a ration pack with a flame and bowl shape on it"),
    ("Blacksmith",    "Pack", "a sealed tool pack with an anvil shape stamped on it"),
    ("Revelations",   "Pack", "a glowing sealed envelope with a broken wax seal"),
    ("Beginning",     "Pack", "a clean new supply crate with a sunrise chevron shape on it"),
    ("Adventure",     "Pack", "a scout kit roll with a compass shape on it"),
    ("Survival",      "Pack", "an emergency supply pack with hazard stripe markings"),
    ("Island",        "Pack", "a waterproof pack with a wave and palm shape on it"),
    ("Construction",  "Pack", "a heavy materials pack with I-beam and hammer icon"),
]


# ============================================================
# GENERATION
# ============================================================

def ensure_dirs() -> None:
    os.makedirs(CARD_ART_DIR, exist_ok=True)
    os.makedirs(PACK_ART_DIR, exist_ok=True)


def build_prompt(category: str, subject: str) -> str:
    if category not in CATEGORY_PREFIX:
        print(f"  WARN unknown category '{category}' — falling back to 'Other' prefix")
    prefix = CATEGORY_PREFIX.get(category, CATEGORY_PREFIX["Other"])
    prompt = (
        f"{prefix}"
        f"subject: {subject}, "
        f"post-apocalyptic survival bunker world, worn industrial technology, "
        f"not fantasy not medieval not painterly, "
        f"{MASTER_STYLE}"
    )
    if len(prompt) > MAX_PROMPT_CHARS:
        print(f"  WARN prompt is {len(prompt)} chars (API limit {MAX_PROMPT_CHARS}) — truncating")
        prompt = prompt[:MAX_PROMPT_CHARS]
    return prompt


def generate_and_save(client: openai.OpenAI, prompt: str, filepath: str) -> None:
    result = client.images.generate(
        model=MODEL,
        prompt=prompt,
        size=SIZE,
        quality=QUALITY,
        n=1,
    )
    url = result.data[0].url

    # Atomic write: download to .tmp first, then rename.
    # A crash mid-download never leaves a corrupt file that would be skipped on rerun.
    tmp = filepath + ".tmp"
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "LastKernelArtGen/1.0"})
        with urllib.request.urlopen(req, timeout=DOWNLOAD_TIMEOUT) as resp:
            with open(tmp, "wb") as f:
                f.write(resp.read())
        os.replace(tmp, filepath)
    except Exception:
        if os.path.exists(tmp):
            os.remove(tmp)
        raise


def process_batch(
    client: openai.OpenAI,
    items: List[Tuple[str, str, str]],
    output_dir: str,
    start_index: int,
    total: int,
) -> Tuple[int, int, int]:
    generated = skipped = failed = 0
    needs_delay = False  # Only delay between actual API calls, not after the last one

    for i, (name, category, subject) in enumerate(items, start=start_index):
        filepath = os.path.join(output_dir, f"{name}.png")

        if os.path.exists(filepath):
            print(f"  skip [{i}/{total}] {name}")
            skipped += 1
            continue

        if needs_delay:
            time.sleep(DELAY_SECONDS)

        print(f"  gen  [{i}/{total}] {name}...")
        prompt = build_prompt(category, subject)
        needs_delay = True  # An API call is about to happen

        for attempt in range(MAX_RETRIES + 1):
            try:
                generate_and_save(client, prompt, filepath)
                print(f"       saved → {filepath}")
                generated += 1
                break
            except openai.RateLimitError as e:
                if attempt < MAX_RETRIES:
                    wait = DELAY_SECONDS + RETRY_DELAY
                    print(f"       Rate limited — waiting {wait}s then retrying...")
                    time.sleep(wait)
                else:
                    print(f"       FAILED (rate limit): {e}")
                    failed += 1
            except Exception as e:
                if attempt < MAX_RETRIES:
                    print(f"       Error (attempt {attempt + 1}) — retrying: {e}")
                    time.sleep(5)
                else:
                    print(f"       FAILED: {e}")
                    failed += 1

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
