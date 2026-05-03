"""
LAST KERNEL — Card & Pack Art Generator
Recraft AI (recraftv3 / digital_illustration) → transparent PNG.

Setup:
    1. recraft.ai → Account → API → create key
    2. $env:RECRAFT_API_KEY = "your-key"
    3. pip install "rembg[cpu]" Pillow

Workflow:
    Tools/run_art.ps1                   # generate all (skips existing)
    Tools/run_art.ps1 --make-style      # preview reference, approve, regenerate all
    Tools/run_art.ps1 --regen Warrior   # regenerate one card

Output (transparent PNG):
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

from PIL import Image, ImageEnhance, ImageFilter

try:
    from rembg import remove as rembg_remove, new_session
    _rembg_session = new_session("isnet-general-use")
    REMBG_AVAILABLE = True
except ImportError:
    REMBG_AVAILABLE = False

# ---------------------------------------------------------------------------
# CONFIG
# ---------------------------------------------------------------------------

API_KEY    = os.getenv("RECRAFT_API_KEY", "")
BASE_URL   = "https://external.api.recraft.ai/v1"
STYLE_FILE = os.path.join(os.path.dirname(__file__), "recraft_style.json")

_TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
CARD_DIR   = os.path.join(_TOOLS_DIR, "CardArt")
PACK_DIR   = os.path.join(_TOOLS_DIR, "PackArt")

MODEL      = "recraftv3"
STYLE      = "digital_illustration"
IMAGE_SIZE = "1024x1024"

MAX_RETRIES = 2
RETRY_DELAY = 10

PIXEL_BLOCK    = 8   # 1024/8 = 128 logical pixels
OUTLINE_WIDTH  = 12  # black stroke around silhouette
PALETTE_COLORS = 24  # colors after enhancement, before pixelation

# ---------------------------------------------------------------------------
# PROMPTS
# ---------------------------------------------------------------------------

NEGATIVE = (
    "text, watermark, border, card frame, UI, "
    "multiple characters, crowd, busy background, "
    "photorealistic, 3d render, muted colors, washed out, "
    "thin lines, sketchy, blurry, noisy, cropped, cut off, "
    "copyrighted character, real brand logo, pop culture reference, "
    "superhero, anime character, movie character, mascot, trademark symbol"
)

CATEGORY_PREFIX: Dict[str, str] = {
    "Character":  "card game character portrait, single figure facing left, three-quarter left view, expressive face, fills frame, consistent bust framing, ",
    "Mob":        "card game creature portrait, single beast facing right, three-quarter right view, menacing expression, fills frame, consistent bust framing, ",
    "Material":   "card game item art, single object centered, vivid colors, bold graphic, ",
    "Consumable": "card game item art, single food or drink centered, vivid colors, bold graphic, ",
    "Equipment":  "card game item art, single weapon or armor centered, dynamic angle, bold graphic, ",
    "Structure":  "card game building art, single structure, isometric view, bold graphic, ",
    "Resource":   "card game nature art, single plant or formation centered, bold graphic, ",
    "Area":       "card game location art, dramatic scene silhouette, bold graphic, ",
    "Currency":   "card game item art, single coin or token centered, vivid colors, bold graphic, ",
    "Valuable":   "card game artifact art, single item centered, glowing, bold graphic, ",
    "Recipe":     "card game item art, single scroll or chip centered, bold graphic, ",
    "Pack":       "card game item art, single supply crate centered, bold graphic, ",
    "Other":      "card game item art, single object centered, bold graphic, ",
}

WORLD_SUFFIX = (
    "cyberpunk post-apocalyptic, bold cel-shaded illustration, "
    "thick black outline, ultra vivid saturated colors, high contrast, "
    "bright neon accents on dark forms, each color clearly distinct, "
    "expressive graphic card game art, readable at small size, Balatro-inspired"
)

# ---------------------------------------------------------------------------
# CARD DATA  (filename, category, subject)
# ---------------------------------------------------------------------------

CARDS: List[Tuple[str, str, str]] = [
    # ── Characters ──────────────────────────────────────────────────────────
    ("Baby",            "Character", "Dependent — fragile infant in a survival pod, tiny body, monitoring cables taped to chest"),
    ("Conscript",       "Character", "Conscript — emergency-drafted street fighter, makeshift mismatched gear, desperate determined face"),
    ("Mage",            "Character", "Netrunner — woman hardwired into signal streams, glowing neural ports in neck, AR visor cracked"),
    ("Ranger",          "Character", "Scavenger — lone survivor, deep scars, hooded recon armor, empty hands, past-the-perimeter stare"),
    ("Villager",        "Character", "Drifter — no sector ID, no record, weathered hollow face, still breathing somehow"),
    ("Warrior",         "Character", "Enforcer — ex-cop, heavy bearing, badge gone, muscle memory intact, cold eyes"),
    # ── Mobs ────────────────────────────────────────────────────────────────
    ("Chicken",         "Mob", "Protein Drone — docile food unit, more firmware than instinct, blank eyes, lays protein pods"),
    ("Cow",             "Mob", "Nutrient Synth — bulky nutrient machine dressed in animal logic, industrial milk port on flank"),
    ("Crab",            "Mob", "Shell Crawler — armored hard-shell creature, difficult to crack, worth the effort"),
    ("CrimsonAcolyte",  "Mob", "Blackout Cultist — robed figure wired into broken doctrine, chest cracked open with glowing red core"),
    ("DemonLord",       "Mob", "Kernel Sovereign — wasn't born, was left running, towering entity with dark energy in both fists"),
    ("Goblin",          "Mob", "Scrapper — hungry and fast, cracked goggles, clawed hands, not wrong about how the world works now"),
    ("Satyr",           "Mob", "Reclaimer — strips living districts for parts, bio-mechanical legs, sensor-fitted horns"),
    ("Slime",           "Mob", "Null Anomaly — crawling mass of nanite failure, dissolves clean edges into static, glowing core"),
    ("Squirrel",        "Mob", "Cache Runner — been in your stores, been in everyone's stores, steel-reinforced claws"),
    ("TrollShaman",     "Mob", "Process Echo — corrupted pattern still running rituals it no longer understands, bone totem wrapped in wires"),
    # ── Materials ───────────────────────────────────────────────────────────
    ("AbyssalCore",     "Material", "Void Cell — shouldn't exist but does, jagged dark crystal shard, dark energy pulsing within"),
    ("Brick",           "Material", "Fired Block — slag heat time in that order, single fired clay brick"),
    ("Clay",            "Material", "Ceramic Slurry — from broken pipes and filter tanks, moldable grey-red block"),
    ("Corpse",          "Material", "Shutdown Unit — someone's colony lost a drifter, your board gained one, armored body face-up"),
    ("DataShard",       "Material", "Data Shard — crystallized signal fragments from derelict broadcast nodes, information ore"),
    ("Fiber",           "Material", "Wirecloth — weak alone, the problem is it's never alone, twisted synthetic bundle"),
    ("Flint",           "Material", "Striker Chip — one chip one spark that's all fire asks, sharp-knapped flint shard"),
    ("GlowingDust",     "Material", "Signal Dust — nobody knows what it is, everyone knows what it does, sealed glowing vial"),
    ("IronIngot",       "Material", "Alloy Ingot — refined metal ready for fabrication, rectangular stamped bar"),
    ("IronOre",         "Material", "Ferrite Vein — pull it from the ground, prove it's worth something, raw ore chunk"),
    ("KernelShard",     "Material", "Kernel Shard — fragment of the original colony process, every faction wants it, crackling sliver"),
    ("Plank",           "Material", "Deck Board — two cuts from timber, one step from something useful, stacked planks"),
    ("Rope",            "Material", "Tension Cord — either holds or it doesn't, you find out at the worst time, coiled cord"),
    ("Stone",           "Material", "Slag Block — heavy enough to stay where you put it, grey hewn block"),
    ("Timber",          "Material", "Load Bearer — the colony stands because something holds it up, structural beam"),
    ("Wood",            "Material", "Salvage Timber — structural not elegant available, stripped log with visible grain"),
    ("WoodenStick",     "Material", "Scrap Rod — the most honest object in the district, sharpened straight rod"),
    # ── Consumables ─────────────────────────────────────────────────────────
    ("Acorn",           "Consumable", "Volt Nut — small energy seed from cracked hydro rack, better toasted than raw"),
    ("AlgaeWafer",      "Consumable", "Vat Plating — pressed algae matting from hydroponic vats, grey-green slab, barely food"),
    ("Apple",           "Consumable", "Synth Apple — printed from old orchard templates, unnaturally perfect surface, small comfort"),
    ("BakedPotato",     "Consumable", "Heated Starch — hot block of dense carbohydrates, keeps workers upright, not elegant"),
    ("Berry",           "Consumable", "Ration Pack — sealed calories from a supply cache, basic fuel for another shift"),
    ("Coconut",         "Consumable", "Coolant Pod — sealed fluid pod from flooded utility bays, drinkable after filtering"),
    ("CoreRation",      "Consumable", "Core Ration — high-density paste from compressed organics, no flavor no texture full caloric load"),
    ("Egg",             "Consumable", "Protein Pod — lab-grown protein in brittle membrane, scan barcode printed on shell"),
    ("FruitSalad",      "Consumable", "Mixed Rations — compact meal from clean produce, clear sealed cup, efficient and stackable"),
    ("Milk",            "Consumable", "Nutrient Milk — cultured feedstock, pressurized cylindrical ration flask, mineral label"),
    ("Milkshake",       "Consumable", "Med Gel — cold recovery slurry for shock and burns, sealed canister with medical markings"),
    ("MycoChip",        "Consumable", "Myco Chip — dried fungal growth from ventilation shafts, dark dense disc, the vents provide"),
    ("Omelette",        "Consumable", "Protein Fold — cooked protein fold on metal camp tray, scorched edges, barely food"),
    ("Potato",          "Consumable", "Starch Root — hardy hydroponic tuber, grown in dirty nutrient trays, knobbled surface"),
    ("RawMeat",         "Consumable", "Raw Meat — protein slab in vacuum-sealed tray, blood pooled at edge"),
    ("RoastedAcorn",    "Consumable", "Toasted Volt Nuts — roasted energy seeds in dented tin bowl, chemical char marks"),
    ("SignalJerky",     "Consumable", "Dried Protein Strip — printed protein cured with mineral salts, shelf-stable dense"),
    ("Soup",            "Consumable", "District Broth — heat-sealed ration pouch, protein scraps in hot water, pull-tab vent"),
    ("Steak",           "Consumable", "Searpack — pressed protein flash-seared on hot plate, good morale per gram, mess tray"),
    ("Stew",            "Consumable", "Reactor Stew — dense communal pot cooked beside waste heat, slow heavy sustaining"),
    ("Turnip",          "Consumable", "Root Ration — mineral-rich root from low-power hydroponics, bulbous with faint veins"),
    ("VatBroth",        "Consumable", "Vat Broth — protein-enriched broth from mixed organics in communal vats, hot barely"),
    # ── Equipment ───────────────────────────────────────────────────────────
    ("Bow",             "Equipment", "Tension Rig — distance is a strategy, so is not missing, recurve composite bow, taut string"),
    ("Cane",            "Equipment", "Signal Cane — subtle surgical expensive, sleek reinforced cane with hidden tech"),
    ("Chainmail",       "Equipment", "Link Coat — heavier than it looks, worth it, folded mesh of interlocked riveted rings"),
    ("CombatChip",      "Equipment", "Combat Implant — neural combat chip, overclocks aggression circuit, burns out after extended use"),
    ("LeatherArmor",    "Equipment", "Reclaimed Vest — assembled from what the others left behind, layered salvaged leather plate"),
    ("Quiver",          "Equipment", "Bolt Cache — more shots, simple math, cylindrical quiver with arrow flights fanned at top"),
    ("SlimeHat",        "Equipment", "Null Cap — doesn't stop hits, stops being found, helmet dripping viscous green slime"),
    ("Slingshot",       "Equipment", "Pocket Launcher — underestimated, good, Y-frame slingshot with reinforced fork"),
    ("Staff",           "Equipment", "Conduit Rod — amplifies what's already there, not always helpfully, glowing tech orb at crown"),
    ("SurgeWeapon",     "Equipment", "Surge Blade — magnetically overcharged, three pulses before capacitors burn out"),
    ("Sword",           "Equipment", "Alloy Cutter — heavy cutting blade for close work in narrow streets, notched salvaged edge"),
    ("Tunic",           "Equipment", "Wirecloth Wrap — not armor, better than nothing by exactly that much, patched vest"),
    ("VitalityAmulet",  "Equipment", "Vital Cell — tracks your heartbeat so you don't have to think about it, circuit-etched amulet"),
    ("WoodenClub",      "Equipment", "Scrap Baton — the first weapon, still works, thick hardwood club with grip tape"),
    # ── Structures ──────────────────────────────────────────────────────────
    ("Anvil",                "Structure", "heavy flat-topped iron anvil on block base, forge station"),
    ("Booster_Warehouse",    "Structure", "Cargo Depot — storage hub, flat-roof depot with large rolling door"),
    ("Booster_Yard",         "Structure", "Logistics Yard — cleared sorting yard for bulky salvage, fenced compound with gate"),
    ("Enclosure_CreatureCage","Structure","Drone Cage — tight containment frame for damaged drones and hostile small units"),
    ("Enclosure_CreaturePen","Structure", "Containment Pen — fenced holding zone that keeps problems measurable, reinforced posts"),
    ("Furnace",              "Structure", "squat box furnace, glowing orange front vent, flue pipe rising"),
    ("Grower_Farm",          "Structure", "Hydroponic Farm — scaled food system, UV grow trays, power-hungry worth it"),
    ("Grower_PlanterBox",    "Structure", "Nutrient Tray — small clean growth tray, simple stackable reliable"),
    ("House",                "Structure", "prefab shelter module, bolted panel walls, single exhaust vent on roof"),
    ("Library",              "Structure", "data terminal kiosk, drive rack shelves behind glass, blinking status lights"),
    ("LoggingCamp",          "Structure", "Recycler Yard — work yard stripping scrap into usable stock, chainsaw arm extended"),
    ("Market",               "Structure", "district market stall, corrugated awning, crates and bartered goods stacked"),
    ("Sawmill",              "Structure", "Cutter Frame — cutting rig turning scrap stock into clean plates, circular blade"),
    ("StoneQuarry",          "Structure", "Rubble Extractor — crusher turning city rubble into circuit-grade salvage, drill rig"),
    # ── Areas ───────────────────────────────────────────────────────────────
    ("Fields",     "Area", "Blackout Yard — open work yard under dead billboards, collapsed scaffolding, poor cover"),
    ("Forest",     "Area", "Relay Graveyard — signal relay towers broken, antenna arrays tangled, cables everywhere"),
    ("Graveyard",  "Area", "Memory Pit — server stacks used as grave markers, ghost signal trails, some entries still answering"),
    ("Highlands",  "Area", "Dead Transit Line — elevated rail corridor above district, exposed walkway, rusted supports"),
    ("Ruins",      "Area", "Collapse Site — crumbled concrete district, buried systems, hostile code residue, rebar exposed"),
    # ── Resources ───────────────────────────────────────────────────────────
    ("AppleTree",      "Resource", "Orchard Node — tagged by old sector authority, still bearing fruit, grow tube encased"),
    ("BasaltColumns",  "Resource", "Basalt Stack — geometric immovable hexagonal pillars, sheared flat tops"),
    ("BerryBush",      "Resource", "Cluster Vine — nobody planted it, nobody needs to, glowing berry clusters on tangled vine"),
    ("ClayPit",        "Resource", "Clay Bed — everything below this was once a lake, open excavation, clay walls"),
    ("Coral",          "Resource", "Reef Token source — island-sector coral formation, branching bioluminescent tips"),
    ("Grass",          "Resource", "Ground Cover — not useful, not nothing, bioluminescent moss on cracked concrete"),
    ("IronDeposit",    "Resource", "Ore Seam — mine sees it first smelter sees it last, exposed iron vein in rock face"),
    ("PalmTree",       "Resource", "Canopy Node — survived the collapse, take what it offers, tall palm with fiber wrapping"),
    ("Rock",           "Resource", "Stone Deposit — slower than ore, free, that's the tradeoff, rubble chunk"),
    ("Soil",           "Resource", "Growth Medium — the only input that doesn't ask what you're building, dark nutrient soil"),
    ("Tree",           "Resource", "Salvage Grove — rooted in old concrete, still growing, bioluminescent vein pattern on trunk"),
    # ── Valuables ───────────────────────────────────────────────────────────
    ("BloodChalice",    "Valuable", "dark goblet with viscous liquid, rim pulsing red glow, ritual object"),
    ("GoldenKey",       "Valuable", "Root Keycard — privileged access card for sealed systems, circuit-etched shaft, glowing"),
    ("SacrificialAltar","Valuable", "Blacksite Conduit — forbidden relay trading flesh data and power, stone altar with conduit cables"),
    ("TreasureChest",   "Valuable", "Encrypted Cache — sealed crate from before the blackout, heavy clasps, glowing tamper seal"),
    # ── Currency / Recipe ────────────────────────────────────────────────────
    ("Coin",   "Currency", "Scrap Chip — district's preferred medium, untraceable by design, worn hexagonal token"),
    ("Recipe", "Recipe",   "Blueprint Packet — compressed build plan, data scroll with circuit and gear shapes printed"),
]

PACKS: List[Tuple[str, str, str]] = [
    ("Starter",      "Pack", "basic sealed survival kit pouch with star emblem on flap"),
    ("Knowledge",    "Pack", "data chip docked into cracked open book spine, signal light emitting"),
    ("Farmstead",    "Pack", "seed packet with sprout illustration stamped on front"),
    ("HeartyMeals",  "Pack", "ration pack with flame and bowl icon heat-pressed on surface"),
    ("Blacksmith",   "Pack", "heavy sealed tool pack with anvil silhouette embossed on front"),
    ("Revelations",  "Pack", "glowing sealed envelope, wax seal cracked open, signal light spilling"),
    ("Beginning",    "Pack", "clean supply crate with sunrise chevron stenciled on lid"),
    ("Adventure",    "Pack", "scout kit roll with compass rose emblem stitched on cover"),
    ("Survival",     "Pack", "emergency supply pack with hazard stripe tape across seams"),
    ("Island",       "Pack", "waterproof dry bag with wave and palm icon printed on side"),
    ("Construction", "Pack", "heavy materials pack with I-beam and hammer icon stenciled on face"),
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
            return json.load(f).get("style_id") or None
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
        "n":               1,
        "size":            IMAGE_SIZE,
        "response_format": "b64_json",
    }
    if style_id:
        payload["style_id"] = style_id
    else:
        payload["style"] = STYLE
    result = api_request("POST", "images/generations", payload)
    return base64.b64decode(result["data"][0]["b64_json"])

# ---------------------------------------------------------------------------
# POST-PROCESSING
# ---------------------------------------------------------------------------

def remove_background(image_bytes: bytes) -> bytes:
    if not REMBG_AVAILABLE:
        return image_bytes
    return rembg_remove(image_bytes, session=_rembg_session)


def fit_to_canvas(image_bytes: bytes, padding: float = 0.06) -> bytes:
    """Crop to opaque bbox, add uniform padding, scale back to original canvas size."""
    img  = Image.open(io.BytesIO(image_bytes)).convert("RGBA")
    bbox = img.getbbox()
    if bbox is None:
        return image_bytes
    w, h = img.size
    x0, y0, x1, y1 = bbox
    pad_x = int((x1 - x0) * padding)
    pad_y = int((y1 - y0) * padding)
    x0, y0 = max(0, x0 - pad_x), max(0, y0 - pad_y)
    x1, y1 = min(w, x1 + pad_x), min(h, y1 + pad_y)
    cropped = img.crop((x0, y0, x1, y1))
    scale   = min((w - 2 * pad_x) / cropped.width, (h - 2 * pad_y) / cropped.height)
    nw, nh  = int(cropped.width * scale), int(cropped.height * scale)
    resized = cropped.resize((nw, nh), Image.LANCZOS)
    canvas  = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.paste(resized, ((w - nw) // 2, (h - nh) // 2), resized)
    out = io.BytesIO()
    canvas.save(out, format="PNG")
    return out.getvalue()


def enhance_colors(image_bytes: bytes) -> bytes:
    img = Image.open(io.BytesIO(image_bytes)).convert("RGBA")
    r, g, b, a = img.split()
    rgb = Image.merge("RGB", (r, g, b))
    rgb = ImageEnhance.Color(rgb).enhance(1.8)
    rgb = ImageEnhance.Contrast(rgb).enhance(1.3)
    qr, qg, qb = rgb.split()
    result = Image.merge("RGBA", (qr, qg, qb, a))
    out = io.BytesIO()
    result.save(out, format="PNG")
    return out.getvalue()


def quantize_palette(image_bytes: bytes) -> bytes:
    img = Image.open(io.BytesIO(image_bytes)).convert("RGBA")
    r, g, b, a = img.split()
    rgb       = Image.merge("RGB", (r, g, b))
    quantized = rgb.quantize(colors=PALETTE_COLORS, method=Image.Quantize.MEDIANCUT).convert("RGB")
    qr, qg, qb = quantized.split()
    result = Image.merge("RGBA", (qr, qg, qb, a))
    out = io.BytesIO()
    result.save(out, format="PNG")
    return out.getvalue()


def pixelate(image_bytes: bytes) -> bytes:
    img    = Image.open(io.BytesIO(image_bytes)).convert("RGBA")
    w, h   = img.size
    small  = img.resize((max(1, w // PIXEL_BLOCK), max(1, h // PIXEL_BLOCK)), Image.BOX)
    result = small.resize((w, h), Image.NEAREST)
    out = io.BytesIO()
    result.save(out, format="PNG")
    return out.getvalue()


def add_outline(image_bytes: bytes) -> bytes:
    img = Image.open(io.BytesIO(image_bytes)).convert("RGBA")
    _, _, _, a  = img.split()
    dilated     = a.filter(ImageFilter.MaxFilter(OUTLINE_WIDTH * 2 + 1))
    black_layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    black_layer.paste(Image.new("RGBA", img.size, (0, 0, 0, 255)), mask=dilated)
    black_layer.paste(img, mask=a)
    out = io.BytesIO()
    black_layer.save(out, format="PNG")
    return out.getvalue()


def post_process(image_bytes: bytes) -> bytes:
    image_bytes = remove_background(image_bytes)
    image_bytes = fit_to_canvas(image_bytes)
    image_bytes = enhance_colors(image_bytes)
    image_bytes = quantize_palette(image_bytes)
    image_bytes = pixelate(image_bytes)
    image_bytes = add_outline(image_bytes)
    image_bytes = fit_to_canvas(image_bytes, padding=0.04)  # pixel-perfect final frame
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
# STYLE REFERENCE
# ---------------------------------------------------------------------------

def clear_art_dirs() -> None:
    for d in (CARD_DIR, PACK_DIR):
        if not os.path.isdir(d):
            continue
        for f in os.listdir(d):
            if f.endswith(".png") and not f.startswith("_"):
                os.remove(os.path.join(d, f))
    print("  Cleared existing card art.")


def make_style() -> bool:
    print("=" * 54)
    print("  LAST KERNEL — Style Reference")
    print("  Generating reference card (Enforcer)...")
    print("=" * 54)

    prompt = build_prompt(
        "Character",
        "Enforcer — armored cyberpunk soldier, glowing visor, cold confident expression, original design",
    )
    raw       = generate_image(prompt, style_id=None)
    os.makedirs(CARD_DIR, exist_ok=True)
    raw_path  = os.path.join(CARD_DIR, "_style_reference_raw.png")
    proc_path = os.path.join(CARD_DIR, "_style_reference.png")
    save_atomic(raw, raw_path)
    save_atomic(post_process(raw), proc_path)

    os.startfile(os.path.abspath(proc_path))
    print(f"\n  Preview → {os.path.abspath(proc_path)}")
    print(f"  Raw     → {os.path.abspath(raw_path)}")

    if input("\n  Accept? [y/n]: ").strip().lower() != "y":
        print("  Rejected. Tweak prompts and rerun --make-style.")
        return False

    clear_art_dirs()
    return True

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
    parser.add_argument("--make-style", action="store_true",
                        help="Generate reference, approve, then regenerate all art")
    parser.add_argument("--regen", metavar="NAME",
                        help="Delete and regenerate a single card by name")
    args = parser.parse_args()

    if not API_KEY:
        print("RECRAFT_API_KEY not set.  $env:RECRAFT_API_KEY='your-key'")
        sys.exit(1)

    style_id = load_style_id()
    total    = len(CARDS) + len(PACKS)

    if args.make_style:
        if not make_style():
            return

    if args.regen:
        name      = args.regen
        all_items = CARDS + PACKS
        match     = next((it for it in all_items if it[0] == name), None)
        if not match:
            print(f"Unknown card: {name}. Valid names: {[it[0] for it in all_items]}")
            sys.exit(1)
        d    = CARD_DIR if match in CARDS else PACK_DIR
        path = os.path.join(d, f"{name}.png")
        if os.path.exists(path):
            os.remove(path)
        os.makedirs(d, exist_ok=True)
        process_batch([match], d, 1, 1, style_id)
        return

    print("=" * 54)
    print("  LAST KERNEL — Art Generator")
    print(f"  Model: {MODEL}  Style: {STYLE}")
    print(f"  Style ID: {style_id or 'none'}")
    print(f"  {len(CARDS)} cards + {len(PACKS)} packs = {total} total")
    print("=" * 54)

    os.makedirs(CARD_DIR, exist_ok=True)
    os.makedirs(PACK_DIR, exist_ok=True)

    cg, cs, cf = process_batch(CARDS, CARD_DIR, 1,              total, style_id)
    pg, ps, pf = process_batch(PACKS, PACK_DIR, len(CARDS) + 1, total, style_id)

    print()
    print("=" * 54)
    print(f"  Generated : {cg + pg}  Skipped : {cs + ps}  Failed : {cf + pf}")
    print("=" * 54)
    if cf + pf:
        print("  Rerun to retry — existing files are skipped automatically.")


if __name__ == "__main__":
    main()
