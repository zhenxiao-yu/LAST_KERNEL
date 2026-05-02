"""
LAST KERNEL Card + Pack Art Generator
Uses Recraft AI — purpose-built for pixel art icons with style consistency.

Plan: Core (~$12/month) — 1000 fast generations/month.
      110 cards fit comfortably; plenty left for retries.

Setup:
    1. Sign up at https://www.recraft.ai
    2. Go to Account → API → Create API key
    3. Set it in PowerShell:  $env:RECRAFT_API_KEY="your-key-here"

Style workflow (run ONCE before batch):
    python generate_last_kernel_art.py --make-style
    Opens the first generated card so you can approve it.
    If good, saves the style_id to recraft_style.json.
    All subsequent generations use that locked style.

Run batch:
    python generate_last_kernel_art.py

Output:
    CardArt\\*.png  — transparent PNG, drop into Assets/_Project/Art/Sprites/CardArt/
    PackArt\\*.png  — transparent PNG, drop into Assets/_Project/Art/Sprites/PackArt/
"""

import argparse
import json
import os
import sys
import time
import urllib.request
import urllib.error
from typing import Dict, List, Tuple, Optional

# ============================================================
# CONFIG
# ============================================================

API_KEY      = os.getenv("RECRAFT_API_KEY", "")
BASE_URL     = "https://external.api.recraft.ai/v1"
STYLE_FILE   = os.path.join(os.path.dirname(__file__), "recraft_style.json")

CARD_ART_DIR = "CardArt"
PACK_ART_DIR = "PackArt"

# Recraft model + style
# "recraft_v3" is their latest; "pixel_art" is a native style — no extra prompting needed.
MODEL        = "recraft_v3"
STYLE        = "pixel_art"
IMAGE_SIZE   = "1024x1024"

# Generation settings
MAX_RETRIES      = 2
RETRY_DELAY      = 10   # seconds between retries
DOWNLOAD_TIMEOUT = 60


# ============================================================
# PROMPTS
#
# Recraft pixel_art style handles the pixel art rendering.
# Prompts should describe CONTENT only — not art style instructions.
# Short, clear, noun-first works best.
# Negative prompt: Recraft uses a "negative_prompt" field.
# ============================================================

NEGATIVE = (
    "text, letters, numbers, watermark, multiple characters, "
    "color palette panel, reference sheet, sprite sheet, "
    "background scenery, environment, room interior, "
    "card frame, border, ui elements, hud, "
    "drop shadow, glow halo, duplicate, split panel"
)

# Per-category pose/composition prefix — kept short for Recraft.
CATEGORY_PREFIX: Dict[str, str] = {
    "Character": "full body character, facing right, combat ready stance, isolated on white, ",
    "Mob":       "full body creature, facing front, aggressive stance, isolated on white, ",
    "Material":  "single raw material object, floating, isometric view, isolated on white, ",
    "Consumable":"single food or container, floating, isometric view, isolated on white, ",
    "Equipment": "single weapon or armor piece, floating, 45 degree angle, isolated on white, ",
    "Structure": "single building or machine, isometric 3/4 view, isolated on white, ",
    "Resource":  "single natural object or plant, floating, isolated on white, ",
    "Area":      "two or three bold landmark silhouettes side by side, isolated on white, ",
    "Currency":  "single coin or chip, floating, isometric view, isolated on white, ",
    "Valuable":  "single rare artifact, floating, isometric view, isolated on white, ",
    "Recipe":    "single rolled paper scroll, floating, isolated on white, ",
    "Pack":      "single sealed supply crate or pack, floating, isometric view, isolated on white, ",
    "Other":     "single object, floating, isometric view, isolated on white, ",
}

WORLD_SUFFIX = "cyberpunk post-apocalyptic bunker world, worn industrial technology"


# ============================================================
# CARD DATA  —  (filename, category, subject description)
# ============================================================

