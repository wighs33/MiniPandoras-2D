"""세계지도: 사용자가 그린 스케치를 따라 도트로 다시 그린다. 웹게임에는 없다.

스케치의 선을 읽어 해안선, 강, 국경, 산줄기, 세계수 뿌리의 자리를 그대로 쓰고 색과 입체감만 입힌다.
공장, 신전, 소용돌이처럼 그려져 있는 것은 같은 자리에 같은 모양으로 그린다.
사용자가 정한 지역 설정: 오른쪽 섬 전체가 힐리아 동부로 농경지이고 뱀성과 뱀열차가 있다. 왼쪽 끝과 오른쪽 끝에 항구가 있다.
섬 안에서는 서쪽 부두(배편이 끊겼다, 끝의 문이 레비아탄의 바다로 간다)와 동쪽 선착장(서부행 배가 떠난다)이 따로 있다.
아이로스의 건물 자리는 최첨단 도시와 군사 지역이다. 마르는 동아시아 문화권이고, 오른쪽 위 섬은 무릉도원이다.
게임에 맵이 있는 곳(PLACES), 맵이 아직 없는 곳(스케치의 점, FUTURE_TOWNS), 이어진 길(ROADS)은 그림에 굽지 않고
world_map.json에 적는다. 지도 창이 매끈한 원과 선으로 그린다(WorldMapRoutes).
화면 세로에 꼭 맞도록 608×432점으로 그려 점 하나 = 2px로 키워 저장한다(가상 304×216, 옆에 설명 칸 80이 남는다).

  Docs/ArtUpgrade/PixelGen/world_map_sketch.png            사용자의 스케치(원본, 고치지 않는다)
  Assets/MiniPandoras/Art/PixelGen/UI/WorldMap/world_map.png   지역 이름이 없는 전체 지도
  .../WorldMap/world_map_blur.png                          아직 가지 않은 곳을 덮는 흐린 지도(게임의 지도 창 바탕)
  .../WorldMap/area_<구역>.png                             다녀온 구역에 얹는 선명한 조각
  .../WorldMap/here.png, world_map.json                    지금 있는 곳 표시, 구역과 맵의 자리(장면을 조립할 때 들여온다)
  Docs/ArtUpgrade/PixelGen/world_map_preview.png           지역 이름을 얹은 미리보기
  Docs/ArtUpgrade/PixelGen/world_map_places.png            점마다 맵 이름을 적은 확인용 그림

사용: python Tools/PixelGen/world_map.py <프로젝트 루트>
"""
import json
import sys
from collections import deque, namedtuple
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

import pixelgen as pg
import world_map_sprites as art
from houses import Canvas
from world_map_sprites import EMBER, INK_LINE

PAGE_LEFT, PAGE_RIGHT = 134, 1653       # 스케치 슬라이드에서 그림이 있는 가로 범위(px)
WIDTH, HEIGHT = 608, 432                # 점
SKETCH_PER_DOT = 2.5
LAND_SHARE = 0.68                       # 한 점에서 이만큼이 뭍이어야 뭍이다. 스케치의 좁은 물길을 살린다.
COAST_GAP = 4                           # 해안선의 이만한 틈(px)은 막힌 것으로 본다
WEST_END = 330                          # 그림 왼쪽 끝에서 이만큼은 강과 국경이 해안을 끊는 곳이다(px)
BLUR_RADIUS = 3.4                       # 가지 않은 곳을 흐리는 정도(점)
FOG = np.array([24.0, 34.0, 48.0])
FOG_SHARE = 0.22
REVEAL_SOLID = 10                       # 다녀온 구역 둘레로 선명하게 드러나는 바다(점)
REVEAL_FADE = 8                         # 그 바깥으로 점 무늬가 되어 사라지는 폭(점)
SPECK = 90                              # 이보다 작은 육지 조각(px)은 선 사이에 낀 틈이다

SEA = pg.WATER[1:6]
STORM_SEA = pg.hexes("#101c33", "#16294a", "#1f3a62", "#2b4f7c")
WHIRL = pg.hexes("#0d1050", "#161c7a", "#2534a8", "#3a55cf", "#6d8fe8", "#b6cdf6")
CLIFF = pg.hexes("#2f2b2c", "#201e23")
BORDER = pg.hexes("#a92f22", "#e8775a")
RIVER = pg.hexes("#3f7f9c", "#6aa7bf")
WIND = pg.hexes("#3fb6cf")[0]
TREE_BARK = pg.hexes("#2a1a12", "#4a2f1e", "#6b4529", "#8c5f38", "#ad7c4c")
CROPS = [pg.hexes(*pair) for pair in (("#c9a94e", "#b3923f"), ("#6f9a45", "#5d8639"), ("#7d5d3c", "#6a4e33"),
                                       ("#9bb85a", "#86a44f"), ("#d6c06a", "#c0aa58"), ("#5d8c4a", "#4f7a40"))]
HEDGE = pg.hexes("#3f5f30")[0]
PLANE_MARK = pg.hexes("#e8e4d0")[0]
PIN = pg.hexes("#3a0a10", "#d8322c", "#ff9a86", "#ffe9c8")
PADDY = pg.hexes("#3f6a50", "#8fc4ac", "#c2e4d0")
ASPHALT = pg.hexes("#303746", "#3c4557", "#4f5b70", "#94a2b6")
CONCRETE = pg.hexes("#474c3e", "#565c4b", "#6b715c", "#d8c04a")
RAIL = pg.hexes("#3a2a20", "#7a6248")
SCALES = pg.hexes("#0f2018", "#163024", "#1f4630", "#2c5f40", "#3f7a52")
BELLY = pg.hexes("#e4d8b8")[0]
CLOUD = pg.hexes("#8f93c4", "#bcc0e4", "#e6e7f7", "#ffffff")
LABEL = pg.hexes("#f6f0dc")[0]

Region = namedtuple("Region", "name label outline ramp shore")
FARMLAND = "힐리아 농경지"   # 지도에는 「힐리아」 한 이름만 적는다.

# 스케치의 빨간 국경을 스케치 좌표로 옮겨 적었다. 육지가 어느 지역에 드는지만 정한다.
NORTH_BORDER = [(134, 268), (232, 266), (300, 232), (400, 200), (470, 190), (540, 170), (630, 145), (720, 152), (740, 150)]
MIDDLE_BORDER = [(134, 510), (232, 505), (250, 470), (300, 440), (335, 423), (365, 403), (430, 400), (470, 410),
                 (510, 435), (555, 500), (590, 522), (620, 560), (680, 610), (770, 665)]
SOUTH_BORDER = [(134, 708), (325, 702), (345, 715), (385, 725), (430, 718), (455, 745), (520, 775), (600, 785), (720, 780), (750, 790)]
ISLES_BORDER = [(750, 790), (800, 850), (835, 900), (860, 1000), (870, 1080)]
EAST_BORDER = [(1135, 500), (1215, 375), (1320, 290), (1440, 165), (1450, 160), (1653, 215)]
WONDER_BORDER = [(1653, 500), (1530, 465), (1300, 525), (1300, 545), (1270, 575), (1190, 610), (1140, 655)]
WONDER_WEST = [(1140, 655), (1170, 730), (1130, 920), (1165, 1080)]

# 세계수의 줄기와 뿌리(스케치 좌표). (꺾은선, 시작 굵기, 끝 굵기). 굵기는 점이다.
TRUNK = [(412, 528), (470, 528), (468, 560), (482, 600), (505, 622), (482, 630), (462, 642), (440, 634), (415, 642),
         (392, 624), (372, 612), (398, 590), (410, 560)]
ROOTS = [
    ([(405, 572), (375, 550), (340, 535)], 6, 2),
    ([(398, 588), (365, 586), (335, 566), (310, 586), (290, 606)], 7, 2),
    ([(400, 598), (370, 612), (350, 622), (344, 648), (338, 668)], 7, 2),
    ([(405, 610), (392, 636), (394, 660), (398, 682)], 6, 2),
    ([(445, 630), (436, 650), (432, 668)], 5, 2),
    ([(465, 610), (490, 632), (510, 652), (526, 682)], 7, 2),
    ([(472, 584), (510, 596), (550, 606), (576, 624)], 7, 2),
    ([(482, 576), (494, 548), (525, 542), (556, 548)], 5, 2),
    ([(505, 622), (550, 630), (572, 652)], 5, 2),
]

