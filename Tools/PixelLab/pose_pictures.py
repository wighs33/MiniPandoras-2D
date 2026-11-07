"""PixelLab이 웹게임 정면 그림을 고쳐 그린 쓰러진 자세 그림을 게임 폴더로 옮긴다(사용자 요청 2026-11-05).

원본은 이 폴더의 raw/<시트 id>_<자세>.png다. 빈 칸을 잘라 내면 그림의 아래 가운데가 인물의 발밑이 된다(PosePicturesImporter가 기준점으로 쓴다).
  Assets/MiniPandoras/Art/Poses/<시트 id>/kneel.png   주저앉은 모습
  Assets/MiniPandoras/Art/Poses/<시트 id>/lie.png     쓰러진 모습

사용: python Tools/PixelLab/pose_pictures.py <프로젝트 루트>
"""
import sys
from pathlib import Path

from PIL import Image

POSES = ("kneel", "lie")


def main(project):
    raw = project / "Tools/PixelLab/raw"
    for path in sorted(raw.glob("*.png")):
        sheet, pose = path.stem.rsplit("_", 1)
        if pose not in POSES:
            continue
        image = Image.open(path).convert("RGBA")
        out = project / "Assets/MiniPandoras/Art/Poses" / sheet
        out.mkdir(parents=True, exist_ok=True)
        image.crop(image.getbbox()).save(out / f"{pose}.png")
        print(sheet, pose, image.getbbox())


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
