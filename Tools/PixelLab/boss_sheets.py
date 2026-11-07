"""PixelLab으로 그린 보스(거대호빗, 미노타우로스)를 게임 인물 시트로 옮긴다(사용자 요청 2026-11-06).

원본은 Tools/PixelLab/raw/boss/<이름>/<묶음>_<번호>.png다(0번이 기준 그림, 그 뒤가 움직임). 각 방향의 걷기는 움직임을 네 장 추리고,
기술 그림은 sheet.json의 moves로 적어 WebGameImporter가 시트에 넣는다.
PixelLab 그림은 인물 그림보다 작아 보스 크기로 키운다(게임 그림은 논리 px의 4배).

  Assets/MiniPandoras/Art/WebGame/Characters/<id>/down_<n>.png, right_<n>.png, up_<n>.png, <기술>_<n>.png, portrait.png, sheet.json

사용: python Tools/PixelLab/boss_sheets.py <프로젝트 루트>
"""
import json
import sys
from pathlib import Path

from PIL import Image

ART_PER_LOGICAL = 4
PORTRAIT = 56
WALK_FRAMES = (2, 4, 6, 8)
BOSSES = {
    "giantHobbit": {
        "scale": 1.5,
        "face": (56, 6, 104, 54),
        "walk": {"down": "walk_down", "right": "walk_right", "up": "walk_up"},
        "moves": {"slam": "slam", "throw": "throw", "stomp": "stomp"},
    },
    "minotaur": {
        "scale": 1.25,
        "face": (52, 10, 108, 66),
        "walk": {"down": "walk_down", "right": "charge", "up": "walk_up"},
        "moves": {"charge": "charge"},
    },
}


def frames_of(folder, clip):
    return sorted(folder.glob(f"{clip}_*.png"), key=lambda path: int(path.stem.rsplit("_", 1)[1]))


def enlarge(path, scale):
    image = Image.open(path).convert("RGBA")
    # 점이 고르게 보이도록 최소공배수로 키웠다가 맞춘다.
    width, height = int(round(image.width * scale)), int(round(image.height * scale))
    width, height = width + (-width) % ART_PER_LOGICAL, height + (-height) % ART_PER_LOGICAL
    return image.resize((width, height), Image.NEAREST)


def build(project, boss, info):
    raw = project / "Tools/PixelLab/raw/boss" / boss
    out = project / "Assets/MiniPandoras/Art/WebGame/Characters" / boss
    out.mkdir(parents=True, exist_ok=True)
    scale = info["scale"]
    counts = {}
    size = None
    feet = 0
    for direction, clip in info["walk"].items():
        all_frames = frames_of(raw, clip)
        chosen = [all_frames[0]] + [all_frames[i] for i in WALK_FRAMES if i < len(all_frames)]
        counts[direction] = len(chosen)
        for n, path in enumerate(chosen):
            image = enlarge(path, scale)
            image.save(out / f"{direction}_{n}.png")
            size = image.size
            if direction == "down" and n == 0:
                feet = image.getbbox()[3]

    moves = []
    for name, clip in info["moves"].items():
        all_frames = frames_of(raw, clip)[1:]
        for n, path in enumerate(all_frames):
            enlarge(path, scale).save(out / f"{name}_{n}.png")
        moves.append({"name": name, "frames": len(all_frames)})

    front = Image.open(frames_of(raw, info["walk"]["down"])[0]).convert("RGBA")
    face = front.crop(info["face"]).resize((PORTRAIT, PORTRAIT), Image.NEAREST)
    face.save(out / "portrait.png")
    width, height = size[0] / ART_PER_LOGICAL, size[1] / ART_PER_LOGICAL
    sheet = {
        "id": boss, "width": width, "height": height, "anchorX": width / 2, "anchorY": feet / ART_PER_LOGICAL - 0.5,
        "framesDown": counts["down"], "framesRight": counts["right"], "framesUp": counts["up"], "hasPortrait": True, "moves": moves,
    }
    (out / "sheet.json").write_text(json.dumps(sheet, indent=1), encoding="utf-8")
    print(boss, sheet)


def main(project):
    for boss, info in BOSSES.items():
        build(project, boss, info)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
