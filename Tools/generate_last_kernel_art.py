"""
LAST KERNEL Card + Pack Art Generator
Uses ComfyUI (local Stable Diffusion) for consistent pixel art generation.

Prerequisites:
  1. ComfyUI running: double-click E:\\StableDiffusion\\start_comfyui.bat
  2. Wait for "To see the GUI go to: http://127.0.0.1:8188" in that window
  3. Then run this script from e:\\FORTSTACK\\Tools\\

Run:
    cd e:\\FORTSTACK\\Tools
    python generate_last_kernel_art.py

Output:
    CardArt\\*.png  — drop into Assets/_Project/Art/Sprites/CardArt/
    PackArt\\*.png  — drop into Assets/_Project/Art/Sprites/PackArt/

Background removal (rembg) automatically strips the white background,
giving true transparent PNGs for Unity's _OverlayTex shader slot.
"""

import json
import os
import time
import uuid
import urllib.request
import urllib.error
from typing import Dict, List, Tuple

# rembg is optional — install with: pip install rembg
try:
    from rembg import remove as rembg_remove
    from PIL import Image
    import io
    REMBG_AVAILABLE = True
except ImportError:
    REMBG_AVAILABLE = False


# ============================================================
# CONFIG
# ============================================================

COMFYUI_URL   = "http://127.0.0.1:8188"
WORKFLOW_FILE = os.path.join(os.path.dirname(__file__), "comfyui_workflow.json")

CARD_ART_DIR = "CardArt"
PACK_ART_DIR = "PackArt"

CHECKPOINT = "sd_xl_base_1.0.safetensors"
LORA       = "pixel-art-xl.safetensors"
LORA_STR   = 1.0   # LoRA strength (0.7–1.2 typical; start at 1.0)
STEPS      = 25
CFG        = 7.0
SAMPLER    = "dpmpp_2m"
SCHEDULER  = "karras"

MAX_RETRIES   = 2
POLL_INTERVAL = 3   # seconds between job status polls
JOB_TIMEOUT   = 300 # seconds before a job is considered stuck


# ============================================================
# PROMPTS
#
# SD prompt style: short, comma-separated tokens, emphasis via parens.
# Negative prompt is equally important — most consistency comes from here.
#
# Pixel art palette for LAST KERNEL (described, no hex codes):
#   dark navy outlines, electric cyan highlights, muted magenta accents,
#   warm amber, off-white lit surfaces.
# ============================================================

NEGATIVE = (
    "(blurry:1.4), (smooth shading:1.3), (3d render:1.4), (photorealistic:1.4), "
    "(painterly:1.2), (watercolor:1.2), (anime:1.2), "
    "(text:1.6), (letters:1.6), (numbers:1.6), (watermark:1.5), (signature:1.4), "
    "(multiple characters:1.5), (duplicate:1.5), (split panel:1.5), "
    "(color palette panel:1.6), (reference sheet:1.6), (sprite sheet:1.5), "
    "(background scenery:1.4), (environment:1.4), (room interior:1.3), "
    "(card frame:1.5), (border:1.4), (ui elements:1.5), (hud:1.5), "
    "(drop shadow:1.3), (glow halo:1.3), (lens flare:1.3), "
    "low quality, worst quality, jpeg artifacts"
)