REGIONS = [
    Region("칼티마", (262, 170), [(134, 0), (740, 0)] + NORTH_BORDER[::-1],
           pg.hexes("#7f8fa6", "#a3b3c6", "#c6d4e0", "#e3ecf2", "#f7fbfd"), pg.hexes("#8e9cae")[0]),
    Region("아이로스", (240, 320), NORTH_BORDER + MIDDLE_BORDER[::-1],
           pg.GRASS[2:], pg.SAND[3]),
    Region("페어리가든", (262, 570), MIDDLE_BORDER + [(770, 790)] + SOUTH_BORDER[::-1],
           pg.hexes("#22402a", "#2c5230", "#3a6936", "#4b803c", "#619842"), pg.SAND[2]),
    Region("힐리아", (400, 1048), SOUTH_BORDER + ISLES_BORDER + [(134, 1080)],
           pg.hexes("#6b5336", "#8a6d45", "#a88a56", "#c2a56b", "#d8c084"), pg.SAND[4]),
    Region(FARMLAND, None, [(500, 806), (750, 790)] + ISLES_BORDER[1:] + [(500, 1080)],
           pg.hexes("#3f6a34", "#50803c", "#649846", "#7cae50", "#98c060"), pg.SAND[3]),
    Region("시트라", (905, 248), [(740, 0), (1060, 0), (1040, 300), (770, 300)],
           pg.STONE[1:5], pg.STONE[3]),
    Region("타이탄 핸드", (885, 545), [(770, 300), (1040, 300), (1030, 570), (760, 660)],
           pg.hexes("#5e574e", "#7c7468", "#9a9184", "#b8afa0", "#d0c8b8"), pg.hexes("#b8afa0")[0]),
    Region("드라니아", (995, 972), [(760, 660), (1030, 570), (1140, 655)] + WONDER_WEST[1:] + [(870, 1080)] + ISLES_BORDER[-2::-1],
           pg.hexes("#3a2224", "#532d29", "#6e3b2e", "#8b4e36", "#a76642"), pg.hexes("#4a4040")[0]),
    Region("마르", (1150, 150), [(1060, 0), (1653, 0)] + EAST_BORDER[::-1] + [(1060, 580)],
           pg.hexes("#3f5a44", "#557352", "#6e8c60", "#8aa572", "#a9bd88"), pg.SAND[3]),
    Region("팬데모니엄", (1415, 410), EAST_BORDER + WONDER_BORDER + [(1065, 580)],
           pg.hexes("#3c3a40", "#54504f", "#6e6760", "#8a7f71", "#a59884"), pg.hexes("#4a4a50")[0]),
    Region("원더랜드", (1340, 965), WONDER_BORDER + WONDER_WEST[1:] + [(1653, 1080)],
           pg.hexes("#6a4a78", "#8a6494", "#ab84ad", "#caa6c4", "#e4c8d8"), pg.hexes("#f0dce0")[0]),
    # 무릉도원은 마르의 세부 지역이라 지도에 이름을 적지 않는다.
    Region("무릉도원", None, [(1430, 10), (1625, 10), (1625, 130), (1430, 130)],
           pg.hexes("#4f8a58", "#6da864", "#8cc470", "#aede88", "#d4eeaa"), pg.hexes("#f4d6dc")[0]),
]

# 뱀열차가 달리는 길(스케치 좌표): 정류장에서 섬 아랫부분을 돌아 동쪽 종착역으로 달린다(이동 거리가 너무 짧았다).
# 뱀열차 그림의 머리는 동쪽을 본다.
RAILWAY = [(650, 1036), (672, 1044), (700, 1042), (722, 1028), (738, 1004), (744, 982), (756, 960)]
SNAKE_TRAIN = [(668, 1043), (692, 1044), (710, 1036), (722, 1028)]

Zones = namedtuple("Zones", "city base paddies grove")

Area = namedtuple("Area", "id arrival_chapter regions label extra")
Place = namedtuple("Place", "map_id name area x y inside")

# 지도 창에서 다녀오면 선명해지는 구역. arrival_chapter는 처음 들어서는 장이고 0이면 1부에서는 이야기가 닿지 않는다.
# extra는 땅 바깥으로 튀어나온 그림(뱀성 꼭대기, 항구)을 함께 드러낼 스케치 좌표의 네모다.
AREAS = [
    Area("hilliaEast", 1, (FARMLAND,), "힐리아", ((560, 780, 700, 905), (770, 1000, 850, 1070), (490, 1020, 570, 1070))),
    Area("hilliaWest", 8, ("힐리아",), None, ((134, 900, 205, 965),)),
    Area("fairyGarden", 10, ("페어리가든",), "페어리가든", ()),
    Area("airos", 0, ("아이로스",), "아이로스", ()),
]

# 게임에 있는 바깥 맵의 자리(맵 id, 표시 이름, 구역, 스케치 좌표, 그 자리에 드는 건물 안 맵).
# 차원공간, 꿈, 배, 디엘의 기억 속 집은 지도에 없다.
PLACES = [
    # 맵의 출구와 방향을 맞춘다. 섬 전체가 힐리아 동부다.
    # 서쪽 해안을 따라 뱀성 → 힐리아 숲 → 벼랑 다리 → 마을 → 외곽이 내려오고, 외곽 남쪽 길은 남서쪽 끝의 서쪽 부두에 닿는다.
    # 뱀열차는 외곽 동쪽 정류장에서 섬 아랫부분을 돌아 동쪽 종착역까지 길게 달린다.
    Place("hillia", "힐리아 숲", "hilliaEast", 574, 938, ()),
    Place("castle", "뱀성", "hilliaEast", 622, 886, ("castleHall", "castleBarracks", "castleVault", "castleRoof")),
    Place("cliffBridge", "벼랑 다리", "hilliaEast", 568, 965, ()),
    Place("town", "힐리아 마을", "hilliaEast", 606, 984, ("adventurersGuild",)),
    Place("outskirts", "힐리아 외곽", "hilliaEast", 600, 1016, ()),
    Place("port", "서쪽 부두", "hilliaEast", 553, 1046, ()),
    Place("trainStop", "뱀열차 정류장", "hilliaEast", 650, 1036, ()),
    Place("hideoutPath", "아지트 가는 숲길", "hilliaEast", 650, 958, ()),
    Place("hideout", "뉴웨이브 아지트", "hilliaEast", 686, 932, ("hideoutUpper", "hideoutRoom")),
    Place("forest", "아지트 뒤 숲", "hilliaEast", 640, 916, ()),
    # 뱀열차로 동쪽 종착역에 닿아 호빗 소굴을 지나고, 산길로 내려와 동쪽 선착장에서 배를 탄다. 호빗 소굴은 선착장 반대쪽 산에 있다.
    Place("station", "뱀열차 종착역", "hilliaEast", 756, 960, ()),
    Place("hobbitTrail", "호빗 소굴 가는 산길", "hilliaEast", 790, 925, ()),
    Place("hobbitCave", "호빗 소굴", "hilliaEast", 762, 898, ()),
    Place("caveRoad", "선착장 가는 산길", "hilliaEast", 784, 994, ()),
    Place("eastPort", "동쪽 선착장", "hilliaEast", 775, 1030, ()),
    # 힐리아 서부와 동부 사이 바다 한가운데. 서쪽 부두 끝의 문으로만 간다.
    Place("leviathanCoast", "레비아탄의 바다", "hilliaEast", 485, 960, ("leviathanDen",)),
    # 배가 닿는 서쪽 끝의 사막 바닷가 마을.
    Place("westSands", "힐리아 서부", "hilliaWest", 232, 912, ()),
    Place("dragonRoad", "드래곤 고원 가는 길", "hilliaWest", 250, 886, ()),
    Place("dragonPlateau", "드래곤 고원", "hilliaWest", 262, 858, ()),
    # 고원에서 산줄기 남쪽 끝을 돌아 해안을 따라 U자로 간다. 철갑 동굴은 산 바깥 자락이다.
    Place("duneField", "모래바람 들판", "hilliaWest", 305, 985, ()),
    Place("coastRoad", "바닷바람 해안길", "hilliaWest", 385, 1005, ()),
    Place("coastFork", "갈림 해안", "hilliaWest", 432, 925, ()),
    Place("ironCave", "철갑 동굴", "hilliaWest", 385, 880, ("ironDepths",)),
    # 페어리가든: 힐리아 서부에서 올라오면 숲 남쪽 끝의 마을이고, 스케치의 꼭대기와 동굴 점이 고목의 꼭대기와 뿌리다.
    Place("fairyGarden", "페어리가든", "fairyGarden", 395, 688, ("fairyNight",)),
    Place("treeInn", "고목의 쉼터", "fairyGarden", 468, 668, ()),
    # 마을 동쪽의 들판과 숲.
    Place("petalMeadow", "꽃잎 들판", "fairyGarden", 498, 696, ()),
    Place("fireflyBrook", "반딧불 냇가", "fairyGarden", 562, 690, ()),
    Place("glowWood", "달빛 버섯 숲", "fairyGarden", 520, 640, ()),
    Place("fairySpring", "요정의 샘", "fairyGarden", 566, 622, ()),
    Place("giantTree", "거대고목", "fairyGarden", 437, 556, ("treeHall", "treeMarket", "treeRooms", "treeLibrary")),
    Place("treeCrown", "거대고목 꼭대기(나리온 정거장)", "fairyGarden", 442, 489, ()),
    Place("treeRoots", "고목의 뿌리", "fairyGarden", 445, 609, ()),
    Place("hiddenGarden", "판도라의 정원", "fairyGarden", 309, 613, ()),
    # 아이로스
    Place("airosRoad", "아이로스 가는 길", "airos", 564, 441, ()),
    Place("eradTower", "에라드의 탑", "airos", 572, 342, ()),
]

