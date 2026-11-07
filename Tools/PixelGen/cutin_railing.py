"""3장 컷인 그림: 아지트 1층에서 올려다본 어두운 2층 난간.

맵에서는 1층만 보이므로, 2층 난간에 걸터앉은 디엘은 필드 대신 띄우는 컷인 화면(CutInView)으로 보여 준다.
카메라는 1층 바닥 가까이에서 올려다본다: 위로 모이는 서까래, 2층 벽과 방문, 판자로 막은 2층 창 틈의 달빛,
난간 곁에 매달린 등불 하나, 아래에서 보이는 2층 바닥 들보와 장선, 그 아래 1층의 어둠과 떠다니는 먼지.

  back.png      배경(768×432, 가상 화면 384×216의 두 배)
  streaks.png   뛰어내릴 때 겹치는 세로 속도선(반투명)
  diel.png      난간에 앉아 다리를 꼰 디엘을 도트 그대로 네 배 키운 것(UI에서 크게 그려도 흐려지지 않게). 다리를 눌러 앉히면 짧아 보인다.
                PixelLab이 웹게임 정면 그림을 고쳐 그린 Tools/PixelLab/diel_crossed_legs_raw.png에서 함께 그려진 나무 막대를 지우고, 머리는 웹게임 그림 그대로 되얹는다.
그림 안 자리(가상 화면 px)는 CutInView를 쓰는 이야기 코드(Chapter3)가 쓴다: 난간 손잡이 윗면 y 103.

사용: python Tools/PixelGen/cutin_railing.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import pixelgen as pg
from houses import IRON, WARM, WOOD, Canvas, outline

W, H = 768, 432
NIGHT = pg.hexes("#07060b", "#0d0b14", "#141019", "#1c1622", "#26202c")
PLASTER = pg.hexes("#1e1a24", "#2a2430", "#383040", "#4a4050")
TIMBER = pg.hexes("#0e0907", "#1a110c", "#2a1c13", "#3e2a1c", "#5a3e28")
MOON = np.array([150, 170, 210])
LAMP = np.array([255, 190, 110])
HANDRAIL_Y = 206          # 난간 손잡이 윗면(점). 가상 화면 103px.
EDGE_Y = 262              # 2층 바닥 앞 들보 위 끝
UNDER_Y = 292             # 들보 아래, 2층 바닥 밑면이 보이기 시작하는 곳
LAMP_AT = (318, 168)
SIT_HIP = 0.64            # 정면 그림에서 엉덩이 높이(위에서부터 비율). 여기가 난간 손잡이에 닿는다.

# PixelLab 그림에서 고치는 자리(112×168의 좌표).
SEAT_ROWS = (106, 123)      # 그림에 함께 그려진 나무 막대의 줄. 맵의 난간에 앉으므로 지운다.
SEAT_LEFT_END = 22          # 이 x보다 왼쪽, 아래 x보다 오른쪽의 막대를 지운다.
SEAT_RIGHT_START = 90
HEAD_ROWS = 57              # 웹게임 그림에서 얼굴이 끝나는 줄. 여기까지는 웹게임 그림 그대로 쓴다.
HEAD_SHIFT = (0, -1)        # PixelLab 그림의 머리는 한 점 위에 있다.
BODY_CELL = (56, 80)        # 몸통 안의 한 점


def glow(image, centre, radius, color, strength):
    yy, xx = np.mgrid[0:H, 0:W]
    d = np.hypot(xx - centre[0], yy - centre[1]) / radius
    light = np.clip(1 - d, 0, 1) ** 1.6
    light = np.floor(light * 6 + (pg.bayer(H, W) - 0.5) * 0.9) / 6
    image[..., :3] = np.clip(image[..., :3] * (1 + light[..., None] * strength * 0.6) + color * light[..., None] * strength * 0.35, 0, 255)


def back(rng):
    c = Canvas(W, H)
    t = (c.yy / H)
    level = np.clip((t * 3.5 + (pg.bayer(H, W) - 0.5) * 0.6).astype(int), 0, 3)
    c.paint(c.rect(0, 0, W, H), NIGHT[level])

    # 위로 모이는 서까래(소실점은 화면 위 바깥)
    vx, vy = W / 2, -520
    for k in range(-5, 6):
        bx = W / 2 + k * 150
        for side, shade in ((-9, 2), (9, 1)):
            c.paint(c.polygon([(vx, vy), (bx + side - 16, 90), (bx + side + 16, 90)]) & (c.yy < 90), TIMBER[shade])
        c.paint(c.polygon([(vx, vy), (bx - 3, 90), (bx + 3, 90)]) & (c.yy < 90), TIMBER[3])
    c.paint(c.rect(0, 84, W, 96), TIMBER[2])
    c.paint(c.rect(0, 84, W, 86), TIMBER[4])

    # 2층 벽: 어두운 회벽, 나무 기둥, 방문, 판자로 막은 창
    wall = c.rect(0, 96, W, EDGE_Y)
    noise = pg.noise(H, W, 9, rng)
    c.image[wall, :3] = PLASTER[np.clip((noise * 2.6).astype(int), 0, 3)][wall]
    for x in range(40, W, 176):
        c.paint(c.rect(x, 96, x + 12, EDGE_Y), TIMBER[2])
        c.paint(c.rect(x, 96, x + 2, EDGE_Y), TIMBER[3])
    door = c.rect(560, 120, 616, EDGE_Y)
    c.paint(door, TIMBER[1])
    c.paint(c.rect(566, 126, 610, EDGE_Y), TIMBER[0])
    c.paint(c.rect(600, 190, 604, 196), IRON[3])
    window = c.rect(110, 120, 176, 186)
    c.paint(window, np.array([40, 52, 80]))
    for k in range(4):
        y = 124 + k * 16
        c.paint(c.polygon([(104, y + (k % 2) * 4), (182, y + 4 - (k % 2) * 4), (182, y + 11 - (k % 2) * 4), (104, y + 7 + (k % 2) * 4)]),
                TIMBER[2 + k % 2])

    # 2층 바닥 앞 들보와, 아래에서 올려다본 바닥 밑면(장선 끝이 줄지어 보인다)
    c.paint(c.rect(0, EDGE_Y, W, UNDER_Y), TIMBER[3])
    c.paint(c.rect(0, EDGE_Y, W, EDGE_Y + 3), TIMBER[4])
    c.paint(c.rect(0, UNDER_Y - 4, W, UNDER_Y), TIMBER[1])
    under = c.rect(0, UNDER_Y, W, UNDER_Y + 34)
    c.paint(under, TIMBER[1])
    for x in range(10, W, 46):
        c.paint(c.polygon([(x, UNDER_Y), (x + 18, UNDER_Y), (x + 22 + (x - W / 2) * 0.05, UNDER_Y + 34),
                           (x + 2 + (x - W / 2) * 0.05, UNDER_Y + 34)]), TIMBER[2])
    c.paint(c.rect(0, UNDER_Y + 34, W, UNDER_Y + 38), TIMBER[0])

    # 난간: 손잡이, 난간동자, 굵은 기둥. 한 칸은 부러져 빠졌다.
    c.paint(c.rect(0, HANDRAIL_Y, W, HANDRAIL_Y + 10), TIMBER[3])
    c.paint(c.rect(0, HANDRAIL_Y, W, HANDRAIL_Y + 2), TIMBER[4])
    for x in range(6, W, 24):
        if 470 <= x < 520:
            continue
        thick = 10 if (x // 24) % 6 == 0 else 6
        c.paint(c.rect(x, HANDRAIL_Y + 10, x + thick, EDGE_Y), TIMBER[2])
        c.paint(c.rect(x, HANDRAIL_Y + 10, x + 2, EDGE_Y), TIMBER[3])
    c.line(478, HANDRAIL_Y + 12, 494, EDGE_Y - 6, TIMBER[2], 5)

    # 매달린 등불
    lx, ly = LAMP_AT
    c.line(lx, 96, lx, ly - 18, IRON[2], 2)
    c.paint(c.rect(lx - 10, ly - 18, lx + 10, ly - 14), IRON[2])
    c.paint(c.rect(lx - 8, ly - 14, lx + 8, ly + 8), WARM[3])
    c.paint(c.rect(lx - 4, ly - 10, lx + 4, ly + 4), WARM[4])
    c.paint(c.rect(lx - 10, ly + 8, lx + 10, ly + 12), IRON[2])

    # 1층의 어둠: 아래로 갈수록 짙고, 판자 틈 달빛 줄기가 비스듬히 지나간다.
    lower = c.rect(0, UNDER_Y + 38, W, H)
    c.paint(lower, NIGHT[1])
    c.paint(lower & (c.yy > H - 60), NIGHT[0])
    image = c.image.astype(float)
    for k, x0 in enumerate((60, 150, 230)):
        shaft = c.polygon([(x0, 96), (x0 + 22, 96), (x0 + 330, H), (x0 + 270, H)])
        mix = 0.10 - k * 0.02
        image[shaft, :3] = image[shaft, :3] * (1 - mix) + MOON * mix
    glow(image, LAMP_AT, 230, LAMP, 1.0)
    # 떠다니는 먼지는 등불과 달빛이 닿는 2층 쪽에만 보인다(아래 어둠에 흩뿌리면 별처럼 보인다).
    motes = (rng.random((H, W)) < 0.0009) & (c.yy > 100) & (c.yy < UNDER_Y)
    image[motes, :3] = image[motes, :3] * 0.4 + np.array([220, 200, 170]) * 0.6
    return image[..., :3].astype(np.uint8)


def streaks(rng):
    """
    아래로 내리꽂힐 때 겹치는 굵은 흐름선: 가운데가 굵고 위아래 끝으로 갈수록 가늘고 옅어지는 세로 획.
    화면 가운데(떨어지는 인물 자리)는 비우고 양옆으로 갈수록 촘촘하다. 진하기는 도트처럼 몇 단계로 끊는다.
    """
    yy, xx = np.mgrid[0:H, 0:W]
    alpha = np.zeros((H, W))
    for _ in range(40):
        side = rng.choice([-1, 1])
        x = W / 2 + side * (W / 2) * (1 - rng.random() ** 1.6) * 0.95
        if abs(x - W / 2) < 70:
            continue
        length = rng.uniform(140, 420)
        top = rng.uniform(-120, H - 60)
        width = rng.uniform(6, 18) * (0.6 + abs(x - W / 2) / W)
        t = (yy - top) / length
        inside = (t >= 0) & (t <= 1)
        half = width / 2 * np.sin(np.clip(t, 0, 1) * np.pi) ** 0.7
        stroke = inside & (np.abs(xx - x) <= half)
        strength = np.sin(np.clip(t, 0, 1) * np.pi) * rng.uniform(0.55, 1.0)
        alpha = np.where(stroke, np.maximum(alpha, strength), alpha)
    alpha = np.floor(alpha * 4 + (pg.bayer(H, W) - 0.5) * 0.8) / 4
    image = np.zeros((H, W, 4), dtype=np.uint8)
    image[..., :3] = np.array([235, 232, 255])
    image[..., 3] = (np.clip(alpha, 0, 1) * 210).astype(np.uint8)
    return image


def connected_to(solid, cell):
    """cell에서 이어진 칠한 점. 막대를 지우고 남은 부스러기를 떼어 낸다."""
    reached = np.zeros_like(solid)
    reached[cell[1], cell[0]] = True
    while True:
        grown = (reached | pg.neighbours(reached)) & solid
        if (grown == reached).all():
            return reached
        reached = grown


def perched(project):
    """다리를 꼰 디엘. 막대는 갈색이고 코트는 검어서, 양옆의 갈색 점과 그에 붙은 윤곽만 지운다."""
    lab = np.array(Image.open(project / "Tools/PixelLab/diel_crossed_legs_raw.png").convert("RGBA"))
    top, bottom = SEAT_ROWS
    box = np.zeros(lab.shape[:2], dtype=bool)
    box[top:bottom, :SEAT_LEFT_END] = True
    box[top:bottom, SEAT_RIGHT_START:] = True
    red, blue = lab[..., 0].astype(int), lab[..., 2].astype(int)
    wood = box & (red > blue + 14) & (red < 150)
    outline = box & pg.neighbours(wood) & (lab[..., :3].max(axis=2) < 40)
    lab[wood | outline] = 0
    lab[~connected_to(lab[..., 3] > 0, BODY_CELL)] = 0
    out = Image.fromarray(lab, "RGBA")
    # 얼굴은 다시 그리지 않는다. 몸과 이어지는 목 아래는 PixelLab 그림을 둔다.
    head = Image.open(project / "Assets/MiniPandoras/Art/WebGame/Characters/diel/down_0.png").convert("RGBA").crop((0, 0, 112, HEAD_ROWS))
    out.paste(Image.new("RGBA", head.size), HEAD_SHIFT)
    out.alpha_composite(head.crop((0, -HEAD_SHIFT[1], 112, HEAD_ROWS)))
    return out


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns/railing"
    out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(31)
    Image.fromarray(back(rng), "RGB").save(out / "back.png")
    Image.fromarray(streaks(rng), "RGBA").save(out / "streaks.png")
    source = Image.open(project / "Assets/MiniPandoras/Art/WebGame/Characters/diel/down_0.png").convert("RGBA")
    seated = perched(project)
    seated.resize((seated.width * 4, seated.height * 4), Image.NEAREST).save(out / "diel.png")
    preview = Image.open(out / "back.png").convert("RGBA")
    scale = 192 / source.height
    figure = seated.resize((int(seated.width * scale), int(seated.height * scale)), Image.NEAREST)
    preview.alpha_composite(figure, (430 - figure.width // 2, HANDRAIL_Y - int(192 * SIT_HIP)))
    preview.save(project / "Docs/ArtUpgrade/PixelGen/cutin_railing_preview.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
