"""6장 라코의 수업 컷인(사용자 요청 2026-11-03: 다음 장의 맵과 그래픽). 웹게임 설명 그림 넷을 생성기로 다시 그린다.

1장 랑의 설명 컷인(cutin_gods.py)과 같은 도구를 쓰고, 신과 갓슬레이어는 정확한 모습 대신 실루엣이다.
장면마다 배경 한 장과, 라코가 말하는 동안 서서히 밝아지는 빛 한 장(가상 384×216, 점은 그 두 배).
대화창이 화면 아래 4분의 1을 덮으므로, 신·이름·갓슬레이어·요람 같은 알맹이는 점 y 310 위에 둔다(SAFE_BOTTOM).
  tenGods.png      태초의 열 신: 구름 받침 위에 두 줄로 선 열 신과 이름. 빛: 신마다 테두리 빛과 빛무리.
  splitPower.png   나눈 힘과 몰아준 힘: 왼쪽은 판도라가 여러 사람에게 조금씩, 오른쪽은 칼립스가 단 한 사람에게 번개처럼.
  war.png          신인전쟁: 먹구름 위로 솟은 거대한 신들, 창을 든 인간의 군대, 가운데 검을 치켜든 갓슬레이어.
  dust.png         판도라 더스트: 스러지는 신들에게서 흩어진 빛 알갱이가 요람 속 갓난아기에게 내려앉는다.

사용: python Tools/PixelGen/cutin_lesson.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np

import pixelgen as pg
from cutin_gods import (GODS, GOLD, H, INK, W, WHITE, XX, YY, aura, color, dot, ellipse, god, lighten, mote_trail,
                        night, paint, person, polygon, save_scene, silhouette, sparkle, starfield, stepped, title, write)


# 대화창 위 끝(가상 y 155, 컷인이 1.1배로 다가가도 가려지지 않는 높이)을 점으로.
SAFE_BOTTOM = 310


def cloud_platform(layer, cx, cy, width, rng, tint):
    """신이 디디고 선 구름 받침: 납작한 구름 덩어리 몇 개, 윗면은 밝고 아래는 어둡다."""
    puff = np.zeros((H, W), dtype=bool)
    for _ in range(6):
        puff |= ellipse(cx + rng.normal(0, width * 0.25), cy + rng.normal(0, 2), width * (0.2 + rng.random() * 0.12), 5 + rng.random() * 3)
    paint(layer, tint * 0.55, puff * 0.9)
    paint(layer, lighten(tint, 0.3) * 0.8, (puff & ~np.roll(puff, 2, axis=0)) * 0.85)


def label(layer, role_and_name, x, top, rgb):
    """두 줄 이름표: 이름(크게) 아래 신의 이름(작게, 옅게)."""
    role, name = role_and_name.split(" ")
    write(layer, name, 13, x, top, rgb)
    write(layer, role, 11, x, top + 16, rgb * 0.8)


def bolt(layer, start, end, rgb, rng, width=2):
    """번개처럼 꺾인 선: 여섯 마디로 꺾이고, 가운데는 밝다."""
    (x0, y0), (x1, y1) = start, end
    points = [(x0, y0)]
    for k in range(1, 6):
        t = k / 6
        points.append((x0 + (x1 - x0) * t + rng.normal(0, 7), y0 + (y1 - y0) * t + rng.normal(0, 5)))
    points.append((x1, y1))
    for (ax, ay), (bx, by) in zip(points, points[1:]):
        for s in np.linspace(0, 1, int(np.hypot(bx - ax, by - ay)) + 1):
            x, y = ax + (bx - ax) * s, ay + (by - ay) * s
            dot(layer, x - width / 2, y - width / 2, rgb, 0.9, size=width)
            dot(layer, x, y, lighten(rgb, 0.7), 1.0)


def storm_clouds(layer, rng, top, bottom, rgb):
    """발치를 덮는 먹구름 띠: 위아래 가장자리 모두 둥근 구름 덩어리로 울퉁불퉁하고, 윗가장자리만 조금 밝다."""
    cloud = (YY > top + 14) & (YY < bottom - 14)
    for x in range(-20, W + 40, 30):
        cloud |= ellipse(x + rng.normal(0, 6), top + 14 + rng.normal(0, 5), 26 + rng.random() * 12, 16 + rng.random() * 8)
        cloud |= ellipse(x + 15 + rng.normal(0, 6), bottom - 16 + rng.normal(0, 4), 22 + rng.random() * 10, 12 + rng.random() * 6)
    paint(layer, rgb, cloud.astype(float))
    paint(layer, lighten(rgb, 0.22), (cloud & ~np.roll(cloud, 3, axis=0)) * 0.8)
    paint(layer, rgb * 0.7, (cloud & ~np.roll(cloud, -2, axis=0)) * 0.8)


# ── 장면 ───────────────────────────────────────────────

def ten_gods(rng):
    """태초의 열 신: 구름 받침 위 두 줄. 빛: 신마다 테두리 빛과 빛무리가 차오른다."""
    canvas = night("#0c0a22", "#261a42", H, rng, "#3c2c6a")
    starfield(canvas, rng, 240, H)
    aura(canvas, W / 2, 190, 220, color("#4a3c86"), 0.35, 1.2)
    title(canvas, "태초의 열 신", "세계 에라디온과 인간을 만든 신들")
    glow = np.zeros((H, W, 4))
    for i, (name, code, _, _) in enumerate(GODS):
        row, col = divmod(i, 5)
        x, foot = 140 + col * 122, (164 if row == 0 else 272)
        rgb = color(code)
        cloud_platform(canvas, x, foot + 2, 64, rng, color("#6c5c9c"))
        god(canvas, i, x, foot, 70, np.random.default_rng(i), 0.72)
        label(canvas, name, x, foot + 6, lighten(rgb, 0.35) * 0.85)
        aura(glow, x, foot - 35, 42, rgb, 0.5, 0.8)
        god(glow, i, x, foot, 70, np.random.default_rng(i), 1.0)
    return canvas, glow


def split_power(rng):
    """나눈 힘과 몰아준 힘: 왼쪽 금빛 판도라가 여러 사람에게, 오른쪽 붉은 칼립스가 단 한 사람에게. 가운데 금줄로 나눈다."""
    canvas = night("#1a1430", "#3a2426", H, rng, "#4a2c3a")
    starfield(canvas, rng, 140, 300)
    paint(canvas, color("#ffd28c"), (XX < W / 2) * 0.06)
    paint(canvas, color("#96141e"), (XX >= W / 2) * 0.26)
    divider = (np.abs(XX + 0.5 - W / 2) < 1) & (YY > 70) & (YY < SAFE_BOTTOM + 10)
    paint(canvas, GOLD, divider * stepped(1 - np.abs(YY - 190) / 130, 4) * 0.8)
    title(canvas, "나눈 힘과 몰아준 힘")
    glow = np.zeros((H, W, 4))

    gold = color(GODS[0][1])
    aura(canvas, 104, 170, 70, gold * 0.8, 0.35)
    hand = silhouette(canvas, 104, 282, 168, gold, "halo", np.random.default_rng(1), 0.9, sway=-0.03, reach=0.52)
    crowd = [(222 + (k % 4) * 40 + (k // 4) * 18, 244 + (k // 4) * 30) for k in range(8)]
    heads = [person(canvas, x, foot, 34, rng, "reach" if k % 3 == 0 else "stand", color("#140e14"), color("#8a6a40"), 0.6)
             for k, (x, foot) in enumerate(crowd)]
    write(canvas, "축복신 판도라 · 모두에게 조금씩", 13, W / 4, 294, lighten(gold, 0.3))
    aura(glow, 104, 170, 90, gold, 0.4)
    aura(glow, hand[0], hand[1], 18, lighten(gold, 0.5), 0.9)
    for k, (x, y) in enumerate(heads):
        mote_trail(glow, hand, (x, y), gold, rng, 20 + 6 * k, 18)
        aura(glow, x, y + 10, 14, gold, 0.35, 1.1)

    red = color(GODS[8][1])
    aura(canvas, 660, 150, 110, color("#ff3a2a"), 0.3)
    claw = silhouette(canvas, 664, 282, 186, red, "hood", np.random.default_rng(8), 1.0, reach=-0.42)
    one = person(canvas, 498, 282, 58, rng, "stand", color("#1a0a0c"), color("#ff6a5a"), 0.9)
    write(canvas, "재앙신 칼립스 · 단 한 사람에게", 13, W * 3 / 4, 294, lighten(red, 0.35))
    aura(glow, 660, 150, 130, color("#ff3a2a"), 0.35)
    for offset in (0, 4):
        bolt(glow, (claw[0], claw[1] + offset), (one[0] + 6, one[1] + 10), color("#ff6a5a"), rng, 2 if offset == 0 else 1)
    aura(glow, one[0], one[1] + 18, 34, color("#ff4a3a"), 0.8, 0.9)
    person(glow, 498, 282, 58, rng, "stand", color("#2a0a0c"), color("#ffb0a0"), 1.0)
    return canvas, glow


def war(rng):
    """신인전쟁: 먹구름 위로 솟은 거대한 신 넷, 창을 든 인간의 군대, 가운데 검을 치켜든 갓슬레이어. 빛: 번개·불티·갓슬레이어의 검."""
    canvas = night("#160a14", "#40161a", H, rng, "#5a1c22")
    starfield(canvas, rng, 60, 200)
    title(canvas, "신인전쟁", "인간을 이끌고 신들과 싸운 갓슬레이어")
    giants = [(3, 118), (1, 290), (4, 478), (6, 650)]
    glow = np.zeros((H, W, 4))
    for index, x in giants:
        god(canvas, index, x, 280, 206, np.random.default_rng(index), 0.4)
        god(glow, index, x, 280, 206, np.random.default_rng(index), 0.75)
    # 빛 장에도 같은 먹구름을 얹어 밝아진 신들의 발치도 구름에 묻힌다.
    for layer in (canvas, glow):
        storm_clouds(layer, np.random.default_rng(5), 220, 290, color("#24121a"))
    for (index, x), (_, x2) in zip(giants, giants[1:]):
        bolt(glow, (x + 40, 110 + rng.integers(0, 30)), (x2 - 40, 118 + rng.integers(0, 30)), color("#ffd0c0"), rng, 1)

    # 인간의 군대: 세 줄, 앞줄일수록 크다. 맨 앞줄 가운데는 갓슬레이어 자리라 비운다.
    # 빛 장에도 그려 빛 장의 먹구름이 뒷줄 창끝을 덮지 않게 한다. 앞줄은 대화창 뒤로 들어가도 된다.
    for layer in (canvas, glow):
        for row in range(3):
            count, foot, height = 18 + row * 2, 304 + row * 26, 24 + row * 6
            for k in range(count):
                x = 14 + (k + (row % 2) * 0.5) * (W - 28) / count
                if row == 2 and abs(x - W / 2) < 60:
                    continue
                person(layer, x, foot, height, rng, "spear", color("#120a0c"), color("#ff8a6a"), 0.35 + 0.15 * row)
    red = color("#ff5a4a")
    # 갓슬레이어는 머리와 치켜든 검이 대화창 위에 오도록 군대 앞줄보다 높이 선다.
    slayer_foot, slayer_height = 372, 150
    aura(canvas, W / 2, slayer_foot - 90, 70, red, 0.3)
    silhouette(canvas, W / 2, slayer_foot, slayer_height, red, "slayer", np.random.default_rng(11), 1.0)
    write(canvas, "갓슬레이어", 14, W / 2 - 24, slayer_foot - 220, lighten(red, 0.45))

    silhouette(glow, W / 2, slayer_foot, slayer_height, red, "slayer", np.random.default_rng(11), 1.3)
    tip_x, tip_y = W / 2 + 0.237 * slayer_height, slayer_foot - slayer_height - 0.52 * slayer_height
    sparkle(glow, tip_x, tip_y, WHITE, 6)
    aura(glow, tip_x, tip_y, 16, color("#ffd0c0"), 0.9)
    for _ in range(46):
        x, y = rng.uniform(10, W - 10), rng.uniform(110, 290)
        dot(glow, x, y, color("#ffb060") if rng.random() < 0.5 else red, 0.5 + rng.random() * 0.5, size=2)
    return canvas, glow


def cradle(layer, cx, foot):
    """요람: 둥근 나무 바구니와 흔들 다리, 강보에 싸인 아기."""
    body = ellipse(cx, foot - 10, 34, 14) & (YY > foot - 12)
    paint(layer, color("#6a4a30"), body.astype(float))
    paint(layer, color("#8a6440"), (body & (YY < foot - 6)).astype(float))
    rocker = ellipse(cx, foot - 30, 42, 32) & ~ellipse(cx, foot - 32, 40, 30) & (YY > foot - 4) & (YY < foot + 2)
    paint(layer, color("#4a3020"), rocker.astype(float))
    paint(layer, color("#f2e6cc"), ellipse(cx + 4, foot - 14, 20, 7).astype(float))
    paint(layer, color("#d8c8a8"), (ellipse(cx + 4, foot - 14, 20, 7) & (YY > foot - 12)).astype(float))
    paint(layer, color("#e8c8a8"), ellipse(cx - 14, foot - 17, 6.5, 6).astype(float))
    paint(layer, color("#5a3a20"), (body & ~np.roll(body, 1, axis=0)).astype(float))


def dust(rng):
    """판도라 더스트: 스러지는 신 셋에게서 흩어진 세 빛깔 알갱이가 요람 속 갓난아기에게 내려앉는다."""
    canvas = night("#0c1226", "#1c2244", H, rng, "#2a3466")
    starfield(canvas, rng, 200, H)
    title(canvas, "판도라 더스트")
    glow = np.zeros((H, W, 4))
    for index, x in ((1, 232), (0, 384), (3, 536)):
        god(canvas, index, x, 156, 84, np.random.default_rng(index), 0.32)
        god(glow, index, x, 156, 84, np.random.default_rng(index), 0.5)
    write(canvas, "신들의 힘과 원념", 12, W / 2, 162, color("#c8c0e0") * 0.85)

    colors = [color("#ffd98a"), color("#8ff0e8"), color("#c8a0ff")]
    cradle_foot = 276
    target = (W / 2 - 14, cradle_foot - 20)
    for k in range(90):
        sx, sy = rng.uniform(150, 620), rng.uniform(140, 200)
        rgb = colors[k % 3]
        steps = rng.integers(3, 7)
        p = rng.uniform(0.1, 0.95)
        for j in range(steps):
            t = max(0.0, p - j * 0.03)
            x = sx + (target[0] - sx) * t * t + np.sin(sy * 0.08 + k) * 10 * (1 - t)
            y = sy + (target[1] - sy) * t
            dot(canvas, x, y, rgb * 0.6, 0.35 * (1 - j / steps))
            dot(glow, x, y, lighten(rgb, 0.3), 0.9 * (1 - j / steps), size=2 if j == 0 else 1)
    write(canvas, "흩어져 떠도는 판도라 더스트", 12, 620, 230, color("#9fe0ff"))

    aura(canvas, W / 2, cradle_foot - 32, 40, color("#ffd98a"), 0.3)
    cradle(canvas, W / 2, cradle_foot)
    write(canvas, "갓 태어난 아기에게 깃들면 판도라", 13, W / 2, cradle_foot + 8, GOLD)
    aura(glow, W / 2 - 10, cradle_foot - 20, 44, color("#ffd98a"), 0.75)
    sparkle(glow, W / 2 - 14, cradle_foot - 30, WHITE, 4)
    return canvas, glow


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/CutIns/lesson"
    for name, draw, seed in (("tenGods", ten_gods, 21), ("splitPower", split_power, 22), ("war", war, 23), ("dust", dust, 24)):
        save_scene(project, out, name, *draw(np.random.default_rng(seed)))
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
