"""웹게임의 화자 목록(story/cast.ts)과 일러스트 목록(art.ts)을 읽어 speakers.json으로 쓴다.

두 파일은 TypeScript 객체 표기라 브라우저에서 꺼낼 수 없어서(모듈 안에만 있다), 원본 글을 읽어 필요한 칸만 뽑는다.
- 화자: 이름(주인공은 정한 이름을 쓰므로 null), 이름 색, 대화창 도트 얼굴(인물 시트 id), 일러스트 id
- 일러스트: 얼굴 칸의 가운데·한 변(픽셀), 그림 크기, 작은 짐승 한 마리 그림인지(whole)

사용: python -I export_speakers.py <웹게임 games/pandoras 폴더> <out.json>
"""
import json
import re
import sys
from pathlib import Path


def speaker_blocks(source):
    body = source[source.index("export const SPEAKERS"):]
    body = body[body.index("{") + 1:]
    depth, start, key = 0, None, None
    for match in re.finditer(r"[{}]|^  ([A-Za-z0-9]+): \{", body, re.M):
        token = match.group(0)
        if match.group(1) and depth == 0:
            key, start, depth = match.group(1), match.end(), 1
        elif token == "{":
            depth += 1
        elif token == "}":
            depth -= 1
            if depth == 0 and key:
                yield key, body[start:match.start()]
                key = None
            if depth < 0:
                return


def field(block, name):
    found = re.search(rf'\b{name}:\s*"([^"]*)"', block)
    return found.group(1) if found else None


def illustrations(source):
    body = source[source.index("export const ILLUSTRATIONS"):]
    for match in re.finditer(r'\{\s*id:\s*"([^"]+)".*?face:\s*\[(\d+),\s*(\d+),\s*(\d+)\].*?size:\s*\[(\d+),\s*(\d+)\](.*?)\}', body, re.S):
        figure_id, fx, fy, fs, w, h, rest = match.groups()
        yield figure_id, {
            "faceX": int(fx), "faceY": int(fy), "faceSize": int(fs),
            "width": int(w), "height": int(h), "whole": "whole: true" in rest,
        }


def main():
    game, out = Path(sys.argv[1]), Path(sys.argv[2])
    figures = dict(illustrations((game / "art.ts").read_text(encoding="utf-8")))
    speakers = []
    for key, block in speaker_blocks((game / "story/cast.ts").read_text(encoding="utf-8")):
        art = field(block, "art")
        speakers.append({
            "id": key,
            "name": field(block, "name"),
            "color": field(block, "color"),
            "portrait": field(block, "portrait"),
            "figure": art,
            "figureData": figures.get(art),
        })
    out.write_text(json.dumps({"speakers": speakers}, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"{len(speakers)} speakers -> {out}")


if __name__ == "__main__":
    main()
