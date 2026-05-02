"""
LAST KERNEL — Card & Pack Art Generator
Recraft AI (recraftv3 / digital_illustration) + phosphor post-processing.

Setup:
    1. recraft.ai → Account → API → create key
    2. $env:RECRAFT_API_KEY = "your-key"
    3. pip install rembg Pillow

Workflow:
    python generate_last_kernel_art.py --make-style   # once: lock visual style
    python generate_last_kernel_art.py                # generate all 110 cards

Output (transparent PNG, drop into Assets/_Project/Art/Sprites/):
    Tools/CardArt/*.png
    Tools/PackArt/*.png
"""

import argparse
import base64
import io
import json
import os
import sys
import time
import urllib.request
import urllib.error
from typing import Dict, List, Optional, Tuple

from PIL import Image, ImageOps

try:
    from rembg import remove as rembg_remove
    REMBG_AVAILABLE = True
except ImportError:
    REMBG_AVAILABLE = False

# ---------------------------------------------------------------------------
# CONFIG
# ---------------------------------------------------------------------------

API_KEY    = os.getenv("RECRAFT_API_KEY", "")
BASE_URL   = "https://external.api.recraft.ai/v1"
STYLE_FILE = os.path.join(os.path.dirname(__file__), "recraft_style.json")

CARD_DIR = "CardArt"
PACK_DIR = "PackArt"

MODEL      = "recraftv3"
STYLE      = "digital_illustration"
IMAGE_SIZE = "1024x1024"

MAX_RETRIES = 2
RETRY_DELAY = 10  # seconds

# Phosphor-green post-processing palette
PHOSPHOR_HI  = (57, 255, 20)   # #39FF14 — bright neon green
PHOSPHOR_LO  = (0,  18,  4)    # near-black green shadow
PIXEL_SIZE   = 8               # grid block size (1024 / 8 = 128 logical pixels)

# ---------------------------------------------------------------------------
# PROMPTS
# ---------------------------------------------------------------------------

NEGATIVE = (
    "text, letters, numbers, watermark, signature, multiple characters, "
    "color palette panel, reference sheet, sprite sheet, "
    "card frame, border, ui elements, hud, "
    "drop shadow, duplicate, split panel, "
    "photorealistic, photograph, 3d render, blurry"
)

CATEGORY_PREFIX: Dict[str, str] = {
    "Character": "full body character, three-quarter view, combat ready, tactical gear, ",
    "Mob":       "full body creature, facing front, aggressive stance, ",
    "Material":  "single raw material object, floating, isometric view, ",
    "Consumable": "single food or container, floating, isometric view, ",
    "Equipment": "single weapon or armor piece, floating, 45 degree angle, ",
    "Structure": "single building or machine, isometric 3/4 view, ",
    "Resource":  "single natural object or plant, floating, ",
    "Area":      "two or three bold landmark silhouettes, ",
    "Currency":  "single coin or chip, floating, isometric view, ",
    "Valuable":  "single rare artifact, floating, isometric view, ",
    "Recipe":    "single rolled paper scroll, floating, ",
    "Pack":      "single sealed supply crate or pack, floating, isometric view, ",
    "Other":     "single object, floating, isometric view, ",
}

WORLD_SUFFIX = (
    "neomilitary corporate dystopia, bold black outline, flat cel-shaded illustration, "
    "slate grey and gunmetal palette, cold blue-white lighting, "
    "near-future tactical gear, sharp angular design, high contrast, clean vector lines"
)

# ---------------------------------------------------------------------------
# CARD DATA  (filename, category, subject)
# ---------------------------------------------------------------------------