# 걸어서 이어진 곳(맵 포탈과 이야기의 길). 뱀열차 구간은 선로 그림이 대신하고, 배로 건너는 곳은 잇지 않는다.
UNKNOWN_LAND = np.array([104, 108, 114], dtype=float)
UNKNOWN_LAND_SHARE = 0.9
ROADS = [
    ("hillia", "castle"), ("hillia", "cliffBridge"), ("cliffBridge", "town"), ("town", "outskirts"), ("town", "hideoutPath"),
    ("hideoutPath", "outskirts"), ("hideoutPath", "hideout"), ("hideout", "forest"),
    ("outskirts", "trainStop"), ("outskirts", "port"), ("port", "leviathanCoast"),
    ("station", "hobbitTrail"), ("hobbitTrail", "hobbitCave"),
    ("hobbitTrail", "caveRoad"), ("caveRoad", "eastPort"),
    ("westSands", "dragonRoad"), ("dragonRoad", "dragonPlateau"), ("westSands", "fairyGarden"),
    ("dragonPlateau", "duneField"), ("duneField", "coastRoad"), ("coastRoad", "coastFork"), ("coastFork", "ironCave"),
    ("coastFork", "airosRoad"),
    ("fairyGarden", "petalMeadow"), ("petalMeadow", "fireflyBrook"), ("petalMeadow", "glowWood"), ("glowWood", "fairySpring"),
    ("fairyGarden", "giantTree"), ("giantTree", "treeInn"), ("giantTree", "treeCrown"), ("giantTree", "treeRoots"),
    ("giantTree", "hiddenGarden"), ("airosRoad", "eradTower"),
]

# 곧게 그으면 다른 점을 지나 이어진 길처럼 보이는 길은 이 자리(스케치 좌표)를 거쳐 꺾는다.
ROAD_BENDS = {("coastFork", "airosRoad"): [(612, 700)]}

# 지도 창에서 누르면 크게 보는 곳(스케치 좌표)과, 그 안의 세부 지역 이름.
FOCUS_TITLE = "힐리아"
FOCUS_BOX = (134, 700, 872, 1080)
FOCUS_AREAS = ("hilliaEast", "hilliaWest")
# 점이 없는 세부 지역의 이름. 섬 전체가 힐리아 동부다.
REGION_NAMES = [("hilliaEast", "힐리아 동부", 730, 802)]

# 맵이 아직 없는 곳에 스케치가 찍어 둔 점.
FUTURE_TOWNS = [
    (304, 71), (449, 110), (600, 101), (373, 150), (253, 199),                                      # 칼티마
    (593, 212), (477, 250), (699, 291), (311, 329), (448, 355), (242, 447), (712, 554),             # 아이로스
    (355, 998),                                                                                     # 힐리아의 푸른 육각 표식
    (835, 81), (862, 147), (928, 114), (809, 187), (915, 200), (939, 229),                          # 시트라
    (866, 390), (897, 384), (845, 430), (947, 419), (908, 466), (835, 485),                         # 타이탄 핸드
    (879, 718), (1036, 696), (963, 770), (880, 820), (1055, 780), (975, 862),                       # 드라니아
    (1526, 65), (1224, 235), (1301, 203), (1197, 333), (1175, 402),                                 # 마르, 무릉도원
    (1446, 256), (1394, 362), (1503, 336), (1290, 371), (1245, 469),                                # 팬데모니엄
    (1467, 613), (1324, 675), (1319, 780), (1416, 771), (1343, 850), (1535, 785),                   # 원더랜드
]


def dot(x, y):
    """스케치 좌표를 점 좌표로."""
    return (x - PAGE_LEFT) / SKETCH_PER_DOT, y / SKETCH_PER_DOT


def dots(length):
    return length / SKETCH_PER_DOT


# ── 마스크 도구 ────────────────────────────────────────

def grow(mask, steps=1):
    for _ in range(steps):
        mask = mask | pg.neighbours(mask)
    return mask


def shrink(mask, steps=1):
    return ~grow(~mask, steps)


def close(mask, steps=1):
    """가까운 선끼리 붙여 면으로 만든다."""
    return shrink(grow(mask, steps), steps)


def shifted(mask, dx, dy):
    """mask를 옮긴다. 그림 밖으로 나간 것은 사라진다."""
    height, width = mask.shape
    out = np.zeros(mask.shape, dtype=bool)
    out[max(0, dy):min(height, height + dy), max(0, dx):min(width, width + dx)] =         mask[max(0, -dy):min(height, height - dy), max(0, -dx):min(width, width - dx)]
    return out


def parts(mask):
    """네 이웃으로 이어진 덩어리에 번호를 매긴다(0은 mask 밖)."""
    height, width = mask.shape
    cells = mask.ravel().tolist()
    labels = [0] * (height * width)
    count = 0
    for start in range(height * width):
        if not cells[start] or labels[start]:
            continue
        count += 1
        labels[start] = count
        queue = deque([start])
        while queue:
            at = queue.popleft()
            row, column = divmod(at, width)
            for next_cell, inside in ((at - width, row > 0), (at + width, row < height - 1),
                                      (at - 1, column > 0), (at + 1, column < width - 1)):
                if inside and cells[next_cell] and not labels[next_cell]:
                    labels[next_cell] = count
                    queue.append(next_cell)
    return np.array(labels, dtype=np.int32).reshape(height, width)


def reach_from_edges(walls):
    """벽을 넘지 않고 그림 가장자리에서 닿는 곳."""
    labels = parts(~walls)
    edge = set(labels[0, :]) | set(labels[-1, :]) | set(labels[:, 0]) | set(labels[:, -1])
    edge.discard(0)
    return np.isin(labels, list(edge))


def steps_from(mask, limit):
    """각 점이 mask에서 몇 걸음 떨어졌는지(limit에서 멈춘다)."""
    away = np.full(mask.shape, limit, dtype=np.int32)
    ring = mask.copy()
    for step in range(limit):
        away[ring & (away == limit)] = step
        ring = grow(ring)
    return away


def boxed(mask, x0, y0, x1, y1):
    """스케치 좌표의 네모 안만 남긴다."""
    out = np.zeros(mask.shape, dtype=bool)
    out[y0:y1, x0 - PAGE_LEFT:x1 - PAGE_LEFT] = True
    return mask & out


def to_dots(mask, threshold=0.5):
    """스케치 해상도의 마스크를 점 격자로 줄인다. 가는 선은 threshold를 낮춰 살린다."""
    small = Image.fromarray(mask.astype(np.uint8) * 255).resize((WIDTH, HEIGHT), Image.BOX)
    return np.asarray(small) / 255.0 >= threshold


# ── 스케치 읽기 ────────────────────────────────────────

Ink = namedtuple("Ink", "line under_label blue pale_blue red brown cyan")


def read_ink(path):
    page = np.asarray(Image.open(path).convert("RGB")).astype(int)[:, PAGE_LEFT:PAGE_RIGHT]
    r, g, b = page[..., 0], page[..., 1], page[..., 2]
    top = page.max(axis=2)
    return Ink(
        line=(top < 130) & (top - page.min(axis=2) < 70),
        # 지역 이름 글씨가 해안선 위를 덮은 곳은 선이 짙은 초록으로 비친다.
        under_label=(g > r + 20) & (g > b + 20) & (g < 200),
        blue=(b > r + 40) & (b > g + 25),
        pale_blue=(b > r + 22) & (b > g + 8) & (b > 120),
        red=(r > 190) & (g < 170) & (b < 150) & (top - page.min(axis=2) > 50),
        brown=(r > g + 28) & (g > b + 14) & (r < 225),
        cyan=(b > 150) & (g > 140) & (r < 150) & (g > r + 30),
    )


