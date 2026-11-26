"""HD-2D 필드의 잔디 포기 그림(맵 들여오기 Editor/WebGame/MapFoliageBuilder의 잔디, 시험 장면 HD2DTestBuilder도 쓴다).

참고 게임의 풀밭은 바닥에 깔린 짧은 잔디다. 낱낱의 풀은 눈에 띄지 않고, 낮은 포기 수만 개가 겹쳐
밝은 연두 털처럼 보이며 포기 사이 틈이 짧은 어두운 줄로 남는다(2026-11-22 사용자: 큰 풀 카드는 열대우림 같다).
그래서 포기 한 장은 1~2도트 높이의 몸에 2~5도트 풀잎 몇 가닥이 솟은 12x8도트 그림이고, 장면에서 1m²에 수십 장을 겹쳐 세운다.
색은 참고 화면의 풀밭을 밝기 순으로 뽑은 값(5%·20%·40%·60%·80%)을 잇는다. 1도트 = 1/32m(땅과 같다).

칸: 가로 SHAPES개 모양, 세로 줄 = 그늘·보통·햇빛 세 가지 밝기 + 보통 밝기에 작은 꽃이 핀 줄.

무더기(clumps.png): 참고 화면의 풀밭은 고르지 않고, 키가 한 뼘쯤 되는 짙은 풀 무더기가 군데군데 솟아
밝은 잔디 사이에 울퉁불퉁한 그늘을 만든다(나무 밑동·바위 둘레에 특히 많다). 무더기 한 장은 짙은 둔덕 위로
부채꼴로 퍼지는 풀잎 11~16가닥(키 8~13도트)이다. 20x14도트, 가로 CLUMP_SHAPES개, 세로 그늘·보통 두 줄.

사용: python Tools/HD2D/draw_lawn.py <Unity 프로젝트 루트>
  Assets/MiniPandoras/Art/Foliage/Lawn/tufts.png
  Assets/MiniPandoras/Art/Foliage/Lawn/clumps.png
"""
import random
import sys
from pathlib import Path

from PIL import Image

CELL_W, CELL_H = 12, 8
SHAPES = 8

# 밑동 → 풀잎 끝. 참고 화면 풀밭의 밝기 백분위 색(5·20·40·60·80%)에 앞뒤로 한 칸씩 이었다.
RAMP = ["#2f4415", "#3f5a1c", "#4f6a22", "#607d29", "#729035", "#89a140", "#9bae4d", "#b4c45a"]
# 밝기 줄마다 RAMP에서 쓰는 다섯 칸의 시작(그늘 0, 보통 1, 햇빛 2).
TINT_START = [0, 1, 2]
FLOWERS = ["#f0ead0", "#e8d77a", "#ffffff"]

CLUMP_W, CLUMP_H = 20, 14
CLUMP_SHAPES = 6
# 무더기 밝기 줄마다 RAMP에서 쓰는 다섯 칸의 시작(그늘 0, 보통 1). 잔디보다 한 칸 짙게 시작한다.
CLUMP_TINT_START = [0, 1]


def rgb(hex_colour):
    return tuple(int(hex_colour[i:i + 2], 16) for i in (1, 3, 5)) + (255,)