CARDS: List[Tuple[str, str, str]] = [
    # Characters
    ("Villager",        "Character", "colonist survivor in patched bunker clothes"),
    ("Warrior",         "Character", "armored enforcer holding a salvaged blade"),
    ("Mage",            "Character", "netrunner with glowing visor and cable arms"),
    ("Ranger",          "Character", "scout in lightweight armor holding a composite bow"),
    ("Baby",            "Character", "small infant wrapped in thermal foil"),
    # Mobs
    ("Slime",           "Mob", "blobby acidic creature with glowing core, dripping"),
    ("Goblin",          "Mob", "wiry scavenger humanoid with large ears and clawed hands"),
    ("Satyr",           "Mob", "goat-legged mutant with bio-mechanical hind legs"),
    ("TrollShaman",     "Mob", "massive hunched troll holding a totem staff"),
    ("CrimsonAcolyte",  "Mob", "robed cultist with glowing red chest markings"),
    ("DemonLord",       "Mob", "towering horned entity with dark energy in both fists"),
    ("Squirrel",        "Mob", "feral mutant squirrel with enlarged claws"),
    ("Chicken",         "Mob", "bio-bred mutant chicken, oversized"),
    ("Cow",             "Mob", "stocky bio-bred cow with metal ear tag"),
    ("Corpse",          "Mob", "fallen colonist body lying flat"),
    # Materials
    ("Wood",            "Material", "rough-cut timber log with visible grain"),
    ("Stone",           "Material", "jagged rock chunk with flat facets"),
    ("Clay",            "Material", "block of raw reddish clay"),
    ("IronOre",         "Material", "rock with metallic iron veins"),
    ("IronIngot",       "Material", "rectangular smelted iron bar"),
    ("Plank",           "Material", "two stacked wooden boards nailed at corner"),
    ("Brick",           "Material", "single fired clay brick with worn edges"),
    ("Timber",          "Material", "long structural beam with rough-hewn ends"),
    ("Flint",           "Material", "sharp angular flint shard"),
    ("Fiber",           "Material", "bundle of twisted fibers tied with string"),
    ("Rope",            "Material", "coiled rope loop with knotted end"),
    ("Soil",            "Material", "rounded mound of dark nutrient soil"),
    ("Rock",            "Material", "small rounded loose rock"),
    # Consumables
    ("Apple",           "Consumable", "round apple with leaf"),
    ("Berry",           "Consumable", "cluster of three round berries on a twig"),
    ("Potato",          "Consumable", "lumpy oval potato with sprout nubs"),
    ("BakedPotato",     "Consumable", "split baked potato in foil with steam"),
    ("Egg",             "Consumable", "single egg in a small wire tray"),
    ("RawMeat",         "Consumable", "raw protein slab with vacuum-sealed edge"),
    ("Steak",           "Consumable", "thick cooked steak on a metal mess tray"),
    ("Milk",            "Consumable", "sealed cylindrical flask"),
    ("Milkshake",       "Consumable", "sealed canister with straw at top"),
    ("Soup",            "Consumable", "sealed thermal pouch with steam vent"),
    ("FruitSalad",      "Consumable", "sealed ration cup with fruit visible through lid"),
    ("Omelette",        "Consumable", "folded omelette on a flat metal camp tray"),
    ("Coconut",         "Consumable", "coconut cracked in half"),
    ("Acorn",           "Consumable", "single acorn with cap"),
    ("RoastedAcorn",    "Consumable", "three roasted acorns in a tin bowl"),
    ("Turnip",          "Consumable", "round turnip with leafy top"),
    # Equipment
    ("Sword",           "Equipment", "straight single-edged salvaged blade with wrapped grip"),
    ("Bow",             "Equipment", "recurve composite bow with tech-wrapped limbs"),
    ("WoodenClub",      "Equipment", "thick wooden club with heavy rounded end"),
    ("WoodenStick",     "Equipment", "sharpened straight wooden staff"),
    ("Slingshot",       "Equipment", "Y-shaped slingshot with elastic band"),
    ("Staff",           "Equipment", "tall staff with glowing tech orb at top"),
    ("Quiver",          "Equipment", "cylindrical arrow quiver with arrow flights visible"),
    ("Tunic",           "Equipment", "sleeveless vest with patch repairs and buckled straps"),
    ("LeatherArmor",    "Equipment", "chest plate of layered leather with shoulder rivets"),
    ("Chainmail",       "Equipment", "folded mesh shirt of interlocked metal rings"),
    ("SlimeHat",        "Equipment", "rounded helmet dripping with green slime"),
    ("VitalityAmulet",  "Equipment", "round amulet with circuit pattern on face"),
    # Structures
    ("Sawmill",         "Structure", "compact sawmill with circular blade and log feed"),
    ("Furnace",         "Structure", "squat box furnace with glowing orange front vent"),
    ("Kiln",            "Structure", "domed kiln with chimney vents"),
    ("Farm",            "Structure", "hydroponic grow tray with seedlings under UV light"),
    ("LoggingCamp",     "Structure", "log processing station with chainsaw arm"),
    ("StoneQuarry",     "Structure", "drill rig on rock shelf with cut stone piles"),
    ("ClayPit",         "Structure", "shallow excavation pit with clay walls and shovel"),
    ("ClayQuarry",      "Structure", "mechanized clay dig with conveyor arm"),
    ("IronMine",        "Structure", "mine shaft entrance with timber supports and cart rail"),
    ("IronDeposit",     "Structure", "rock face with exposed iron veins"),
    ("CreaturePen",     "Structure", "reinforced pen with metal fence posts and gate"),
    ("CreatureCage",    "Structure", "portable metal cage with heavy padlock"),
    ("Warehouse",       "Structure", "flat-roofed storage depot with large sliding door"),
    ("Library",         "Structure", "data terminal kiosk with drive shelves and blinking lights"),
    ("House",           "Structure", "small prefab shelter module with bolted panel walls"),
    ("Hearth",          "Structure", "compact heating unit with glowing coil and fan vents"),
    ("Bonfire",         "Structure", "metal burn barrel with flames at top rim"),
    ("Anvil",           "Structure", "heavy flat-topped iron anvil on block base"),
    ("Yard",            "Structure", "fenced compound with gate and perimeter posts"),
    # Resources / Areas
    ("Forest",          "Area",     "three twisted mutant trees with glowing roots"),
    ("Highlands",       "Area",     "rocky plateau silhouette with jagged peaks"),
    ("Ruins",           "Area",     "crumbled concrete pillars with rebar and rubble"),
    ("Fields",          "Area",     "grid of cracked dry furrows, abandoned cropland"),
    ("Graveyard",       "Area",     "three grave mounds with cross markers and hazard flag"),
    ("Grass",           "Resource", "patch of bioluminescent moss on cracked concrete"),
    ("Coral",           "Resource", "branching mutant coral formation"),
    ("BasaltColumns",   "Resource", "tight cluster of hexagonal basalt pillars"),
    ("AppleTree",       "Resource", "slender apple tree in grow tube with two apples"),
    ("BerryBush",       "Resource", "compact shrub with small round berries"),
    ("Tree",            "Resource", "bioluminescent tree with glowing vein pattern on trunk"),
    ("PalmTree",        "Resource", "tall thin palm tree with splayed fronds"),
    # Valuables
    ("GoldenKey",       "Valuable", "ornate key with abstract circuit etchings"),
    ("TreasureChest",   "Valuable", "military supply crate with heavy clasps and glowing seal"),
    ("WoodenChest",     "Valuable", "small wooden box with metal latch and hinged lid"),
    ("GlowingDust",     "Valuable", "sealed vial of glowing bioluminescent dust"),
    ("BloodChalice",    "Valuable", "goblet with dark liquid and glowing rim"),
    ("AbyssalCore",     "Valuable", "jagged crystal shard with dark energy pulsing within"),
    ("SacrificialAltar","Valuable", "flat stone block altar with power conduit cables"),
    ("GrandPortal",     "Valuable", "tall arched gateway frame with crackling energy between posts"),
    # Currency / Other
    ("Coin",            "Currency", "round coin with abstract hexagon pattern on face"),
    ("Sign",            "Other",    "rectangular rusted metal sign on post"),
    ("Grave",           "Other",    "single grave mound with crude marker stone"),
    ("Recipe",          "Recipe",   "curled paper scroll with abstract gear shapes"),
]