CARDS: List[Tuple[str, str, str]] = [
    # ── Characters ───────────────────────────────────────────
    ("Villager",       "Character", "colonist survivor in patched bunker clothes"),
    ("Warrior",        "Character", "armored enforcer holding a salvaged blade"),
    ("Mage",           "Character", "netrunner with glowing visor and cable arms"),
    ("Ranger",         "Character", "scout in lightweight armor holding a composite bow"),
    ("Baby",           "Character", "small infant wrapped in thermal foil"),

    # ── Mobs ─────────────────────────────────────────────────
    ("Slime",          "Mob", "blobby acidic creature with glowing core, dripping"),
    ("Goblin",         "Mob", "wiry scavenger humanoid with large ears and clawed hands"),
    ("Satyr",          "Mob", "goat-legged mutant with bio-mechanical hind legs"),
    ("TrollShaman",    "Mob", "massive hunched troll holding a totem staff"),
    ("CrimsonAcolyte", "Mob", "robed cultist with glowing red chest markings"),
    ("DemonLord",      "Mob", "towering horned entity with dark energy in both fists"),
    ("Squirrel",       "Mob", "feral mutant squirrel with enlarged claws"),
    ("Chicken",        "Mob", "bio-bred mutant chicken, oversized"),
    ("Cow",            "Mob", "stocky bio-bred cow with metal ear tag"),
    ("Corpse",         "Mob", "fallen colonist body lying flat"),

    # ── Materials ─────────────────────────────────────────────
    ("Wood",           "Material", "rough-cut timber log with visible grain"),
    ("Stone",          "Material", "jagged rock chunk with flat facets"),
    ("Clay",           "Material", "block of raw reddish clay"),
    ("IronOre",        "Material", "rock with metallic iron veins"),
    ("IronIngot",      "Material", "rectangular smelted iron bar"),
    ("Plank",          "Material", "two stacked wooden boards nailed at corner"),
    ("Brick",          "Material", "single fired clay brick with worn edges"),
    ("Timber",         "Material", "long structural beam with rough-hewn ends"),
    ("Flint",          "Material", "sharp angular flint shard"),
    ("Fiber",          "Material", "bundle of twisted fibers tied with string"),
    ("Rope",           "Material", "coiled rope loop with knotted end"),
    ("Soil",           "Material", "rounded mound of dark nutrient soil"),

    # ── Consumables ───────────────────────────────────────────
    ("Apple",          "Consumable", "round apple with leaf"),
    ("Berry",          "Consumable", "cluster of three round berries on a twig"),
    ("Potato",         "Consumable", "lumpy oval potato with sprout nubs"),
    ("BakedPotato",    "Consumable", "split baked potato in foil with steam"),
    ("Egg",            "Consumable", "single egg in a small wire tray"),
    ("RawMeat",        "Consumable", "raw protein slab with vacuum-sealed edge"),
    ("Steak",          "Consumable", "thick cooked steak on a metal mess tray"),
    ("Milk",           "Consumable", "sealed cylindrical flask"),
    ("Milkshake",      "Consumable", "sealed canister with straw at top"),
    ("Soup",           "Consumable", "sealed thermal pouch with steam vent"),
    ("FruitSalad",     "Consumable", "sealed ration cup with fruit visible through lid"),
    ("Omelette",       "Consumable", "folded omelette on a flat metal camp tray"),
    ("Coconut",        "Consumable", "coconut cracked in half"),
    ("Acorn",          "Consumable", "single acorn with cap"),
    ("RoastedAcorn",   "Consumable", "three roasted acorns in a tin bowl"),
    ("Turnip",         "Consumable", "round turnip with leafy top"),

    # ── Equipment ─────────────────────────────────────────────
    ("Sword",          "Equipment", "straight single-edged salvaged blade with wrapped grip"),
    ("Bow",            "Equipment", "recurve composite bow with tech-wrapped limbs"),
    ("WoodenClub",     "Equipment", "thick wooden club with heavy rounded end"),
    ("WoodenStick",    "Equipment", "sharpened straight wooden staff"),
    ("Slingshot",      "Equipment", "Y-shaped slingshot with elastic band"),
    ("Staff",          "Equipment", "tall staff with glowing tech orb at top"),
    ("Quiver",         "Equipment", "cylindrical arrow quiver with arrow flights visible"),
    ("Tunic",          "Equipment", "sleeveless vest with patch repairs and buckled straps"),
    ("LeatherArmor",   "Equipment", "chest plate of layered leather with shoulder rivets"),
    ("Chainmail",      "Equipment", "folded mesh shirt of interlocked metal rings"),
    ("SlimeHat",       "Equipment", "rounded helmet dripping with green slime"),
    ("VitalityAmulet", "Equipment", "round amulet with circuit pattern on face"),

    # ── Structures ────────────────────────────────────────────
    ("Sawmill",        "Structure", "compact sawmill with circular blade and log feed"),
    ("Furnace",        "Structure", "squat box furnace with glowing orange front vent"),
    ("Kiln",           "Structure", "domed kiln with chimney vents"),
    ("Farm",           "Structure", "hydroponic grow tray with seedlings under UV light"),
    ("LoggingCamp",    "Structure", "log processing station with chainsaw arm"),
    ("StoneQuarry",    "Structure", "drill rig on rock shelf with cut stone piles"),
    ("ClayPit",        "Structure", "shallow excavation pit with clay walls and shovel"),
    ("ClayQuarry",     "Structure", "mechanized clay dig with conveyor arm"),
    ("IronMine",       "Structure", "mine shaft entrance with timber supports and cart rail"),
    ("IronDeposit",    "Structure", "rock face with exposed iron veins"),
    ("CreaturePen",    "Structure", "reinforced pen with metal fence posts and gate"),
    ("CreatureCage",   "Structure", "portable metal cage with heavy padlock"),
    ("Warehouse",      "Structure", "flat-roofed storage depot with large sliding door"),
    ("Library",        "Structure", "data terminal kiosk with drive shelves and blinking lights"),
    ("House",          "Structure", "small prefab shelter module with bolted panel walls"),
    ("Hearth",         "Structure", "compact heating unit with glowing coil and fan vents"),
    ("Bonfire",        "Structure", "metal burn barrel with flames at top rim"),
    ("Anvil",          "Structure", "heavy flat-topped iron anvil on block base"),
    ("Yard",           "Structure", "fenced compound with gate and perimeter posts"),

    # ── Resources / Areas ─────────────────────────────────────
    ("Forest",         "Area",     "three twisted mutant trees with glowing roots"),
    ("Grass",          "Resource", "patch of bioluminescent moss on cracked concrete"),
    ("Highlands",      "Area",     "rocky plateau silhouette with jagged peaks"),
    ("Ruins",          "Area",     "crumbled concrete pillars with rebar and rubble"),
    ("Fields",         "Area",     "grid of cracked dry furrows, abandoned cropland"),
    ("Graveyard",      "Area",     "three grave mounds with cross markers and hazard flag"),
    ("Coral",          "Resource", "branching mutant coral formation"),
    ("BasaltColumns",  "Resource", "tight cluster of hexagonal basalt pillars"),
    ("AppleTree",      "Resource", "slender apple tree in grow tube with two apples"),
    ("BerryBush",      "Resource", "compact shrub with small round berries"),
    ("Tree",           "Resource", "bioluminescent tree with glowing vein pattern on trunk"),
    ("PalmTree",       "Resource", "tall thin palm tree with splayed fronds"),

    # ── Valuables ─────────────────────────────────────────────
    ("GoldenKey",      "Valuable", "ornate key with abstract circuit etchings"),
    ("TreasureChest",  "Valuable", "military supply crate with heavy clasps and glowing seal"),
    ("WoodenChest",    "Valuable", "small wooden box with metal latch and hinged lid"),
    ("GlowingDust",    "Valuable", "sealed vial of glowing bioluminescent dust"),
    ("BloodChalice",   "Valuable", "goblet with dark liquid and glowing rim"),
    ("AbyssalCore",    "Valuable", "jagged crystal shard with dark energy pulsing within"),
    ("SacrificialAltar","Valuable","flat stone block altar with power conduit cables"),
    ("GrandPortal",    "Valuable", "tall arched gateway frame with crackling energy between posts"),

    # ── Currency / Other ──────────────────────────────────────
    ("Coin",           "Currency", "round coin with abstract hexagon pattern on face"),
    ("Sign",           "Other",    "rectangular rusted metal sign on post"),
    ("Grave",          "Other",    "single grave mound with crude marker stone"),
    ("Recipe",         "Recipe",   "curled paper scroll with abstract gear shapes"),
    ("Rock",           "Material", "small rounded loose rock"),
]