# Per-category positive prefix — locked first so pose/form is always correct.
# SD reads left-to-right with higher weight on early tokens.
CATEGORY_PREFIX: Dict[str, str] = {

    "Character": (
        "(pixel art:1.5), (16-bit sprite:1.3), white background, "
        "single character, full body, facing right, "
        "combat ready stance, cyberpunk post-apocalyptic survivor, "
        "bold dark outline, flat cel shading, "
    ),
    "Mob": (
        "(pixel art:1.5), (16-bit sprite:1.3), white background, "
        "single creature, full body, facing front, "
        "aggressive threatening stance, post-apocalyptic mutant, "
        "bold dark outline, flat cel shading, "
    ),
    "Material": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single raw material object, floating centered, isometric tilt, "
        "bold chunky pixel shapes, bold dark outline, "
    ),
    "Consumable": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single food or container, floating centered, isometric tilt, "
        "bold readable pixel shapes, bold dark outline, "
    ),
    "Equipment": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single weapon or armor piece, floating centered, "
        "45 degree diagonal angle, bold chunky pixel shapes, bold dark outline, "
    ),
    "Structure": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single building or machine, isometric 3/4 view, "
        "front face and roof visible, chunky readable architecture, bold dark outline, "
    ),
    "Resource": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single natural object or plant, floating centered, "
        "bold chunky silhouette, bold dark outline, "
    ),
    "Area": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "two or three bold landmark silhouettes side by side, "
        "isolated shapes on white, bold dark outline, "
    ),
    "Currency": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single coin or chip, floating centered, isometric tilt, "
        "glinting highlight pixel on top face, bold dark outline, "
    ),
    "Valuable": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single rare artifact, floating centered, isometric tilt, "
        "striking color contrast implies rarity, bold dark outline, "
    ),
    "Recipe": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single rolled paper scroll, floating centered, "
        "curled ends clearly visible, bold dark outline, "
    ),
    "Pack": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single sealed supply crate or pack, floating centered, isometric tilt, "
        "bold chunky silhouette, bold dark outline, "
    ),
    "Other": (
        "(pixel art:1.5), (16-bit icon:1.3), white background, "
        "single object, floating centered, isometric tilt, "
        "bold chunky shapes, bold dark outline, "
    ),
}

STYLE_SUFFIX = (
    "dark navy outlines, electric cyan highlights, muted magenta accents, "
    "warm amber warm surfaces, off-white lit pixels, "
    "cyberpunk post-apocalyptic bunker world, worn industrial technology, "
    "professional polished pixel art, no background"
)


# ============================================================
# CARD DATA  —  (filename, category, subject description)
# ============================================================