def trace_land(ink):
    """검은 선으로 둘러싸인 안쪽이 육지다. 바다는 그림 가장자리에서 흘러들어 온다."""
    walls = ink.line | ink.under_label
    walls[:, :WEST_END] |= ink.blue[:, :WEST_END] | ink.red[:, :WEST_END]
    walls[-5:, 480:530] = True      # 맨 아래 섬은 스케치 끝에서 잘렸다.
    sea = grow(reach_from_edges(grow(walls, COAST_GAP)), COAST_GAP)
    land = grow(shrink(~sea))
    labels = parts(land)
    sizes = np.bincount(labels.ravel())
    keep = sizes >= SPECK
    keep[0] = False
    return keep[labels]


def region_map():
    canvas = Image.new("L", (WIDTH, HEIGHT), 0)
    draw = ImageDraw.Draw(canvas)
    for index, region in enumerate(REGIONS):
        draw.polygon([dot(*point) for point in region.outline], fill=index)
    return np.asarray(canvas)


# ── 바다와 땅 ──────────────────────────────────────────

def paint_sea(world, land, rng):
    offshore = steps_from(land, 16)
    swell = pg.fractal_noise(HEIGHT, WIDTH, rng, layers=((60, 0.6), (20, 0.4)))
    light = 0.18 + 0.62 * np.exp(-offshore / 4.5) + (swell - 0.5) * 0.3
    world[:] = pg.quantize(np.clip(light, 0, 0.999), SEA, dither=0.12)
    foam = (offshore == 2) & (pg.noise(HEIGHT, WIDTH, 5, rng) > 0.42)
    world[foam] = pg.WATER[6]
    deep = (offshore >= 12) & (np.arange(HEIGHT)[:, None] < HEIGHT - 2)
    for x, y in scatter(deep, 15, rng):
        world[y, x:x + 4] = pg.WATER[4]
        world[y + 1, x + 4:x + 7] = pg.WATER[4]


def paint_storm(world, land, ink, rng):
    """시트라: 섬 고리를 감싸는 폭풍. 어두운 바다 위에 스케치의 소용돌이 선을 그대로 얹는다."""
    cx, cy = dot(878, 145)
    radius = dots(148)
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    away = np.hypot(xx + 0.5 - cx, yy + 0.5 - cy) / radius
    swell = pg.noise(HEIGHT, WIDTH, 9, rng)
    inside = (away < 1.0 + (swell - 0.5) * 0.18) & ~land
    light = 0.2 + away * 0.62 + (swell - 0.5) * 0.25
    world[inside] = pg.quantize(np.clip(light, 0, 0.999), STORM_SEA, dither=0.14)[inside]
    arcs = to_dots(boxed(ink.pale_blue, 735, 5, 1020, 285), 0.14) & ~land
    world[arcs] = pg.WATER[5]


def paint_whirlpool(world, land):
    cx, cy = dot(1322, 675)
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    u, v = (xx + 0.5 - cx) / dots(80), (yy + 0.5 - cy) / dots(72)
    away = np.hypot(u, v)
    angle = np.arctan2(v, u)
    swirl = np.sin(angle * 2 + np.log(away + 0.03) * 7)
    water = (away < 1.04) & ~land
    light = 0.08 + away * 0.62 + swirl * 0.2
    world[water] = pg.quantize(np.clip(light, 0, 0.999), WHIRL, dither=0.06)[water]
    return water


def paint_land(world, land, regions, rng):
    inland = steps_from(~land, 24)
    lumps = pg.fractal_noise(HEIGHT, WIDTH, rng, layers=((42, 0.5), (15, 0.32), (6, 0.18)))
    height = 1 - np.exp(-inland / 7.0) + lumps * 2.4
    dy, dx = np.gradient(height)
    light = 0.46 + (dx * 0.6 + dy * 0.8) * 3.0 + (lumps - 0.5) * 0.7
    light = np.clip(light, 0, 0.999)
    for index, region in enumerate(REGIONS):
        area = land & (regions == index)
        world[area] = pg.quantize(light, region.ramp, dither=0.13)[area]
        world[area & (inland <= 1)] = region.shore


def blob(x, y, width, height, rng):
    """스케치 좌표의 가운데와 반지름으로 정한 구불구불한 덩어리."""
    cx, cy = dot(x, y)
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    away = np.hypot((xx - cx) / dots(width), (yy - cy) / dots(height))
    return away + (pg.noise(HEIGHT, WIDTH, 9, rng) - 0.5) * 0.4 < 1


def district_zones(land, regions, rng):
    inland = steps_from(~land, 6)
    settled = land & (inland >= 2)
    farmland = regions == next(i for i, region in enumerate(REGIONS) if region.name == FARMLAND)
    mar = regions == next(i for i, region in enumerate(REGIONS) if region.name == "마르")
    base = blob(676, 298, 62, 66, rng) & settled
    city = blob(586, 325, 104, 98, rng) & settled & ~base
    paddies = (blob(1212, 440, 92, 56, rng) | blob(1362, 196, 56, 36, rng) | blob(1120, 300, 40, 60, rng)) & settled & mar
    grove = blob(672, 948, 44, 26, rng) & settled & farmland
    return Zones(city, base, paddies, grove)


def paint_farmland(world, land, regions, zones):
    """힐리아 오른쪽 땅: 조금 비스듬한 밭 조각들. 밭마다 작물이 다르고 사이에 산울타리가 있다."""
    inland = steps_from(~land, 4)
    farmland = regions == next(i for i, region in enumerate(REGIONS) if region.name == FARMLAND)
    area = farmland & land & (inland >= 2) & ~zones.grove
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    u, v = xx + yy * 0.28, yy - xx * 0.16
    column, row = np.floor(u / 13).astype(int), np.floor(v / 8).astype(int)
    crop = (column * 7 + row * 13 + (column * row) % 5) % len(CROPS)
    for index, colors in enumerate(CROPS):
        field = area & (crop == index)
        furrows = (v.astype(int) % 2 == 0) if index % 2 else (u.astype(int) % 2 == 0)
        world[field] = colors[0]
        world[field & furrows] = colors[1]
    world[area & ((u % 13 < 1) | (v % 8 < 1))] = HEDGE


def paint_paddies(world, zones):
    """마르의 계단식 논: 물이 찬 논배미가 물결처럼 겹친다."""
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    terrace = ((yy + 2.4 * np.sin(xx / 7.0)) % 5).astype(int)
    world[zones.paddies] = PADDY[1]
    world[zones.paddies & (terrace == 2)] = PADDY[2]
    world[zones.paddies & (terrace == 0)] = PADDY[0]


def paint_city(world, zones, rng):
    """아이로스의 도시 바닥: 격자 도로와 옥상들."""
    city = zones.city
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    world[city] = ASPHALT[1]
    world[city & (pg.noise(HEIGHT, WIDTH, 4, rng) > 0.58)] = ASPHALT[0]
    world[city & ((xx % 10 == 0) | (yy % 8 == 0))] = ASPHALT[2]
    world[city & ~shrink(city)] = ASPHALT[3]


def paint_military_base(world, zones, rng):
    """군사 지역 바닥: 콘크리트, 경고색 철책, 활주로, 헬기장."""
    base = zones.base
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    world[base] = CONCRETE[1]
    world[base & (pg.noise(HEIGHT, WIDTH, 5, rng) > 0.6)] = CONCRETE[0]
    fence = base & ~shrink(base)
    world[fence] = CONCRETE[3]
    world[fence & ((xx + yy) % 4 < 2)] = ASPHALT[0]
    _, top = dot(0, 322)
    strip = base & ~fence & (yy >= top) & (yy < top + 6)
    world[strip] = ASPHALT[0]
    world[strip & (yy == int(top) + 2) & (xx % 6 < 3)] = PLANE_MARK
    for x, y in ((702, 270), (652, 262)):
        cx, cy = dot(x, y)
        away = np.hypot(xx - cx, yy - cy)
        world[base & (away < 5)] = ASPHALT[1]
        world[base & (away >= 4) & (away < 5)] = PLANE_MARK
        left, top_row = int(cx) - 1, int(cy) - 2
        for row in range(5):
            world[top_row + row, left] = PLANE_MARK
            world[top_row + row, left + 3] = PLANE_MARK
        world[top_row + 2, left:left + 4] = PLANE_MARK


