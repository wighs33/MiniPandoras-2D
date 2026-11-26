"""HD-2D 필드의 활엽수(맵 들여오기 Editor/WebGame/MapFoliageBuilder의 나무, 시험 장면 HD2DTestBuilder도 쓴다).

참고 게임의 나무는 갈라진 줄기 끝마다 잎 덩어리(엽괴)가 달려 있고, 덩어리마다 윗면이 밝고 아랫면이 어두우며,
나무 전체로도 해를 향한 쪽은 밝고 반대쪽은 짙다(2026-11-22 사용자: 나무 모양도 다듬어 줘. 납작한 한 장 그림은 빛을 받지 않는다).
그래서 나무를 줄기 그림 한 장 + 잎 덩어리 카드 여러 장으로 나누고, 장면에서 잎 덩어리를 수관(타원체) 둘레의 앞뒤 깊이에 건다.
잎 덩어리는 저마다 수관 바깥을 향한 법선을 받아 햇빛에 따라 덩어리째 밝거나 어두워지고, 서로와 땅에 그림자를 드리운다.

그림 안의 음영은 "해를 향한 덩어리"의 모습이다(빛: 오른쪽 위 앞). 장면의 빛이 그늘진 덩어리를 어둡게 만든다.
1도트 = 1/32m(땅·인물과 같다). 색은 참고 화면 나무의 밝기 백분위 색.

사용: python Tools/HD2D/draw_trees.py <Unity 프로젝트 루트>
  Assets/MiniPandoras/Art/Foliage/Trees/leaves.png        잎 덩어리 모음(가로 LEAF_VARIANTS칸)
  Assets/MiniPandoras/Art/Foliage/Trees/<종류>_trunk.png  줄기·가지
  Assets/MiniPandoras/Art/Foliage/Trees/<종류>.json       잎 덩어리 자리(도트, 줄기 그림 아래 가운데 기준)·법선
"""
import json
import math
import random
import sys
from pathlib import Path

from PIL import Image

# 잎: 틈(가장 어두움) → 빛을 정면으로 받는 끝.
LEAF = ["#17230d", "#2b3a13", "#4e5f24", "#748939", "#8da247", "#bdc464"]
BARK = ["#24160f", "#4a2e1d", "#6e4429", "#93613b", "#b08055"]
# 그림 속 음영의 빛 방향(오른쪽, 위, 앞). 필드의 해(Field 장면의 Sun: 오른쪽 조금 뒤 높은 곳, 참고 그림처럼 그림자가 왼쪽 앞으로 짧게)를
# 세운 그림(World.Upright) 안에서 본 방향이다.
LIGHT = (0.70, 0.71, 0.0)

LEAF_CELL = (48, 44)   # 송이가 덩어리 타원 밖으로 4도트까지 나오므로 넉넉히(모자라면 칸 끝에서 잘려 곧은 모서리가 생긴다)
LEAF_VARIANTS = 8   # 앞 넷은 큰 덩어리, 뒤 넷은 작은 덩어리


def rgb(hex_colour):
    return tuple(int(hex_colour[i:i + 2], 16) for i in (1, 3, 5)) + (255,)


def normalise(v):
    length = math.sqrt(sum(c * c for c in v)) or 1.0
    return tuple(c / length for c in v)


L = normalise(LIGHT)


# ---------------------------------------------------------------- 잎 덩어리

def blob_mask(rng, width, height, rx, ry):
    """가운데 타원 위에 작은 원 몇 개를 얹은 울퉁불퉁한 덩어리. 아래는 평평하게 눌려 무게가 실린다."""
    cx, cy = width / 2, height / 2 + 2
    circles = [(cx, cy, rx, ry)]
    for _ in range(rng.randint(4, 6)):
        angle = rng.uniform(math.pi * 0.05, math.pi * 0.95)
        r = rng.uniform(0.38, 0.55) * rx
        circles.append((cx + math.cos(angle) * rx * 0.62, cy - math.sin(angle) * ry * 0.55, r, r * 0.85))
    mask = set()
    for y in range(height):
        for x in range(width):
            for (ox, oy, ax, ay) in circles:
                if ((x + 0.5 - ox) / ax) ** 2 + ((y + 0.5 - oy) / ay) ** 2 <= 1:
                    mask.add((x, y))
                    break
    return mask, (cx, cy, rx, ry)


