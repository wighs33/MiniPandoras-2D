"""타이틀 화면에만 쓰는 그림: 풍경 위 글자가 읽히도록 까는 어둠. 풍경은 title_scene.py, 창 틀·버튼 같은 부품은 ui_kit.py가 그린다.

어둠 그림은 부드러운 알파라서 Unity에서 선형 필터로 들인다. 늘려 쓰므로 점 크기와 상관없다.

  Assets/MiniPandoras/Art/PixelGen/UI/Title/shade.png       로고 뒤에 까는, 가운데가 짙은 밤색 타원
  Assets/MiniPandoras/Art/PixelGen/UI/Title/plate_shade.png 메뉴 글자 뒤에 까는, 가운데가 고르게 짙은 둥근 판

사용: python Tools/PixelGen/title_ui.py <프로젝트 루트>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

from ui_kit import NIGHT


def night_alpha(alpha):
    rgba = np.zeros((*alpha.shape, 4), np.uint8)
    rgba[..., :3] = NIGHT
    rgba[..., 3] = (np.clip(alpha, 0, 1) * 255).round().astype(np.uint8)
    return rgba


def shade():
    """가운데가 짙고 둘레로 사라지는 타원(128×64점)."""
    w, h = 128, 64
    y, x = np.mgrid[0:h, 0:w]
    d = np.hypot((x - (w - 1) / 2) / (w / 2), (y - (h - 1) / 2) / (h / 2))
    return night_alpha(np.clip(1 - d, 0, 1) ** 1.6)


def plate_shade():
    """가운데가 고르게 짙고 가장자리 3할에서만 사라지는 둥근 판(160×48점). 메뉴 글자 뒤에 깔아 뒤 그림이 밝아도 읽히게 한다."""
    w, h = 160, 48
    y, x = np.mgrid[0:h, 0:w]
    dx = np.abs(x - (w - 1) / 2) / (w / 2)
    dy = np.abs(y - (h - 1) / 2) / (h / 2)
    d = np.maximum(dx, dy * 0.85 + dx * 0.15)
    t = np.clip((1 - d) / 0.3, 0, 1)
    return night_alpha(t * t * (3 - 2 * t))


def main(root):
    title = root / "Assets/MiniPandoras/Art/PixelGen/UI/Title"
    title.mkdir(parents=True, exist_ok=True)
    for name, rgba in {"shade.png": shade(), "plate_shade.png": plate_shade()}.items():
        Image.fromarray(rgba, "RGBA").save(title / name)
        print((title / name).relative_to(root).as_posix(), rgba.shape[1], "x", rgba.shape[0])


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
