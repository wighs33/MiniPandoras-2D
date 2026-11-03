"""1장 랑이 세계를 설명하는 컷인 그림(사용자 요청 2026-11-03: 웹게임 설명 그림 대신 생성기로, 신들은 정확한 모습이 아닌 실루엣으로).

세 장면(가상 384×216, 점은 그 두 배)마다 배경 한 장과, 그 위에 겹쳐 랑이 말하는 동안 서서히 밝아지는 빛 한 장을 그린다.
  creation.png, creation_glow.png  태초의 열 신: 별밭에 줄지어 선 열 신과 그 아래 떠 있는 세계 에라디온.
                                   빛: 대지신 모니아(주황)·창조신 카타(초록)·해신 아쿠아(파랑)의 빛줄기가 에라디온에 내리고 땅에 생명이 돋는다.
  humans.png, humans_glow.png      신을 돕는 인간: 먼 하늘의 열 신과 언덕 위 사람들. 빛: 신들에게서 사람들에게 내려오는 빛 알갱이.
  blessing.png, blessing_glow.png  축복신 판도라: 커다란 판도라가 손을 뻗어 오른쪽 사람들에게 힘을 나눈다.
                                   빛: 뒤의 빛무리와 손끝에서 사람들에게 흐르는 금빛 알갱이.
신들은 모두 정확한 모습 대신 얼굴도 옷도 보이지 않는 어두운 실루엣에 신의 빛깔로 테두리 빛이 도는 모습이다(silhouette).
신마다 실루엣 모양(후광, 뿔, 여섯 날개, 지팡이, 둘, 거구, 두건, 낫 …)과 둘레를 떠도는 기운이 다르다.
차례·빛깔·이름은 웹게임 설명 그림을 따른다.

사용: python Tools/PixelGen/cutin_gods.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

import pixelgen as pg
from station import text_mask

W, H = 768, 432             # 점(가상 화면 384×216의 두 배). 그림은 두 배로 키워 쓴다.
GOLD = pg.hexes("#f0c868")[0].astype(float)
INK = pg.hexes("#100c1c")[0].astype(float)
WHITE = np.array([255.0, 255.0, 255.0])

# 열 신: 이름, 빛깔, 실루엣 모양, 둘레를 떠도는 기운
GODS = [
    ("축복신 판도라", "#e6c068", "halo", None),
    ("마신 바알", "#a06ae4", "horns", "smoke"),
    ("대지신 모니아", "#f0a050", "earth", "motes"),
    ("천신 미카엘", "#f0e4b0", "wings", None),
    ("창조신 카타", "#70c468", "staff", None),
    ("차원신 디바인", "#98b2f2", "twin", None),
    ("거신 헤토스", "#c08e7c", "giant", "ground"),
    ("해신 아쿠아", "#60c4f6", "sea", "drops"),
    ("재앙신 칼립스", "#e05050", "hood", "embers"),
    ("사신 데스", "#b0b0bc", "reaper", None),
]
MONIA, KATA, AQUA = 2, 4, 7

YY, XX = np.mgrid[0:H, 0:W]
DITHER = pg.bayer(H, W)


# ── 칠하기 ─────────────────────────────────────────────

def color(code):
    return pg.hexes(code)[0].astype(float)


def lighten(rgb, amount):
    return rgb + (WHITE - rgb) * amount


def stepped(value, levels):
    """0~1 값을 levels 단계로 끊고 단계 경계는 4×4 디더로 섞는다(도트 그라데이션)."""
    return np.clip(np.floor(value * levels + (DITHER - 0.5) * 0.9) / levels, 0, 1)


def paint(layer, rgb, alpha):
    """layer(RGB 배경이나 RGBA 빛, 실수)에 그 색을 칸마다 alpha(0~1)만큼 덧칠한다."""
    a = np.clip(alpha, 0, 1)
    if np.ndim(a) == 0:
        a = np.full((H, W), float(a))
    a = a[..., None]
    if layer.shape[-1] == 3:
        layer[:] = layer * (1 - a) + rgb * a
        return
    under = layer[..., 3:4]
    total = a + under * (1 - a)
    layer[..., :3] = (rgb * a + layer[..., :3] * under * (1 - a)) / np.where(total > 0, total, 1)
    layer[..., 3:4] = total


def polygon(points):
    """점 목록으로 둘러싼 칸(네 배로 그려 줄여 가장자리를 고르게)."""
    image = Image.new("L", (W * 4, H * 4), 0)
    ImageDraw.Draw(image).polygon([(x * 4, y * 4) for x, y in points], fill=255)
    return np.array(image.resize((W, H), Image.BOX)) > 127


def ellipse(cx, cy, rx, ry):
    return ((XX + 0.5 - cx) / rx) ** 2 + ((YY + 0.5 - cy) / ry) ** 2 <= 1


def falloff(cx, cy, radius, squash=1.0):
    """(cx, cy)에서 멀어질수록 0으로 줄어드는 빛 세기(가운데 1)."""
    return np.exp(-np.hypot(XX + 0.5 - cx, (YY + 0.5 - cy) * squash) / radius)


def aura(layer, cx, cy, radius, rgb, strength, squash=1.0):
    """둥근 빛무리(단계로 끊어 도트답게)."""
    paint(layer, rgb, stepped(falloff(cx, cy, radius, squash), 6) * strength)


def dot(layer, x, y, rgb, alpha=1.0, size=1):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W - size + 1 and 0 <= y < H - size + 1:
        a = alpha
        if layer.shape[-1] == 3:
            layer[y:y + size, x:x + size] = layer[y:y + size, x:x + size] * (1 - a) + rgb * a
        else:
            mask = np.zeros((H, W))
            mask[y:y + size, x:x + size] = a
            paint(layer, rgb, mask)


def sparkle(layer, x, y, rgb, arm=2, alpha=1.0):
    """십자로 반짝이는 빛 한 점."""
    mask = np.zeros((H, W))
    x, y = int(round(x)), int(round(y))
    for k in range(-arm, arm + 1):
        fade = alpha * (1 - abs(k) / (arm + 1))
        if 0 <= y < H and 0 <= x + k < W:
            mask[y, x + k] = max(mask[y, x + k], fade)
        if 0 <= y + k < H and 0 <= x < W:
            mask[y + k, x] = max(mask[y + k, x], fade)
    paint(layer, rgb, mask)


def write(layer, text, size, cx, top, rgb, shadow=INK):
    """맑은 고딕 굵게 글자를 가운데 맞춰 쓴다(아래·오른쪽 한 점에 진한 그림자). 돌려주는 값: 글자 너비."""
    mask = text_mask(text, size, 70 if size <= 11 else 80 if size <= 13 else 110)
    h, w = mask.shape
    left = int(round(cx - w / 2))
    for dx, dy, ink, alpha in ((1, 1, shadow, 0.9), (0, 0, rgb, 1.0)):
        layer_mask = np.zeros((H, W))
        layer_mask[top + dy:top + dy + h, left + dx:left + dx + w] = mask * alpha
        paint(layer, ink, layer_mask)
    return w


def title(layer, text, sub=None):
    """맨 위 가운데 금빛 제목과 양옆 금줄, 있으면 그 아래 작은 설명."""
    width = write(layer, text, 24, W / 2, 14, GOLD)
    for side in (-1, 1):
        inner = W / 2 + side * (width / 2 + 12)
        outer = inner + side * 64
        line = np.zeros((H, W))
        line[26:28, int(min(inner, outer)):int(max(inner, outer))] = 0.7
        paint(layer, GOLD * 0.8, line)
        diamond = np.abs(XX + 0.5 - inner) + np.abs(YY + 0.5 - 27) <= 3
        paint(layer, GOLD, diamond)
    if sub:
        write(layer, sub, 13, W / 2, 46, lighten(GOLD, 0.45) * 0.92)


def starfield(layer, rng, count, bottom):
    """작은 별(한 점, 밝기 여러 가지)과 드문 반짝이는 별."""
    for _ in range(count):
        x, y = rng.integers(0, W), rng.integers(0, bottom)
        dot(layer, x, y, lighten(color("#c8c4ff"), rng.random() * 0.6), 0.35 + rng.random() * 0.5)
    for _ in range(count // 14):
        sparkle(layer, rng.integers(10, W - 10), rng.integers(8, bottom), color("#fff4d8"), 2, 0.8)


def night(top_code, bottom_code, horizon, rng, haze=None):
    """위에서 아래로 두 빛깔이 섞이는 밤하늘과 성운 같은 옅은 얼룩."""
    t = stepped(np.clip(YY / horizon, 0, 1), 14)
    canvas = color(top_code) * (1 - t[..., None]) + color(bottom_code) * t[..., None]
    if haze is not None:
        cloud = pg.fractal_noise(H, W, rng, ((90, 0.6), (30, 0.3), (9, 0.1)))
        paint(canvas, color(haze), stepped(np.clip((cloud - 0.5) * 2.2, 0, 1), 4) * 0.22)
    return canvas


# ── 신의 실루엣 ────────────────────────────────────────

def bezier(p0, p1, p2, count=12):
    """p0에서 p2로 p1 쪽으로 휘는 곡선 위 점들(p0 포함, p2 뺌)."""
    t = np.linspace(0, 1, count, endpoint=False)[:, None]
    points = (1 - t) ** 2 * np.array(p0) + 2 * (1 - t) * t * np.array(p1) + t ** 2 * np.array(p2)
    return [tuple(point) for point in points]


def placer(cx, top, h, wide=1.0, sway=0.0):
    """키에 대한 비율 자리(u: 가운데에서 옆, v: 정수리에서 아래)를 점 자리로. 옷자락은 아래로 갈수록 sway만큼 옆으로 날린다."""
    def at(u, v):
        drift = sway * max(0.0, (v - 0.4) / 0.6) ** 2
        return cx + (u * wide + drift) * h, top + v * h
    return at


def robed(at, h, flare=0.24, wave=0.012, ragged=False):
    """머리·목·둥근 어깨·옆에 붙인 팔·아래로 퍼지는 옷자락. ragged면 끝단이 찢긴 듯 들쭉날쭉하다."""
    def side(sign):
        return (bezier(at(sign * 0.028, 0.15), at(sign * 0.04, 0.2), at(sign * 0.13, 0.215))
                + bezier(at(sign * 0.13, 0.215), at(sign * 0.17, 0.24), at(sign * 0.155, 0.36))
                + bezier(at(sign * 0.155, 0.36), at(sign * 0.15, 0.48), at(sign * 0.125, 0.52))
                + bezier(at(sign * 0.125, 0.52), at(sign * 0.15, 0.75), at(sign * flare, 0.97)))

    hem = []
    for k, u in enumerate(np.linspace(flare, -flare, 17)):
        drop = (0.03 if k % 2 else -0.01) if ragged else wave * np.sin(u * 60 + 0.5)
        hem.append(at(u, 0.97 + drop))
    head_x, head_y = at(0, 0.085)
    head_rx = at(0.062, 0)[0] - at(0, 0)[0]
    return polygon(side(1) + hem + side(-1)[::-1]) | ellipse(head_x, head_y, head_rx, 0.085 * h)


def long_hair(at, flow=0.0):
    """어깨 아래까지 흘러내린 긴 머리. flow만큼 머리끝이 옆으로 날린다."""
    return polygon([at(-0.05, 0.02), at(-0.095, 0.13), at(-0.125 + flow * 0.4, 0.3), at(-0.1 + flow, 0.48)]
                   + [at(-0.06 + flow * 0.3, 0.34), at(0.06, 0.34), at(0.095 + flow * 0.3, 0.44), at(0.115, 0.3), at(0.095, 0.13), at(0.05, 0.02)])


def figure_shape(cx, foot, h, shape, sway=0.0, reach=0.0, train=False):
    """
    신 하나의 실루엣 칸과, 실루엣 밖에서 신의 빛깔로 밝게 빛나는 칸(후광, 지팡이 끝 구슬, 낫 날, 갓슬레이어의 검).
    reach가 있으면 팔을 앞으로 뻗고(양수면 오른쪽, 음수면 왼쪽, 그 아래로 소매가 늘어진다), train이면 옷자락이 날리는 쪽으로 길게 끌린다.
    돌려주는 값: (실루엣, 빛나는 칸, 손 자리 또는 None)
    """
    top = foot - h
    wide = 1.35 if shape == "giant" else 1.0
    at = placer(cx, top, h, wide, sway)
    body = robed(at, h, flare=0.3 if shape == "sea" else 0.24, wave=0.025 if shape == "sea" else 0.012, ragged=shape == "hood")
    bright = np.zeros((H, W), dtype=bool)
    hand = None
    if shape in ("halo", "earth", "sea"):
        body |= long_hair(at, -0.1 if train else 0.0)
    if shape == "halo":
        x, y = at(0, -0.04)
        bright |= ellipse(x, y, 0.1 * h, 0.03 * h) & ~ellipse(x, y, 0.074 * h, 0.017 * h)
    elif shape == "earth":
        for u, v in ((-0.045, 0.015), (0, -0.002), (0.045, 0.015)):
            x, y = at(u, v)
            body |= ellipse(x, y, 0.02 * h, 0.02 * h)
    elif shape == "horns":
        for sign in (-1, 1):
            body |= polygon(bezier(at(sign * 0.035, 0.035), at(sign * 0.15, 0.04), at(sign * 0.13, -0.1))
                            + bezier(at(sign * 0.13, -0.1), at(sign * 0.09, 0.0), at(sign * 0.02, 0.02)))
            body |= polygon([at(sign * 0.1, 0.22), at(sign * 0.2, 0.17), at(sign * 0.16, 0.25)])
    elif shape == "wings":
        for sign in (-1, 1):
            for k in range(3):
                root, tip = at(sign * 0.05, 0.24 + 0.05 * k), at(sign * (0.44 - 0.08 * k), 0.02 + 0.16 * k)
                end = at(sign * 0.08, 0.42 + 0.08 * k)
                lower = bezier(tip, at(sign * (0.3 - 0.05 * k), 0.36 + 0.1 * k), end, 9)
                feathers = [(x + sign * (0 if j % 2 else -0.025 * h), y + (0.03 * h if j % 2 else 0)) for j, (x, y) in enumerate(lower)]
                body |= polygon(bezier(root, at(sign * (0.2 - 0.03 * k), 0.0 + 0.1 * k), tip, 9) + feathers + [end])
    elif shape == "staff":
        body |= polygon([at(0.12, 0.215), at(0.16, 0.21), at(0.225, 0.33), at(0.19, 0.37)])
        body |= polygon([at(0.2, -0.06), at(0.225, -0.06), at(0.225, 0.99), at(0.2, 0.99)])
        x, y = at(0.212, -0.1)
        bright |= ellipse(x, y, 0.045 * h, 0.045 * h)
        for sign in (-1, 1):
            leaf_x, leaf_y = at(0.212 + sign * 0.055, -0.07)
            bright |= ellipse(leaf_x, leaf_y, 0.03 * h, 0.014 * h)
    elif shape == "twin":
        return figure_shape(cx - 0.09 * h, foot, h, "plain", sway)[0] | figure_shape(cx + 0.1 * h, foot, h * 0.94, "plain", -sway)[0], bright, None
    elif shape == "hood":
        body |= polygon([at(0, -0.07), at(0.085, 0.05), at(0.12, 0.2), at(-0.12, 0.2), at(-0.085, 0.05)])
    elif shape == "slayer":
        body |= polygon(bezier(at(-0.12, 0.22), at(-0.3, 0.5), at(-0.42, 0.98)) + [at(0.38, 0.98)]
                        + bezier(at(0.38, 0.98), at(0.26, 0.5), at(0.12, 0.22)))
        body |= polygon([at(0.1, 0.2), at(0.16, 0.19), at(0.26, -0.02), at(0.21, -0.04)])
        blade = polygon([at(0.215, -0.02), at(0.25, -0.02), at(0.26, -0.46), at(0.237, -0.52), at(0.212, -0.46)])
        body |= blade | polygon([at(0.17, -0.03), at(0.3, -0.03), at(0.3, -0.005), at(0.17, -0.005)])
        bright |= blade & (XX < at(0.237, 0)[0])
    elif shape == "reaper":
        x, y = at(0, 0.08)
        body |= ellipse(x, y, 0.085 * h, 0.095 * h)
        body |= polygon([at(-0.2, 0.98), at(-0.175, 0.98), at(0.175, -0.07), at(0.15, -0.07)])
        blade_x, blade_y = at(-0.02, -0.03)
        blade = ellipse(blade_x, blade_y, 0.21 * h, 0.075 * h) & ~ellipse(blade_x + 0.025 * h, blade_y + 0.025 * h, 0.2 * h, 0.07 * h)
        blade &= (XX < at(0.17, 0)[0]) & (YY < blade_y + 0.02 * h)
        body |= blade
        bright |= blade & pg.neighbours(~blade) & (YY > blade_y - 0.04 * h)
    if reach:
        s = 1 if reach > 0 else -1
        body |= polygon(bezier(at(s * 0.1, 0.205), at(reach * 0.5, 0.2), at(reach, 0.275))
                        + [at(reach + s * 0.03, 0.28), at(reach + s * 0.035, 0.3), at(reach, 0.31)]
                        + bezier(at(reach - s * 0.03, 0.31), at(reach - s * 0.06, 0.38), at(reach - s * 0.2, 0.42))
                        + bezier(at(reach - s * 0.2, 0.42), at(s * 0.3, 0.43), at(s * 0.16, 0.36)))
        hand = at(reach + s * 0.04, 0.295)
    if train:
        flow = -1 if sway < 0 else 1
        ripple = [at(flow * u, 0.99 - 0.012 * np.sin(u * 50)) for u in np.linspace(0.5, 0.0, 12)]
        body |= polygon(bezier(at(flow * 0.12, 0.5), at(flow * 0.22, 0.88), at(flow * 0.5, 0.99)) + ripple)
    return body, bright, hand


def silhouette(layer, cx, foot, height, rgb, shape, rng, brightness=1.0, sway=0.0, reach=0.0, train=False):
    """
    얼굴도 옷도 보이지 않는 어두운 실루엣과 신의 빛깔로 두른 테두리 빛(바깥 한 점은 밝게, 안쪽 두 점은 옅게 번진다).
    후광·지팡이 구슬·낫 날은 빛나는 칸으로 따로 밝다. 옷자락 끝은 몇 줄 흩어져 발밑 빛무리로 사라진다. 돌려주는 값: 손 자리.
    """
    body, bright, hand = figure_shape(cx, foot, height, shape, sway, reach, train)
    paint_silhouette(layer, body, bright, cx, foot, height, rgb, rng, brightness, sway)
    return hand


def paint_silhouette(layer, body, bright, cx, foot, height, rgb, rng, brightness=1.0, sway=0.0):
    """실루엣 칸을 칠한다: 어두운 몸, 빛깔 테두리 빛, 빛나는 칸, 발밑에서 흩어지는 옷자락과 빛무리(다른 모양의 실루엣도 이것으로 칠한다)."""
    fade = np.clip((YY + 0.5 - (foot - 0.06 * height)) / (0.06 * height), 0, 1)
    body &= DITHER >= fade
    aura(layer, cx + sway * height, foot - 0.02 * height, 0.12 * height, lighten(rgb, 0.3) * brightness, 0.3 * brightness, 3.2)

    paint(layer, INK * 0.9 + rgb * 0.06, body.astype(float))
    # 테두리 빛: 바깥 한 점은 밝고, 그 안쪽으로 몇 점(크게 그릴수록 넓게) 옅어지며 번진다.
    ring = body & pg.neighbours(~body)
    paint(layer, lighten(rgb, 0.55) * brightness, ring * 0.95)
    seen = ring
    for k in range(max(2, int(height / 45))):
        ring = body & ~seen & pg.neighbours(ring)
        seen = seen | ring
        paint(layer, rgb * brightness, ring * 0.45 * 0.62 ** k)
    paint(layer, lighten(rgb, 0.65) * brightness, bright * 0.95)
    for _ in range(int(height / 4)):
        x = cx + sway * height + rng.normal(0, 0.15 * height)
        y = foot - rng.random() * 0.06 * height
        dot(layer, x, y, lighten(rgb, 0.6) * brightness, 0.3, size=2)


def particles(layer, kind, cx, foot, height, rgb, rng, brightness=1.0):
    """실루엣 둘레를 떠도는 신의 기운: 흙빛 알갱이, 물방울, 불티, 보랏빛 연기, 거신의 발밑 빛."""
    h, top = height, foot - height
    if kind == "smoke":
        for side in (-1, 1):
            for k in range(22):
                s = k / 22
                x = cx + side * (0.12 * h + 0.1 * h * s + 0.03 * h * np.sin(s * 9))
                dot(layer, x, top - 0.05 * h - 0.25 * h * s, lighten(rgb, 0.3) * brightness, 0.45 * (1 - s), size=2)
    elif kind == "ground":
        aura(layer, cx, foot, 0.3 * h, rgb * brightness, 0.4, 3.0)
    elif kind in ("motes", "drops", "embers"):
        rising = {"motes": (0.5, 1.0), "drops": (0.2, 0.9), "embers": (0.0, 0.8)}[kind]
        for _ in range(int(h / 3)):
            x = cx + rng.normal(0, 0.22 * h)
            y = top + h * (rising[0] + rng.random() * (rising[1] - rising[0]))
            dot(layer, x, y, lighten(rgb, rng.random() * 0.5) * brightness, 0.4 + rng.random() * 0.5, size=1 + int(rng.random() < 0.3))


def god(layer, index, cx, foot, height, rng, brightness=1.0):
    """열 신 가운데 하나: 둘레의 옅은 빛무리, 떠도는 기운, 실루엣. 거신은 넓고 크다."""
    _, code, shape, kind = GODS[index]
    rgb = color(code)
    height = height * (1.12 if shape == "giant" else 1.0)
    aura(layer, cx, foot - height * 0.5, height * 0.34, rgb * brightness, 0.24 * brightness, 0.8)
    particles(layer, kind, cx, foot, height, rgb, rng, brightness)
    silhouette(layer, cx, foot, height, rgb, shape, rng, brightness, sway=0.04 * np.sin(index * 2.1))


def row_of_gods(layer, rng, height, foot, brightness, labels):
    """열 신을 가로로 줄 세운다(가운데가 조금 높은 활 모양). 돌려주는 값: 신마다 (x, 발밑 y)."""
    places = []
    for i in range(len(GODS)):
        x = 54 + i * (W - 108) / (len(GODS) - 1)
        y = foot + 10 * ((i - 4.5) / 4.5) ** 2
        god(layer, i, x, y, height, rng, brightness)
        if labels:
            title_name, code, _, _ = GODS[i]
            role, name = title_name.split(" ")
            tint = lighten(color(code), 0.35) * (0.6 + 0.4 * brightness)
            write(layer, name, 13, x, int(y + 5), tint)
            write(layer, role, 11, x, int(y + 21), tint * 0.8)
        places.append((x, y))
    return places


# ── 세계와 사람 ────────────────────────────────────────

def eradion(canvas, rng, cx, cy, rx, ry):
    """
    하늘에 뜬 세계 에라디온: 둥근 윗면(바다와 대륙, 산과 숲, 구름)과 아래로 뾰족한 흙·바위 밑동, 가장자리로 쏟아지는 폭포.
    돌려주는 값: 땅 칸(빛이 생명을 돋게 할 자리).
    """
    u, v = (XX + 0.5 - cx) / rx, (YY + 0.5 - cy) / ry
    top = u ** 2 + v ** 2 <= 1
    aura(canvas, cx, cy + 20, rx * 0.5, color("#4a78c0"), 0.25, 1.8)

    # 밑동: 앞 가장자리 아래로 흙과 바위 켜가 쌓여 내려가다 뾰족하게 끝난다.
    edge = cy + ry * np.sqrt(np.clip(1 - u ** 2, 0, 1))
    wobble = pg.noise(1, W, 12, rng)[0] * 10
    depth = (ry * 1.1) * np.clip(1 - u ** 2, 0, 1) ** 1.3 + wobble * np.clip(1 - u ** 2, 0, 1)
    under = (np.abs(u) < 1) & (YY + 0.5 > edge) & (YY + 0.5 < edge + depth)
    below = (YY + 0.5 - edge) / np.maximum(depth, 1)
    band = np.floor((YY + 0.5 - edge) / 6 + pg.noise(H, W, 10, rng) * 1.6).astype(int) % 3
    rock = np.where(band[..., None] == 1, color("#5a4a5a"), np.where(band[..., None] == 0, color("#6e5238"), color("#4c3a2c")))
    shade = stepped(np.clip(1 - below * 0.75 - np.clip(u, 0, 1) * 0.35, 0, 1), 4)[..., None]
    paint(canvas, rock * (0.35 + 0.65 * shade), under.astype(float))
    lip = under & (YY + 0.5 < edge + 3)
    paint(canvas, color("#4f8a3c"), lip.astype(float))

    # 윗면: 바다 위에 대륙, 빛은 왼쪽 위에서.
    lit = np.clip(0.75 - 0.35 * u - 0.25 * v, 0, 1)
    land_noise = pg.fractal_noise(H, W, rng, ((46, 0.6), (16, 0.3), (6, 0.1)))
    land = top & (land_noise > 0.52) & (u ** 2 + v ** 2 < 0.8)
    paint(canvas, pg.quantize(np.clip(lit * 0.7 + land_noise * 0.25, 0, 1), pg.WATER[1:6], 0.15).astype(float), top.astype(float))
    shore = top & ~land & pg.neighbours(land)
    paint(canvas, pg.WATER[6].astype(float), shore.astype(float))
    beach = land & pg.neighbours(~land)
    grass = pg.quantize(np.clip(lit * 0.75 + (land_noise - 0.52) * 0.8, 0, 1), pg.GRASS[2:7], 0.15).astype(float)
    paint(canvas, grass, land.astype(float))
    paint(canvas, pg.SAND[3].astype(float), beach.astype(float))
    trees = land & ~beach & (pg.noise(H, W, 9, rng) > 0.58) & ((XX + YY * 2) % 3 == 0)
    paint(canvas, pg.LEAF[1].astype(float), trees.astype(float))
    peaks = land & (land_noise > 0.72)
    paint(canvas, pg.STONE[3].astype(float), peaks.astype(float))
    paint(canvas, pg.STONE[5].astype(float), (peaks & (land_noise > 0.765)).astype(float))
    rim = top & pg.neighbours(~top) & (YY + 0.5 > cy)
    paint(canvas, color("#9ad0e8"), rim * 0.8)

    # 가장자리로 쏟아지는 폭포와 그 아래 물보라
    for fall_u in (-0.62, -0.18, 0.34, 0.7):
        x = int(cx + fall_u * rx)
        y0 = int(cy + ry * np.sqrt(1 - fall_u ** 2)) - 1
        length = int(ry * 1.1 * (1 - fall_u ** 2) ** 1.3 + 8)
        for k in range(length):
            alpha = 0.85 * (1 - k / length)
            for w in range(-1, 3):
                dot(canvas, x + w + int(np.sin((k + w) * 0.7) * 0.6), y0 + k, lighten(pg.WATER[5].astype(float), 0.25 + 0.2 * (w == 0)), alpha)
        for _ in range(10):
            dot(canvas, x + rng.normal(0, 3), y0 + length + rng.normal(0, 2), WHITE, 0.3, size=2)

    # 윗면에 걸린 작은 구름
    for cloud_x, cloud_y in ((cx - rx * 0.45, cy - ry * 0.3), (cx + rx * 0.3, cy + ry * 0.1), (cx + rx * 0.05, cy - ry * 0.55)):
        puff = np.zeros((H, W), dtype=bool)
        for _ in range(5):
            puff |= ellipse(cloud_x + rng.normal(0, 7), cloud_y + rng.normal(0, 2), 6 + rng.random() * 5, 3 + rng.random() * 2)
        paint(canvas, color("#eef4fa"), puff * 0.85)
        paint(canvas, color("#b8c8dc"), (puff & ~np.roll(puff, -2, axis=0)) * 0.7)
    return land


def person(layer, cx, foot, height, rng, pose, ink, rim, rim_alpha=0.9):
    """사람 실루엣 하나(머리·몸·팔다리). pose: stand, reach(두 팔을 위로), point(한 팔을 앞으로), child, dress, spear(긴 창)."""
    h, top = height, foot - height
    head_r = 0.13 * h
    shape = ellipse(cx, top + head_r, head_r, head_r * 1.05)
    torso_bottom = 0.6 if pose != "dress" else 0.92
    hem = 0.15 if pose != "dress" else 0.22
    shape |= polygon([(cx - 0.17 * h, top + 0.27 * h), (cx + 0.17 * h, top + 0.27 * h),
                      (cx + hem * h, top + torso_bottom * h), (cx - hem * h, top + torso_bottom * h)])
    shape |= ellipse(cx, top + 0.26 * h, 0.17 * h, 0.04 * h)
    if pose != "dress":
        for side in (-1, 1):
            shape |= polygon([(cx + side * 0.03 * h, top + 0.58 * h), (cx + side * 0.13 * h, top + 0.58 * h),
                              (cx + side * 0.11 * h, foot), (cx + side * 0.04 * h, foot)])
    arm = 0.055 * h
    for side in (-1, 1):
        sx, sy = cx + side * 0.15 * h, top + 0.3 * h
        if pose == "reach":
            ex, ey = cx + side * 0.27 * h, top - 0.02 * h
        elif pose == "point" and side == 1:
            ex, ey = cx + 0.42 * h, top + 0.2 * h
        else:
            ex, ey = cx + side * 0.2 * h, top + 0.58 * h
        shape |= polygon([(sx - arm, sy), (sx + arm, sy), (ex + arm, ey), (ex - arm, ey)])
    if pose == "spear":
        x = cx + 0.24 * h
        shape |= polygon([(x - 0.02 * h, foot - 0.05 * h), (x + 0.02 * h, foot - 0.05 * h), (x + 0.02 * h, top - 0.3 * h), (x - 0.02 * h, top - 0.3 * h)])
        shape |= polygon([(x - 0.06 * h, top - 0.3 * h), (x + 0.06 * h, top - 0.3 * h), (x, top - 0.48 * h)])
    paint(layer, ink, shape.astype(float))
    lit = shape & pg.neighbours(~shape) & ~np.roll(shape, -1, axis=0)
    paint(layer, rim, lit * rim_alpha)
    return cx, top + head_r


def mote_trail(layer, start, end, rgb, rng, lift, count, size=1):
    """start에서 end로 위로 휘며 흐르는 빛 알갱이(2차 곡선 위에 드문드문, 끝으로 갈수록 밝다)."""
    (x0, y0), (x1, y1) = start, end
    mx, my = (x0 + x1) / 2, min(y0, y1) - lift
    for k in range(count):
        t = (k + rng.random() * 0.6) / count
        x = (1 - t) ** 2 * x0 + 2 * (1 - t) * t * mx + t ** 2 * x1 + rng.normal(0, 1.5)
        y = (1 - t) ** 2 * y0 + 2 * (1 - t) * t * my + t ** 2 * y1 + rng.normal(0, 1.5)
        if rng.random() < 0.18:
            sparkle(layer, x, y, lighten(rgb, 0.6), 2, 0.9)
        else:
            dot(layer, x, y, lighten(rgb, 0.3 + 0.5 * t), 0.55 + 0.4 * t, size=size + int(rng.random() < 0.3))


def beam(layer, start, end, rgb, rng):
    """신에게서 세계로 내리는 빛줄기: 갈수록 넓어지는 띠, 가운데는 밝고, 둘레에 빛 알갱이."""
    (x0, y0), (x1, y1) = start, end
    length = np.hypot(x1 - x0, y1 - y0)
    tx, ty = (x1 - x0) / length, (y1 - y0) / length
    along = np.clip(((XX + 0.5 - x0) * tx + (YY + 0.5 - y0) * ty) / length, 0, 1)
    across = np.abs((XX + 0.5 - x0) * -ty + (YY + 0.5 - y0) * tx)
    width = 2 + 7 * along
    strength = np.clip(1 - across / width, 0, 1) * (0.35 + 0.65 * along)
    inside = (along > 0) & (along < 1) & (across < width)
    paint(layer, rgb, stepped(strength, 4) * inside * 0.75)
    paint(layer, lighten(rgb, 0.7), (strength > 0.75) * inside * 0.8)
    for _ in range(int(length / 5)):
        t = rng.random()
        side = rng.normal(0, 2 + 6 * t)
        dot(layer, x0 + tx * length * t - ty * side, y0 + ty * length * t + tx * side, lighten(rgb, 0.5), 0.8)
    aura(layer, x1, y1, 14, lighten(rgb, 0.3), 0.8, 1.8)


# ── 장면 ───────────────────────────────────────────────

def creation(rng):
    """태초의 열 신과 에라디온. 빛: 세 신의 빛줄기, 세 신의 빛무리, 땅에 돋는 초록 생명."""
    canvas = night("#0b0a1e", "#221a3c", H, rng, "#3a2c66")
    starfield(canvas, rng, 260, H)
    title(canvas, "태초의 열 신")
    gods = row_of_gods(canvas, rng, 58, 124, 0.7, labels=True)
    cx, cy, rx, ry = W / 2, 282, 232, 56
    land = eradion(canvas, rng, cx, cy, rx, ry)
    write(canvas, "에라디온", 14, cx, 404, lighten(GOLD, 0.2))

    glow = np.zeros((H, W, 4))
    targets = {MONIA: (cx - 96, cy - 6), KATA: (cx + 6, cy - 22), AQUA: (cx + 104, cy + 4)}
    for index, target in targets.items():
        x, y = gods[index]
        rgb = color(GODS[index][1])
        aura(glow, x, y - 30, 34, rgb, 0.55, 0.8)
        god(glow, index, x, y, 58, np.random.default_rng(index), 1.0)
        beam(glow, (x, y + 18), target, rgb, rng)
    green = color(GODS[KATA][1])
    ys, xs = np.nonzero(land)
    for k in rng.choice(len(xs), 40, replace=False):
        if rng.random() < 0.3:
            sparkle(glow, xs[k], ys[k], lighten(green, 0.6), 2, 0.9)
        else:
            dot(glow, xs[k], ys[k], lighten(green, 0.4), 0.9)
    return canvas, glow


def humans(rng):
    """신을 돕는 인간: 먼 하늘에 열 신, 언덕 위에 사람들. 빛: 신들에게서 사람들에게 내려오는 빛 알갱이."""
    canvas = night("#0d1430", "#2d4f68", 300, rng, "#2c4a7a")
    starfield(canvas, rng, 200, 260)
    title(canvas, "신을 돕는 인간", "세계가 커지자 신들은 자신들을 도울 인간을 만들었다")
    gods = row_of_gods(canvas, rng, 40, 118, 0.55, labels=False)

    # 먼 언덕(작은 나무와 불 켜진 오두막)과 가까운 언덕. 밤이라 풀빛을 푸르게 누른다.
    far = 258 + 12 * np.sin(XX / 70 + 1) + 6 * np.sin(XX / 23)
    paint(canvas, color("#1c3236"), (YY > far).astype(float))
    for x in range(8, W, 11):
        if rng.random() < 0.55:
            base = far[0, x] + 2
            paint(canvas, color("#14262a"), ellipse(x, base - 6, 4 + rng.random() * 2, 6 + rng.random() * 3).astype(float))
    for hut_x in (112, 640, 688):
        base = int(far[0, hut_x]) + 8
        paint(canvas, color("#120e14"), polygon([(hut_x - 9, base), (hut_x - 9, base - 8), (hut_x, base - 15), (hut_x + 9, base - 8), (hut_x + 9, base)]))
        paint(canvas, color("#ffc870"), ((np.abs(XX - hut_x) < 2) & (YY > base - 7) & (YY < base - 3)).astype(float))
        aura(canvas, hut_x, base - 5, 7, color("#ffb050"), 0.35)
    near = 318 + 14 * np.sin(XX / 110 + 2.2) + 5 * np.sin(XX / 31)
    hill = YY > near
    light = np.clip(0.8 - (YY - near) / 130 + pg.noise(H, W, 14, rng) * 0.25, 0, 1)
    grass = pg.quantize(light, pg.GRASS[1:6], 0.18).astype(float) * 0.62 + color("#1c2c48") * 0.38
    paint(canvas, grass, hill.astype(float))
    paint(canvas, pg.GRASS[5].astype(float) * 0.8, (hill & ~np.roll(hill, 1, axis=0)) * 0.9)
    for _ in range(70):
        x = rng.integers(0, W)
        dot(canvas, x, near[0, x] + rng.integers(14, 100), pg.FLOWERS[rng.integers(0, 4)].astype(float) * 0.85, 0.85)
    # 앞쪽 풀 무더기: 화면 아래 끝을 어두운 풀잎으로 두른다.
    for x in range(0, W, 3):
        tall = 10 + rng.integers(0, 14) + 8 * np.sin(x / 40)
        lean = rng.integers(-2, 3)
        for k in range(int(tall)):
            dot(canvas, x + lean * k / tall, H - 1 - k, color("#0e1a1c"), 1.0, size=2)

    xs = [150, 192, 230, 268, 306, 344, 384, 422, 460, 498, 536, 574, 616]
    heights = [42, 36, 46, 30, 44, 40, 48, 32, 44, 40, 46, 36, 42]
    poses = ["stand", "reach", "dress", "child", "point", "reach", "stand", "child", "dress", "reach", "stand", "dress", "point"]
    heads = []
    ground = [near[0, x] + 14 for x in xs]
    for x, g, h, pose in zip(xs, ground, heights, poses):
        heads.append(person(canvas, x, g, h if pose != "child" else h * 0.8, rng, pose, color("#141224"), color("#9cc0d8"), 0.6))

    glow = np.zeros((H, W, 4))
    for i, ((gx, gy), (_, code, _, _)) in enumerate(zip(gods, GODS)):
        hx, hy = heads[round(i * (len(heads) - 1) / (len(GODS) - 1))]
        mote_trail(glow, (gx, gy + 6), (hx, hy - 4), color(code), rng, -40, 26)
    for x, g, h, pose in zip(xs, ground, heights, poses):
        person(glow, x, g, h if pose != "child" else h * 0.8, rng, pose, color("#141224"), color("#ffe6a8"), 1.0)
    for x, y in heads:
        aura(glow, x, y + 8, 16, color("#ffd88a"), 0.45, 1.2)
    return canvas, glow


def blessing(rng):
    """축복신 판도라: 커다란 판도라의 실루엣이 손을 뻗어 사람들에게 힘을 나눈다. 빛: 빛무리·후광·금빛 알갱이."""
    canvas = night("#1a1226", "#3e2a1c", H, rng, "#5a3c2c")
    starfield(canvas, rng, 140, 300)
    title(canvas, "축복신 판도라", "인간이 스스로 살아가도록 자신의 힘을 나누어 준 신")
    gold = color(GODS[0][1])
    cx, foot, height = 196, 420, 300
    aura(canvas, cx, foot - height * 0.55, 120, gold * 0.8, 0.35)

    # 땅: 어두운 들판과 지평선의 옅은 금빛
    ground = 372 + 4 * np.sin(XX / 60)
    paint(canvas, color("#6a4a2a"), stepped(np.exp(-np.abs(YY - ground) / 10), 3) * 0.4)
    paint(canvas, color("#1a1410"), (YY > ground).astype(float))
    for _ in range(90):
        x = rng.integers(0, W)
        dot(canvas, x, ground[0, x] - rng.integers(0, 3), color("#2c2216"), 1.0, size=2)

    hand = silhouette(canvas, cx, foot, height, gold, "halo", rng, 0.9, sway=-0.06, reach=0.56, train=True)

    xs = [470, 512, 552, 594, 634, 676, 716]
    heights = [62, 54, 70, 50, 66, 58, 64]
    poses = ["reach", "stand", "reach", "child", "dress", "point", "stand"]
    heads = []
    for x, h, pose in zip(xs, heights, poses):
        heads.append(person(canvas, x, ground[0, x] + 2, h, rng, pose, color("#120c10"), color("#7a5a3a"), 0.6))

    glow = np.zeros((H, W, 4))
    aura(glow, cx, foot - height * 0.55, 150, gold, 0.45)
    aura(glow, hand[0], hand[1], 22, lighten(gold, 0.5), 0.9)
    silhouette(glow, cx, foot, height, gold, "halo", np.random.default_rng(5), 1.0, sway=-0.06, reach=0.56, train=True)
    sparkle(glow, hand[0] + 3, hand[1], WHITE, 5)
    for k, (x, y) in enumerate(heads):
        mote_trail(glow, hand, (x, y + 6), gold, rng, 40 + 10 * k, 30)
    for x, h, pose in zip(xs, heights, poses):
        person(glow, x, ground[0, x] + 2, h, rng, pose, color("#120c10"), lighten(gold, 0.4), 1.0)
    for x, y in heads:
        aura(glow, x, y + 14, 20, gold, 0.4, 1.1)
    return canvas, glow


def save_scene(project, out, name, canvas, glow):
    """장면 하나를 두 배로 키워 저장한다: <name>.png(배경), <name>_glow.png(빛), 빛이 다 찬 미리보기(Docs/ArtUpgrade/PixelGen)."""
    out.mkdir(parents=True, exist_ok=True)
    back = np.clip(canvas, 0, 255).astype(np.uint8)
    light = np.clip(glow, 0, 255)
    light[..., 3] *= 255
    light = light.astype(np.uint8)
    Image.fromarray(pg.upscale(back), "RGB").save(out / f"{name}.png")
    Image.fromarray(pg.upscale(light), "RGBA").save(out / f"{name}_glow.png")
    lit = Image.fromarray(pg.upscale(back), "RGB").convert("RGBA")
    lit.alpha_composite(Image.fromarray(pg.upscale(light), "RGBA"))
    lit.convert("RGB").save(project / f"Docs/ArtUpgrade/PixelGen/cutin_{name}_preview.png")


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns/gods"
    for name, draw, seed in (("creation", creation, 11), ("humans", humans, 12), ("blessing", blessing, 13)):
        save_scene(project, out, name, *draw(np.random.default_rng(seed)))
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