CARDS: List[Tuple[str, str, str]] = [
    # ── Characters ───────────────────────────────────────────
    ("Villager",       "Character", "colonist survivor, patched bunker clothes, no helmet"),
    ("Warrior",        "Character", "armored enforcer, salvaged blade, heavy shoulder plates"),
    ("Mage",           "Character", "netrunner, glowing visor, cable arms"),
    ("Ranger",         "Character", "scout, lightweight armor, composite bow"),
    ("Baby",           "Character", "small infant, wrapped in thermal foil, wide eyes"),

    # ── Mobs ─────────────────────────────────────────────────
    ("Slime",          "Mob", "blobby acidic creature, glowing core, dripping"),
    ("Goblin",         "Mob", "wiry scavenger humanoid, large ears, clawed hands"),
    ("Satyr",          "Mob", "goat-legged mutant, bio-mechanical hind legs"),
    ("TrollShaman",    "Mob", "massive hunched troll, totem staff, glowing markings"),
    ("CrimsonAcolyte", "Mob", "robed cultist, glowing red chest markings"),
    ("DemonLord",      "Mob", "towering horned entity, dark energy in both fists"),
    ("Squirrel",       "Mob", "feral mutant squirrel, enlarged claws"),
    ("Chicken",        "Mob", "bio-bred mutant chicken, oversized, beady eyes"),
    ("Cow",            "Mob", "stocky bio-bred cow, metal ear tag"),
    ("Corpse",         "Mob", "fallen colonist body lying flat, hazard warning markers"),

    # ── Materials ─────────────────────────────────────────────
    ("Wood",           "Material", "rough-cut timber log, visible grain"),
    ("Stone",          "Material", "jagged rock chunk, flat facets"),
    ("Clay",           "Material", "block of raw reddish clay, fingerprint mark"),
    ("IronOre",        "Material", "rock with metallic iron veins"),
    ("IronIngot",      "Material", "rectangular smelted iron bar, stamped surface"),
    ("Plank",          "Material", "two stacked wooden boards, nailed at corner"),
    ("Brick",          "Material", "single fired clay brick, worn edges"),
    ("Timber",         "Material", "long structural beam, rough-hewn ends"),
    ("Flint",          "Material", "sharp angular flint shard, knapped edges"),
    ("Fiber",          "Material", "bundle of twisted fibers tied with string"),
    ("Rope",           "Material", "coiled rope loop, knotted end"),
    ("Soil",           "Material", "rounded mound of dark nutrient soil"),

    # ── Consumables ───────────────────────────────────────────
    ("Apple",          "Consumable", "round apple with leaf, oversized stem"),
    ("Berry",          "Consumable", "cluster of three round berries on a twig"),
    ("Potato",         "Consumable", "lumpy oval potato, sprout nubs"),
    ("BakedPotato",    "Consumable", "split baked potato wrapped in foil, steam"),
    ("Egg",            "Consumable", "single egg in a small wire tray"),
    ("RawMeat",        "Consumable", "raw protein slab, vacuum-sealed edge"),
    ("Steak",          "Consumable", "thick cooked steak on a metal mess tray"),
    ("Milk",           "Consumable", "sealed cylindrical flask, drop marking on side"),
    ("Milkshake",      "Consumable", "sealed canister with straw at top"),
    ("Soup",           "Consumable", "sealed thermal pouch with steam vent nozzle"),
    ("FruitSalad",     "Consumable", "sealed ration cup, fruit pieces visible through lid"),
    ("Omelette",       "Consumable", "folded omelette on a flat metal camp tray"),
    ("Coconut",        "Consumable", "coconut cracked in half, liquid inside"),
    ("Acorn",          "Consumable", "single acorn with cap, compact and round"),
    ("RoastedAcorn",   "Consumable", "three small roasted acorns in a tin bowl"),
    ("Turnip",         "Consumable", "round turnip, leafy top, roots at bottom"),

    # ── Equipment ─────────────────────────────────────────────
    ("Sword",          "Equipment", "straight single-edged salvaged blade, wrapped grip"),
    ("Bow",            "Equipment", "recurve composite bow, tech-wrapped limbs"),
    ("WoodenClub",     "Equipment", "thick wooden club, heavy rounded end"),
    ("WoodenStick",    "Equipment", "sharpened straight wooden staff, pointed tip"),
    ("Slingshot",      "Equipment", "Y-shaped slingshot, elastic band"),
    ("Staff",          "Equipment", "tall staff, bright glowing tech orb at top"),
    ("Quiver",         "Equipment", "cylindrical arrow quiver, three arrow flights visible"),
    ("Tunic",          "Equipment", "sleeveless vest, patch repairs, buckled straps"),
    ("LeatherArmor",   "Equipment", "chest plate of layered leather, shoulder rivets"),
    ("Chainmail",      "Equipment", "folded mesh shirt of interlocked metal rings"),
    ("SlimeHat",       "Equipment", "rounded helmet dripping with green slime"),
    ("VitalityAmulet", "Equipment", "round amulet, circuit pattern etched into face"),

    # ── Structures ────────────────────────────────────────────
    ("Sawmill",        "Structure", "compact sawmill, large circular blade, log feed"),
    ("Furnace",        "Structure", "squat box furnace, glowing orange front vent"),
    ("Kiln",           "Structure", "domed kiln, chimney vents, heat shimmer"),
    ("Farm",           "Structure", "hydroponic grow tray, seedling rows, UV strip light"),
    ("LoggingCamp",    "Structure", "felled-log processing station, chainsaw arm"),
    ("StoneQuarry",    "Structure", "drill rig on rock shelf, cut stone piles"),
    ("ClayPit",        "Structure", "shallow excavation pit, clay walls, shovel"),
    ("ClayQuarry",     "Structure", "mechanized clay dig, conveyor arm"),
    ("IronMine",       "Structure", "mine shaft entrance, timber supports, cart rail"),
    ("IronDeposit",    "Structure", "rock face with exposed iron veins"),
    ("CreaturePen",    "Structure", "reinforced pen, metal fence posts, gate latch"),
    ("CreatureCage",   "Structure", "portable metal cage, heavy padlock"),
    ("Warehouse",      "Structure", "flat-roofed storage depot, large sliding door"),
    ("Library",        "Structure", "data terminal kiosk, drive shelves, blinking lights"),
    ("House",          "Structure", "small prefab shelter module, bolted panel walls, hatch door"),
    ("Hearth",         "Structure", "compact heating unit, glowing coil element, fan vents"),
    ("Bonfire",        "Structure", "metal burn barrel, flames at top rim"),
    ("Anvil",          "Structure", "heavy flat-topped iron anvil, low block base"),
    ("Yard",           "Structure", "fenced compound, gate, perimeter posts"),

    # ── Resources / Areas ─────────────────────────────────────
    ("Forest",         "Area",     "three twisted mutant trees, glowing roots"),
    ("Grass",          "Resource", "patch of bioluminescent moss on cracked concrete"),
    ("Highlands",      "Area",     "rocky plateau silhouette, jagged peaks"),
    ("Ruins",          "Area",     "crumbled concrete pillars, rebar exposed, rubble"),
    ("Fields",         "Area",     "grid of cracked dry furrows, abandoned cropland"),
    ("Graveyard",      "Area",     "three grave mounds, simple cross markers, hazard flag"),
    ("Coral",          "Resource", "branching mutant coral formation"),
    ("BasaltColumns",  "Resource", "tight cluster of hexagonal basalt pillars"),
    ("AppleTree",      "Resource", "slender apple tree in grow tube, two apples"),
    ("BerryBush",      "Resource", "compact shrub, small round berries on branch tips"),
    ("Tree",           "Resource", "bioluminescent tree, glowing vein pattern on trunk"),
    ("PalmTree",       "Resource", "tall thin palm tree, splayed fronds at top"),

    # ── Valuables ─────────────────────────────────────────────
    ("GoldenKey",      "Valuable", "ornate key, abstract circuit etchings on bow"),
    ("TreasureChest",  "Valuable", "military supply crate, heavy clasps, glowing seal"),
    ("WoodenChest",    "Valuable", "small wooden box, metal latch, hinged lid"),
    ("GlowingDust",    "Valuable", "sealed vial of glowing bioluminescent dust"),
    ("BloodChalice",   "Valuable", "goblet with dark liquid, glowing rim"),
    ("AbyssalCore",    "Valuable", "jagged crystal shard, dark energy pulsing within"),
    ("SacrificialAltar","Valuable","flat stone block altar, power conduit cables"),
    ("GrandPortal",    "Valuable", "tall arched gateway frame, crackling energy between posts"),

    # ── Currency / Other ──────────────────────────────────────
    ("Coin",           "Currency", "round coin, abstract hexagon circuit pattern on face"),
    ("Sign",           "Other",    "rectangular rusted metal sign on post, blank face"),
    ("Grave",          "Other",    "single grave mound, crude flat marker stone"),
    ("Recipe",         "Recipe",   "curled paper scroll, abstract gear and arrow shapes"),
    ("Rock",           "Material", "small rounded loose rock"),
]

