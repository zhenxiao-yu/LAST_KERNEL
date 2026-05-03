#!/usr/bin/env python3
"""
Assigns Pixeltier Cyberpunk RPG Icon Pack sprites to card ScriptableObjects.
Only targets cards that are using the shared placeholder art (Conscript GUID).
Run from any directory — uses paths relative to this script file.
"""

import os
import re

_TOOLS_DIR  = os.path.dirname(os.path.abspath(__file__))
_ASSETS_DIR = os.path.join(_TOOLS_DIR, "..", "Assets")
PACK_DIR    = os.path.join(_ASSETS_DIR, "ThirdParty", "Pixeltiers_Cyberpunk_RPG_Icon_Pack")
CARDS_DIR   = os.path.join(_ASSETS_DIR, "_Project", "Data", "Resources", "Cards")

PLACEHOLDER_GUID = "e2c6ebc9cdc9d4746996dd96a61f52b9"

# card .asset path (relative to CARDS_DIR) → Pixeltier PNG path (relative to PACK_DIR)
MAPPING = {
    # Materials — tech / data
    "Materials/Card_DataShard.asset":
        "Modifications & Hardware/computer_chip_001.png",
    "Materials/Card_KernelShard.asset":
        "Modifications & Hardware/computer_chip_007.png",

    # Equipment — cyberpunk gear
    "Equipments/Card_CombatChip.asset":
        "Modifications & Hardware/computer_chip_003.png",
    "Equipments/Card_SurgeWeapon.asset":
        "Melee/sword_laser_001.png",

    # Consumables — cyberpunk food / meds
    "Consumables/Card_AlgaeWafer.asset":
        "Consumables & Meds/supplements_001.png",
    "Consumables/Card_MycoChip.asset":
        "Consumables & Meds/pickled_mushrooms.png",
    "Consumables/Card_CoreRation.asset":
        "Consumables & Meds/fast_food_001.png",
    "Consumables/Card_SignalJerky.asset":
        "Consumables & Meds/sewer_skewer.png",
    "Consumables/Card_VatBroth.asset":
        "Consumables & Meds/cup_noodles_001.png",

    # Structures — cyberpunk buildings / terminals
    "Structures/Card_Market.asset":
        "Currency/credit_card_001.png",
    "Structures/Card_OverseerCore.asset":
        "Modifications & Hardware/cyberdeck_001.png",
    "Structures/Card_SignalDrone.asset":
        "Modifications & Hardware/tablet.png",
    "Structures/Card_StasisPod.asset":
        "Modifications & Hardware/laptop.png",
    "Structures/Card_TapNode.asset":
        "Modifications & Hardware/phone.png",
}


def get_guid(png_path: str) -> str | None:
    meta_path = png_path + ".meta"
    if not os.path.exists(meta_path):
        return None
    with open(meta_path, "r", encoding="utf-8") as f:
        for line in f:
            m = re.match(r"^guid:\s+([0-9a-f]+)", line.strip())
            if m:
                return m.group(1)
    return None


ART_PATTERN = re.compile(r"artTexture: \{fileID: \d+, guid: [0-9a-f]+, type: \d+\}")


def update_art(card_path: str, new_guid: str) -> bool:
    with open(card_path, "r", encoding="utf-8") as f:
        content = f.read()

    # Verify it currently uses the placeholder before touching it
    if PLACEHOLDER_GUID not in content:
        print(f"  [SKIP] Not using placeholder art — leaving unchanged")
        return False

    new_ref = f"artTexture: {{fileID: 2800000, guid: {new_guid}, type: 3}}"
    new_content = ART_PATTERN.sub(new_ref, content)

    if new_content == content:
        print(f"  [SKIP] artTexture field not found")
        return False

    with open(card_path, "w", encoding="utf-8") as f:
        f.write(new_content)
    return True


def main():
    updated, skipped = 0, 0
    print(f"Pack dir : {PACK_DIR}")
    print(f"Cards dir: {CARDS_DIR}\n")

    for card_rel, sprite_rel in MAPPING.items():
        card_path   = os.path.join(CARDS_DIR, card_rel)
        sprite_path = os.path.join(PACK_DIR, sprite_rel)

        card_name = os.path.basename(card_rel).replace(".asset", "")
        print(f"{card_name}")

        if not os.path.exists(card_path):
            print(f"  [ERROR] Card not found: {card_path}")
            skipped += 1
            continue

        if not os.path.exists(sprite_path):
            print(f"  [ERROR] Sprite not found: {sprite_path}")
            skipped += 1
            continue

        guid = get_guid(sprite_path)
        if not guid:
            print(f"  [ERROR] GUID not found in meta: {sprite_path}")
            skipped += 1
            continue

        print(f"  -> {sprite_rel}")
        print(f"     guid: {guid}")

        if update_art(card_path, guid):
            print(f"  [OK]")
            updated += 1
        else:
            skipped += 1

    print(f"\n{'='*40}")
    print(f"Updated: {updated}   Skipped/Error: {skipped}")


if __name__ == "__main__":
    main()
