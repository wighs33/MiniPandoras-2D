"""PixelLab 휘두르기 그림에서 방망이를 떼어 낸다(사용자 요청 2026-11-03: 장비를 바꿔 끼울 수 있게, 가려진 곳은 계산으로 메운다).

장마다 이렇게 나눈다.
- 몸: 방망이·궤적을 지운 주인공. 지운 자리 가운데 몸 밖은 투명하게, 몸 안은 메운다.
  - 머리가 가려졌던 자리는 PixelLab에 넣은 첫 장(방망이가 머리를 가리지 않은 그림)의 머리를 이 장의 보이는 머리와 가장 잘 겹치게 옮겨 와 채운다.
  - 나머지(재킷·바지를 가로지른 가는 띠)는 둘레 색에서 번져 채운다(라플라스 방정식을 야코비 반복으로 푼다).
  - 새로 드러난 몸 가장자리에는 테두리를 한 줄 다시 긋는다.
- 앞손: 손잡이를 감싼 손(살색 덩어리와 그 테두리). 무기 위에 다시 얹어 쥔 것처럼 보이게 한다.
- 궤적: 곧은 방망이에서 벗어난 갈색 잔상. 무기마다 색을 입힐 수 있게 알파만 남긴다.
- 무기 자리: 손잡이를 쥔 점(손 가운데), 방망이가 뻗는 각도, 보이는 길이(앞뒤로 누우면 짧아진다), 몸 뒤에 있는지
  (축 위에서 방망이 대신 몸이 보이면 몸이 앞이다. 머리 뒤로 든 방망이).

확인: 뗀 몸에 무기 자리대로 방망이와 검을 다시 그려 끼워 본다(recomposed.png: 원본, 뗀 몸, 방망이, 검).

방망이 찾기: 방망이와 궤적은 주인공 색(검정·회색·흰색·살색)과 색상이 달라 색으로 가려낸다(갈색).
- 쥔 손은 거기서 곧게 뻗는 갈색 점이 가장 많은 작은 살색 덩어리다(얼굴은 크다). 방망이는 손에서 곧게 뻗고, 궤적은 휘어 있다.
- 그 손에서 곧게 뻗어 갈색 점을 가장 많이 지나는 각도가 방망이 축이다.
- 축 둘레 띠 안에서는 갈색과 따뜻한 어두운 색(방망이 그늘 쪽·테두리)을 지운다. 머리카락·재킷은 차가운 검정이라 남는다.
- 띠 밖에서는 큰 갈색 덩어리만 궤적으로 지운다. 얼굴의 작은 그림자 점은 나무가 아니다.

  Docs/ArtUpgrade/PixelLab/hero_swing/separated/body_<방향>_<장>.png, hand_…, trail_…, weapon_(bat|sword)_…, weapon.json, compare.png, recomposed.png

사용: python Tools/PixelLab/separate_weapon.py <프로젝트 루트>
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

DIRECTIONS = ("down", "right", "up")
FRAMES = 3
BAT_LENGTH = 52.0        # 손잡이 쥔 점에서 방망이 머리 끝까지(px). 첫 장에 쥐여 준 방망이 길이
BAT_RADIUS = 6.0         # 방망이 몸통 반지름 + 테두리
KNOB = 7.0               # 쥔 점 뒤로 나온 손잡이 끝
HAND_MAX = 160           # 손 살색 덩어리의 최대 점 수. 얼굴은 200점이 넘는다
BROWN_MIN = 25           # 띠 밖에서 궤적으로 볼 갈색 덩어리의 최소 점 수
SPECK = 40               # 몸에서 떨어진 이보다 작은 조각은 궤적의 속도 점이라 지운다
HEAD_LINE = 118          # 첫 장에서 머리와 후드 깃의 아래 끝 줄
HEAD_SHIFT = 14          # 첫 장의 머리를 옮겨 맞춰 볼 범위(px)
OUTLINE = np.array([0x1a, 0x14, 0x18], np.uint8)


# ── 점 모양 다루기 ───────────────────────────────────────

def grow(mask, steps=1):
    out = mask.copy()
    for _ in range(steps):
        p = np.pad(out, 1)
        out = out | p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:]
    return out


def shrink(mask, steps=1):
    return ~grow(~mask, steps)


def shifted(array, dx, dy):
    """그림을 (dx, dy)만큼 옮긴다. 밖으로 나간 자리는 0이다."""
    out = np.zeros_like(array)
    height, width = array.shape[:2]
    ys, yd = (slice(0, height - dy), slice(dy, height)) if dy >= 0 else (slice(-dy, height), slice(0, height + dy))
    xs, xd = (slice(0, width - dx), slice(dx, width)) if dx >= 0 else (slice(-dx, width), slice(0, width + dx))
    out[yd, xd] = array[ys, xs]
    return out


def pieces(mask):
    """4방향으로 이어진 덩어리마다 (y, x) 칸 배열."""
    height, width = mask.shape
    labels = np.zeros(mask.shape, int)
    found = []
    for y, x in zip(*np.nonzero(mask)):
        if labels[y, x]:
            continue
        labels[y, x] = len(found) + 1
        stack, cells = [(y, x)], []
        while stack:
            cy, cx = stack.pop()
            cells.append((cy, cx))
            for ny, nx in ((cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)):
                if 0 <= ny < height and 0 <= nx < width and mask[ny, nx] and not labels[ny, nx]:
                    labels[ny, nx] = len(found) + 1
                    stack.append((ny, nx))
        found.append(np.array(cells))
    return found


def keep_large(mask, minimum):
    out = np.zeros(mask.shape, bool)
    for cells in pieces(mask):
        if len(cells) >= minimum:
            out[cells[:, 0], cells[:, 1]] = True
    return out


def segment_mask(shape, start, direction, length, radius):
    yy, xx = np.mgrid[0:shape[0], 0:shape[1]] + 0.5
    px, py = xx - start[0], yy - start[1]
    t = np.clip(px * direction[0] + py * direction[1], 0, length)
    return np.hypot(px - t * direction[0], py - t * direction[1]) <= radius


# ── 색 ────────────────────────────────────────────────

def hsv(rgba):
    rgb = rgba[..., :3].astype(float) / 255
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    top, low = rgb.max(-1), rgb.min(-1)
    delta = top - low + 1e-9
    hue = np.where(top == r, ((g - b) / delta) % 6, np.where(top == g, (b - r) / delta + 2, (r - g) / delta + 4)) * 60
    saturation = np.where(top > 0, (top - low) / np.maximum(top, 1e-9), 0)
    return hue, saturation, top


def classify(rgba):
    """
    살색, 갈색(나무·궤적), 따뜻한 어두운 색. 살색은 밝고 덜 짙고, 나무는 더 짙고 어둡다(아홉 장의 색 분포로 정한 경계).
    머리카락·재킷의 검정은 보랏빛이 돌아 따뜻한 색에 들지 않는다.
    """
    opaque = rgba[..., 3] > 0
    hue, sat, val = hsv(rgba)
    warm = opaque & (hue > 5) & (hue < 50) & (sat > 0.15)
    skin = warm & ((val >= 0.85) | ((sat < 0.38) & (val >= 0.78)))
    brown = warm & ~skin & (val >= 0.25) & (sat >= 0.38)
    dark = warm & ~skin & (val < 0.3)
    return opaque, skin, brown, dark


# ── 방망이 찾기 ──────────────────────────────────────────

def hands(skin, brown):
    """방망이를 쥔 손: 갈색에 닿은 작은 살색 덩어리 가운데 거기서 곧게 뻗는 갈색이 가장 많은 것과, 그 둘레의 다른 작은 덩어리(두 손)."""
    touching = grow(brown, 2)
    small = [cells for cells in pieces(skin) if 8 <= len(cells) <= HAND_MAX]
    # 손이 방망이에 가려 갈색에 닿지 않는 장도 있어, 그때는 작은 덩어리 모두를 본다.
    candidates = [cells for cells in small if touching[cells[:, 0], cells[:, 1]].any()] or small
    straight = [best_ray(cells.mean(axis=0)[::-1] + 0.5, brown)[0] for cells in candidates]
    main = candidates[int(np.argmax(straight))]
    centre = main.mean(axis=0)
    mask = np.zeros(skin.shape, bool)
    for cells in small:
        if np.hypot(*(cells.mean(axis=0) - centre)) <= 12 + 0.5 * np.sqrt(len(cells)):
            mask[cells[:, 0], cells[:, 1]] = True
    return mask


def best_ray(start, brown):
    """start에서 곧게 뻗어 갈색 점을 가장 많이 지나는 (점 수, 각도°)."""
    best = (-1, 0.0)
    for degrees in range(0, 360, 2):
        a = np.radians(degrees)
        d = np.array([np.cos(a), np.sin(a)])
        count = (segment_mask(brown.shape, start, d, BAT_LENGTH, 2.5) & brown).sum()
        if count > best[0]:
            best = (count, float(degrees))
    return best


def find_bat(grip_hands, brown):
    """쥔 손의 가운데를 쥔 점으로, 거기서 곧게 뻗어 갈색 점을 가장 많이 지나는 각도와 그 끝까지 보이는 길이."""
    ys, xs = np.nonzero(grip_hands)
    grip = np.array([xs.mean(), ys.mean()]) + 0.5
    best = best_ray(grip, brown)
    a = np.radians(best[1])
    d = np.array([np.cos(a), np.sin(a)])
    near_brown = grow(brown, 1)
    seen = 0.0
    for step in np.arange(0, BAT_LENGTH + 12, 1.0):
        x, y = (grip + d * step).astype(int)
        if 0 <= y < brown.shape[0] and 0 <= x < brown.shape[1] and near_brown[y, x]:
            seen = step
    return grip, best[1], seen


# ── 메우기 ─────────────────────────────────────────────

def borrow_head(rgba, visible, removed, reference):
    """
    첫 장의 머리를 이 장의 보이는 머리와 가장 잘 겹치는 자리로 옮겨, 지운 자리 가운데 그 머리에 드는 칸의 색을 준다.
    겹침은 보이는 칸의 색 차이와, 첫 장 머리 자리인데 이 장에서는 비어 있는 칸 수로 잰다.
    """
    rows = np.arange(rgba.shape[0])[:, None]
    head = (reference[..., 3] > 0) & (rows < HEAD_LINE)
    if not (removed & grow(head, HEAD_SHIFT)).any():
        return np.zeros(removed.shape, bool), reference[..., :3]
    frame_rgb = rgba[..., :3].astype(float)
    empty = (rgba[..., 3] == 0) & ~removed
    best = (np.inf, 0, 0)
    for dy in range(-HEAD_SHIFT, HEAD_SHIFT + 1):
        for dx in range(-HEAD_SHIFT, HEAD_SHIFT + 1):
            moved = shifted(head, dx, dy)
            overlap = moved & visible
            if overlap.sum() < 150:
                continue
            colour = np.abs(frame_rgb[overlap] - shifted(reference[..., :3], dx, dy)[overlap].astype(float)).mean()
            miss = (moved & empty).sum() / moved.sum()
            score = colour + 200 * miss
            if score < best[0]:
                best = (score, dx, dy)
    _, dx, dy = best
    return removed & shifted(head, dx, dy), shifted(reference[..., :3], dx, dy)


def between(mask, normal, reach):
    """mask 칸이 normal 방향 앞뒤 양쪽 reach px 안에 다 있는 자리."""
    ahead = np.zeros(mask.shape, bool)
    behind = np.zeros(mask.shape, bool)
    for step in range(1, reach + 1):
        dx, dy = int(round(normal[0] * step)), int(round(normal[1] * step))
        ahead |= shifted(mask, -dx, -dy)
        behind |= shifted(mask, dx, dy)
    return ahead & behind


def fill(rgb, known, unknown, passes=400):
    """unknown 자리를 known 둘레 색에서 번져 채운다(라플라스 방정식을 야코비 반복으로 푼다)."""
    out = rgb.astype(float).copy()
    weight = known.astype(float)
    out[~known] = 0
    for _ in range(passes):
        p = np.pad(out * weight[..., None], ((1, 1), (1, 1), (0, 0)))
        w = np.pad(weight, 1)
        total = p[:-2, 1:-1] + p[2:, 1:-1] + p[1:-1, :-2] + p[1:-1, 2:]
        count = w[:-2, 1:-1] + w[2:, 1:-1] + w[1:-1, :-2] + w[1:-1, 2:]
        ready = unknown & (count > 0)
        out[ready] = total[ready] / count[ready][..., None]
        weight = np.where(ready, 1.0, weight)
    return out.clip(0, 255).astype(np.uint8)


def separate(rgba, reference):
    opaque, skin, brown, dark = classify(rgba)
    trail_brown = keep_large(brown, BROWN_MIN)
    grip_hands = hands(skin, trail_brown)
    grip, angle, seen = find_bat(grip_hands, trail_brown)
    d = np.array([np.cos(np.radians(angle)), np.sin(np.radians(angle))])
    bat = segment_mask(opaque.shape, grip - d * KNOB, d, seen + KNOB, BAT_RADIUS)
    trail = trail_brown & ~grow(bat, 1)
    # 얼굴(가장 큰 살색 덩어리)과 쥔 손은 지키고, 방망이 띠 안의 밝은 나무(살색으로 보이는 빛 받은 쪽)는 지운다.
    face_cells = max(pieces(skin), key=len)
    face = np.zeros(skin.shape, bool)
    face[face_cells[:, 0], face_cells[:, 1]] = True
    keep = face | grip_hands
    removed = ((bat & (brown | dark | skin)) | grow(trail_brown, 1)) & opaque & ~keep
    body = opaque & ~removed
    head, head_rgb = borrow_head(rgba, body, removed, reference)
    rgb = rgba[..., :3].copy()
    rgb[head] = head_rgb[head]
    known = body | head
    # 방망이 자리는 축에 수직으로 양쪽 6px 안에 몸이 있을 때만 몸을 가로질렀던 것이다. 궤적 자리는 작은 틈만 몸이다.
    across = between(known, np.array([-d[1], d[0]]), 6)
    inside = removed & ((bat & across) | (~bat & shrink(grow(known, 3), 3)) | head)
    rgb = np.where((inside & ~head)[..., None], fill(rgb, known, inside & ~head), rgb)
    final = body | inside
    # 지운 자리 둘레에 남은 방망이·궤적 테두리(가는 선)를 지우고, 가장 큰 몸 덩어리 곁에 붙지 않은 조각(속도 점, 궤적 끝)도 지운다.
    final &= ~(final & ~grow(shrink(final, 1), 1) & grow(removed, 3))
    parts = sorted(pieces(final), key=len, reverse=True)
    main_body = np.zeros(final.shape, bool)
    main_body[parts[0][:, 0], parts[0][:, 1]] = True
    near_body = grow(main_body, 4)
    for cells in parts[1:]:
        if len(cells) < SPECK or not near_body[cells[:, 0], cells[:, 1]].any():
            final[cells[:, 0], cells[:, 1]] = False
    out = np.zeros_like(rgba)
    out[final, :3] = rgb[final]
    out[final, 3] = 255
    # 방망이가 붙어 있던 몸 가장자리는 테두리가 같이 지워졌으므로 한 줄 다시 긋는다.
    edge = grow(final, 1) & ~final & grow(removed, 2)
    out[edge, :3] = OUTLINE
    out[edge, 3] = 255
    hand = np.zeros_like(rgba)
    hand_mask = grow(grip_hands, 1) & opaque & ~trail
    hand[hand_mask] = rgba[hand_mask]
    trail_image = np.zeros_like(rgba)
    trail_image[trail, :3] = 255
    trail_image[trail, 3] = 255
    # 축 위(손에서 8px 뒤부터)에서 방망이 대신 몸이 보이는 점이 많으면 방망이가 몸 뒤에 있었다.
    samples = [(grip + d * t).astype(int) for t in np.arange(8, max(9.0, seen), 1.0)]
    hidden = sum(1 for x, y in samples if 0 <= y < opaque.shape[0] and 0 <= x < opaque.shape[1]
                 and opaque[y, x] and not (brown | dark | skin)[y, x])
    behind = bool(samples) and hidden / len(samples) > 0.25
    place = {"grip": [round(float(grip[0]), 1), round(float(grip[1]), 1)], "angle": angle, "length": round(seen, 1),
             "behind": behind, "headFromFirstFrame": int(head.sum())}
    return out, hand, trail_image, place


# ── 저장과 비교 ──────────────────────────────────────────

WOOD = np.array([[0x3e, 0x2a, 0x1d], [0x6a, 0x48, 0x31], [0x8e, 0x66, 0x45], [0xb0, 0x86, 0x5c], [0xcc, 0xa4, 0x76]], float)
STEEL = np.array([[0x5a, 0x60, 0x6c], [0x8a, 0x92, 0x9e], [0xb8, 0xc0, 0xca], [0xdc, 0xe2, 0xe8], [0xf4, 0xf6, 0xf8]], float)
GOLD = np.array([[0x6a, 0x52, 0x26], [0xb8, 0x95, 0x4e], [0xe2, 0xc2, 0x6e]], float)
LEATHER = np.array([[0x2c, 0x22, 0x1c], [0x4a, 0x38, 0x2a]], float)


def weapon_layer(size, place, kind, ss=4):
    """무기 자리대로 무기 한 자루를 그린 RGBA(4배로 그려 줄여 가장자리가 부드럽다). 확인용 미리보기다."""
    n = size * ss
    yy, xx = np.mgrid[0:n, 0:n] / ss
    g = np.array(place["grip"])
    d = np.array([np.cos(np.radians(place["angle"])), np.sin(np.radians(place["angle"]))])
    side = np.array([-d[1], d[0]])
    length = max(place["length"], 12.0)
    px, py = xx - g[0], yy - g[1]
    along = px * d[0] + py * d[1]
    across = px * side[0] + py * side[1]
    rgb = np.zeros((n, n, 3))
    alpha = np.zeros((n, n))

    def piece(start, end, r0, r1, palette):
        t = np.clip((along - start) / max(end - start, 1e-6), 0, 1)
        radius = r0 + (r1 - r0) * t
        inside = (along >= start - r0) & (along <= end + r1) & (np.abs(across) <= radius)
        outline = (along >= start - r0 - 1.2) & (along <= end + r1 + 1.2) & (np.abs(across) <= radius + 1.2)
        light = np.clip(0.55 - across / (radius + 0.01) * 0.45, 0, 0.999)
        rgb[outline & (alpha == 0)] = (0x16, 0x10, 0x0c)
        alpha[outline] = 1
        rgb[inside] = palette[(light * len(palette)).astype(int)][inside]

    if kind == "bat":
        piece(-KNOB, length, 2.0, 5.0, WOOD)
    else:
        piece(-6, 4, 1.8, 1.8, LEATHER)
        piece(6, length + 6, 2.4, 0.6, STEEL)
        guard = (np.abs(along - 5) <= 1.4) & (np.abs(across) <= 6.5)
        rgb[grow(guard, 1) & (alpha == 0)] = (0x16, 0x10, 0x0c)
        alpha[grow(guard, 1)] = 1
        rgb[guard] = GOLD[1]
    image = np.dstack([rgb, alpha * 255])
    small = image.reshape(size, ss, size, ss, 4)
    a = small[..., 3].mean(axis=(1, 3))
    colour = (small[..., :3] * small[..., 3:4]).sum(axis=(1, 3)) / np.maximum(small[..., 3].sum(axis=(1, 3))[..., None], 1e-6)
    return np.dstack([colour, a]).clip(0, 255).astype(np.uint8)


def recompose(body, hand, place, kind):
    """몸·무기·앞손을 겹친다. 무기가 몸 뒤에 있던 장은 무기를 먼저 그린다."""
    size = body.shape[1]
    weapon = Image.fromarray(weapon_layer(size, place, kind))
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    layers = [weapon, Image.fromarray(body)] if place["behind"] else [Image.fromarray(body), weapon]
    for layer in layers + [Image.fromarray(hand)]:
        out.alpha_composite(layer)
    return out


def on_grass(image):
    bg = Image.new("RGBA", image.size, (110, 140, 100, 255))
    bg.alpha_composite(image)
    return bg


def main(project):
    source = project / "Assets/MiniPandoras/Art/PixelLab/Characters/hero"
    raw = project / "Docs/ArtUpgrade/PixelLab/hero_swing"
    out = raw / "separated"
    out.mkdir(parents=True, exist_ok=True)
    places = {}
    rows = []
    for direction in DIRECTIONS:
        reference = np.array(Image.open(raw / f"first_{direction}.png").convert("RGBA"))
        for i in range(FRAMES):
            name = f"{direction}_{i}"
            rgba = np.array(Image.open(source / f"swing_{name}.png").convert("RGBA"))
            body, hand, trail, place = separate(rgba, reference)
            places[name] = place
            Image.fromarray(body).save(out / f"body_{name}.png")
            Image.fromarray(hand).save(out / f"hand_{name}.png")
            Image.fromarray(trail).save(out / f"trail_{name}.png")
            rows.append((rgba, body, hand, place))
    (out / "weapon.json").write_text(json.dumps({"pixelsPerLogical": 4, "frames": places}, indent=1), encoding="utf-8")
    # 비교: 위는 PixelLab 원본, 아래는 방망이를 뗀 몸.
    size = rows[0][0].shape[1]
    sheet = Image.new("RGBA", (size * len(rows), size * 2), (255, 255, 255, 255))
    for k, (original, body, _, _) in enumerate(rows):
        sheet.paste(on_grass(Image.fromarray(original)), (k * size, 0))
        sheet.paste(on_grass(Image.fromarray(body)), (k * size, size))
    sheet.save(out / "compare.png")
    # 끼워 보기: 위에서부터 원본, 뗀 몸, 방망이를 다시 끼운 것, 검을 끼운 것.
    sheet = Image.new("RGBA", (size * len(rows), size * 4), (255, 255, 255, 255))
    for k, (original, body, hand, place) in enumerate(rows):
        sheet.paste(on_grass(Image.fromarray(original)), (k * size, 0))
        sheet.paste(on_grass(Image.fromarray(body)), (k * size, size))
        sheet.paste(on_grass(recompose(body, hand, place, "bat")), (k * size, size * 2))
        sheet.paste(on_grass(recompose(body, hand, place, "sword")), (k * size, size * 3))
    sheet.save(out / "recomposed.png")
    # 확인용 무기 레이어(장마다, 몸과 같은 판): 분해도 페이지가 몸·앞손과 겹쳐 그린다.
    for direction in DIRECTIONS:
        for i in range(FRAMES):
            name = f"{direction}_{i}"
            for kind in ("bat", "sword"):
                Image.fromarray(weapon_layer(size, places[name], kind)).save(out / f"weapon_{kind}_{name}.png")
    print(out)


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