PACKS: List[Tuple[str, str, str]] = [
    ("Starter",       "Pack", "basic sealed survival kit pouch, star symbol"),
    ("Knowledge",     "Pack", "data chip slotted into open book spine"),
    ("Farmstead",     "Pack", "seed packet, sprout shape on front"),
    ("HeartyMeals",   "Pack", "ration pack, flame and bowl shape"),
    ("Blacksmith",    "Pack", "sealed tool pack, anvil shape stamped on it"),
    ("Revelations",   "Pack", "glowing sealed envelope, broken wax seal"),
    ("Beginning",     "Pack", "clean supply crate, sunrise chevron shape"),
    ("Adventure",     "Pack", "scout kit roll, compass shape"),
    ("Survival",      "Pack", "emergency supply pack, hazard stripe markings"),
    ("Island",        "Pack", "waterproof pack, wave and palm shape"),
    ("Construction",  "Pack", "heavy materials pack, I-beam and hammer shape"),
]


# ============================================================
# COMFYUI API
# ============================================================

def comfyui_get(endpoint: str) -> dict:
    req = urllib.request.Request(f"{COMFYUI_URL}/{endpoint}")
    with urllib.request.urlopen(req, timeout=10) as resp:
        return json.loads(resp.read())


def comfyui_post(endpoint: str, data: dict) -> dict:
    body = json.dumps(data).encode("utf-8")
    req = urllib.request.Request(
        f"{COMFYUI_URL}/{endpoint}",
        data=body,
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(req, timeout=10) as resp:
        return json.loads(resp.read())


def check_comfyui() -> bool:
    try:
        comfyui_get("system_stats")
        return True
    except Exception:
        return False


def build_workflow(positive: str, seed: int) -> dict:
    with open(WORKFLOW_FILE, "r") as f:
        wf = json.load(f)

    # Patch checkpoint + LoRA via a LoraLoader node inserted between checkpoint and sampler
    # Node "1" = CheckpointLoaderSimple  → feeds model+clip into LoraLoader
    # Node "L" = LoraLoader              → feeds patched model+clip forward
    wf["1"]["inputs"]["ckpt_name"] = CHECKPOINT

    # Insert LoRA loader as node "L"
    wf["L"] = {
        "inputs": {
            "lora_name":      LORA,
            "strength_model": LORA_STR,
            "strength_clip":  LORA_STR,
            "model":          ["1", 0],
            "clip":           ["1", 1],
        },
        "class_type": "LoraLoader",
    }

    # Rewire sampler to use LoRA model output (node L, slot 0)
    wf["5"]["inputs"]["model"] = ["L", 0]
    wf["5"]["inputs"]["seed"]  = seed

    # Rewire CLIP encoders to use LoRA clip output (node L, slot 1)
    wf["3"]["inputs"]["clip"] = ["L", 1]
    wf["4"]["inputs"]["clip"] = ["L", 1]

    wf["3"]["inputs"]["text"] = positive
    wf["4"]["inputs"]["text"] = NEGATIVE

    return wf


def submit_job(workflow: dict) -> str:
    client_id = str(uuid.uuid4())
    result = comfyui_post("prompt", {"prompt": workflow, "client_id": client_id})
    return result["prompt_id"]


def wait_for_job(prompt_id: str) -> dict:
    deadline = time.time() + JOB_TIMEOUT
    while time.time() < deadline:
        history = comfyui_get(f"history/{prompt_id}")
        if prompt_id in history:
            job = history[prompt_id]
            if job.get("status", {}).get("completed"):
                return job
            if job.get("status", {}).get("status_messages"):
                for msg in job["status"]["status_messages"]:
                    if msg[0] == "execution_error":
                        raise RuntimeError(f"ComfyUI error: {msg[1]}")
        time.sleep(POLL_INTERVAL)
    raise TimeoutError(f"Job {prompt_id} did not complete in {JOB_TIMEOUT}s")


def download_result(job: dict, filepath: str) -> None:
    # Find SaveImage node output (node "7")
    images = job["outputs"]["7"]["images"]
    img_info = images[0]

    filename  = img_info["filename"]
    subfolder = img_info.get("subfolder", "")
    img_type  = img_info.get("type", "output")

    url = (f"{COMFYUI_URL}/view"
           f"?filename={urllib.parse.quote(filename)}"
           f"&subfolder={urllib.parse.quote(subfolder)}"
           f"&type={img_type}")

    tmp = filepath + ".tmp"
    try:
        req = urllib.request.Request(url)
        with urllib.request.urlopen(req, timeout=60) as resp:
            image_bytes = resp.read()

        if REMBG_AVAILABLE:
            # Strip white background → true transparent PNG
            output_bytes = rembg_remove(image_bytes)
        else:
            output_bytes = image_bytes

        with open(tmp, "wb") as f:
            f.write(output_bytes)
        os.replace(tmp, filepath)
    except Exception:
        if os.path.exists(tmp):
            os.remove(tmp)
        raise


def build_positive(category: str, subject: str) -> str:
    if category not in CATEGORY_PREFIX:
        print(f"  WARN unknown category '{category}' — using 'Other'")
    prefix = CATEGORY_PREFIX.get(category, CATEGORY_PREFIX["Other"])
    return f"{prefix}{subject}, {STYLE_SUFFIX}"


# ============================================================
# BATCH PROCESSING
# ============================================================

def process_batch(
    items: List[Tuple[str, str, str]],
    output_dir: str,
    start_index: int,
    total: int,
    base_seed: int,
) -> Tuple[int, int, int]:
    generated = skipped = failed = 0

    for i, (name, category, subject) in enumerate(items, start=start_index):
        filepath = os.path.join(output_dir, f"{name}.png")

        if os.path.exists(filepath):
            print(f"  skip [{i:>3}/{total}] {name}")
            skipped += 1
            continue

        print(f"  gen  [{i:>3}/{total}] {name}...")
        positive = build_positive(category, subject)
        seed = base_seed + i

        for attempt in range(MAX_RETRIES + 1):
            try:
                workflow   = build_workflow(positive, seed)
                prompt_id  = submit_job(workflow)
                job        = wait_for_job(prompt_id)
                download_result(job, filepath)
                rembg_tag = " (transparent)" if REMBG_AVAILABLE else ""
                print(f"       saved{rembg_tag} → {filepath}")
                generated += 1
                break
            except Exception as e:
                if attempt < MAX_RETRIES:
                    print(f"       retry {attempt + 1}: {e}")
                    time.sleep(5)
                else:
                    print(f"       FAILED: {e}")
                    failed += 1

    return generated, skipped, failed


# ============================================================
# MAIN
# ============================================================

def main() -> None:
    import urllib.parse   # needed for URL encoding in download_result

    # Inject urllib.parse into module scope for download_result
    globals()["urllib"] = urllib

    print("=" * 52)
    print("  LAST KERNEL — Art Generator (ComfyUI/SD)")
    print(f"  Model:    {CHECKPOINT}")
    print(f"  LoRA:     {LORA}  (str={LORA_STR})")
    print(f"  rembg:    {'YES — transparent PNG output' if REMBG_AVAILABLE else 'NO — white background (pip install rembg)'}")
    print(f"  Cards:    {len(CARDS)}  |  Packs: {len(PACKS)}  |  Total: {len(CARDS)+len(PACKS)}")
    print("=" * 52)

    if not check_comfyui():
        print()
        print("  ERROR: ComfyUI is not running.")
        print("  Start it first: double-click E:\\StableDiffusion\\start_comfyui.bat")
        print("  Wait until you see 'To see the GUI go to: http://127.0.0.1:8188'")
        return

    print("  ComfyUI online. Starting generation...\n")

    os.makedirs(CARD_ART_DIR, exist_ok=True)
    os.makedirs(PACK_ART_DIR, exist_ok=True)

    base_seed = 42
    total = len(CARDS) + len(PACKS)

    cg, cs, cf = process_batch(CARDS, CARD_ART_DIR, 1, total, base_seed)
    pg, ps, pf = process_batch(PACKS, PACK_ART_DIR, len(CARDS) + 1, total, base_seed)

    print()
    print("=" * 52)
    print(f"  Generated : {cg + pg}")
    print(f"  Skipped   : {cs + ps}")
    print(f"  Failed    : {cf + pf}")
    print("=" * 52)
    if cf + pf:
        print("  Rerun to retry failed items — existing files are skipped.")
    if not REMBG_AVAILABLE:
        print("  TIP: pip install rembg for automatic transparent PNG output.")


if __name__ == "__main__":
    main()