def paint_railway(world):
    """뱀열차의 길: 굽은 선로와 침목."""
    path = [np.array(dot(*point)) for point in RAILWAY]
    for a, b in zip(path, path[1:]):
        steps = int(np.linalg.norm(b - a)) + 1
        for k in range(steps):
            x, y = (a + (b - a) * k / steps).astype(int)
            world[y - 1:y + 2, x - 2:x + 3] = RAIL[0] if k % 2 else RAIL[1]


def paint_coast(world, land):
    """남쪽 해안에 절벽을, 뭍 둘레에 스케치처럼 진한 선을 두른다."""
    near = shifted(land, 0, 1) & ~land
    far = (shifted(land, 0, 2) | shifted(land, 0, 3)) & ~land & ~near
    world[near] = CLIFF[0]
    world[far] = CLIFF[1]
    solid = land | near | far
    world[grow(solid) & ~solid] = INK_LINE


def paint_rivers(world, land, ink):
    rivers = boxed(ink.blue, 134, 330, 800, 1080) & ~boxed(ink.blue, 330, 975, 380, 1022)
    thin = to_dots(rivers, 0.16) & land
    world[thin] = RIVER[1]
    world[shifted(thin, 0, 1) & land & ~thin] = RIVER[0]


def border_lines(ink):
    """스케치의 빨간 선. 산과 뿌리를 그린 갈색 선이 섞이는 곳은 뺀다."""
    lines = ink.red.copy()
    for x0, y0, x1, y1 in ((285, 525, 592, 700), (150, 712, 365, 955), (820, 660, 1135, 905)):
        lines[y0:y1, x0 - PAGE_LEFT:x1 - PAGE_LEFT] = False
    return to_dots(lines, 0.12)


def paint_borders(world, land, ink):
    border = border_lines(ink)
    world[border & land] = BORDER[0]
    world[border & ~land] = BORDER[1]


def paint_wind(world, ink):
    world[to_dots(boxed(ink.cyan, 380, 30, 460, 105), 0.14)] = WIND


# ── 배치 ───────────────────────────────────────────────

def scatter(area, spacing, rng):
    """area 안에서 서로 spacing 점쯤 떨어진 자리들."""
    taken = np.zeros(area.shape, dtype=bool)
    spots = []
    ys, xs = np.nonzero(area)
    for i in rng.permutation(len(xs)):
        x, y = int(xs[i]), int(ys[i])
        if taken[y, x]:
            continue
        spots.append((x, y))
        taken[max(0, y - spacing):y + spacing + 1, max(0, x - spacing):x + spacing + 1] = True
    return spots


def stamp_all(world, placed):
    """앞(아래)에 있는 것이 뒤를 가리도록 발 위치 순서로 찍는다."""
    for _, x, image, pivot, y in sorted(placed, key=lambda item: item[0]):
        pg.stamp(world, image, x, y, pivot)