PACKS: List[Tuple[str, str, str]] = [
    ("Starter",      "Pack", "basic sealed survival kit pouch with star symbol"),
    ("Knowledge",    "Pack", "data chip slotted into open book spine"),
    ("Farmstead",    "Pack", "seed packet with sprout shape on front"),
    ("HeartyMeals",  "Pack", "ration pack with flame and bowl shape"),
    ("Blacksmith",   "Pack", "sealed tool pack with anvil shape"),
    ("Revelations",  "Pack", "glowing sealed envelope with broken wax seal"),
    ("Beginning",    "Pack", "clean supply crate with sunrise chevron shape"),
    ("Adventure",    "Pack", "scout kit roll with compass shape"),
    ("Survival",     "Pack", "emergency supply pack with hazard stripe markings"),
    ("Island",       "Pack", "waterproof pack with wave and palm shape"),
    ("Construction", "Pack", "heavy materials pack with I-beam and hammer shape"),
]

# ---------------------------------------------------------------------------
# API
# ---------------------------------------------------------------------------

def api_request(method: str, endpoint: str, body: Optional[dict] = None) -> dict:
    url  = f"{BASE_URL}/{endpoint}"
    data = json.dumps(body).encode() if body else None
    req  = urllib.request.Request(
        url, data=data, method=method,
        headers={"Authorization": f"Bearer {API_KEY}", "Content-Type": "application/json"},
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read())
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"HTTP {e.code}: {e.read().decode(errors='replace')}") from e