PACKS: List[Tuple[str, str, str]] = [
    ("Starter",       "Pack", "basic sealed survival kit pouch with star symbol"),
    ("Knowledge",     "Pack", "data chip slotted into open book spine"),
    ("Farmstead",     "Pack", "seed packet with sprout shape on front"),
    ("HeartyMeals",   "Pack", "ration pack with flame and bowl shape"),
    ("Blacksmith",    "Pack", "sealed tool pack with anvil shape"),
    ("Revelations",   "Pack", "glowing sealed envelope with broken wax seal"),
    ("Beginning",     "Pack", "clean supply crate with sunrise chevron shape"),
    ("Adventure",     "Pack", "scout kit roll with compass shape"),
    ("Survival",      "Pack", "emergency supply pack with hazard stripe markings"),
    ("Island",        "Pack", "waterproof pack with wave and palm shape"),
    ("Construction",  "Pack", "heavy materials pack with I-beam and hammer shape"),
]


# ============================================================
# RECRAFT API
# ============================================================

def api_request(method: str, endpoint: str, body: dict = None) -> dict:
    url  = f"{BASE_URL}/{endpoint}"
    data = json.dumps(body).encode("utf-8") if body else None
    req  = urllib.request.Request(
        url, data=data, method=method,
        headers={
            "Authorization": f"Bearer {API_KEY}",
            "Content-Type":  "application/json",
        },
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read())
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"HTTP {e.code}: {body}") from e


