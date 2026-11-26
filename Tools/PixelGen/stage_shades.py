"""컷인 무대에 서는 거대한 형상(신, 갓슬레이어, 십이장, 에라드)의 실루엣과 에라디온 원반.

아직 나오지 않은 인물이라 얼굴도 옷차림도 보이지 않고 윤곽만 보인다(사용자 결정 2026-11-26). 그림은 안이 꽉 찬 흰 모양이고,
무대의 셰이더(Art/Shaders/Stage/StageShade)가 어둠으로 칠하고 윤곽에만 저마다의 빛을 두른다. 그래서 윤곽만으로 서로 다른 인물임이 드러나야 한다.
- 웹게임 원화가 있는 신과 에라드 간부는 원화의 윤곽이다. 판도라 여신은 타이틀과 같은 윤곽이다.
- 십이장은 원화의 몸 윤곽(옷자락과 자세)에 다른 머리와 무기를 얹어 새 인물로 만든다.
- 나머지는 처음부터 그린다(silhouette.py의 부드러운 다각형과 띠).

  Art/PixelGen/CutInStage/shade_<이름>.png, world_top.png(원반 윗면), world_rock.png(원반 옆면), waterfall.png

사용: python Tools/PixelGen/stage_shades.py <프로젝트 루트>
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

import pixelgen as pg
from silhouette import Brush, blade, spear, strands, tatter
from title_stage import goddess_layers, ramp, save, tone

W, H = 400, 500
CX, FOOT = W // 2, H - 10
GODS = {"baal": "baal", "michael": "michael", "kata": "kata", "hetos": "hetos-giant", "divine_b": "divine-intellect"}
OFFICERS = ("rotata-stroud", "kyle-eve", "satyr", "kyurik-tremor")


def keep(folder, name, alpha):
    save(np.dstack([np.full_like(alpha, 255)] * 3 + [alpha]).astype(np.uint8), folder, f"shade_{name}.png")


def finish(b, folder, name):
    keep(folder, name, np.asarray(pg.upscale(np.dstack([np.where(b.mask() > 0.43, 255, 0)] * 4).astype(np.uint8)))[..., 3])


def outline(project, name):
    """원화의 윤곽. 반투명 이펙트 자락은 윤곽을 흐리므로 뚜렷한 곳만 남기고, 가는 틈은 메운다."""
    art = Image.open(project.parent.parent.parent / f"PANDORA_AI/public/games/pandoras/art/{name}.webp").convert("RGBA")
    mask = Image.fromarray(np.where(np.asarray(art)[..., 3] > 110, 255, 0).astype(np.uint8))
    mask = mask.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.MinFilter(5))
    return mask.crop(mask.getbbox())


class Figure(Brush):
    """원화의 윤곽을 세운 몸. at(가로, 세로)는 몸 그림의 비율(0~1)을 캔버스의 점으로 바꾼다."""

    def __init__(self, project, art, height=420, flip=False, x=CX, wide=1.0, seed=1):
        super().__init__(W, H, seed)
        mask = outline(project, art)
        if flip:
            mask = mask.transpose(Image.FLIP_LEFT_RIGHT)
        self.tall = height
        self.span = height * mask.width / mask.height * wide
        self.left, self.top = x - self.span / 2, FOOT - height
        body = mask.resize((int(self.span), height), Image.LANCZOS)
        self.stamp(body, self.left, self.top)

    def at(self, u, v):
        return self.left + u * self.span, self.top + v * self.tall

    def path(self, *points):
        return [self.at(u, v) for u, v in points]


# ---------------------------------------------------------------- 신

def head(b, x=CX, y=104, r=23):
    b.ellipse(x, y, r * 0.84, r)
    b.ribbon([(x, y + 12), (x, y + 44)], [10, 12])


def torso(b, shoulder=48, waist=27, hip=34, top=144, middle=226, bottom=282, lean=0):
    b.shape([(CX - 12, top - 8), (CX - shoulder, top + 10), (CX - shoulder * 0.9, top + 34), (CX - waist + lean * 0.5, middle), (CX - hip + lean, bottom),
             (CX + hip + lean, bottom), (CX + waist + lean * 0.5, middle), (CX + shoulder * 0.9, top + 34), (CX + shoulder, top + 10), (CX + 12, top - 8)])


def arm(b, shoulder, elbow, hand, thick=12):
    b.ribbon([shoulder, elbow, hand], [thick, thick * 0.78, thick * 0.56])
    b.ellipse(hand[0], hand[1], thick * 0.66, thick * 0.66)


def gods(project, folder):
    for name, art in GODS.items():
        mask = outline(project, art)
        mask.thumbnail((720, 900), Image.LANCZOS)
        keep(folder, name, np.where(np.asarray(mask) > 110, 255, 0))
    body, hair, _, light = goddess_layers()
    layers = np.maximum.reduce([body, hair, light])
    alpha = np.asarray(Image.fromarray((layers * 255).astype(np.uint8)).resize((600, 900), Image.LANCZOS))
    keep(folder, "pandora", np.where(alpha > 110, 255, 0))

    b = Brush(W, H, 1)  # 대지신 모니아: 나뭇가지 뿔, 땅에 뿌리내리는 넓은 치마, 어깨에 앉은 새
    torso(b, 38, 24, 34)
    b.shape([(172, 258), (150, 340), (104, 420), (56, 478), (40, FOOT), (360, FOOT), (344, 478), (296, 420), (250, 340), (228, 258)])
    for x, sway in ((52, -40), (84, -30), (120, -14), (160, -6), (240, 6), (280, 14), (316, 30), (348, 40)):
        b.ribbon([(x, FOOT - 40), (x + sway * 0.5, FOOT - 14), (x + sway, FOOT + 2), (x + sway * 1.5, FOOT + 6)], [14, 9, 5, 0.6])
    head(b)
    strands(b, (CX, 92), 24, 0, 250, 15, 10, 20)
    for side in (-1, 1):
        b.ribbon([(CX + side * 12, 88), (CX + side * 38, 68), (CX + side * 60, 34), (CX + side * 54, 2)], [7, 6, 4, 0.8])
        for at, tip in (((32, 72), (74, 62)), ((48, 52), (100, 36)), ((58, 36), (92, 6)), ((44, 60), (32, 26)), ((24, 80), (22, 50))):
            b.ribbon([(CX + side * at[0], at[1]), (CX + side * (at[0] + tip[0]) / 2, (at[1] + tip[1]) / 2 - 5), (CX + side * tip[0], tip[1])], [4.6, 3.2, 0.6])
        for k in range(4):
            angle = math.radians(-150 + k * 36) if side < 0 else math.radians(-30 - k * 36)
            x, y = CX + side * (66 + k * 8), 30 + k * 16
            b.polygon([(x, y), (x + math.cos(angle) * 10 - 4, y + math.sin(angle) * 10), (x + math.cos(angle) * 18, y + math.sin(angle) * 18), (x + math.cos(angle) * 8 + 4, y + math.sin(angle) * 8 + 4)])
        arm(b, (CX + side * 36, 162), (CX + side * 80, 214), (CX + side * 120, 196), 11)
        b.shape([(CX + side * 70, 206), (CX + side * 104, 198), (CX + side * 112, 250), (CX + side * 94, 300), (CX + side * 84, 350), (CX + side * 74, 290)])
    b.shape([(314, 184), (326, 164), (348, 168), (374, 152), (364, 176), (344, 192), (322, 196)])
    b.polygon([(316, 180), (304, 176), (318, 172)])
    finish(b, folder, "monia")

    b = Brush(W, H, 2)  # 해신 아쿠아: 물고기 꼬리를 감고 앉은 몸, 삼지창, 물결치는 머리, 튀는 물방울
    torso(b, 38, 24, 31)
    b.ribbon([(CX, 268), (CX - 30, 336), (CX + 4, 408), (CX + 74, 448), (CX + 122, 410), (CX + 108, 356), (CX + 130, 310)], [33, 36, 34, 28, 20, 13, 6])
    for points in ([(328, 322), (286, 300), (268, 262), (300, 274), (312, 250), (332, 300)], [(334, 320), (346, 262), (376, 232), (376, 268), (394, 280), (350, 326)]):
        b.shape(points)
    for x, y, d in ((150, 340, -24), (152, 372, -26), (166, 404, -26)):
        b.polygon([(x + 18, y - 12), (x + d, y + 4), (x + 22, y + 12)])
    b.shape([(120, 436), (70, 462), (30, 480), (20, FOOT), (200, FOOT), (250, 478), (210, 446)])
    head(b)
    strands(b, (CX - 10, 92), 22, -86, 240, 15, 10, 46)
    for k in range(5):
        x = CX - 26 + k * 13
        b.polygon([(x - 6, 92), (x + (k - 2) * 5, 46 + abs(k - 2) * 12), (x + 6, 92)])
    for side in (-1, 1):
        b.polygon([(CX + side * 18, 96), (CX + side * 46, 80), (CX + side * 40, 100), (CX + side * 50, 114), (CX + side * 20, 112)])
    arm(b, (CX + 36, 162), (CX + 74, 206), (CX + 96, 168), 10)
    arm(b, (CX - 36, 162), (CX - 66, 216), (CX - 30, 258), 10)
    b.line((292, 440), (304, 70), 5)
    b.ribbon([(274, 26), (276, 66), (304, 80), (332, 66), (334, 26)], [0.6, 4.4, 4.4, 4.4, 0.6])
    b.polygon([(298, 80), (304, 6), (310, 80)])
    for x, y, r in ((60, 420, 5), (44, 396, 3.4), (246, 440, 4), (352, 420, 5), (368, 396, 3), (78, 446, 4)):
        b.ellipse(x, y, r, r * 1.2)
    finish(b, folder, "aqua")

    b = Brush(W, H, 3)  # 재앙신 칼립스: 찢긴 두건 옷과 뒤틀린 뿔, 등에서 뻗은 여섯 개의 긴 팔
    b.shape([(CX - 30, 120), (CX - 62, 172), (CX - 50, 270), (CX - 64, 360), (CX - 84, 430), (CX + 84, 430), (CX + 64, 360), (CX + 50, 270), (CX + 62, 172), (CX + 30, 120)])
    tatter(b, (CX - 78, 430), (CX + 78, 430), 62, 10)
    b.shape([(CX - 34, 152), (CX - 38, 106), (CX - 12, 64), (CX + 10, 56), (CX + 38, 106), (CX + 34, 152)])
    b.ribbon([(CX - 16, 84), (CX - 62, 78), (CX - 84, 36), (CX - 48, 6)], [11, 9, 5, 0.6])
    b.ribbon([(CX + 16, 84), (CX + 54, 66), (CX + 98, 54), (CX + 100, 4)], [11, 9, 5, 0.6])
    for side in (-1, 1):
        for elbow, hand in (((108, 116), (156, 34)), ((132, 190), (184, 142)), ((116, 262), (172, 292))):
            b.ribbon([(CX + side * 40, 182), (CX + side * elbow[0], elbow[1]), (CX + side * hand[0], hand[1])], [14, 8, 4])
            b.polygon([(CX + side * (elbow[0] - 8), elbow[1] - 4), (CX + side * (elbow[0] + 12), elbow[1] + 18), (CX + side * (elbow[0] + 6), elbow[1] - 4)])
            for k in (-1.4, -0.4, 0.6, 1.4):
                tip = (hand[0] + 24 - abs(k) * 5, hand[1] + k * 13 - 4)
                b.ribbon([(CX + side * hand[0], hand[1]), (CX + side * (hand[0] + tip[0]) / 2, (hand[1] + tip[1]) / 2 + k * 3), (CX + side * tip[0], tip[1])], [3.6, 2.6, 0.5])
    finish(b, folder, "calypse")

    b = Brush(W, H, 4)  # 사신 데스: 발이 없는 긴 두건 옷, 앙상한 손, 달처럼 큰 낫
    b.shape([(CX - 22, 118), (CX - 46, 170), (CX - 40, 260), (CX - 50, 350), (CX + 54, 340), (CX + 44, 250), (CX + 50, 170), (CX + 24, 118)])
    tatter(b, (CX - 46, 350), (CX + 50, 344), 126, 9)
    b.shape([(CX - 32, 152), (CX - 36, 102), (CX - 6, 54), (CX + 30, 98), (CX + 32, 152)])
    b.shape([(CX + 4, 58), (CX + 34, 60), (CX + 66, 92), (CX + 34, 96)])
    for side, elbow, hand in ((-1, (CX - 76, 218), (CX - 56, 262)), (1, (CX + 72, 212), (CX + 38, 246))):
        arm(b, (CX + side * 38, 172), elbow, hand, 15)
        b.shape([(elbow[0] - 14, elbow[1] - 16), (elbow[0] + 14, elbow[1] - 16), (elbow[0] + 10, elbow[1] + 30), (elbow[0] + side * 6, elbow[1] + 92), (elbow[0] - 12, elbow[1] + 34)])
    b.line((116, 474), (282, 50), 6)
    b.ribbon([(286, 46), (236, 14), (156, 14), (84, 54), (40, 130)], [10, 16, 16, 11, 0.6])
    b.polygon([(276, 40), (310, 34), (296, 66)])
    finish(b, folder, "death")


# ---------------------------------------------------------------- 십이장

def lords(project, folder):
    # 0 기사: 날개 투구, 방패와 검, 바람에 날리는 긴 망토
    b = Figure(project, "extra-soldier", 396, flip=True, x=246)
    b.shape(b.path((0.42, 0.2), (0.2, 0.28), (-0.02, 0.36), (-0.26, 0.42), (-0.2, 0.54), (-0.42, 0.64), (-0.28, 0.72), (-0.36, 0.88), (-0.1, 0.8), (0.0, 0.94), (0.16, 0.8), (0.4, 0.6)))
    b.ellipse(*b.at(0.505, 0.085), 24, 29)
    for side, u in ((-1, 0.44), (1, 0.57)):
        x, y = b.at(u, 0.08)
        b.shape([(x, y + 12), (x + side * 22, y - 14), (x + side * 46, y - 52), (x + side * 42, y - 22), (x + side * 58, y - 18), (x + side * 38, y + 2), (x + side * 46, y + 12), (x + side * 12, y + 22)])
    finish(b, folder, "lord_0")

    # 1 왕: 털 깃 망토와 높은 관, 땅에 짚은 큰 검
    b = Figure(project, "hass", 400)
    for k in range(9):
        t = k / 8
        b.ellipse(*b.at(0.08 + t * 0.84, 0.235 - math.sin(t * math.pi) * 0.06), 15, 13)
    b.shape(b.path((0.1, 0.24), (-0.06, 0.5), (-0.14, 0.8), (-0.22, 0.98), (0.0, 0.93), (0.1, 0.99), (0.22, 0.8), (0.28, 0.5), (0.26, 0.28)))
    b.shape(b.path((0.9, 0.24), (1.06, 0.5), (1.1, 0.8), (1.18, 0.98), (0.98, 0.93), (0.9, 0.99), (0.8, 0.8), (0.72, 0.5), (0.74, 0.28)))
    x, y = b.at(0.5, 0.03)
    for k in range(5):
        b.polygon([(x - 34 + k * 14, y + 8), (x - 26 + k * 13, y - 48 + abs(k - 2) * 13), (x - 20 + k * 14, y + 8)])
        b.ellipse(x - 26 + k * 13, y - 48 + abs(k - 2) * 13, 4.4, 4.4)
    b.polygon([(x - 32, y + 16), (x - 34, y), (x + 34, y), (x + 32, y + 16)])
    blade(b, b.at(1.2, 0.52), b.at(1.21, 1.0), 10)
    arm(b, b.at(0.86, 0.28), b.at(1.08, 0.36), b.at(1.2, 0.49), 14)
    finish(b, folder, "lord_1")

    # 2 뿔 달린 거구: 휜 뿔과 갈기, 어깨에 멘 도끼
    b = Figure(project, "rotata-stroud", 392, flip=True, wide=1.1, x=176)
    x, y = b.at(0.55, 0.07)
    strands(b, (x, y - 6), 30, 0, 110, 11, 12, 16)
    for side in (-1, 1):
        b.ribbon([(x + side * 16, y - 6), (x + side * 62, y + 2), (x + side * 92, y - 34), (x + side * 70, y - 78)], [14, 13, 9, 0.6])
        for k in range(4):
            px, py = b.at(0.5 + side * (0.22 + k * 0.06), 0.14 + k * 0.03)
            b.polygon([(px - 9, py + 10), (px + side * 10, py - 26 + k * 4), (px + 9, py + 10)])
    b.line(b.at(0.62, 0.6), b.at(1.0, -0.06), 8)
    x, y = b.at(0.97, 0.0)
    b.shape([(x - 8, y + 6), (x + 22, y - 44), (x + 62, y - 30), (x + 70, y + 24), (x + 44, y + 58), (x + 10, y + 30)])
    b.shape([(x - 4, y), (x - 44, y - 6), (x - 58, y - 44), (x - 26, y - 40), (x + 8, y - 28)])
    finish(b, folder, "lord_2")

    # 3 떠 있는 현자: 뾰족한 두건, 고리 지팡이, 둘레를 도는 책
    b = Figure(project, "dinel-eve", 410, flip=True, x=196)
    x, y = b.at(0.56, 0.02)
    b.shape([(x - 36, y + 40), (x - 20, y - 4), (x + 8, y - 40), (x + 52, y - 60), (x + 34, y - 18), (x + 36, y + 40)])
    b.line(b.at(1.06, 0.98), b.at(1.0, 0.14), 5)
    x, y = b.at(0.995, 0.06)
    b.ring(x, y, 32, 34, 6)
    b.polygon([(x, y - 26), (x + 10, y), (x, y + 26), (x - 10, y)])
    for u, v, t in ((-0.12, 0.2, 0.2), (-0.2, 0.62, -0.3), (1.3, 0.5, 0.35), (-0.02, 0.02, -0.25)):
        px, py = b.at(u, v)
        c, q = math.cos(t), math.sin(t)
        b.polygon([(px + m * c - n * q, py + m * q + n * c) for m, n in ((-22, -12), (0, -6), (22, -12), (22, 12), (0, 17), (-22, 12))])
    finish(b, folder, "lord_3")

    # 4 중갑: 깃털 투구, 가시 돋은 어깨, 큰 방패와 검
    b = Figure(project, "neo-radun", 408, flip=True)
    x, y = b.at(0.565, 0.065)
    b.ellipse(x, y + 4, 25, 30)
    b.ribbon([(x, y - 20), (x + 26, y - 52), (x + 70, y - 46), (x + 98, y), (x + 92, y + 60)], [9, 15, 17, 13, 0.6])
    for u, v, sx, sy in ((0.28, 0.14, -18, -44), (0.38, 0.11, -8, -40)):
        px, py = b.at(u, v)
        b.polygon([(px - 11, py + 10), (px + sx, py + sy), (px + 11, py + 10)])
    finish(b, folder, "lord_4")

    # 5 궁수: 높이 묶은 긴 머리, 큰 활, 깃털 달린 화살통
    b = Figure(project, "buddy", 410, flip=True, x=190)
    x, y = b.at(0.5, 0.06)
    b.ribbon([(x - 10, y), (x - 50, y - 44), (x - 100, y - 10), (x - 110, y + 70), (x - 160, y + 110)], [12, 17, 19, 13, 0.6])
    b.ribbon([(x - 10, y), (x - 60, y - 30), (x - 92, y + 30), (x - 88, y + 110), (x - 120, y + 170)], [12, 14, 14, 9, 0.6])
    bow = b.path((0.98, -0.06), (1.1, 0.08), (1.2, 0.3), (1.12, 0.52), (1.0, 0.7))
    b.ribbon(bow, [1.5, 6, 7, 6, 1.5])
    for end in (bow[0], bow[-1]):
        b.ribbon([end, (end[0] - 12, end[1] + (8 if end is bow[-1] else -8))], [3, 0.8])
    b.line(bow[0], b.at(0.94, 0.32), 1.8)
    b.line(bow[-1], b.at(0.94, 0.32), 1.8)
    for k in range(4):
        px, py = b.at(0.2 + k * 0.06, 0.2)
        b.line((px, py + 30), (px - 24, py - 40), 3)
        b.polygon([(px - 30, py - 34), (px - 34, py - 62), (px - 16, py - 56), (px - 16, py - 36)])
    finish(b, folder, "lord_5")

    # 6 마녀: 구부러진 큰 모자, 바닥에 끌리는 옷, 둘레를 떠도는 등불
    b = Figure(project, "kate", 400, flip=True)
    x, y = b.at(0.5, 0.035)
    b.ellipse(x, y + 6, 80, 15)
    b.shape([(x - 40, y + 6), (x - 28, y - 30), (x + 2, y - 60), (x + 46, y - 74), (x + 82, y - 52), (x + 46, y - 46), (x + 34, y - 22), (x + 40, y + 6)])
    for u, v in ((-0.06, 0.3), (1.08, 0.12)):
        px, py = b.at(u, v)
        b.line((px, py - 34), (px, py - 18), 2)
        b.polygon([(px - 10, py - 18), (px + 10, py - 18), (px + 14, py + 10), (px, py + 22), (px - 14, py + 10)])
    finish(b, folder, "lord_6")

    # 7 사냥꾼: 말린 양 뿔, 해진 긴 겉옷, 초승달 낫, 채찍 꼬리
    b = Figure(project, "erus", 410)
    x, y = b.at(0.43, 0.09)
    for side in (-1, 1):
        b.ribbon([(x + side * 18, y - 26), (x + side * 62, y - 44), (x + side * 92, y - 6), (x + side * 66, y + 26), (x + side * 46, y + 4)], [13, 14, 11, 8, 0.6])
    b.ribbon(b.path((0.75, 0.6), (1.1, 0.66), (1.2, 0.84), (1.04, 0.95), (1.24, 1.0)), [10, 9, 7, 5, 1])
    b.line(b.at(1.14, 0.9), b.at(0.98, -0.02), 6)
    x, y = b.at(0.98, -0.02)
    b.ribbon([(x, y), (x - 52, y - 36), (x - 120, y - 34), (x - 176, y + 14)], [9, 13, 10, 0.6])
    finish(b, folder, "lord_7")

    # 8 춤꾼: 두 부채, 몸을 감아 도는 긴 띠, 올린 머리와 비녀
    b = Figure(project, "aurora", 396, flip=True)
    x, y = b.at(0.56, 0.05)
    b.ellipse(x - 2, y - 14, 20, 18)
    b.line((x - 46, y - 34), (x + 40, y + 4), 3.4)
    b.line((x + 44, y - 36), (x - 42, y + 2), 3.4)
    for u, v, turn in ((-0.02, 0.24, -130), (1.06, 0.32, -50)):
        px, py = b.at(u, v)
        for k in range(9):
            angle = math.radians(turn - 68 + k * 17)
            b.polygon([(px, py), (px + math.cos(angle - 0.16) * 60, py + math.sin(angle - 0.16) * 60), (px + math.cos(angle) * 68, py + math.sin(angle) * 68), (px + math.cos(angle + 0.12) * 60, py + math.sin(angle + 0.12) * 60)])
    b.ribbon(b.path((-0.02, 0.26), (-0.28, 0.4), (-0.1, 0.56), (0.4, 0.6), (1.1, 0.62), (1.3, 0.76), (1.1, 0.9), (1.34, 0.99)), [2, 6, 8, 7, 8, 8, 6, 0.8])
    finish(b, folder, "lord_8")

    # 9 여왕: 햇살 같은 관, 치솟은 깃, 길게 끌리는 자락, 홀
    b = Figure(project, "jamie-eve", 386)
    b.shape(b.path((0.1, 0.62), (-0.1, 0.8), (-0.4, 0.95), (-0.8, 1.0), (1.5, 1.0), (1.9, 0.99), (1.3, 0.93), (1.06, 0.8), (0.9, 0.6)))
    x, y = b.at(0.42, 0.09)
    for k in range(11):
        angle = math.radians(-172 + k * 16.4)
        reach = 84 if k % 2 == 0 else 60
        b.polygon([(x + math.cos(angle - 0.1) * 30, y + math.sin(angle - 0.1) * 34), (x + math.cos(angle) * reach, y + math.sin(angle) * reach), (x + math.cos(angle + 0.1) * 30, y + math.sin(angle + 0.1) * 34)])
    for side, u in ((-1, 0.2), (1, 0.72)):
        px, py = b.at(u, 0.25)
        b.shape([(px - side * 10, py + 6), (px + side * 26, py - 40), (px + side * 46, py - 72), (px + side * 50, py - 20), (px + side * 34, py + 20)])
    b.line(b.at(1.22, 0.98), b.at(1.16, 0.2), 5)
    x, y = b.at(1.16, 0.17)
    b.ellipse(x, y, 13, 13)
    b.polygon([(x - 11, y - 6), (x, y - 46), (x + 11, y - 6)])
    b.ring(x, y, 24, 24, 4)
    arm(b, b.at(0.8, 0.32), b.at(1.04, 0.42), b.at(1.17, 0.36), 11)
    finish(b, folder, "lord_9")

    # 10 자객: 등에 멘 두 자루 칼, 여우 귀 두건, 길게 날리는 목도리
    b = Figure(project, "largo", 410, flip=True, x=206)
    x, y = b.at(0.58, 0.09)
    b.ellipse(x, y, 33, 38)
    for side in (-1, 1):
        b.polygon([(x + side * 8, y - 28), (x + side * 34, y - 70), (x + side * 34, y - 14)])
    blade(b, b.at(0.3, 0.2), b.at(0.98, 0.64), 6)
    blade(b, b.at(0.84, 0.2), b.at(0.16, 0.64), 6)
    b.ribbon(b.path((0.5, 0.2), (0.16, 0.14), (-0.1, 0.22), (-0.3, 0.12), (-0.54, 0.2)), [14, 15, 13, 10, 0.6])
    b.ribbon(b.path((0.5, 0.22), (0.14, 0.24), (-0.12, 0.34), (-0.36, 0.28)), [12, 12, 10, 0.6])
    finish(b, folder, "lord_10")

    # 11 무사: 초승달 투구, 두 자루 긴 칼, 넓은 어깨판
    b = Figure(project, "hetos", 408, flip=True)
    b.polygon(b.path((0.78, -0.05), (1.2, -0.05), (1.2, 0.47), (0.84, 0.47), (0.82, 0.3)), 0)
    x, y = b.at(0.5, 0.06)
    b.ellipse(x, y, 27, 31)
    b.ribbon([(x, y + 10), (x, y + 60)], [16, 30])
    for side in (-1, 1):
        b.shape([(x + side * 10, y - 6), (x + side * 52, y + 6), (x + side * 62, y + 36), (x + side * 26, y + 28)])
        b.ribbon([(x, y - 22), (x + side * 40, y - 36), (x + side * 64, y - 72), (x + side * 58, y - 106)], [7, 10, 7, 0.6])
        px, py = b.at(0.5 + side * 0.32, 0.25)
        b.shape([(px - side * 24, py - 14), (px + side * 22, py - 10), (px + side * 40, py + 62), (px + side * 18, py + 70), (px - side * 14, py + 40)])
    b.ribbon(b.path((0.86, 0.46), (0.4, 0.58), (-0.1, 0.72), (-0.42, 0.88)), [4, 6, 6, 1.5])
    b.ribbon(b.path((0.8, 0.5), (0.3, 0.6), (-0.3, 0.7)), [4, 5, 1.5])
    b.line(b.at(0.86, 0.46), b.at(1.02, 0.4), 8)
    finish(b, folder, "lord_11")


# ---------------------------------------------------------------- 갓슬레이어와 에라드

def people(project, folder):
    b = Brush(W, H, 11)  # 갓슬레이어: 하늘로 치켜든 큰 검, 한쪽으로 휘날리는 망토와 긴 머리
    torso(b, 48, 27, 32, lean=4)
    for hip, knee, foot, toe in (((CX - 12, 276), (CX - 42, 370), (CX - 64, FOOT - 6), -1), ((CX + 18, 276), (CX + 46, 360), (CX + 38, FOOT - 6), 1)):
        b.ribbon([hip, knee, foot], [20, 14, 10])
        b.shape([(foot[0] - 11, foot[1] - 34), (foot[0] + 12, foot[1] - 34), (foot[0] + 12, foot[1] + 4), (foot[0] + toe * 18, foot[1] + 6), (foot[0] - 12, foot[1] + 4)])
        b.polygon([(knee[0] - 14, knee[1] - 14), (knee[0] + toe * 4, knee[1] - 26), (knee[0] + 14, knee[1] - 12), (knee[0] + 10, knee[1] + 14), (knee[0] - 10, knee[1] + 14)])
    b.shape([(CX + 10, 146), (CX + 66, 182), (CX + 112, 236), (CX + 176, 260), (CX + 194, 318), (CX + 150, 330), (CX + 176, 396), (CX + 122, 380), (CX + 128, 446), (CX + 84, 396), (CX + 70, 470), (CX + 46, 380), (CX + 28, 280)])
    for side in (-1, 1):
        b.shape([(CX + side * 26, 140), (CX + side * 60, 134), (CX + side * 78, 156), (CX + side * 62, 184), (CX + side * 38, 182)])
        b.polygon([(CX + side * 52, 142), (CX + side * 82, 116), (CX + side * 72, 158)])
        b.shape([(CX + side * 8, 262), (CX + side * 44, 268), (CX + side * 56, 330), (CX + side * 28, 324), (CX + side * 8, 300)])
    head(b)
    for k in range(9):
        b.ribbon([(CX - 10 + k * 2, 86 + k * 3), (CX + 34, 78 + k * 8), (CX + 78 + k * 6, 100 + k * 7), (CX + 122 + k * 5, 84 + k * 14)], [9, 10, 6, 0.5])
    arm(b, (CX - 44, 160), (CX - 76, 116), (CX - 52, 70), 14)
    blade(b, (CX - 52, 72), (CX - 66, 4), 8)
    arm(b, (CX + 44, 162), (CX + 64, 220), (CX + 40, 266), 13)
    finish(b, folder, "godslayer")

    b = Brush(W, H, 61)  # 에라드 관리자 페르니: 불꽃처럼 치솟은 깃과 관, 양팔을 벌려 펼친 찢긴 망토
    torso(b, 42, 25, 31)
    b.shape([(CX - 29, 250), (CX - 44, 330), (CX - 66, 420), (CX - 110, FOOT), (CX + 110, FOOT), (CX + 66, 420), (CX + 44, 330), (CX + 29, 250)])
    for side in (-1, 1):
        b.shape([(CX + side * 22, 148), (CX + side * 96, 146), (CX + side * 166, 104), (CX + side * 186, 160), (CX + side * 154, 190), (CX + side * 178, 250), (CX + side * 136, 252), (CX + side * 150, 326), (CX + side * 108, 312), (CX + side * 112, 390), (CX + side * 74, 356), (CX + side * 66, 420), (CX + side * 36, 330)])
        for k in range(4):
            x = CX + side * (22 + k * 13)
            b.ribbon([(x, 162), (x + side * (4 + k * 5), 118), (x + side * (4 + k * 10), 74 - (3 - k) * 12 + k * 8)], [11, 10, 0.6])
        arm(b, (CX + side * 40, 160), (CX + side * 110, 148), (CX + side * 172, 92), 13)
        for k in range(5):
            angle = math.radians(-140 + k * 26) if side > 0 else math.radians(-40 - k * 26)
            b.ribbon([(CX + side * 172, 92), (CX + side * 172 + math.cos(angle) * 26, 92 + math.sin(angle) * 26)], [4, 0.6])
    head(b, r=22)
    strands(b, (CX, 96), 20, 0, 150, 11, 9, 16)
    for k in range(7):
        t = k / 6 * 2 - 1
        x = CX + t * 27
        b.polygon([(x - 7, 92), (x + t * 16, 26 + abs(t) * 30), (x + 7, 92)])
    finish(b, folder, "perni")

    for i, art in enumerate(OFFICERS):
        mask = outline(project, art)
        mask.thumbnail((720, 900), Image.LANCZOS)
        keep(folder, f"officer_{i}", np.where(np.asarray(mask) > 110, 255, 0))
    # 4 짐승 간부: 호랑이 귀와 갈기, 발톱, 굵은 꼬리
    b = Figure(project, "hass", 410, flip=True, wide=1.02, x=178)
    x, y = b.at(0.5, 0.075)
    strands(b, (x, y - 4), 34, 0, 96, 13, 12, 14)
    for side in (-1, 1):
        b.shape([(x + side * 4, y - 6), (x + side * 14, y - 46), (x + side * 38, y - 42), (x + side * 40, y + 4)])
        px, py = b.at(0.5 + side * 0.5, 0.56)
        arm(b, b.at(0.5 + side * 0.34, 0.26), b.at(0.5 + side * 0.52, 0.42), (px, py), 20)
        for k in range(4):
            b.ribbon([(px - 12 + k * 8, py), (px - 14 + k * 10, py + 18), (px - 10 + k * 9 + side * 6, py + 40)], [6, 4.6, 0.5])
    b.ribbon(b.path((0.62, 0.66), (1.06, 0.8), (1.22, 0.64), (1.12, 0.46), (1.28, 0.36)), [12, 13, 12, 11, 5])
    finish(b, folder, "officer_4")
    # 5 갈라진 간부: 같은 그림자가 셋으로 갈라진다
    b = Figure(project, "shadow-figure", 420)
    mask = outline(project, "shadow-figure")
    for dx, height in ((-74, 372), (78, 364)):
        ghost = mask.resize((int(height * mask.width / mask.height), height), Image.LANCZOS)
        if dx > 0:
            ghost = ghost.transpose(Image.FLIP_LEFT_RIGHT)
        b.stamp(ghost, CX + dx - ghost.width / 2, FOOT - height)
    finish(b, folder, "officer_5")


def world(project, folder):
    """에라디온 원반: 윗면은 게임의 세계지도를 둥글게 오린 것, 옆면은 층층의 흙과 바위."""
    source = Image.open(project / "Assets/MiniPandoras/Art/PixelGen/UI/WorldMap/world_map.png").convert("RGBA")
    size = 1024
    sea = np.array([38, 92, 140], dtype=float)
    base = Image.new("RGBA", (size, size), tuple(int(v) for v in sea) + (255,))
    scale = size * 0.92 / max(source.size)
    land = source.resize((int(source.width * scale), int(source.height * scale * 1.28)), Image.NEAREST)
    base.alpha_composite(land, ((size - land.width) // 2, (size - land.height) // 2))
    pixels = np.asarray(base).astype(float)
    yy, xx = np.mgrid[0:size, 0:size]
    reach = np.hypot(xx - size / 2, yy - size / 2) / (size / 2)
    edge = np.clip((reach - 0.88) / 0.12, 0, 1)[..., None]
    foam = (np.abs(reach - 0.985) < 0.012)
    pixels[..., :3] = pixels[..., :3] * (1 - edge * 0.5) + np.array([120, 190, 220]) * edge * 0.5
    pixels[foam, :3] = (230, 246, 255)
    pixels[..., 3] = (reach < 1) * 255
    save(pixels, folder, "world_top.png")

    rng = np.random.default_rng(7)
    w, h = 512, 256
    yy, xx = np.mgrid[0:h, 0:w]
    layer = np.floor((yy + pg.tileable_noise(512, 8, rng)[:h] * 40) / 16)
    noise = pg.tileable_noise(512, 32, rng)[:h] * 0.6 + pg.tileable_noise(512, 16, rng)[:h] * 0.4
    shade = np.clip(0.75 - yy / h * 0.55 + ((layer % 3) - 1) * 0.12 + (noise - 0.5) * 0.5, 0, 1)
    rock = tone(shade, ramp("#120e1e", "#241a2c", "#3b2a38", "#573f45", "#7a5a52", "#9f7d66"))
    grass = yy < 10 + pg.tileable_noise(512, 32, rng)[:h] * 18
    rock[grass] = tone(noise, ramp("#22402c", "#356038", "#4f8446", "#76a857"))[grass]
    save(np.dstack([rock, np.full((h, w), 255)]), folder, "world_rock.png")

    w, h = 64, 256
    streak = pg.tileable_noise(256, 64, rng)[:, :w] * 0.6 + pg.tileable_noise(256, 32, rng)[:, :w] * 0.4
    streak = np.repeat(streak[:1], h, axis=0) * 0.55 + streak * 0.45
    fade = np.clip(1 - np.arange(h) / h, 0, 1)[:, None]
    sides = np.sin(np.linspace(0, math.pi, w))[None]
    alpha = np.clip((streak * 1.5 - 0.3), 0, 1) * fade ** 0.7 * sides * 255
    image = np.dstack([np.full((h, w), 214), np.full((h, w), 238), np.full((h, w), 255), alpha])
    save(image, folder, "waterfall.png")


def main(project):
    folder = project / "Assets/MiniPandoras/Art/PixelGen/CutInStage"
    folder.mkdir(parents=True, exist_ok=True)
    gods(project, folder)
    lords(project, folder)
    people(project, folder)
    world(project, folder)


if __name__ == "__main__":
    main(Path(sys.argv[1]))
