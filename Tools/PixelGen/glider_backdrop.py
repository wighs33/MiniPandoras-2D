"""글라이더 탈출 화면의 배경(배경 그래픽 향상).

글라이더 화면은 가상 화면 384×216에 그린다. 배경 층은 맵과 같은 도트 밀도(가상 1px = 2점)로 그린다.
  sky.png      노을 하늘(디더링한 띠, 희미한 별, 해와 햇무리)                  1024×576, 넓은 화면·높은 화면까지 덮는다
  far.png      먼 산맥: 해 쪽(오른쪽) 비탈이 붉게 빛나고 봉우리에 눈, 아래는 안개   1536×220, 좌우가 이어진다
  near.png     가까운 숲 능선: 나무 우듬지 실루엣, 해 쪽 가장자리에 노을빛 테두리     1536×150, 좌우가 이어진다
  cloud_0~2    해 쪽이 밝은 뭉게구름
  castle.png   멀어지는 뱀탑: 산꼭대기 위 뱀탑 실루엣(창 불빛과 붉은 보석 눈만 빛난다)
  town.png     다가오는 힐리아 마을: 지붕·굴뚝·길드 첨탑 실루엣과 창 불빛

사용: python Tools/PixelGen/glider_backdrop.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

import castle
import pixelgen as pg

W, H = 768, 432
# 화면 비율이 16:9가 아니면 가상 화면이 오른쪽이나 아래로 넓어진다(CanvasScaler Expand). 하늘은 그만큼 크게 그린다.
SKY_W, SKY_H = 1024, 576
SKY_STOPS = [(0.0, "#241c48"), (0.5, "#8a4a6a"), (0.85, "#f0985a"), (1.0, "#ffc878")]
SUN = (600, 262)
FAR = pg.hexes("#3a2442", "#4a2c4a", "#5c3654", "#7a4660", "#a85e6a", "#d8867a", "#e8c0c8")
NEAR = pg.hexes("#160e1c", "#1f1428", "#2c1c34", "#3e2640", "#6a3a44", "#b0604a", "#e89058")
CLOUD = pg.hexes("#5a3a5a", "#8a5a7a", "#c88aa0", "#f0b8b0", "#ffe0c8")
SILHOUETTE = pg.hexes("#2a1c34", "#34243c", "#4a3050")
LAMP = pg.hexes("#ffcf7a")[0]
EYE = pg.hexes("#ff5a2a")[0]


def sky(rng):
    """노을 하늘: 위에서 아래로 띠를 나눠 칠하고 띠 경계만 디더로 섞는다. 위쪽에 희미한 별, 아래 오른쪽에 해와 햇무리."""
    yy, xx = np.mgrid[0:SKY_H, 0:SKY_W]
    t = (yy + 0.5) / H
    bands = 14
    stepped = np.floor(t * bands + (pg.bayer(SKY_H, SKY_W) - 0.5) * 0.9) / bands
    image = pg.mix_stops(SKY_STOPS, np.clip(stepped, 0, 1))
    distance = np.hypot(xx - SUN[0], (yy - SUN[1]) * 1.25)
    glow = np.exp(-distance / 90)
    steps = 6
    glow = np.floor(glow * steps + (pg.bayer(SKY_H, SKY_W) - 0.5) * 0.9) / steps
    image = image * (1 - glow[..., None] * 0.7) + np.array([255, 214, 150]) * glow[..., None] * 0.7
    distance = np.hypot(xx - SUN[0], yy - SUN[1])
    image[distance < 30] = pg.hexes("#fff0c0")[0]
    image[distance < 26] = pg.hexes("#ffe4a0")[0]
    stars = (rng.random((SKY_H, SKY_W)) < 0.0016) & (t < 0.36)
    image[stars] = image[stars] * 0.4 + np.array([255, 240, 220]) * 0.6
    return image.astype(np.uint8)


def periodic_profile(width, rng, waves):
    """좌우가 이어지는 높이 곡선: 너비로 나누어떨어지는 파장의 사인을 더한다."""
    x = np.arange(width)
    profile = np.zeros(width)
    for cycles, amplitude in waves:
        profile += amplitude * np.sin(2 * np.pi * cycles * x / width + rng.uniform(0, 2 * np.pi))
    return profile


def far_range(rng):
    """
    먼 산맥: 봉우리마다 꼭짓점에서 내려오는 등줄기로 면을 나눠, 해가 오는 오른쪽 면은 붉게 빛나고 왼쪽 면은 그늘이다.
    높은 봉우리를 먼저 그려 낮은 봉우리가 앞에서 겹치게 하고, 높은 봉우리에는 눈이 덮인다. 아래로 갈수록 안개에 잠긴다.
    """
    width, height = 2 * W, 220
    yy, xx = np.mgrid[0:height, 0:width]
    jag = np.round(np.abs(periodic_profile(width, rng, ((37, 1.2), (71, 0.8), (113, 0.6))))).astype(int)
    level = np.full((height, width), -1)
    peaks = []
    x = 0.0
    while x < width - 40:
        peaks.append((x + rng.uniform(0, 30), rng.uniform(34, 120), rng.uniform(0.7, 1.1), rng.uniform(0.55, 0.9),
                      rng.uniform(-0.3, 0.3)))
        x += rng.uniform(60, 110)
    for px, top, left_slope, right_slope, lean in sorted(peaks, key=lambda p: p[1]):
        for cx in (px - width, px, px + width):
            dx = xx - cx
            below = yy - top
            edge = np.where(dx < 0, -dx * left_slope, dx * right_slope) + jag[None, :]
            inside = below >= edge
            spine = cx + below * lean + np.sin(below / 7) * 2
            lit = xx > spine
            face = np.where(lit, np.where(below < 30, 4, 3), 1)
            gully = lit & (((xx - spine) * 0.8 - below * 0.5) % 19 < 1.5) & (below > 12)
            face = np.where(gully, 2, face)
            rim = lit & (below - edge < 3)
            face = np.where(rim, 5, np.where(~lit & (below - edge < 2), 2, face))
            snow = (top < 80) & (below < (80 - top) * 0.5 + jag[None, :] * 2)
            face = np.where(snow, np.where(lit, 6, 3), face)
            level = np.where(inside, face, level)
    inside = level >= 0
    rgb = FAR[np.maximum(level, 0)].astype(float)
    haze = np.clip((yy - 120) / 100, 0, 1)[..., None]
    haze = np.floor(haze * 5 + (pg.bayer(height, width)[..., None] - 0.5) * 0.9) / 5
    rgb = rgb * (1 - haze * 0.55) + np.array([214, 130, 120]) * haze * 0.55
    image = np.zeros((height, width, 4), dtype=np.uint8)
    image[inside, :3] = rgb[inside].astype(np.uint8)
    image[inside, 3] = 255
    return image


def near_forest(rng):
    """가까운 숲 능선: 둥근 우듬지와 뾰족한 침엽수가 섞인 실루엣, 해 쪽 오른쪽 가장자리에 노을빛 테두리."""
    width, height = 2 * W, 150
    ground = 90 + periodic_profile(width, rng, ((2, 14), (5, 8), (11, 4)))
    top = ground.copy()
    x = 0
    while x < width:
        crown_w = int(rng.integers(14, 30))
        conifer = rng.random() < 0.35
        lift = rng.integers(14, 34)
        for dx in range(-crown_w, crown_w + 1):
            px = (x + dx) % width
            u = dx / crown_w
            bump = lift * (1 - abs(u)) if conifer else lift * np.sqrt(max(0.0, 1 - u * u))
            top[px] = min(top[px], ground[px] - bump)
        x += int(crown_w * rng.uniform(0.9, 1.4))
    yy, _ = np.mgrid[0:height, 0:width]
    inside = yy >= top[None, :]
    depth = yy - top[None, :]
    rising = np.repeat((np.gradient(top) > 0.2)[None, :], height, axis=0)
    level = np.where(depth < 2, 3, 2)
    level = np.where(rising & (depth < 2), 6, np.where(rising & (depth < 4), 5, level))
    level = np.where(rising & (depth >= 4) & (depth < 7), 4, level)
    level = np.where(depth > 40, 1, level)
    image = np.zeros((height, width, 4), dtype=np.uint8)
    image[inside, :3] = NEAR[level][inside]
    image[inside, 3] = 255
    return image


def cloud(rng, width=150, height=50):
    """뭉게구름: 둥근 덩어리를 겹쳐 모양을 잡고, 해 쪽(오른쪽 아래) 가장자리는 밝게, 왼쪽 위는 보랏빛 그늘."""
    yy, xx = np.mgrid[0:height, 0:width]
    shape = np.zeros((height, width), dtype=bool)
    lumps = [(width * 0.5, height * 0.62, width * 0.42, height * 0.36)]
    for _ in range(6):
        lumps.append((rng.uniform(0.2, 0.8) * width, rng.uniform(0.35, 0.7) * height, rng.uniform(0.12, 0.24) * width,
                      rng.uniform(0.25, 0.4) * height))
    light = np.zeros((height, width))
    for cx, cy, rx, ry in lumps:
        d = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2
        shape |= d <= 1
        light = np.maximum(light, np.where(d <= 1, ((xx - cx) / rx * 0.6 + (yy - cy) / ry * 0.2 + 1) / 2, 0))
    shape &= yy < height * 0.86
    level = pg.dither_levels(light, (0.3, 0.5, 0.68, 0.82), 0.12)
    image = np.zeros((height, width, 4), dtype=np.uint8)
    image[shape, :3] = CLOUD[level][shape]
    image[shape, 3] = 255
    return image


def silhouette(image, scale):
    """그림을 줄여 실루엣으로: 밝기 세 단계 어두운 보라, 창 불빛(따뜻한 색)과 보석 눈(붉은색)만 빛으로 남긴다."""
    src = Image.fromarray(image, "RGBA")
    small = np.array(src.resize((int(src.width * scale), int(src.height * scale)), Image.LANCZOS))
    alpha = small[..., 3] > 110
    rgb = small[..., :3].astype(float)
    lum = rgb @ np.array([0.3, 0.55, 0.15]) / 255
    out = np.zeros_like(small)
    out[alpha, :3] = SILHOUETTE[np.clip((lum[alpha] * 4).astype(int), 0, 2)]
    out[alpha, 3] = 255
    warm = alpha & (rgb[..., 0] > 200) & (rgb[..., 1] > 130) & (rgb[..., 2] < 140)
    red = alpha & (rgb[..., 0] > 180) & (rgb[..., 1] < 110)
    out[warm, :3] = LAMP
    out[red, :3] = EYE
    head = np.arange(out.shape[0])[:, None] < out.shape[0] * 0.25
    out[head & (warm | red), :3] = EYE
    return out


def castle_on_peak(rng):
    """멀어지는 뱀탑: 바위 봉우리 위에 뱀탑 실루엣."""
    tower = silhouette(castle.serpent_tower(np.random.default_rng(9))[0], 0.3)
    width = 240
    height = tower.shape[0] + 70
    image = np.zeros((height, width, 4), dtype=np.uint8)
    yy, xx = np.mgrid[0:height, 0:width]
    peak_top = height - 80
    peak = yy >= peak_top + np.abs(xx - width / 2) * 0.55 + np.sin(xx / 9) * 3
    image[peak, :3] = SILHOUETTE[1]
    image[peak & (xx > width / 2) & (yy < peak_top + 30), :3] = SILHOUETTE[2]
    image[peak, 3] = 255
    left = (width - tower.shape[1]) // 2
    top = peak_top - tower.shape[0] + 14
    region = image[top:top + tower.shape[0], left:left + tower.shape[1]]
    opaque = tower[..., 3] > 0
    region[opaque] = tower[opaque]
    return image


def town(rng):
    """다가오는 마을: 여러 높이의 집 지붕(세모·네모), 굴뚝, 가운데 길드 첨탑, 창마다 불빛."""
    width, height = 420, 120
    image = np.zeros((height, width, 4), dtype=np.uint8)
    yy, xx = np.mgrid[0:height, 0:width]
    x = 0
    while x < width:
        w = int(rng.integers(26, 44))
        wall_top = height - int(rng.integers(26, 46))
        if 180 < x < 230:
            wall_top = height - 60
        body = (xx >= x) & (xx < x + w - 3) & (yy >= wall_top)
        roof_h = int(rng.integers(10, 20))
        roof = (yy >= wall_top - roof_h + np.abs(xx - (x + w / 2 - 1.5)) * roof_h / (w / 2)) & (yy < wall_top) & \
               (xx >= x - 2) & (xx < x + w - 1)
        shape = body | roof
        image[shape, :3] = SILHOUETTE[0 if (x // 10) % 2 else 1]
        image[shape, 3] = 255
        if rng.random() < 0.6:
            cx = x + int(w * 0.7)
            chimney = (xx >= cx) & (xx < cx + 4) & (yy >= wall_top - roof_h - 2) & (yy < wall_top)
            image[chimney, :3] = SILHOUETTE[0]
            image[chimney, 3] = 255
        for wx in range(x + 5, x + w - 8, 9):
            for wy in range(wall_top + 6, height - 10, 12):
                if rng.random() < 0.55:
                    image[wy:wy + 4, wx:wx + 3, :3] = LAMP
        x += w - 2
    spire_x = 205
    spire = (yy >= 8 + np.abs(xx - spire_x) * 2.6) & (yy < height - 60) & (np.abs(xx - spire_x) < 10)
    image[spire, :3] = SILHOUETTE[1]
    image[spire, 3] = 255
    return image


def main(project):
    out = project / "Assets/MiniPandoras/Art/PixelGen/Glider"
    out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(23)
    Image.fromarray(sky(rng), "RGB").save(out / "sky.png")
    Image.fromarray(far_range(rng), "RGBA").save(out / "far.png")
    Image.fromarray(near_forest(rng), "RGBA").save(out / "near.png")
    for k in range(3):
        Image.fromarray(cloud(np.random.default_rng(40 + k)), "RGBA").save(out / f"cloud_{k}.png")
    Image.fromarray(castle_on_peak(rng), "RGBA").save(out / "castle.png")
    Image.fromarray(town(rng), "RGBA").save(out / "town.png")
    preview(project, out)


def preview(project, out):
    """게임 화면처럼 겹쳐 본 미리보기(성은 왼쪽, 마을은 오른쪽에 반쯤 들어온 때)."""
    scene = Image.open(out / "sky.png").convert("RGBA").crop((0, 0, W, H))
    for k, (x, y) in enumerate(((60, 40), (330, 90), (520, 30))):
        scene.alpha_composite(Image.open(out / f"cloud_{k}.png"), (x, y))
    far = Image.open(out / "far.png")
    scene.alpha_composite(far.crop((0, 0, W, far.height)), (0, H - far.height - 40))
    tower = Image.open(out / "castle.png")
    scene.alpha_composite(tower, (40, H - tower.height - 70))
    near = Image.open(out / "near.png")
    scene.alpha_composite(near.crop((0, 0, W, near.height)), (0, H - near.height))
    village = Image.open(out / "town.png")
    scene.alpha_composite(village.crop((0, 0, 300, village.height)), (W - 300, H - village.height))
    path = project / "Docs/ArtUpgrade/PixelGen/glider_preview.png"
    scene.save(path)
    print(path)


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