def load_style_id() -> Optional[str]:
    if os.path.exists(STYLE_FILE):
        with open(STYLE_FILE) as f:
            return json.load(f).get("style_id")
    return None


def save_style_id(style_id: str) -> None:
    with open(STYLE_FILE, "w") as f:
        json.dump({"style_id": style_id}, f, indent=2)
    print(f"  Style saved → {STYLE_FILE}")


def build_prompt(category: str, subject: str) -> str:
    prefix = CATEGORY_PREFIX.get(category, CATEGORY_PREFIX["Other"])
    return f"{prefix}{subject}, {WORLD_SUFFIX}"


def generate_image(prompt: str, style_id: Optional[str]) -> bytes:
    """Call Recraft API and return raw PNG bytes."""
    payload = {
        "prompt":          prompt,
        "negative_prompt": NEGATIVE,
        "model":           MODEL,
        "style":           STYLE,
        "n":               1,
        "size":            IMAGE_SIZE,
        "response_format": "url",
    }
    if style_id:
        payload["style_id"] = style_id

    result = api_request("POST", "images/generations", payload)
    url    = result["data"][0]["url"]

    req = urllib.request.Request(url)
    with urllib.request.urlopen(req, timeout=DOWNLOAD_TIMEOUT) as resp:
        return resp.read()


def generate_and_save(prompt: str, filepath: str, style_id: Optional[str]) -> None:
    tmp = filepath + ".tmp"
    try:
        image_bytes = generate_image(prompt, style_id)
        with open(tmp, "wb") as f:
            f.write(image_bytes)
        os.replace(tmp, filepath)
    except Exception:
        if os.path.exists(tmp):
            os.remove(tmp)
        raise


# ============================================================
# STYLE SETUP (--make-style)
# ============================================================

