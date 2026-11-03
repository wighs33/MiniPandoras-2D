"""7장 루가의 설명 컷인(사용자 요청 2026-11-03: 다음 장들의 그래픽을 생성기로). 웹게임 설명 그림 넷을 생성기로 다시 그린다.

1장(cutin_gods.py)·6장(cutin_lesson.py)과 같은 도구를 쓰고, 십이장·에라드·거신은 정확한 모습 대신 실루엣이다.
장면마다 배경 한 장과, 루가가 말하는 동안 서서히 밝아지는 빛 한 장(가상 384×216, 점은 그 두 배).
대화창이 화면 아래 4분의 1을 덮으므로 알맹이는 점 y 310 위에 둔다.
  twelve.png        십이장: 열두 자리로 갈라진 땅을 둘러선 열두 강자(뒷줄 여섯, 앞줄 여섯). 빛: 강자마다 테두리 빛, 땅을 가른 금.
  chaos.png         주인 없는 세상: 쓰러지고 비틀거리는 열두 강자, 갈라진 땅의 불길과 피어오르는 연기. 빛: 불길과 불티.
  erad.png          에라드: 붉게 빛나는 검은 탑 앞에 두 팔을 벌린 페르니와 양옆의 강자들. 빛: 탑 창, 페르니를 감는 재앙의 소용돌이.
  threePowers.png   세 번째 권력: 1대 신들 → 2대 십이장 → 3대 에라드, 아래에 사슬에 묶여 무릎 꿇은 거신. 빛: 에라드의 붉은 빛.
열두 강자는 몸집·망토·무기(창·검·도끼·지팡이·활)·머리(뿔·왕관·두건·깃털·투구)를 섞어 서로 다르게 보인다.

사용: python Tools/PixelGen/cutin_ruga.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np

import pixelgen as pg
from cutin_gods import (GODS, GOLD, H, INK, W, XX, YY, aura, bezier, color, dot, ellipse, eradion, god, lighten, night, paint,
                        paint_silhouette, placer, polygon, robed, save_scene, silhouette, sparkle, starfield, stepped, title, write)

# 열두 강자: 테두리 빛깔과 생김새(몸집, 망토, 무기, 머리)
TWELVE = [
    ("#e0b060", 1.15, True, "sword", "crown"),
    ("#70a8e0", 1.0, False, "spear", "helm"),
    ("#c070d0", 0.95, True, "staff", "hood"),
    ("#e07060", 1.3, False, "axe", "horns"),
    ("#60c0a0", 0.9, False, "bow", "plume"),
    ("#d0d070", 1.05, True, "spear", None),
    ("#a0a0e0", 1.0, True, "sword", "helm"),
    ("#e09040", 1.25, False, "axe", None),
    ("#80d070", 0.95, False, "staff", "crown"),
    ("#e070a0", 1.0, True, None, "hood"),
    ("#90c8e8", 1.1, False, "sword", "plume"),
    ("#c8a080", 1.35, True, None, "horns"),
]
ERAD_RED = color("#ff3a3a")


def warlord_shape(cx, foot, h, look):
    """강자 하나의 실루엣 칸과 빛나는 칸(창끝, 지팡이 구슬, 칼날). look = (빛깔, 몸집, 망토, 무기, 머리)."""
    _, bulk, cape, weapon, head = look
    top = foot - h
    at = placer(cx, top, h, bulk)
    body = robed(at, h, flare=0.2, wave=0.0)
    bright = np.zeros((H, W), dtype=bool)
    if cape:
        body |= polygon([at(-0.15, 0.21), at(0.15, 0.21), at(0.33, 0.98), at(-0.33, 0.98)])
    if head == "horns":
        for sign in (-1, 1):
            body |= polygon(bezier(at(sign * 0.04, 0.04), at(sign * 0.13, 0.03), at(sign * 0.11, -0.07))
                            + bezier(at(sign * 0.11, -0.07), at(sign * 0.08, 0.0), at(sign * 0.02, 0.03)))
    elif head == "crown":
        for u in (-0.05, 0.0, 0.05):
            body |= polygon([at(u - 0.02, 0.02), at(u + 0.02, 0.02), at(u, -0.045)])
    elif head == "hood":
        body |= polygon([at(0, -0.06), at(0.08, 0.04), at(0.11, 0.19), at(-0.11, 0.19), at(-0.08, 0.04)])
    elif head == "plume":
        body |= polygon(bezier(at(0.0, 0.0), at(-0.12, -0.08), at(-0.2, 0.06)) + [at(-0.16, 0.08), at(-0.02, 0.03)])
    elif head == "helm":
        body |= polygon([at(-0.07, 0.11), at(-0.07, 0.0), at(0.0, -0.03), at(0.07, 0.0), at(0.07, 0.11)])
        body |= polygon([at(-0.01, -0.03), at(0.01, -0.03), at(0.01, -0.08), at(-0.01, -0.08)])
    if weapon == "spear":
        body |= polygon([at(0.22, -0.28), at(0.245, -0.28), at(0.245, 0.98), at(0.22, 0.98)])
        tip = polygon([at(0.2, -0.28), at(0.265, -0.28), at(0.232, -0.42)])
        body |= tip
        bright |= tip
    elif weapon == "sword":
        body |= polygon([at(0.13, 0.36), at(0.19, 0.34), at(0.22, 0.52), at(0.16, 0.53)])
        blade = polygon([at(0.18, 0.5), at(0.21, 0.5), at(0.205, 0.95), at(0.19, 0.98), at(0.175, 0.95)])
        body |= blade | polygon([at(0.14, 0.49), at(0.25, 0.49), at(0.25, 0.51), at(0.14, 0.51)])
        bright |= blade & (XX < at(0.193, 0)[0])
    elif weapon == "axe":
        body |= polygon([at(0.22, -0.08), at(0.245, -0.08), at(0.245, 0.98), at(0.22, 0.98)])
        x, y = at(0.245, -0.02)
        head_mask = ellipse(x + 0.04 * h, y, 0.08 * h, 0.1 * h) & ~ellipse(x + 0.1 * h, y, 0.06 * h, 0.08 * h) & (XX > x)
        body |= head_mask
        bright |= head_mask & pg.neighbours(~head_mask) & (XX > x + 0.06 * h)
    elif weapon == "staff":
        body |= polygon([at(-0.24, -0.05), at(-0.215, -0.05), at(-0.215, 0.98), at(-0.24, 0.98)])
        x, y = at(-0.228, -0.09)
        bright |= ellipse(x, y, 0.045 * h, 0.045 * h)
    elif weapon == "bow":
        x, y = at(-0.12, 0.45)
        arc = ellipse(x, y, 0.12 * h, 0.4 * h) & ~ellipse(x + 0.025 * h, y, 0.11 * h, 0.38 * h) & (XX < x)
        body |= arc | polygon([at(-0.12, 0.06), at(-0.11, 0.06), at(-0.11, 0.84), at(-0.12, 0.84)])
    return body, bright


def tilt(mask, cx, foot, angle):
    """발밑을 축으로 그만큼(라디안, 양수면 오른쪽으로) 기울인 칸."""
    c, s = np.cos(angle), np.sin(angle)
    dx, dy = XX + 0.5 - cx, YY + 0.5 - foot
    sx = np.clip(np.round(cx + dx * c + dy * s).astype(int), 0, W - 1)
    sy = np.clip(np.round(foot - dx * s + dy * c).astype(int), 0, H - 1)
    inside = (cx + dx * c + dy * s >= 0) & (cx + dx * c + dy * s < W) & (foot - dx * s + dy * c >= 0) & (foot - dx * s + dy * c < H)
    return mask[sy, sx] & inside


def warlord(layer, cx, foot, h, index, rng, brightness=1.0, angle=0.0):
    look = TWELVE[index]
    body, bright = warlord_shape(cx, foot, h, look)
    if angle:
        body, bright = tilt(body, cx, foot, angle), tilt(bright, cx, foot, angle)
    paint_silhouette(layer, body, bright, cx, foot, h, color(look[0]), rng, brightness)


def seats():
    """십이장이 서는 자리: 땅 뒤로 여섯(작게), 앞으로 여섯(크게). (번호, x, 발밑, 키)"""
    back = [(k, 184 + k * 80, 176, 54) for k in range(6)]
    front = [(6 + k, 104 + k * 112, 296, 74) for k in range(6)]
    return back, front


def divided_land(canvas, glow, rng, cracked):
    """열두 자리로 금 그은 하늘에 뜬 땅. cracked면 금이 쩍 갈라지고 곳곳에 불길이 인다."""
    cx, cy, rx, ry = W / 2, 214, 178, 44
    eradion(canvas, rng, cx, cy, rx, ry)
    for i in range(12):
        a = i / 12 * np.pi * 2 + 0.13
        points = [(cx, cy)]
        for k in range(1, 9):
            r = k / 8
            jitter = (rng.random() - 0.5) * 10 if cracked else 0
            points.append((cx + np.cos(a) * rx * r + jitter, cy + np.sin(a) * ry * r))
        for (ax, ay), (bx, by) in zip(points, points[1:]):
            for t in np.linspace(0, 1, int(np.hypot(bx - ax, by - ay)) + 1):
                x, y = ax + (bx - ax) * t, ay + (by - ay) * t
                if cracked:
                    dot(canvas, x - 1, y - 1, color("#140808"), 1.0, size=3)
                    dot(glow, x, y, color("#ff7a2a"), 0.55)
                else:
                    dot(canvas, x, y, color("#e8d8a8"), 0.8)
                    dot(glow, x, y, lighten(color(TWELVE[i][0]), 0.4), 0.9)
    if cracked:
        for _ in range(7):
            fx, fy = cx + rng.uniform(-0.6, 0.6) * rx, cy + rng.uniform(-0.6, 0.5) * ry
            aura(canvas, fx, fy, 14, color("#ff6a20"), 0.5, 1.6)
            aura(glow, fx, fy, 22, color("#ff7a2a"), 0.9, 1.6)
            for _ in range(10):
                dot(glow, fx + rng.normal(0, 6), fy - rng.random() * 26, color("#ffc060"), 0.8)


# ── 장면 ───────────────────────────────────────────────

def twelve(rng):
    """십이장: 열두 자리로 나뉜 땅을 둘러선 열두 강자."""
    canvas = night("#151325", "#2a2238", H, rng, "#3a2c56")
    starfield(canvas, rng, 160, 260)
    glow = np.zeros((H, W, 4))
    title(canvas, "십이장", "신들이 사라진 뒤 세상을 나눠 가진 열두 강자")
    back, front = seats()
    for index, x, foot, h in back:
        warlord(canvas, x, foot, h, index, np.random.default_rng(index), 0.8)
        warlord(glow, x, foot, h, index, np.random.default_rng(index), 1.0)
    divided_land(canvas, glow, rng, cracked=False)
    for index, x, foot, h in front:
        warlord(canvas, x, foot, h, index, np.random.default_rng(index), 0.85)
        warlord(glow, x, foot, h, index, np.random.default_rng(index), 1.05)
    return canvas, glow


def chaos(rng):
    """주인 없는 세상: 쓰러지고 비틀거리는 열두 강자, 갈라진 땅의 불길과 연기."""
    canvas = night("#2a1014", "#4a1a12", H, rng, "#5a2418")
    starfield(canvas, rng, 50, 160)
    glow = np.zeros((H, W, 4))
    title(canvas, "주인 없는 세상", "균형이 깨지고, 수백 년 동안 주인 없는 세상이 이어졌다")
    back, front = seats()
    tilts = [0.5, -0.25, 1.35, -0.15, 0.3, -1.3, 0.2, -0.45, 1.4, -0.3, 0.6, -0.2]
    for index, x, foot, h in back:
        warlord(canvas, x + (rng.random() - 0.5) * 20, foot, h, index, np.random.default_rng(index), 0.4, tilts[index])
    divided_land(canvas, glow, rng, cracked=True)
    for index, x, foot, h in front:
        warlord(canvas, x + (rng.random() - 0.5) * 24, foot, h, index, np.random.default_rng(index), 0.45, tilts[index])
    # 피어오르는 연기 기둥
    for k in range(16):
        x0, y0 = rng.uniform(260, 510), rng.uniform(190, 230)
        for j in range(6):
            t = j / 6
            r = 10 + t * 26
            puff = ellipse(x0 + t * 22 + np.sin(k + j) * 6, y0 - t * 140, r, r * 0.8)
            paint(canvas, color("#2a2226"), puff * (0.22 * (1 - t)))
    return canvas, glow


def spire(canvas, glow, cx, base, top, width, rng, windows=5):
    """에라드의 검은 탑: 아래가 넓고 위가 뾰족하며 붉은 빛줄이 두르고, 창이 붉게 깜빡인다."""
    shape = polygon([(cx - width / 2, base), (cx - width * 0.3, top + 28), (cx, top), (cx + width * 0.3, top + 28), (cx + width / 2, base)])
    paint(canvas, color("#0a0810"), shape.astype(float))
    paint(canvas, color("#ff5050") * 0.6, (shape & pg.neighbours(~shape)) * 0.5)
    for i in range(windows):
        wy = top + 44 + i * (base - top - 60) / windows
        window = (np.abs(XX + 0.5 - cx) < 2) & (YY + 0.5 > wy) & (YY + 0.5 < wy + 6)
        paint(canvas, color("#7a1a1a"), window.astype(float))
        paint(glow, color("#ff4a3a"), window.astype(float))
        aura(glow, cx, wy + 3, 10, color("#ff3a2a"), 0.5)


def erad(rng):
    """에라드: 검은 탑 앞에 두 팔을 벌린 페르니, 양옆의 강자들."""
    canvas = night("#100c14", "#2a0f14", H, rng, "#3a141c")
    starfield(canvas, rng, 70, 200)
    glow = np.zeros((H, W, 4))
    aura(canvas, W / 2, 180, 220, ERAD_RED, 0.22, 1.0)
    title(canvas, "에라드", "재앙의 판도라를 가진 페르니를 중심으로 모인 강자들")
    spire(canvas, glow, W / 2, 302, 70, 108, rng)
    for k, (x, foot, h) in enumerate(((124, 300, 80), (204, 290, 88), (284, 280, 96), (484, 280, 96), (564, 290, 88), (644, 300, 80))):
        index = (k + 3) % 12
        body, bright = warlord_shape(x, foot, h, TWELVE[index])
        paint_silhouette(canvas, body, bright, x, foot, h, color("#b04848"), np.random.default_rng(k), 0.95)
        paint_silhouette(glow, body, bright, x, foot, h, color("#b04848"), np.random.default_rng(k), 0.6)
    # 페르니: 두 팔을 벌린 긴 옷의 실루엣. 재앙의 판도라가 붉고 검게 감아 돈다.
    cx, foot, h = W / 2, 236, 116
    at = placer(cx, foot - h, h, 1.3)
    body = robed(at, h, flare=0.3, wave=0.02)
    # 두 팔을 옆으로 비스듬히 벌리고, 넓은 소매가 아래로 늘어진다.
    for sign in (-1, 1):
        body |= polygon([at(sign * 0.12, 0.21), at(sign * 0.2, 0.19), at(sign * 0.46, 0.12), at(sign * 0.47, 0.17),
                         at(sign * 0.4, 0.3), at(sign * 0.2, 0.38)])
    bright = np.zeros((H, W), dtype=bool)
    paint_silhouette(canvas, body, bright, cx, foot, h, ERAD_RED, np.random.default_rng(31), 1.0)
    paint_silhouette(glow, body, bright, cx, foot, h, ERAD_RED, np.random.default_rng(31), 1.5)
    for k in range(3):
        start = k * np.pi * 2 / 3 + 0.4
        for a in np.linspace(start, start + np.pi * 1.2, 90):
            x, y = cx + np.cos(a) * 70, 164 + np.sin(a) * 28
            dot(glow, x - 1, y - 1, color("#ff5a4a") if k != 1 else color("#300808"), 0.75, size=3)
    aura(glow, cx, 164, 60, ERAD_RED, 0.45, 1.2)
    write(canvas, "페르니 · 재앙의 판도라", 13, cx, foot + 6, color("#ff9a9a"))
    return canvas, glow


def arrow(canvas, x0, x1, y):
    line = (YY + 0.5 > y - 1) & (YY + 0.5 < y + 1.5) & (XX + 0.5 > x0) & (XX + 0.5 < x1 - 8)
    head = polygon([(x1, y), (x1 - 12, y - 8), (x1 - 12, y + 8)])
    paint(canvas, GOLD * 0.7, (line | head).astype(float))


def three_powers(rng):
    """세 번째 권력: 1대 신들 → 2대 십이장 → 3대 에라드, 아래에 사슬에 묶여 무릎 꿇은 거신."""
    canvas = night("#13121e", "#22202e", H, rng, "#2c2840")
    starfield(canvas, rng, 120, 300)
    glow = np.zeros((H, W, 4))
    title(canvas, "세 번째 권력", "신들 → 십이장 → 에라드")
    # 1대: 열 신(두 줄로 작게)
    for i in range(len(GODS)):
        god(canvas, i, 92 + (i % 5) * 24, 128 + (i // 5) * 42, 34, np.random.default_rng(i), 0.85)
    write(canvas, "1대 · 신들", 15, 140, 182, GOLD)
    arrow(canvas, 222, 278, 132)
    # 2대: 십이장(둥글게 둘러선다)
    for i in range(12):
        a = i / 12 * np.pi * 2 - np.pi / 2
        warlord(canvas, W / 2 + np.cos(a) * 62, 150 + np.sin(a) * 24, 30, i, np.random.default_rng(i), 0.85)
    write(canvas, "2대 · 십이장", 15, W / 2, 182, color("#c8c0e0"))
    arrow(canvas, 490, 546, 132)
    # 3대: 에라드(지금의 권력이라 붉게 빛난다)
    aura(canvas, 628, 124, 92, ERAD_RED, 0.25)
    aura(glow, 628, 124, 92, ERAD_RED, 0.35)
    spire(canvas, glow, 628, 160, 68, 44, rng, windows=3)
    for i, x in enumerate((580, 604, 652, 676)):
        body, bright = warlord_shape(x, 164, 32, TWELVE[i])
        paint_silhouette(canvas, body, bright, x, 164, 32, color("#b04848"), np.random.default_rng(i), 0.9)
    write(canvas, "3대 · 에라드", 15, 628, 182, color("#ff9a9a"))
    # 사슬에 묶여 무릎 꿇은 거신(거신 헤토스의 빛깔): 굽은 큰 등, 앞으로 떨군 머리, 앞으로 낸 무릎, 늘어진 팔.
    # 사슬은 땅에 박은 말뚝에서 두 손목으로 팽팽하다.
    gx, gf, gh = 610, 300, 100
    body = ellipse(gx - 0.02 * gh, gf - 0.55 * gh, 0.3 * gh, 0.28 * gh)
    body |= ellipse(gx + 0.26 * gh, gf - 0.58 * gh, 0.09 * gh, 0.1 * gh)
    body |= ellipse(gx + 0.16 * gh, gf - 0.13 * gh, 0.24 * gh, 0.13 * gh)
    body |= polygon([(gx - 0.3 * gh, gf - 0.45 * gh), (gx + 0.2 * gh, gf - 0.3 * gh), (gx + 0.05 * gh, gf), (gx - 0.34 * gh, gf)])
    wrists = []
    for sign, (sx, sy) in ((1, (gx + 0.16 * gh, gf - 0.72 * gh)), (-1, (gx - 0.24 * gh, gf - 0.68 * gh))):
        ex, ey = sx + sign * 0.18 * gh, gf - 0.16 * gh
        body |= polygon([(sx - 0.06 * gh, sy), (sx + 0.06 * gh, sy), (ex + 0.05 * gh, ey), (ex - 0.05 * gh, ey)])
        wrists.append((ex, ey))
    body &= YY < gf
    paint_silhouette(canvas, body, np.zeros((H, W), dtype=bool), gx, gf, gh, color(GODS[6][1]), np.random.default_rng(7), 0.9)
    for (wx, wy), stake in zip(wrists, (gx + 0.62 * gh, gx - 0.72 * gh)):
        for t in np.linspace(0, 1, 50):
            x, y = wx + (stake - wx) * t, wy + (gf - wy) * t
            if int(t * 50) % 3 != 2:
                dot(canvas, x - 1, y - 1, color("#8a8070"), 1.0, size=3)
        paint(canvas, color("#4a4440"), ((np.abs(XX + 0.5 - stake) < 3) & (YY + 0.5 > gf - 10) & (YY + 0.5 < gf + 2)).astype(float))
    write(canvas, "봉인돼 있던 거신까지", 13, 470, 250, color("#d0b8a0"))
    return canvas, glow


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns/ruga"
    for name, draw, seed in (("twelve", twelve, 71), ("chaos", chaos, 72), ("erad", erad, 73), ("threePowers", three_powers, 74)):
        save_scene(project, out, name, *draw(np.random.default_rng(seed)))
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