def draw_tuft(image, left, top, shape_rng, tint, flower):
    """포기 하나: 가운데가 높은 낮은 몸 위로 풀잎이 솟고, 끝으로 갈수록 밝다. 그림 아래 줄이 땅에 닿는다."""
    ramp = [rgb(c) for c in RAMP[TINT_START[tint]:TINT_START[tint] + 5]]
    bottom = top + CELL_H - 1
    span = shape_rng.randint(8, 11)
    start = left + (CELL_W - span) // 2

    # 몸: 1~2도트. 맨 아래는 밝기 줄과 상관없이 가장 어두운 색이라 포기 사이 틈이 참고 화면처럼 짙은 줄로 남는다.
    for x in range(start, start + span):
        edge = x in (start, start + span - 1)
        body = 1 if edge else shape_rng.choice((1, 2, 2))
        for k in range(body):
            image.putpixel((x, bottom - k), rgb(RAMP[0]) if k == 0 else ramp[1])

    # 풀잎: 한 도트 굵기, 키 2~5. 끝은 가끔 옆으로 한 칸 눕는다.
    for _ in range(shape_rng.randint(3, 5)):
        x = shape_rng.randint(start + 1, start + span - 2)
        middle = abs(x - (left + CELL_W / 2)) < 3
        height = shape_rng.randint(3, 5) if middle else shape_rng.randint(2, 4)
        lean = shape_rng.choice((0, 0, -1, 1))
        for k in range(height):
            px = x + (lean if k == height - 1 else 0)
            level = 2 + min(2, k * 3 // max(1, height))
            image.putpixel((px, bottom - 1 - k), ramp[level])

    if flower:
        x = shape_rng.randint(start + 2, start + span - 3)
        y = bottom - shape_rng.randint(2, 3)
        image.putpixel((x, y), rgb(shape_rng.choice(FLOWERS)))


def draw_clump(image, left, top, shape_rng, tint):
    """무더기 하나: 짙은 둔덕 위로 부채꼴로 퍼지는 키 큰 풀잎. 밑동은 짙고 끝으로 갈수록 밝다. 그림 아래 줄이 땅에 닿는다."""
    ramp = [rgb(c) for c in RAMP[CLUMP_TINT_START[tint]:CLUMP_TINT_START[tint] + 5]]
    bottom = top + CLUMP_H - 1
    centre = left + CLUMP_W / 2
    # 둔덕: 가로 12도트, 높이 5도트 반타원. 잎 사이로 비치는 짙은 속이다.
    for y in range(5):
        half = 6 * (1 - (y / 5) ** 2) ** 0.5
        for x in range(int(round(centre - half)), int(round(centre + half))):
            image.putpixel((x, bottom - y), ramp[0] if y < 2 else ramp[1])
    blades = shape_rng.randint(11, 16)
    for b in range(blades):
        # 바깥 잎일수록 낮고 옆으로 눕는다.
        spread = (b + 0.5) / blades * 2 - 1
        height = int(round(13 - abs(spread) * 5 + shape_rng.uniform(-2, 1)))
        base_x = centre + spread * 4 + shape_rng.uniform(-0.5, 0.5)
        top_x = centre + spread * 8.5 + shape_rng.uniform(-1, 1)
        previous_x = None
        for k in range(height):
            t = k / max(1, height - 1)
            x = int(round(base_x + (top_x - base_x) * t * t))
            # 끝으로 갈수록 눕는 잎이 한 줄에 두 칸 이상 옮겨 가면 끊겨 보이므로 한 칸씩만 옮긴다.
            if previous_x is not None and abs(x - previous_x) > 1:
                x = previous_x + (1 if x > previous_x else -1)
            previous_x = x
            y = bottom - k
            level = 0 if k == 0 else 1 if t < 0.35 else 2 if t < 0.7 else 3
            # 해를 향한 오른쪽 잎의 끝은 한 칸 더 밝다.
            if t > 0.75 and spread > -0.2:
                level = 4
            image.putpixel((x, y), ramp[level])
            # 잎 몸: 아래쪽 절반은 두 도트 굵기, 왼쪽에 짙은 그늘.
            if t < 0.5 and 0 <= x - 1 - left < CLUMP_W:
                image.putpixel((x - 1, y), ramp[max(0, level - 1)])
    # 밑동 그늘: 땅에 닿는 줄은 가장 짙다.
    for x in range(int(centre - 6), int(centre + 6)):
        image.putpixel((x, bottom), rgb(RAMP[0]))


def main(project):
    rows = len(TINT_START) + 1
    image = Image.new("RGBA", (CELL_W * SHAPES, CELL_H * rows), (0, 0, 0, 0))
    for shape in range(SHAPES):
        for row in range(rows):
            # 같은 모양 번호는 밝기 줄이 달라도 같은 모양이다(밝기만 바뀌도록 씨앗을 모양으로 고정).
            shape_rng = random.Random(100 + shape)
            tint = row if row < len(TINT_START) else 1
            draw_tuft(image, shape * CELL_W, row * CELL_H, shape_rng, tint, flower=row == len(TINT_START))

    out = Path(project) / "Assets/MiniPandoras/Art/Foliage/Lawn"
    out.mkdir(parents=True, exist_ok=True)
    image.save(out / "tufts.png")
    print(out / "tufts.png", image.size)

    clumps = Image.new("RGBA", (CLUMP_W * CLUMP_SHAPES, CLUMP_H * len(CLUMP_TINT_START)), (0, 0, 0, 0))
    for shape in range(CLUMP_SHAPES):
        for row in range(len(CLUMP_TINT_START)):
            draw_clump(clumps, shape * CLUMP_W, row * CLUMP_H, random.Random(200 + shape), row)
    clumps.save(out / "clumps.png")
    print(out / "clumps.png", clumps.size)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else ".")