def place(placed, image, x, y):
    """발이 (x, y)에 오도록 그림을 줄 세운다."""
    height, width = image.shape[:2]
    placed.append((y, x, image, (width // 2, height - 1), y))


def keep_out_mask():
    """나무를 심지 않을 큰 그림 자리(스케치 좌표)."""
    canvas = Image.new("1", (WIDTH, HEIGHT), 0)
    draw = ImageDraw.Draw(canvas)
    for x0, y0, x1, y1 in ((362, 445, 512, 558), (1335, 262, 1515, 366), (1470, 780, 1562, 866), (1395, 20, 1625, 130)):
        draw.rectangle([dot(x0, y0), dot(x1, y1)], fill=1)
    for x, y in FUTURE_TOWNS + [(place.x, place.y) for place in PLACES]:
        cx, cy = dot(x, y)
        draw.ellipse([cx - 5, cy - 5, cx + 5, cy + 7], fill=1)
    tall = grow(world_tree_shapes(), 3) | tapered(Canvas(WIDTH, HEIGHT), RAILWAY, 9, 9)
    return np.asarray(canvas) | tall | shifted(tall, 0, 4) | shifted(tall, 0, 8)


def peaks_from_ink(placed, area, spacing, sizes, ramp, rng):
    """스케치의 갈색 산줄기 위에 산을 줄지어 세운다."""
    for x, y in scatter(area, spacing, rng):
        width = int(rng.integers(*sizes[0]))
        height = int(rng.integers(*sizes[1]))
        place(placed, art.mountain(width, height, ramp, rng), x, y + height // 3)


def plant(placed, area, spacing, images, rng):
    for x, y in scatter(area, spacing, rng):
        place(placed, images[int(rng.integers(len(images)))], x, y)


def place_at(placed, image, x, y):
    """스케치 좌표에 그림을 세운다."""
    place(placed, image, *dot(x, y))


def place_mountains(placed, land, ink, rng):
    inland = steps_from(~land, 12)

    # 칼티마의 눈 덮인 세 봉우리(스케치의 회색 산).
    snow_ramp = pg.hexes("#3f4655", "#5b6476", "#8390a3", "#a9b6c6")
    for (x, y), (width, height) in zip(((270, 122), (332, 98), (380, 92)), ((30, 22), (22, 20), (26, 25))):
        place_at(placed, art.mountain(width, height, snow_ramp, rng, snow=True), x, y)

    desert_ramp = pg.hexes("#3d2c23", "#5d4330", "#7b5b3f", "#9c7a52", "#bb9a6c")
    desert = close(to_dots(boxed(ink.brown, 150, 700, 365, 955), 0.1), 2) & land & (inland >= 3)
    desert &= ~keep_clear_of(((232, 912), (186, 945)), 16)
    peaks_from_ink(placed, desert, 7, ((12, 19), (10, 16)), desert_ramp, rng)

    dragon_ramp = pg.hexes("#20141a", "#3a2022", "#5b3029", "#7d4531", "#a0603c")
    dragon = close(to_dots(boxed(ink.brown, 820, 660, 1135, 905), 0.1), 2) & land & (inland >= 2)
    dragon &= ~keep_clear_of(((879, 718), (1036, 696), (963, 770)), 12)
    peaks_from_ink(placed, dragon, 6, ((9, 14), (8, 12)), dragon_ramp, rng)

    # 호빗 소굴과 선착장 가는 산길은 농경지 끝의 낮은 산속이다.
    hill_ramp = pg.hexes("#2f4a2a", "#45663a", "#5f8448", "#7ea25a", "#a0bc70")
    for x, y, width, height in ((652, 862, 15, 12), (676, 856, 13, 11), (640, 852, 12, 10), (664, 842, 11, 9),
                                (770, 926, 13, 11), (752, 915, 12, 10), (738, 905, 11, 9), (796, 944, 10, 8), (774, 972, 13, 11),
                                (766, 1000, 12, 10)):
        place_at(placed, art.mountain(width, height, hill_ramp, rng), x, y)


def keep_clear_of(points, radius):
    """스케치 좌표의 점들 둘레 radius 점."""
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    near = np.zeros((HEIGHT, WIDTH), dtype=bool)
    for x, y in points:
        cx, cy = dot(x, y)
        near |= np.hypot(xx - cx, yy - cy) < radius
    return near


def place_airos(placed, zones, rng):
    """최첨단 도시: 스케치의 건물 세 자리는 에라드의 탑, 쌍둥이 건물, 복합 건물이고 둘레에 작은 고층 건물이 늘어선다."""
    place_at(placed, art.erad_tower(rng), 572, 345)
    place_at(placed, art.twin_towers(rng), 618, 335)
    place_at(placed, art.civic_block(rng), 578, 398)
    towers = zones.city & ~keep_clear_of(((572, 330), (618, 320), (578, 385)), 13)
    for x, y in scatter(shrink(towers, 3), 8, rng):
        place(placed, art.glass_tower(int(rng.integers(5, 8)), int(rng.integers(9, 20)), rng), x, y)

    # 군사 지역
    for image, x, y in ((art.hangar(), 662, 283), (art.hangar(), 700, 303), (art.radar(), 640, 262),
                        (art.radar(), 712, 335), (art.barracks(), 640, 300), (art.barracks(), 672, 300),
                        (art.barracks(), 640, 316), (art.tank(), 655, 345), (art.tank(), 676, 345), (art.tank(), 697, 345),
                        (art.watchtower(), 625, 250), (art.watchtower(), 680, 245), (art.watchtower(), 728, 325)):
        place_at(placed, image, x, y)


def place_hillia_farms(placed, zones, free, farmland, rng):
    place_at(placed, art.snake_castle(), 622, 906)
    for x, y in ((590, 972), (622, 975), (586, 996), (616, 996), (704, 980), (700, 1010), (670, 1000)):
        place_at(placed, art.farmhouse(), x, y)
    for x, y in ((576, 1030), (622, 1060), (690, 1072)):
        place_at(placed, art.windmill(), x, y)
    plant(placed, free & farmland & ~zones.grove, 19, [art.round_tree(pg.LEAF)], rng)
    forest = pg.hexes("#1b2e20", "#2b4a2c", "#3b6432", "#507f3a", "#74a347")
    plant(placed, free & zones.grove, 4, [art.round_tree(forest), art.round_tree(pg.LEAF)], rng)

    harbor = art.harbor()
    place_at(placed, harbor, 186, 945)
    place_at(placed, harbor[:, ::-1], 810, 1041)
    place_at(placed, art.old_pier(), 538, 1054)


def place_mar(placed, free, rng):
    """마르: 궁궐 정문과 목탑, 기와집 마을, 대숲과 소나무와 매화."""
    place_at(placed, art.palace_gate(), 1300, 226)
    place_at(placed, art.pagoda(), 1190, 290)
    house = art.tiled_house()
    for x, y in ((1240, 250), (1262, 268), (1345, 250), (1368, 232), (1320, 262), (1168, 352), (1212, 350), (1190, 368),
                 (1232, 372), (1150, 416), (1195, 420), (1262, 335), (1290, 322)):
        place_at(placed, house, x, y)
    plum = pg.hexes("#5a2c3c", "#b0506a", "#e088a0", "#f4b8c4", "#ffe4e8")
    plant(placed, free, 12, [art.bamboo(), art.umbrella_pine(), art.round_tree(plum)], rng)


def place_paradise(placed, free, rng):
    """무릉도원: 복사꽃 숲 사이로 산수화 같은 바위산과 정자, 둘레에 안개구름."""
    peach = pg.hexes("#7a3a58", "#d0689a", "#f09cc0", "#ffc8dc", "#fff0f4")
    plant(placed, free & ~keep_clear_of(((1530, 92),), 8), 5, [art.round_tree(peach)], rng)
    for x, y, width, height in ((1484, 86, 15, 27), (1578, 82, 17, 31)):
        place_at(placed, art.karst(width, height, rng), x, y)
    place_at(placed, art.pavilion(), 1530, 100)


def place_paradise_mist(world):
    """안개는 섬 앞에 깔린다."""
    placed = []
    for x, y, width in ((1452, 108, 22), (1612, 52, 18), (1590, 122, 20)):
        place_at(placed, art.cloud(width), x, y)
    stamp_all(world, placed)


def place_scenery(world, land, regions, zones, ink, rng):
    placed = []
    inland = steps_from(~land, 12)
    busy = keep_out_mask() | zones.city | zones.base
    free = land & (inland >= 4) & ~busy & ~grow(border_lines(ink), 3)

    def region_of(name):
        return regions == next(i for i, region in enumerate(REGIONS) if region.name == name)

    place_mountains(placed, land, ink, rng)
    place_airos(placed, zones, rng)
    place_hillia_farms(placed, zones, free & (inland >= 5), region_of(FARMLAND), rng)
    place_mar(placed, free & region_of("마르") & ~zones.paddies, rng)
    place_paradise(placed, land & (inland >= 2) & region_of("무릉도원"), rng)

    forest = pg.hexes("#1b2e20", "#2b4a2c", "#3b6432", "#507f3a", "#74a347")
    plant(placed, free & region_of("페어리가든"), 4, [art.round_tree(forest), art.round_tree(pg.LEAF)], rng)
    plant(placed, free & region_of("아이로스"), 16, [art.round_tree(pg.LEAF)], rng)
    plant(placed, free & region_of("칼티마"), 18, [art.snowy_pine()], rng)
    plant(placed, free & region_of("힐리아"), 22, [art.shrub(pg.hexes("#3a3a22", "#55582c", "#6f7238", "#8c8c46"))], rng)
    bloom = pg.hexes("#5a3050", "#a0507c", "#d078a4", "#f0a8c8", "#ffd8e8")
    plant(placed, free & region_of("원더랜드"), 15, [art.round_tree(bloom)], rng)

    for image, x, y in ((art.smoke_plume(), 1475, 305), (art.chimney(), 1428, 358), (art.gear(5), 1357, 345),
                        (art.gear(6), 1395, 348), (art.sawtooth_hall(), 1458, 360), (art.temple(), 1516, 858)):
        place_at(placed, image, x, y)
    stamp_all(world, placed)
    place_paradise_mist(world)


# ── 큰 그림 ────────────────────────────────────────────

def bevel(world, mask, ramp):
    """왼쪽 위 가장자리는 밝게, 오른쪽 아래는 어둡게. 둘레에 테두리."""
    world[grow(mask) & ~mask] = INK_LINE
    world[mask] = ramp[2]
    world[mask & ~shifted(mask, 1, 1)] = ramp[-1]
    world[mask & ~shifted(mask, -1, -1)] = ramp[1]


def tapered(canvas, points, start_width, end_width):
    """스케치 좌표의 꺾은선을 따라 끝으로 갈수록 가늘어지는 면."""
    path = [np.array(dot(*point)) for point in points]
    lengths = [np.linalg.norm(b - a) for a, b in zip(path, path[1:])]
    total = sum(lengths)
    shape = np.zeros(canvas.xx.shape, dtype=bool)
    walked = 0.0
    for a, b, length in zip(path, path[1:], lengths):
        for t in np.linspace(0, 1, int(length) + 2):
            x, y = a + (b - a) * t
            radius = (start_width + (end_width - start_width) * (walked + length * t) / total) / 2
            shape |= canvas.ellipse(x, y, radius, radius)
        walked += length
    return shape


def world_tree_shapes():
    """줄기와 뿌리가 차지하는 점들. 나무를 심지 않을 자리를 고를 때도 쓴다."""
    canvas = Canvas(WIDTH, HEIGHT)
    body = Image.new("1", (WIDTH, HEIGHT), 0)
    ImageDraw.Draw(body).polygon([dot(*point) for point in TRUNK], fill=1)
    wood = np.asarray(body)
    for points, start_width, end_width in ROOTS:
        wood = wood | tapered(canvas, points, start_width, end_width)
    return wood


def paint_world_tree(world, ink, rng):
    """페어리가든의 세계수: 뿌리는 스케치의 갈색 선을 따라 옮겨 적었고, 잎은 스케치의 초록 윤곽 그대로다. 꼭대기에 둥지, 줄기 밑에 동굴."""
    wood = world_tree_shapes()
    bevel(world, wood, TREE_BARK)
    yy, xx = np.mgrid[0:HEIGHT, 0:WIDTH]
    grain = ((xx + yy // 2) % 5 == 0) & (pg.noise(HEIGHT, WIDTH, 4, rng) > 0.42) & shrink(wood, 2)
    world[grain] = TREE_BARK[1]

    loop = to_dots(boxed(ink.under_label, 362, 448, 512, 536), 0.1)
    canopy = ~reach_from_edges(grow(loop))
    cx, cy = dot(437, 492)
    dome = 0.92 - np.hypot((xx - cx + 10) / 62.0, (yy - cy + 7) / 30.0)
    clumps = pg.noise(HEIGHT, WIDTH, 4, rng)
    world[grow(canopy) & ~canopy] = INK_LINE
    world[canopy] = pg.quantize(np.clip(dome + (clumps - 0.5) * 0.55, 0, 0.999), pg.LEAF, dither=0.1)[canopy]

    nest = np.hypot((xx + 0.5 - cx) / 12.5, (yy + 0.5 - cy) / 5.2) <= 1
    hollow = np.hypot((xx + 0.5 - cx) / 8.5, (yy + 0.5 - cy) / 2.8) <= 1
    world[nest] = TREE_BARK[3]
    world[hollow] = TREE_BARK[1]

    canvas = Canvas(WIDTH, HEIGHT)
    cave = canvas.polygon([dot(445, 586), dot(466, 624), dot(424, 624)])
    world[grow(cave) & ~cave] = TREE_BARK[0]
    world[cave] = pg.hexes("#12121e")[0]


def paint_cloud_land(world, shape, land, rng):
    """스케치의 긴 땅 모양 그대로 떠 있는 구름. 뭉게구름 덩이를 뒤에서 앞으로 겹쳐 쌓고 바다에 그림자를 드리운다."""
    canvas = Canvas(WIDTH, HEIGHT)
    shadow = shifted(grow(shape, 3), 5, 7) & ~land
    world[shadow] = (world[shadow] * 0.62).astype(np.uint8)
    for x, y in sorted(scatter(shape, 4, rng), key=lambda spot: spot[1]):
        radius = rng.uniform(4.5, 7.5)
        world[canvas.ellipse(x, y, radius + 1, radius * 0.86 + 1)] = CLOUD[0]
        world[canvas.ellipse(x, y, radius, radius * 0.86)] = CLOUD[1]
        world[canvas.ellipse(x - 1, y - 1.2, radius - 1.2, (radius - 1.2) * 0.8)] = CLOUD[2]
        world[canvas.ellipse(x - 1.8, y - 2.2, radius - 3.4, (radius - 3.4) * 0.7)] = CLOUD[3]


def paint_snake_train(world):
    """뱀열차: 게임의 뱀열차처럼 등은 짙은 초록 비늘에 돌기가 서고, 배는 크림색이며, 옆구리에 불 켜진 창이 있다."""
    canvas = Canvas(WIDTH, HEIGHT)
    head_x, head_y = dot(*SNAKE_TRAIN[-1])
    train = tapered(canvas, SNAKE_TRAIN[::-1], 9, 3) | canvas.ellipse(head_x + 2, head_y, 7, 4.6)
    bevel(world, train, SCALES)
    world[shrink(train) & ((canvas.xx * 2 + canvas.yy * 3) % 5 == 0)] = SCALES[3]
    world[train & ~shifted(train, 0, -2)] = BELLY
    spikes = shifted(train, 0, -2) & ~grow(train, 1) & (canvas.xx % 4 == 0)
    world[spikes] = INK_LINE
    path = [np.array(dot(*point)) for point in SNAKE_TRAIN]
    walked = 0
    for a, b in zip(path, path[1:]):
        steps = int(np.linalg.norm(b - a)) + 1
        for k in range(steps):
            walked += 1
            if walked % 6 == 0:
                x, y = (a + (b - a) * k / steps).astype(int)
                world[y - 3:y, x:x + 2] = EMBER[3]
                world[y - 3, x:x + 2] = EMBER[4]
    eye_x, eye_y = int(head_x) + 5, int(head_y) - 3
    world[eye_y:eye_y + 2, eye_x:eye_x + 2] = EMBER[3]
    world[eye_y + 1, eye_x] = INK_LINE
    world[int(head_y) + 1, int(head_x) + 9:int(head_x) + 13] = pg.hexes("#e0506a")[0]


def paint_craters(world, land):
    """드라니아: 북쪽 두 분화구와 가운데 섬의 용암 구멍."""
    canvas = Canvas(WIDTH, HEIGHT)
    for sketch_x, sketch_y in ((879, 718), (1036, 696)):
        cx, cy = dot(sketch_x, sketch_y)
        for k in range(12):
            angle = k * np.pi / 6
            canvas.line(cx + np.cos(angle) * 8, cy + np.sin(angle) * 7,
                        cx + np.cos(angle) * 13, cy + np.sin(angle) * 11, (0, 0, 0))
        world[canvas.image[..., 3] > 0] = pg.DIRT[1]
        rim = canvas.ellipse(cx, cy, 9, 7.5)
        world[rim] = pg.DIRT[3]
        world[canvas.ellipse(cx, cy + 0.5, 6.5, 5)] = EMBER[1]
        world[canvas.ellipse(cx, cy + 0.5, 4.5, 3.2)] = EMBER[2]
        world[canvas.ellipse(cx - 1, cy, 2.5, 1.6)] = EMBER[3]
    lava_outline = [dot(964, 728), dot(988, 765), dot(966, 815), dot(942, 765)]
    world[grow(canvas.polygon(lava_outline), 1)] = pg.DIRT[1]
    world[canvas.polygon(lava_outline)] = EMBER[2]
    inner = [dot(964, 745), dot(978, 768), dot(965, 798), dot(951, 768)]
    world[canvas.polygon(inner)] = EMBER[3]
    world[canvas.ellipse(*dot(964, 770), 3, 5)] = EMBER[4]


def paint_hexagon(world, ink):
    """힐리아 끝의 푸른 육각 표식."""
    ring = to_dots(boxed(ink.blue, 330, 975, 380, 1022), 0.14)
    world[grow(ring) & ~ring] = INK_LINE
    world[ring] = pg.hexes("#3a56e0")[0]


def paint_labels(world, font_path):
    """지역 이름을 쓰고, 이름마다 글자가 차지한 자리를 돌려준다."""
    font = ImageFont.truetype(str(font_path), 17)
    spots = {}
    for region in REGIONS:
        if region.label is None:
            continue
        layer = Image.new("L", (WIDTH, HEIGHT), 0)
        draw = ImageDraw.Draw(layer)
        draw.text(dot(*region.label), region.name, font=font, fill=255, anchor="mm")
        glyph = np.asarray(layer) >= 120
        spots[region.name] = grow(shifted(glyph, 0, 1)) | grow(glyph)
        world[spots[region.name]] = INK_LINE
        world[glyph] = LABEL
    return spots


def blurred_world(world, places):
    """아직 가지 않은 곳: 흐리게 뭉개고 살짝 어둡게 해서 다녀온 구역이 또렷이 떠오르게 한다."""
    soft = np.asarray(Image.fromarray(world).filter(ImageFilter.GaussianBlur(BLUR_RADIUS))).astype(float)
    fogged = soft * (1 - FOG_SHARE) + FOG * FOG_SHARE
    # 흐리기만 하면 어떤 땅인지 색으로 다 보인다. 가 보지 않은 땅은 회색을 덧칠한다.
    shore = Image.fromarray((places * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(BLUR_RADIUS))
    share = np.asarray(shore).astype(float)[..., None] / 255 * UNKNOWN_LAND_SHARE
    grey = fogged.mean(axis=2, keepdims=True) * 0.3 + UNKNOWN_LAND * 0.7
    return (fogged * (1 - share) + grey * share).astype(np.uint8)


def reveal_mask(area, land, regions, label_spots):
    """구역의 땅과 둘레 바다. 바깥쪽은 점 무늬로 흩어져 흐린 지도에 녹아든다. 이웃 구역의 땅은 덮지 않는다."""
    mine = land & np.isin(regions, [i for i, region in enumerate(REGIONS) if region.name in area.regions])
    away = steps_from(mine, REVEAL_SOLID + REVEAL_FADE)
    near = away < REVEAL_SOLID + pg.bayer(HEIGHT, WIDTH) * REVEAL_FADE
    mask = near & ~(land & ~mine)
    for x0, y0, x1, y1 in area.extra:
        left, top = dot(x0, y0)
        right, bottom = dot(x1, y1)
        box = np.zeros(mask.shape, dtype=bool)
        box[int(top):int(bottom), int(left):int(right)] = True
        mask |= box & ~(land & ~mine)
    if area.label is not None:
        mask |= label_spots[area.label]
    return mask


def save_reveals(folder, named, land, regions, label_spots):
    """구역마다 선명한 조각을 저장하고, 조각의 자리를 world_map.json에 쓸 값으로 돌려준다."""
    areas = []
    for area in AREAS:
        mask = reveal_mask(area, land, regions, label_spots)
        rows, columns = np.nonzero(mask.any(axis=1))[0], np.nonzero(mask.any(axis=0))[0]
        top, bottom, left, right = int(rows[0]), int(rows[-1]) + 1, int(columns[0]), int(columns[-1]) + 1
        pixels = np.dstack((named, mask.astype(np.uint8) * 255))[top:bottom, left:right]
        Image.fromarray(pixels).save(folder / f"area_{area.id}.png")
        areas.append({"id": area.id, "arrivalChapter": area.arrival_chapter, "x": left, "y": top,
                      "width": right - left, "height": bottom - top})
    return areas


def atlas_focus():
    left, top = dot(FOCUS_BOX[0], FOCUS_BOX[1])
    right, bottom = dot(FOCUS_BOX[2], FOCUS_BOX[3])
    return {"title": FOCUS_TITLE, "x": left, "y": top, "width": right - left, "height": bottom - top}


def atlas_names():
    """크게 볼 때 적을 이름들. below면 점 아래에 적는다(이웃 이름과 겹치는 곳)."""
    names = []
    for place in PLACES:
        if place.area in FOCUS_AREAS:
            x, y = dot(place.x, place.y)
            names.append({"area": place.area, "text": place.name, "x": x, "y": y, "below": place.map_id in NAME_BELOW, "region": False})
    for area, text, sketch_x, sketch_y in REGION_NAMES:
        x, y = dot(sketch_x, sketch_y)
        names.append({"area": area, "text": text, "x": x, "y": y, "below": False, "region": True})
    return names


def atlas_places():
    places = []
    for place in PLACES:
        x, y = dot(place.x, place.y)
        places.append({"map": place.map_id, "area": place.area, "x": x, "y": y, "inside": list(place.inside)})
    return places


def road_stops(start, end):
    """길이 지나는 자리(스케치 좌표): 양 끝과 그 사이 꺾이는 곳."""
    spots = {place.map_id: place for place in PLACES}
    first, last = spots[start], spots[end]
    return [(first.x, first.y)] + ROAD_BENDS.get((start, end), []) + [(last.x, last.y)]


def atlas_roads():
    """이어진 길의 토막들. 길은 나중에 닿는 곳의 구역을 따른다(그 구역을 다녀와야 보인다)."""
    spots = {place.map_id: place for place in PLACES}
    roads = []
    for start, end in ROADS:
        stops = road_stops(start, end)
        for a, b in zip(stops, stops[1:]):
            ax, ay = dot(*a)
            bx, by = dot(*b)
            roads.append({"area": spots[end].area, "ax": ax, "ay": ay, "bx": bx, "by": by})
    return roads


def atlas_future_towns():
    return [{"x": x, "y": y} for x, y in (dot(*town) for town in FUTURE_TOWNS)]


def fog_preview(fog, folder):
    """1부의 지도 창이 어떻게 보이는지(힐리아 동부까지 다녀왔을 때)."""
    image = Image.fromarray(fog).convert("RGBA")
    atlas = json.loads((folder / "world_map.json").read_text(encoding="utf-8"))
    first = atlas["areas"][0]
    piece = Image.open(folder / f"area_{first['id']}.png")
    image.alpha_composite(piece, (first["x"], first["y"]))
    return image.resize((WIDTH * pg.SCALE, HEIGHT * pg.SCALE), Image.NEAREST)


def here_mark():
    """지금 있는 곳을 가리키는 빨간 핀."""
    c = Canvas(11, 15)
    c.paint(c.ellipse(5.5, 5.5, 5, 5), PIN[0])
    c.paint(c.polygon([(1.6, 8), (9.4, 8), (5.5, 15)]), PIN[0])
    c.paint(c.ellipse(5.5, 5.5, 4, 4), PIN[1])
    c.paint(c.polygon([(3, 8), (8, 8), (5.5, 13)]), PIN[1])
    c.paint(c.ellipse(4.6, 4.6, 1.4, 1.4), PIN[2])
    c.paint(c.ellipse(5.5, 5.5, 1.8, 1.8), PIN[3])
    return c.image


NAME_BELOW = {"trainStop", "outskirts", "caveRoad", "eastPort", "port", "leviathanCoast", "station"}


def place_check_sheet(world, font_path):
    """점마다 맵 이름을 적어 자리를 확인하는 그림. 힐리아 오른쪽 땅은 점이 많아서 크게 키운다."""
    font = ImageFont.truetype(str(font_path), 15)
    panels = (((460, 800, 880, 1080), 5), ((134, 420, 640, 1040), 3))
    sheets = []
    for (x0, y0, x1, y1), zoom in panels:
        left, top = dot(x0, y0)
        right, bottom = dot(x1, y1)
        part = world[int(top):int(bottom), int(left):int(right)]
        image = Image.fromarray(np.repeat(np.repeat(part, zoom, axis=0), zoom, axis=1))
        draw = ImageDraw.Draw(image)

        def spot(x, y):
            cx, cy = dot(x, y)
            return (cx - int(left) + 0.5) * zoom, (cy - int(top) + 0.5) * zoom

        for start, end in ROADS:
            draw.line([spot(*stop) for stop in road_stops(start, end)], fill=(246, 230, 180), width=3)
        for place in PLACES:
            x, y = spot(place.x, place.y)
            draw.ellipse((x - 6, y - 6, x + 6, y + 6), fill=(246, 226, 122), outline=(26, 20, 8), width=2)
        for place in PLACES:
            cx, cy = dot(place.x, place.y)
            if left <= cx < right and top <= cy < bottom:
                lift = 7.5 if place.map_id in NAME_BELOW else -3.5
                at = ((cx - int(left) + 0.5) * zoom, (cy - int(top) + lift) * zoom)
                draw.text(at, place.name, font=font, fill=(255, 250, 225), anchor="ms", stroke_width=2, stroke_fill=(14, 24, 32))
        sheets.append(image)
    width = sum(image.width for image in sheets) + 8
    sheet = Image.new("RGB", (width, max(image.height for image in sheets)), (14, 24, 32))
    sheet.paste(sheets[0], (0, 0))
    sheet.paste(sheets[1], (sheets[0].width + 8, 0))
    return sheet


# ── 만들기 ─────────────────────────────────────────────

def draw_world(root):
    rng = np.random.default_rng(1104)
    ink = read_ink(root / "Docs/ArtUpgrade/PixelGen/world_map_sketch.png")
    land = to_dots(trace_land(ink), LAND_SHARE)
    # 원더랜드의 두 땅은 스케치에서 맞닿아 있고 사이에 선 하나만 있다. 좁은 물길로 남긴다.
    land &= ~to_dots(boxed(ink.line, 1340, 590, 1560, 945), 0.25)
    labels = parts(land)
    cloud_x, cloud_y = dot(1520, 800)
    cloud_land = labels == labels[int(cloud_y), int(cloud_x)]
    land &= ~cloud_land
    regions = region_map()
    zones = district_zones(land, regions, rng)
    world = np.zeros((HEIGHT, WIDTH, 3), dtype=np.uint8)
    paint_sea(world, land, rng)
    paint_storm(world, land, ink, rng)
    whirlpool = paint_whirlpool(world, land)
    paint_coast(world, land)
    paint_land(world, land, regions, rng)
    paint_farmland(world, land, regions, zones)
    paint_paddies(world, zones)
    paint_city(world, zones, rng)
    paint_military_base(world, zones, rng)
    paint_rivers(world, land, ink)
    paint_railway(world)
    paint_borders(world, land, ink)
    paint_wind(world, ink)
    paint_cloud_land(world, cloud_land, land, rng)
    paint_world_tree(world, ink, rng)
    paint_craters(world, land)
    place_scenery(world, land, regions, zones, ink, rng)
    paint_snake_train(world)
    paint_hexagon(world, ink)
    return world, land, regions, land | cloud_land | whirlpool


def main():
    root = Path(sys.argv[1])
    world, land, regions, places = draw_world(root)
    folder = root / "Assets/MiniPandoras/Art/PixelGen/UI/WorldMap"
    folder.mkdir(parents=True, exist_ok=True)
    Image.fromarray(pg.upscale(world)).save(folder / "world_map.png")
    named = world.copy()
    font = root / "Assets/ThirdParty/Pretendard/Pretendard-Bold.otf"
    label_spots = paint_labels(named, font)
    docs = root / "Docs/ArtUpgrade/PixelGen"
    Image.fromarray(pg.upscale(named)).save(docs / "world_map_preview.png")
    place_check_sheet(world, font).save(docs / "world_map_places.png")

    fog = blurred_world(world, places)
    Image.fromarray(fog).save(folder / "world_map_blur.png")
    Image.fromarray(here_mark()).save(folder / "here.png")
    atlas = {"areas": save_reveals(folder, named, land, regions, label_spots), "places": atlas_places(),
             "focus": atlas_focus(), "names": atlas_names(), "roads": atlas_roads(), "futureTowns": atlas_future_towns()}
    (folder / "world_map.json").write_text(json.dumps(atlas, ensure_ascii=False, indent=2), encoding="utf-8")
    fog_preview(fog, folder).save(docs / "world_map_fog_preview.png")
    print("세계지도", world.shape[1], "×", world.shape[0], "점, 게임 맵", len(PLACES), "곳, 맵 없는 점", len(FUTURE_TOWNS), "곳, 구역", len(AREAS))


if __name__ == "__main__":
    main()
