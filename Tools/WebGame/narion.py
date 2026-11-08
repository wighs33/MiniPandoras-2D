"""나리온: 요정들이 기르는 거대한 새(11장 정거장·하늘길·엔딩, 2부 타이틀).

웹게임 원화(public/games/pandoras/dust-style/environment/narion-mount.png, 세 줄: 앉은 모습, 날개를 뒤로 젖힌 날갯짓, 날개를 내린 날갯짓)를
오픈소스 PixelOE(contrast 모드)로 우리 점 크기에 맞춰 바꾼다. 원화는 게임 그림보다 네 배쯤 촘촘한 가짜 도트라 그대로 줄이면 흐려진다.
PixelOE는 칸마다 대비를 살려 한 색을 골라 눈·부리·깃 끝이 또렷하게 남는다.
자르는 법은 웹게임과 같다(gfx/dust-assets.ts): 줄마다 가로를 288 논리 px로 줄이고, 안장 윗면(줄마다 saddle)을 논리 y 40에 맞춘다.
테두리는 웹게임 인물처럼 실루엣 안쪽 한 겹을 어두운 남색 쪽으로 섞는다(PixelOE는 투명도를 다루지 않아 알파는 따로 줄인다).
탄 일행은 웹게임 drawNarion과 같다: 오른쪽을 보는 첫 그림의 위 67%(랑은 0.8배 전신)를 자리 x = 117 + 18i(꼬리 쪽부터 여섯)에 앉히고,
안장 앞쪽 난간(x 108~215, 안장 높이부터 6)을 다시 덮어 허리가 안장 안에 들어가게 한다.

두 크기로 굽는다.
  게임: 논리 px 하나 = 1점으로 바꾼 뒤 4배로 키운다(게임 그림은 논리 px 하나 = 4px).
    Assets/MiniPandoras/Art/PixelGen/Stage/narion_perch.png        앉은 새(아무도 타지 않음)
    Assets/MiniPandoras/Art/PixelGen/Stage/narion_perch_1~6.png    앉은 새와 꼬리 쪽부터 그만큼 탄 일행(정거장에서 한 사람씩 탈 때)
    Assets/MiniPandoras/Art/PixelGen/Stage/narion_fly0.png, narion_fly1.png   날갯짓 두 장, 여섯이 다 탄 모습(이륙, 하늘길·엔딩 컷인)
  2부 타이틀: 사구 위를 멀리 나는 모습이라 게임 크기의 3/4이고, 점 하나가 타이틀 풍경의 점(가상 px 반)과 같다(논리 px 하나 = 1.5점).
    밤 풍경에 맞춰 달빛 쪽으로 어둡게 물들이고, 달(오른쪽 위)을 향한 가장자리에 달빛을 얹는다.
    Assets/MiniPandoras/Art/PixelGen/UI/Title/Part2/narion_0.png, narion_1.png   여섯을 태운 날갯짓 두 장
  Docs/ArtUpgrade/PixelGen/narion_preview.png

PixelOE는 따로 만든 가상환경에서 돈다(E:/ArtTools/venv-pixeloe, Docs/THIRD_PARTY.md).
사용: E:/ArtTools/venv-pixeloe/Scripts/python.exe -I Tools/WebGame/narion.py <프로젝트 루트> <웹게임 저장소 루트(PANDORA_AI)>
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from pixeloe.legacy.pixelize import pixelize

W, H = 288, 136                    # 논리 px(웹게임 NARION)
SEAT_Y = 40
GAME_ART = 4                       # 게임 그림: 논리 px 하나 = 4px
TITLE_DOTS = 1.5                   # 2부 타이틀: 논리 px 하나 = 1.5점(게임 크기의 3/4)
# 원화의 줄(웹게임 NARION_FRAMES): 이름, 위 y, 높이, 그 줄에서 안장 윗면의 y
FRAMES = (("perch", 0, 418, 118), ("fly0", 419, 355, 104), ("fly1", 775, 479, 108))
RIDERS = ["largo", "isabel", "aurora", "diel", "hero", "rang"]
OUTLINE = np.array([12, 14, 34], dtype=float)
OUTLINE_MIX = 0.6
NIGHT = np.array([0.56, 0.6, 0.84])
MOONLIT = np.array([200, 196, 232], dtype=float)


def seat_x(index):
    return 117 + index * 18


def pixelized(row, size):
    """원화 한 줄을 size 점으로: 색은 PixelOE(회색 바탕에 합쳐 돌림), 알파는 칸 평균을 반에서 자른다."""
    flat = Image.alpha_composite(Image.new("RGBA", row.size, (128, 128, 128, 255)), row).convert("RGB")
    bgr = np.array(flat)[:, :, ::-1].copy()
    patch = max(2, int(round(row.width / size[0])))
    out = pixelize(bgr, mode="contrast", target_size=size, patch_size=patch, thickness=0, no_upscale=True)
    rgb = Image.fromarray(out[:, :, ::-1].copy()).convert("RGB")
    if rgb.size != size:
        rgb = rgb.resize(size, Image.NEAREST)
    alpha = np.array(row.getchannel("A").resize(size, Image.BOX)) >= 128
    return np.array(rgb).astype(float), alpha


def outlined(rgb, alpha):
    """실루엣 가장자리(옆 칸 넷 가운데 하나라도 비었으면) 한 겹을 어두운 남색 쪽으로 섞는다."""
    padded = np.pad(alpha, 1)
    inside = padded[:-2, 1:-1] & padded[2:, 1:-1] & padded[1:-1, :-2] & padded[1:-1, 2:]
    edge = alpha & ~inside
    rgb = rgb.copy()
    rgb[edge] = rgb[edge] * (1 - OUTLINE_MIX) + OUTLINE * OUTLINE_MIX
    rgba = np.zeros(alpha.shape + (4,), dtype=np.uint8)
    rgba[..., :3] = np.clip(rgb, 0, 255).astype(np.uint8)
    rgba[..., 3] = alpha * 255
    return Image.fromarray(rgba, "RGBA")


def bird(sheet, y, h, saddle, dots):
    """원화 한 줄 → 논리 px 하나 = dots 점인 288×136 논리 판(안장 높이를 맞춤)."""
    scale = W * dots / sheet.width
    size = (int(round(W * dots)), int(round(h * scale)))
    rgb, alpha = pixelized(sheet.crop((0, y, sheet.width, y + h)), size)
    board = Image.new("RGBA", (int(round(W * dots)), int(round(H * dots))))
    board.alpha_composite(outlined(rgb, alpha), (0, int(round(SEAT_Y * dots - saddle * scale))))
    return board


def with_riders(project, picture, dots, riders=RIDERS):
    """새 그림(논리 px 하나 = dots 점)에 탄 사람을 얹는다(웹게임 drawNarion). 인물 그림은 논리 px 하나 = 4px라 dots가 4보다 작으면 칸 평균으로 줄인다."""
    picture = picture.copy()
    bird_only = picture.copy()
    shrink = dots / GAME_ART
    for i, rider in enumerate(riders):
        folder = project / f"Assets/MiniPandoras/Art/WebGame/Characters/{rider}"
        sheet = json.loads((folder / "sheet.json").read_text(encoding="utf-8"))
        source = Image.open(folder / "right_0.png").convert("RGBA")
        cut = 1.0 if rider == "rang" else 0.67
        size = 0.8 if rider == "rang" else 1.0
        part = source.crop((0, 0, source.width, int(round(source.height * cut))))
        width, height = int(round(part.width * size * shrink)), int(round(part.height * size * shrink))
        if shrink < 1:
            part = part.resize((width, height), Image.BOX)
            part.putalpha(part.getchannel("A").point(lambda v: 255 if v >= 128 else 0))
        else:
            part = part.resize((width, height), Image.NEAREST)
        left = (seat_x(i) - sheet["anchorX"] * size) * dots
        top = (SEAT_Y + 3 - sheet["height"] * cut * size) * dots
        picture.alpha_composite(part, (int(round(left)), int(round(top))))
    if riders:
        box = tuple(int(round(v * dots)) for v in (108, SEAT_Y, 215, SEAT_Y + 6))
        picture.alpha_composite(bird_only.crop(box), box[:2])
    return picture


def by_night(picture):
    """달빛 쪽으로 어둡게 물들이고, 위와 오른쪽(달을 향한) 가장자리에 달빛을 얹는다."""
    rgba = np.asarray(picture).astype(float)
    alpha = rgba[..., 3] >= 128
    rgb = rgba[..., :3] * NIGHT
    above = np.vstack([np.zeros((1, alpha.shape[1]), bool), alpha[:-1]])
    right = np.hstack([alpha[:, 1:], np.zeros((alpha.shape[0], 1), bool)])
    rim = alpha & (~above | ~right)
    rgb[rim] = rgb[rim] * 0.45 + MOONLIT * 0.55
    out = np.zeros(alpha.shape + (4,), np.uint8)
    out[..., :3] = np.clip(rgb, 0, 255).astype(np.uint8)
    out[..., 3] = alpha * 255
    return Image.fromarray(out, "RGBA")


def main(project, web):
    sheet = Image.open(web / "public/games/pandoras/dust-style/environment/narion-mount.png").convert("RGBA")
    stage = project / "Assets/MiniPandoras/Art/PixelGen/Stage"
    stage.mkdir(parents=True, exist_ok=True)
    game = {name: bird(sheet, y, h, saddle, 1).resize((W * GAME_ART, H * GAME_ART), Image.NEAREST) for name, y, h, saddle in FRAMES}
    game["perch"].save(stage / "narion_perch.png")
    for count in range(1, len(RIDERS) + 1):
        with_riders(project, game["perch"], GAME_ART, RIDERS[:count]).save(stage / f"narion_perch_{count}.png")
    for pose in ("fly0", "fly1"):
        with_riders(project, game[pose], GAME_ART).save(stage / f"narion_{pose}.png")

    title = project / "Assets/MiniPandoras/Art/PixelGen/UI/Title/Part2"
    title.mkdir(parents=True, exist_ok=True)
    for k, (name, y, h, saddle) in enumerate(FRAMES[1:]):
        by_night(with_riders(project, bird(sheet, y, h, saddle, TITLE_DOTS), TITLE_DOTS)).save(title / f"narion_{k}.png")

    preview = Image.new("RGBA", (W * GAME_ART, H * GAME_ART * 3), (150, 190, 230, 255))
    for k, name in enumerate(("perch_6", "fly0", "fly1")):
        preview.alpha_composite(Image.open(stage / f"narion_{name}.png"), (0, k * H * GAME_ART))
    preview.save(project / "Docs/ArtUpgrade/PixelGen/narion_preview.png")
    print(stage, title)


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]))