def shade(x, y, ellipse):
    """덩어리를 반구로 보고 그 점의 빛 받는 정도(0~1)."""
    cx, cy, rx, ry = ellipse
    dx = (x + 0.5 - cx) / (rx * 1.15)
    dy = (cy - y - 0.5) / (ry * 1.15)
    dz = math.sqrt(max(0.0, 1 - dx * dx - dy * dy))
    return max(0.0, dx * L[0] + dy * L[1] + dz * L[2])


def leaf_cluster(rng, r):
    """잎 송이 하나의 도트(가운데 기준). 둥근 몸에 뾰족한 잎 끝이 한두 개 나온다."""
    cells = set()
    for y in range(-int(r) - 1, int(r) + 2):
        for x in range(-int(r) - 1, int(r) + 2):
            if (x + 0.5) ** 2 + ((y + 0.5) * 1.15) ** 2 <= r * r:
                cells.add((x, y))
    for _ in range(rng.randint(1, 2)):
        angle = rng.uniform(-math.pi * 0.9, -math.pi * 0.1)   # 위쪽으로
        tip = (int(round(math.cos(angle) * (r + 1))), int(round(math.sin(angle) * (r + 0.6))))
        cells.add(tip)
    return cells


def draw_leaf_clump(image, left, top, rng, big):
    """잎 덩어리 하나: 작은 잎 송이를 아래 것부터 위로 겹쳐 찍는다.

    송이마다 해를 받는 오른쪽 위가 한 칸 밝고, 왼쪽 아래 테두리가 초승달 모양 그늘이 되어 송이 하나하나가 둥글게 솟아 보인다
    (그늘을 송이 아랫변 전체에 두면 가로줄이 이어져 층층이 쌓은 판처럼 보인다).
    송이의 기본 밝기는 덩어리 전체에서 그 자리가 빛을 받는 정도를 따른다(위·오른쪽이 밝고 아래·왼쪽이 짙다).
    덩어리의 윤곽도 송이들의 합이라 둥근 공이 아니라 울퉁불퉁한 잎 무더기가 된다.
    """
    width, height = LEAF_CELL
    rx = rng.uniform(12.5, 15) if big else rng.uniform(8.5, 10.5)
    # 납작하면 덩어리가 층층이 쌓인 판처럼 보여서 거의 둥글게.
    ry = rx * rng.uniform(0.86, 0.95)
    ellipse = (width / 2, height / 2 + 1, rx, ry)
    cx, cy = ellipse[0], ellipse[1]

    # 송이 자리: 타원 안에 서로 4도트 이상 떨어지게.
    centres = []
    for _ in range(800):
        x, y = rng.uniform(cx - rx, cx + rx), rng.uniform(cy - ry, cy + ry)
        if ((x - cx) / (rx - 2)) ** 2 + ((y - cy) / (ry - 2)) ** 2 > 1:
            continue
        if all((x - ox) ** 2 + (y - oy) ** 2 > 4.0 ** 2 for ox, oy in centres):
            centres.append((x, y))
    centres.sort(key=lambda c: -c[1])   # 아래 송이부터

    canvas = {}
    for (x, y) in centres:
        light = shade(x, y, ellipse) + rng.uniform(-0.08, 0.08)
        base = 1 if light < 0.25 else 2 if light < 0.5 else 3 if light < 0.72 else 4
        cells = leaf_cluster(rng, rng.uniform(3.0, 4.2))
        for (dx, dy) in cells:
            px, py = int(x) + dx, int(y) + dy
            if not (0 <= px < width and 0 <= py < height):
                continue
            rim = any((dx + ox, dy + oy) not in cells for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            tone = base
            if base >= 2 and dx - dy >= 2:
                tone = base + 1
            elif rim and dy - dx * 0.6 > 0.5:
                tone = max(0, base - 1)
            canvas[(px, py)] = tone

    # 덩어리 맨 아래 테두리는 틈 색이라 덩어리가 아래 것 위에 떠 보인다.
    colours = [rgb(c) for c in LEAF]
    for (px, py), tone in canvas.items():
        if (px, py + 1) not in canvas:
            tone = 0
        image.putpixel((left + px, top + py), colours[tone])


# ---------------------------------------------------------------- 줄기

def stroke(pixels, a, b, r0, r1):
    """a에서 b로 굵기가 r0에서 r1로 줄어드는 가지를 원을 찍어 그린다. 도트마다 (가지 방향 기준 좌우 위치)를 남긴다."""
    ax, ay = a
    bx, by = b
    length = max(1.0, math.hypot(bx - ax, by - ay))
    steps = int(length * 2) + 1
    nx, ny = (by - ay) / length, -(bx - ax) / length   # 가지 오른쪽 수직
    for s in range(steps + 1):
        t = s / steps
        cx, cy = ax + (bx - ax) * t, ay + (by - ay) * t
        r = r0 + (r1 - r0) * t
        for y in range(int(cy - r - 1), int(cy + r + 2)):
            for x in range(int(cx - r - 1), int(cx + r + 2)):
                ox, oy = x + 0.5 - cx, y + 0.5 - cy
                if ox * ox + oy * oy <= r * r:
                    side = (ox * nx + oy * ny) / max(r, 0.5)
                    if (x, y) not in pixels or abs(side) < abs(pixels[(x, y)]):
                        pixels[(x, y)] = side


def draw_trunk(spec, rng):
    width, height = spec["canvas"]
    base_x = width // 2
    ground = height - 1
    pixels = {}
    fork = (base_x + spec["lean"], ground - spec["fork"])
    stroke(pixels, (base_x, ground), fork, spec["base_r"], spec["fork_r"])
    # 뿌리: 밑동에서 양옆으로 짧게 퍼진다.
    for direction in (-1, 1):
        stroke(pixels, (base_x + direction * spec["base_r"] * 0.4, ground - 3),
               (base_x + direction * (spec["base_r"] + 5), ground), spec["base_r"] * 0.45, 1.0)
    # 줄기가 굵은 가지 두셋으로 갈라지고(갈림목 via), 굵은 가지가 다시 잔가지로 갈라져 잎 덩어리(tips)에 닿는다.
    for via, tips in spec["limbs"]:
        joint = (base_x + via[0], ground - via[1])
        stroke(pixels, fork, joint, spec["fork_r"] * 0.8, spec["fork_r"] * 0.55)
        for tip in tips:
            end = (base_x + tip[0], ground - tip[1])
            bend = ((joint[0] + end[0]) / 2 + rng.uniform(-2, 2), (joint[1] + end[1]) / 2 + rng.uniform(-2, 1))
            stroke(pixels, joint, bend, spec["fork_r"] * 0.5, spec["fork_r"] * 0.35)
            stroke(pixels, bend, end, spec["fork_r"] * 0.35, 0.8)
            if rng.random() < 0.6:
                twig = (bend[0] + (end[0] - joint[0]) * 0.4 + rng.uniform(-5, 5), bend[1] - rng.uniform(4, 8))
                stroke(pixels, bend, twig, 1.0, 0.5)

    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    colours = [rgb(c) for c in BARK]
    for (x, y), side in pixels.items():
        if not (0 <= x < width and 0 <= y < height):
            continue
        # 해가 오른쪽에서 오므로 가지의 오른쪽(side > 0)이 밝다. 테두리는 가장 어둡다.
        edge = any((x + dx, y + dy) not in pixels for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        if edge and side < 0.2:
            tone = 0
        elif side > 0.45:
            tone = 3
        elif side > -0.1:
            tone = 2
        else:
            tone = 1
        # 나무껍질 결: 세로로 이어지는 어두운 줄.
        if tone >= 2 and (x * 7 + (y // 3) * 3) % 11 == 0:
            tone -= 1
        if tone == 3 and (x + y) % 9 == 0:
            tone = 4
        image.putpixel((x, y), colours[tone])
    return image


# ---------------------------------------------------------------- 나무 배치

def place_lobes(spec, rng):
    """수관 타원체(도트, 밑동 기준)에 잎 덩어리를 건다. 앞면 덩어리가 가지 끝을 덮고, 뒤쪽 덩어리가 틈을 메운다."""
    cx, cy = spec["crown_centre"]
    rx, ry, rz = spec["crown_radius"]
    lobes = []

    def add(x, y, front, variant):
        dx, dy = (x - cx) / rx, (y - cy) / ry
        inside = max(0.0, 1 - dx * dx - dy * dy)
        dz = math.sqrt(inside) * (-1 if front else 1)
        normal = normalise((dx, dy + 0.35, dz))
        lobes.append({"x": round(x, 1), "y": round(y, 1), "z": round(dz * rz, 1), "variant": variant,
                      "nx": round(normal[0], 3), "ny": round(normal[1], 3), "nz": round(normal[2], 3)})

    # 뒤쪽: 수관 안쪽을 듬성듬성 메운다(앞 덩어리 사이 틈으로 짙게 보인다).
    for _ in range(spec["back_lobes"]):
        a = rng.uniform(0, math.tau)
        r = math.sqrt(rng.uniform(0.1, 0.7))
        add(cx + math.cos(a) * rx * r, cy + math.sin(a) * ry * r, False, rng.randrange(0, LEAF_VARIANTS))
    # 앞쪽: 가지 끝마다 큰 덩어리 하나, 그 둘레에 작은 덩어리.
    for tip in [tip for _, tips in spec["limbs"] for tip in tips]:
        add(tip[0], tip[1] + 4, True, rng.randrange(0, 4))
        for _ in range(spec["satellites"]):
            a = rng.uniform(0, math.tau)
            add(tip[0] + math.cos(a) * 14, tip[1] + 4 + math.sin(a) * 9, True, rng.randrange(4, LEAF_VARIANTS))
    # 수관 꼭대기와 테두리를 둥글게 닫는 덩어리.
    # 자리를 조금씩 흔든다(가지런히 두면 덩어리 가장자리가 한 줄로 서서 곧은 모서리가 된다).
    for i in range(spec["rim_lobes"]):
        a = math.pi * (0.1 + 0.8 * i / max(1, spec["rim_lobes"] - 1)) + rng.uniform(-0.15, 0.15)
        r = rng.uniform(0.72, 0.9)
        add(cx + math.cos(a) * rx * r, cy + math.sin(a) * ry * r, True, rng.randrange(0, LEAF_VARIANTS))
    return lobes


TREES = {
    # 큰 활엽수: 키 약 4m, 수관 폭 약 3.6m. 줄기는 1m 남짓에서 갈라지고 수관이 낮게 내려온다.
    "broadleaf": {
        "canvas": (124, 132), "lean": 2, "fork": 30, "base_r": 7.0, "fork_r": 5.0,
        "limbs": [((-14, 50), [(-40, 66), (-22, 88)]),
                  ((14, 48), [(34, 80), (46, 62)]),
                  ((2, 60), [(6, 98), (-4, 72)])],
        "crown_centre": (0, 78), "crown_radius": (54, 34, 28),
        "back_lobes": 7, "satellites": 1, "rim_lobes": 6,
    },
    # 어린 나무: 키 약 2.4m.
    "sapling": {
        "canvas": (72, 80), "lean": 1, "fork": 20, "base_r": 3.4, "fork_r": 2.6,
        "limbs": [((-5, 32), [(-16, 44)]), ((4, 34), [(2, 56), (18, 46)])],
        "crown_centre": (0, 52), "crown_radius": (26, 22, 16),
        "back_lobes": 3, "satellites": 0, "rim_lobes": 3,
    },
}


def main(project):
    out = Path(project) / "Assets/MiniPandoras/Art/Foliage/Trees"
    out.mkdir(parents=True, exist_ok=True)

    leaves = Image.new("RGBA", (LEAF_CELL[0] * LEAF_VARIANTS, LEAF_CELL[1]), (0, 0, 0, 0))
    for variant in range(LEAF_VARIANTS):
        draw_leaf_clump(leaves, variant * LEAF_CELL[0], 0, random.Random(300 + variant), big=variant < 4)
    leaves.save(out / "leaves.png")

    for name, spec in TREES.items():
        rng = random.Random(name)
        draw_trunk(spec, rng).save(out / f"{name}_trunk.png")
        lobes = place_lobes(spec, rng)
        (out / f"{name}.json").write_text(json.dumps({"trunkWidth": spec["canvas"][0], "trunkHeight": spec["canvas"][1],
                                                      "leafCell": list(LEAF_CELL), "leafVariants": LEAF_VARIANTS,
                                                      "lobes": lobes}), encoding="utf-8")
        print(name, len(lobes), "lobes")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else ".")