def load_style_id() -> Optional[str]:
    if os.path.exists(STYLE_FILE):
        with open(STYLE_FILE) as f:
            return json.load(f).get("style_id")
    return None


def save_style_id(style_id: str) -> None:
    with open(STYLE_FILE, "w") as f:
        json.dump({"style_id": style_id}, f, indent=2)


def build_prompt(category: str, subject: str) -> str:
    prefix = CATEGORY_PREFIX.get(category, CATEGORY_PREFIX["Other"])
    return f"{prefix}{subject}, {WORLD_SUFFIX}"


def generate_image(prompt: str, style_id: Optional[str]) -> bytes:
    payload: dict = {
        "prompt":          prompt,
        "negative_prompt": NEGATIVE,
        "model":           MODEL,
        "style":           STYLE,
        "n":               1,
        "size":            IMAGE_SIZE,
        "response_format": "b64_json",
    }
    if style_id:
        payload["style_id"] = style_id
    result = api_request("POST", "images/generations", payload)
    return base64.b64decode(result["data"][0]["b64_json"])


def upload_style(raw_png: bytes) -> str:
    """POST raw illustration PNG to Recraft /styles, return style_id."""
    boundary = "----RecraftStyleBoundary"
    body = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="style"\r\n\r\n'
        f"{STYLE}\r\n"
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="file"; filename="reference.png"\r\n'
        f"Content-Type: image/png\r\n\r\n"
    ).encode() + raw_png + f"\r\n--{boundary}--\r\n".encode()

    req = urllib.request.Request(
        f"{BASE_URL}/styles", data=body, method="POST",
        headers={
            "Authorization": f"Bearer {API_KEY}",
            "Content-Type":  f"multipart/form-data; boundary={boundary}",
        },
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read())["id"]
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"Style upload HTTP {e.code}: {e.read().decode()}") from e

# ---------------------------------------------------------------------------
# POST-PROCESSING
# ---------------------------------------------------------------------------

def remove_background(image_bytes: bytes) -> bytes:
    if not REMBG_AVAILABLE:
        return image_bytes
    return rembg_remove(image_bytes)


