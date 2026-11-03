"""다시 그린 맵에 집터·소품(나무)·세운 것을 얹은 미리보기 그림을 만든다(Unity에서 보이는 모습 확인용).

사용: python Tools/PixelGen/preview_map.py <프로젝트 루트> <맵 id> [축소 배율]
  Docs/ArtUpgrade/PixelGen/<id>_preview.png
새로 짠 맵(layout.py, 그 폴더에 map.json이 있다)은 그 데이터를, 아니면 웹게임 맵 데이터와 uprights.json을 쓴다.
소품 벌은 MapImporter와 같은 규칙(칸 좌표로 정한 번호)으로 고른다.
"""
import json
import sys
from pathlib import Path

from PIL import Image


# 다시 심은 맵에서 웹게임 대신 trees.json을 쓰는 나무 소품(MapImporter.Trees와 같다)
TREES = {"tree", "pine", "oak", "maple", "birch", "spruce"}


def variant(x, y, count):
    """MapImporter.Variant와 같은 규칙."""
    return (x * 7 + y * 13) % count


def paste(scene, sprite, left, top):
    """맵 밖으로 나간 부분은 잘라 내고 얹는다(alpha_composite는 음수 자리를 받지 않는다)."""
    x0, y0 = max(0, -left), max(0, -top)
    x1, y1 = min(sprite.width, scene.width - left), min(sprite.height, scene.height - top)
    if x1 > x0 and y1 > y0:
        scene.alpha_composite(sprite.crop((x0, y0, x1, y1)), (left + x0, top + y0))


def shown(entry, look):
    """모습이 없는 것은 늘 보이고, 모습이 붙은 것은 그 모습일 때만 보인다."""
    return entry.get("look") in (None, look)


def compose(project, folder, look=None):
    """look을 주면 그 모습으로 그린다(layout.py 모습). 주지 않으면 모습이 붙은 것은 빼고 그린다."""
    art = project / "Assets/MiniPandoras/Art"
    scene = Image.open(folder / "ground.png").convert("RGBA")
    designed = (folder / "map.json").exists()
    data = json.loads(((folder if designed else art / f"WebGame/Maps/{folder.name}") / "map.json").read_text(encoding="utf-8"))
    for decal in (d for d in data.get("decals", []) if shown(d, look)):
        paste(scene, Image.open(folder / f"{decal['image']}.png").convert("RGBA"), decal["left"], decal["top"])
    meta = {p["id"]: p for p in json.loads((art / "PixelGen/Props/props.json").read_text(encoding="utf-8"))["props"]}
    # 소품(나무·가로등)과 세운 것(집·상자 …)을 발밑이 위쪽인 것부터 그린다(아래쪽 것이 앞에 온다). 발밑은 논리 px다.
    layers = []
    placed = data["props"]
    if not designed and (folder / "trees.json").exists():
        replanted = json.loads((folder / "trees.json").read_text(encoding="utf-8"))["props"]
        placed = [p for p in placed if p["id"] not in TREES] + replanted
    for prop in (p for p in placed if shown(p, look)):
        info = meta.get(prop["id"])
        if info is None:
            continue
        sprite = Image.open(art / f"PixelGen/Props/{prop['id']}_{variant(prop['x'], prop['y'], info['variants'])}.png").convert("RGBA")
        foot_x, foot_y = (prop["x"] + 0.5) * 64, (prop["y"] + 15 / 16) * 64
        layers.append((foot_y / 4, sprite, int(foot_x - info["anchorX"] * 4), int(foot_y - info["anchorY"] * 4)))
    if designed:
        uprights = data["uprights"]
    elif (folder / "uprights.json").exists():
        uprights = json.loads((folder / "uprights.json").read_text(encoding="utf-8"))["uprights"]
    else:
        uprights = []
    for entry in (u for u in uprights if shown(u, look)):
        sprite = Image.open(folder / f"{entry['image']}.png").convert("RGBA")
        layers.append((entry["foot"] - 1.5, sprite, entry["left"], entry["top"]))
    for _, sprite, left, top in sorted(layers, key=lambda layer: layer[0]):
        paste(scene, sprite, left, top)
    return scene


def main(project, map_id, shrink):
    scene = compose(project, project / f"Assets/MiniPandoras/Art/PixelGen/Maps/{map_id}")
    out = project / f"Docs/ArtUpgrade/PixelGen/{map_id}_preview.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    scene.resize((scene.width // shrink, scene.height // shrink), Image.LANCZOS).save(out)
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve(), sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 3)
