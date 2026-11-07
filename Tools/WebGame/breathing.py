"""웹게임 인물의 서 있는 그림에 숨쉬는 그림을 굽는다.

서 있는 그림을 sprite-gen breathe의 smooth 모드에 넣는다. 머리와 발은 픽셀 하나 바뀌지 않고 가슴과 어깨만 오르내린다.
결과는 Art/Breathing/<id>/breathe_<방향>_<단계>.png와 breathe.json이고, 메뉴 MiniPandoras/Import Breathing Frames가 시트에 넣는다.

들숨과 날숨이 대칭이라 쉬는 그림(서 있는 그림 그대로)과 세 단계만 있으면 한 호흡이 된다.
텍스처 메모리를 아끼려고 한 호흡을 6칸으로 구워 1~3칸만 남기고, 재생 순서는 CharacterAnimator가 정한다.

    python Tools/WebGame/breathing.py [--only hero,diel] [--out 폴더] [--sprite-gen 실행파일]
"""
import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CHARACTERS = ROOT / "Assets/MiniPandoras/Art/WebGame/Characters"
OUT = ROOT / "Assets/MiniPandoras/Art/Breathing"
# 환경 변수 SPRITE_GEN으로 바꿀 수 있다(--sprite-gen이 있으면 그것이 우선).
SPRITE_GEN = os.environ.get("SPRITE_GEN", "E:/ArtTools/venv-spritegen/Scripts/sprite-gen.exe")

DIRECTIONS = ("down", "right", "up")
LOOP_CELLS = 6
KEPT_CELLS = (1, 2, 3)

# 굽지 않는 인물. 49명을 다 구워 바뀐 픽셀의 자리를 보고 골랐다. 앞모습에서도 가슴이 아니라 다른 곳이 움직인다.
STAYS_STILL = frozenset({
    # 짐승과 슬라임: 사람 몸으로 재므로 거부되거나 머리까지 바뀐다.
    "boar", "dog", "mossSlime", "rang", "rangBeast", "sandWolf", "wolf",
    # 앉아 있거나 긴 옷에 가린 인물과 거인: 옷자락이나 다리만 움직인다.
    "divineEmotion", "divineIntellect", "giantHobbit", "goddess",
    # 작은 요정: 다리 끝만 몇 픽셀 바뀐다.
    "apia", "apiaCursed", "fairy", "guardFairy",
})

# 가슴 가중치를 넓게 잡아 목에 힘이 몰리지 않게 한다. sprite-gen이 변형이 크다고 거부하면 다음 세기로 내려간다.
STRENGTHS = (
    {"depth": 0.02, "depth_x": 0.03, "chest_rx": 1.5, "chest_ry": 0.6},
    {"depth": 0.015, "depth_x": 0.025, "chest_rx": 1.5, "chest_ry": 0.6},
    {"depth": 0.012, "depth_x": 0.02, "chest_rx": 1.5, "chest_ry": 0.6},
)


def breathe(sprite_gen, source, work):
    """서 있는 그림 한 장으로 한 호흡을 굽는다. 어느 세기로도 거부되면 (None, 마지막 거부 메시지)를 돌려준다."""
    frames = work / "frames"
    frames.mkdir()
    for cell in range(LOOP_CELLS):
        shutil.copy(source, frames / f"f{cell}.png")

    environment = dict(os.environ, PYTHONUTF8="1", PYTHONIOENCODING="utf-8")
    refusal = ""
    for attempt, strength in enumerate(STRENGTHS):
        baked = work / f"baked{attempt}"
        command = [
            sprite_gen, "breathe", str(frames), str(baked),
            "--mode", "smooth",
            # 들숨과 날숨이 같고 어깨가 늦지 않아야 앞뒤 칸이 같은 그림이 된다.
            "--inhale", "0.5", "--shoulder-lag", "0",
            "--depth", str(strength["depth"]), "--depth-x", str(strength["depth_x"]),
            "--chest-rx", str(strength["chest_rx"]), "--chest-ry", str(strength["chest_ry"]),
        ]
        result = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", env=environment)
        if result.returncode == 0:
            return baked, strength
        refusal = (result.stderr or result.stdout).strip().splitlines()[-1]
    return None, refusal


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def bake_character(sprite_gen, sheet_id, out):
    """한 인물의 세 방향을 굽고, 방향마다 쓴 세기를 돌려준다. 한 방향이라도 안 되면 그 인물은 통째로 뺀다."""
    folder = CHARACTERS / sheet_id
    sheet = json.loads((folder / "sheet.json").read_text(encoding="utf-8"))
    used = {}
    with tempfile.TemporaryDirectory() as temporary:
        results = {}
        for direction in DIRECTIONS:
            work = Path(temporary) / direction
            work.mkdir()
            baked, detail = breathe(sprite_gen, folder / f"{direction}_0.png", work)
            if baked is None:
                return None, f"{direction}: {detail}"
            if digest(baked / "f1.png") != digest(baked / "f5.png") or digest(baked / "f2.png") != digest(baked / "f4.png"):
                return None, f"{direction}: 들숨과 날숨이 달라 세 단계로 줄일 수 없다"
            results[direction] = baked
            used[direction] = detail["depth"]

        target = out / sheet_id
        shutil.rmtree(target, ignore_errors=True)
        target.mkdir(parents=True)
        for direction, baked in results.items():
            for stage, cell in enumerate(KEPT_CELLS):
                shutil.copy(baked / f"f{cell}.png", target / f"breathe_{direction}_{stage}.png")

    manifest = {
        "id": sheet["id"],
        "width": sheet["width"],
        "height": sheet["height"],
        "anchorX": sheet["anchorX"],
        "anchorY": sheet["anchorY"],
        "frames": len(KEPT_CELLS),
    }
    (out / sheet_id / "breathe.json").write_text(json.dumps(manifest, indent=1), encoding="utf-8")
    return used, ""


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--only", help="이 인물들만 굽는다(쉼표로 나눈다)")
    parser.add_argument("--out", default=str(OUT))
    parser.add_argument("--sprite-gen", default=SPRITE_GEN)
    args = parser.parse_args()

    ids = args.only.split(",") if args.only else sorted(p.name for p in CHARACTERS.iterdir() if p.is_dir())
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    failed = []
    baked = 0
    for sheet_id in ids:
        if sheet_id in STAYS_STILL:
            continue

        used, why = bake_character(args.sprite_gen, sheet_id, out)
        if used is None:
            failed.append(sheet_id)
            print(f"{sheet_id}: 실패 - {why}")
        else:
            baked += 1
            print(f"{sheet_id}: {used}")

    print(f"끝: {baked}명, 실패 {len(failed)}명 {failed}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