def apply_phosphor(image_bytes: bytes) -> bytes:
    """Grayscale → Floyd-Steinberg dither → phosphor green colorize → pixel grid."""
    img = Image.open(io.BytesIO(image_bytes)).convert("RGBA")
    w, h = img.size
    r, g, b, alpha = img.split()
    gray = Image.merge("RGB", (r, g, b)).convert("L")

    try:
        dithered = gray.convert("1", dither=Image.Dither.FLOYDSTEINBERG).convert("L")
    except AttributeError:
        dithered = gray.convert("1", dither=Image.FLOYDSTEINBERG).convert("L")

    colorized = ImageOps.colorize(dithered, black=PHOSPHOR_LO, white=PHOSPHOR_HI)

    pw = max(1, w // PIXEL_SIZE)
    ph = max(1, h // PIXEL_SIZE)
    colorized = colorized.resize((pw, ph), Image.LANCZOS).resize((w, h), Image.NEAREST)
    alpha     = alpha    .resize((pw, ph), Image.LANCZOS).resize((w, h), Image.NEAREST)

    cr, cg, cb = colorized.split()
    out = io.BytesIO()
    Image.merge("RGBA", (cr, cg, cb, alpha)).save(out, format="PNG")
    return out.getvalue()


def post_process(image_bytes: bytes) -> bytes:
    image_bytes = remove_background(image_bytes)
    image_bytes = apply_phosphor(image_bytes)
    return image_bytes


def save_atomic(data: bytes, filepath: str) -> None:
    tmp = filepath + ".tmp"
    try:
        with open(tmp, "wb") as f:
            f.write(data)
        os.replace(tmp, filepath)
    except Exception:
        if os.path.exists(tmp):
            os.remove(tmp)
        raise

# ---------------------------------------------------------------------------
# STYLE SETUP
# ---------------------------------------------------------------------------

def make_style() -> None:
    print("=" * 54)
    print("  LAST KERNEL — Style Setup")
    print("  Generating reference card (Warrior)...")
    print("=" * 54)

    prompt = build_prompt(
        "Character",
        "corporate soldier dropping from an AV cargo door, "
        "slate grey modular exo-armour, cold blue AR visor, "
        "near-future assault rifle, aggressive descent pose, "
        "white corporate logo on chest plate",
    )
    raw = generate_image(prompt, style_id=None)

    os.makedirs(CARD_DIR, exist_ok=True)
    raw_path  = os.path.join(CARD_DIR, "_style_reference_raw.png")
    proc_path = os.path.join(CARD_DIR, "_style_reference.png")
    save_atomic(raw, raw_path)
    save_atomic(post_process(raw), proc_path)

    print(f"\n  Raw (with bg)  → {raw_path}")
    print(f"  Processed      → {proc_path}")
    print("  Check both. Raw = illustration quality. Processed = final card look.")

    if input("  Accept? [y/n]: ").strip().lower() != "y":
        print("  Rejected — delete previews and run --make-style again.")
        return

    print("  Uploading style to Recraft...")
    style_id = upload_style(raw)  # send raw illustration, not phosphor image
    save_style_id(style_id)
    print(f"  Style ID: {style_id}")
    print("  Now run: python generate_last_kernel_art.py")

# ---------------------------------------------------------------------------
# BATCH GENERATION
# ---------------------------------------------------------------------------

def process_batch(
    items: List[Tuple[str, str, str]],
    output_dir: str,
    start_index: int,
    total: int,
    style_id: Optional[str],
) -> Tuple[int, int, int]:
    generated = skipped = failed = 0
    for i, (name, category, subject) in enumerate(items, start=start_index):
        path = os.path.join(output_dir, f"{name}.png")
        if os.path.exists(path):
            print(f"  skip [{i:>3}/{total}] {name}")
            skipped += 1
            continue
        print(f"  gen  [{i:>3}/{total}] {name}...")
        prompt = build_prompt(category, subject)
        for attempt in range(MAX_RETRIES + 1):
            try:
                raw = generate_image(prompt, style_id)
                save_atomic(post_process(raw), path)
                print(f"       → {path}")
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


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--make-style", action="store_true")
    args = parser.parse_args()

    if not API_KEY:
        print("RECRAFT_API_KEY not set.  $env:RECRAFT_API_KEY='your-key'")
        sys.exit(1)

    if args.make_style:
        make_style()
        return

    style_id = load_style_id()
    total    = len(CARDS) + len(PACKS)

    print("=" * 54)
    print("  LAST KERNEL — Art Generator")
    print(f"  Model: {MODEL}  Style: {STYLE}")
    print(f"  Style ID: {style_id or 'none (run --make-style first)'}")
    print(f"  {len(CARDS)} cards + {len(PACKS)} packs = {total} total")
    print("=" * 54)

    os.makedirs(CARD_DIR, exist_ok=True)
    os.makedirs(PACK_DIR, exist_ok=True)

    cg, cs, cf = process_batch(CARDS, CARD_DIR, 1,                total, style_id)
    pg, ps, pf = process_batch(PACKS, PACK_DIR, len(CARDS) + 1,  total, style_id)

    print()
    print("=" * 54)
    print(f"  Generated : {cg + pg}  Skipped : {cs + ps}  Failed : {cf + pf}")
    print("=" * 54)
    if cf + pf:
        print("  Rerun to retry — existing files are skipped automatically.")


if __name__ == "__main__":
    main()