def make_style() -> None:
    """Generate one reference card, let user approve, save style_id."""
    print("=" * 52)
    print("  LAST KERNEL — Style Setup")
    print("  Generating one reference card (Warrior)...")
    print("=" * 52)

    # Use no style_id for the first generation — pure model defaults
    prompt = build_prompt("Character", "armored enforcer holding a salvaged blade, cyberpunk")
    payload = {
        "prompt":          prompt,
        "negative_prompt": NEGATIVE,
        "model":           MODEL,
        "style":           STYLE,
        "n":               1,
        "size":            IMAGE_SIZE,
        "response_format": "url",
    }
    result   = api_request("POST", "images/generations", payload)
    img_url  = result["data"][0]["url"]
    img_id   = result["data"][0].get("id") or result.get("id")

    # Save preview
    os.makedirs(CARD_ART_DIR, exist_ok=True)
    preview = os.path.join(CARD_ART_DIR, "_style_reference.png")
    req = urllib.request.Request(img_url)
    with urllib.request.urlopen(req, timeout=DOWNLOAD_TIMEOUT) as resp:
        with open(preview, "wb") as f:
            f.write(resp.read())

    print(f"\n  Preview saved → {preview}")
    print("  Open it and check the style. Does it look good?")
    answer = input("  Accept this as the style reference? [y/n]: ").strip().lower()

    if answer != "y":
        print("  Rejected. Delete the preview and run --make-style again.")
        return

    # Create a Recraft style from this image — multipart/form-data required
    print("  Creating style from reference image...")
    with open(preview, "rb") as f:
        img_bytes = f.read()

    boundary = "----RecraftStyleBoundary"
    body = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="style"\r\n\r\n'
        f"{STYLE}\r\n"
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="file"; filename="reference.png"\r\n'
        f"Content-Type: image/png\r\n\r\n"
    ).encode() + img_bytes + f"\r\n--{boundary}--\r\n".encode()

    style_req = urllib.request.Request(
        f"{BASE_URL}/styles",
        data=body,
        method="POST",
        headers={
            "Authorization": f"Bearer {API_KEY}",
            "Content-Type":  f"multipart/form-data; boundary={boundary}",
        },
    )
    try:
        with urllib.request.urlopen(style_req, timeout=60) as resp:
            style_result = json.loads(resp.read())
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"Style creation failed HTTP {e.code}: {e.read().decode()}") from e
    style_id = style_result["id"]
    save_style_id(style_id)

    print(f"\n  Style ID: {style_id}")
    print("  All subsequent generations will match this style.")
    print("  Now run: python generate_last_kernel_art.py")


# ============================================================
# BATCH GENERATION
# ============================================================

def process_batch(
    items: List[Tuple[str, str, str]],
    output_dir: str,
    start_index: int,
    total: int,
    style_id: Optional[str],
) -> Tuple[int, int, int]:
    generated = skipped = failed = 0

    for i, (name, category, subject) in enumerate(items, start=start_index):
        filepath = os.path.join(output_dir, f"{name}.png")

        if os.path.exists(filepath):
            print(f"  skip [{i:>3}/{total}] {name}")
            skipped += 1
            continue

        print(f"  gen  [{i:>3}/{total}] {name}...")
        prompt = build_prompt(category, subject)

        for attempt in range(MAX_RETRIES + 1):
            try:
                generate_and_save(prompt, filepath, style_id)
                print(f"       saved → {filepath}")
                generated += 1
                break
            except Exception as e:
                if attempt < MAX_RETRIES:
                    print(f"       retry {attempt + 1}: {e}")
                    time.sleep(RETRY_DELAY)
                else:
                    print(f"       FAILED: {e}")
                    failed += 1

    return generated, skipped, failed


# ============================================================
# MAIN
# ============================================================

def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--make-style", action="store_true",
                        help="Generate one reference card and lock it as the style for all others")
    args = parser.parse_args()

    if not API_KEY:
        print("No API key found.")
        print("Set it with:  $env:RECRAFT_API_KEY='your-key-here'")
        sys.exit(1)

    if args.make_style:
        make_style()
        return

    style_id = load_style_id()
    total    = len(CARDS) + len(PACKS)

    print("=" * 52)
    print("  LAST KERNEL — Art Generator (Recraft AI)")
    print(f"  Model:    {MODEL}  |  Style: {STYLE}")
    print(f"  Style ID: {style_id or 'none — run --make-style first for consistency'}")
    print(f"  Cards:    {len(CARDS)}  |  Packs: {len(PACKS)}  |  Total: {total}")
    print("=" * 52)

    if not style_id:
        print()
        print("  TIP: Run --make-style first to lock a consistent visual style.")
        print("       Continuing without style lock (each card may look different).")
        print()

    os.makedirs(CARD_ART_DIR, exist_ok=True)
    os.makedirs(PACK_ART_DIR, exist_ok=True)

    cg, cs, cf = process_batch(CARDS, CARD_ART_DIR, 1, total, style_id)
    pg, ps, pf = process_batch(PACKS, PACK_ART_DIR, len(CARDS) + 1, total, style_id)

    print()
    print("=" * 52)
    print(f"  Generated : {cg + pg}")
    print(f"  Skipped   : {cs + ps}")
    print(f"  Failed    : {cf + pf}")
    print("=" * 52)
    if cf + pf:
        print("  Rerun to retry failed items — existing files are skipped.")


if __name__ == "__main__":
    main()
