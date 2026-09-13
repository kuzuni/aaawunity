#!/usr/bin/env python3
from hashlib import sha256
from pathlib import Path
import json

from PIL import Image, ImageChops


ROOT = Path(__file__).resolve().parents[3]
ASSETS = {
    "node.devil": "Assets/Art/ChihuahuaGameUI/Nodes/devil.png",
    "node.angel": "Assets/Art/ChihuahuaGameUI/Nodes/angel.png",
    "env.expedition.field": "Assets/Art/ChihuahuaGameUI/World/Expedition/field.png",
    "env.expedition.road": "Assets/Art/ChihuahuaGameUI/World/Expedition/road.png",
    "env.expedition.prop1": "Assets/Art/ChihuahuaGameUI/World/Expedition/prop1.png",
    "env.expedition.prop2": "Assets/Art/ChihuahuaGameUI/World/Expedition/prop2.png",
    "env.expedition.prop3": "Assets/Art/ChihuahuaGameUI/World/Expedition/prop3.png",
    "env.hell.field": "Assets/Art/ChihuahuaGameUI/World/Hell/field.png",
    "env.hell.road": "Assets/Art/ChihuahuaGameUI/World/Hell/road.png",
    "env.hell.prop1": "Assets/Art/ChihuahuaGameUI/World/Hell/prop1.png",
    "env.hell.prop2": "Assets/Art/ChihuahuaGameUI/World/Hell/prop2.png",
    "env.hell.prop3": "Assets/Art/ChihuahuaGameUI/World/Hell/prop3.png",
    "ui.dungeon.expedition": "Assets/Art/ChihuahuaGameUI/DungeonCards/expedition.png",
    "ui.dungeon.hell": "Assets/Art/ChihuahuaGameUI/DungeonCards/hell.png",
}


def inspect(key: str, relative: str) -> dict:
    path = ROOT / relative
    image = Image.open(path)
    result = {
        "key": key,
        "path": relative,
        "sha256": sha256(path.read_bytes()).hexdigest(),
        "bytes": path.stat().st_size,
        "size": list(image.size),
        "mode": image.mode,
        "alpha_extrema": None,
        "checks": {},
    }
    if ".prop" in key or key.startswith("node."):
        if "A" not in image.mode:
            raise ValueError(f"{key}: transparent asset is {image.mode}, not RGBA")
        alpha = image.getchannel("A")
        result["alpha_extrema"] = list(alpha.getextrema())
        if alpha.getextrema() != (0, 255):
            raise ValueError(f"{key}: alpha extrema are not 0..255")
        bbox = alpha.getbbox()
        margins = [bbox[0], bbox[1], image.width - bbox[2], image.height - bbox[3]]
        result["checks"]["transparent_margin_px"] = margins
        if min(margins) < 32:
            raise ValueError(f"{key}: insufficient transparent margin {margins}")
    if key.endswith(".field") or key.endswith(".road"):
        rgb = image.convert("RGB")
        left = rgb.crop((0, 0, 1, image.height))
        right = rgb.crop((image.width - 1, 0, image.width, image.height))
        exact = ImageChops.difference(left, right).getbbox() is None
        result["checks"]["horizontal_edges_exact"] = exact
        if not exact:
            raise ValueError(f"{key}: horizontal edges do not match")
    if key.startswith("ui.dungeon."):
        ratio = image.width / image.height
        result["checks"]["aspect_ratio"] = ratio
        if ratio != 1.5:
            raise ValueError(f"{key}: expected 3:2, got {image.size}")
    return result


if __name__ == "__main__":
    print(json.dumps([inspect(*item) for item in ASSETS.items()], indent=2))
