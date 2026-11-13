"""「판도라즈 대본」 아티팩트 페이지를 만든다: 게임 대사 파일(Data/Resources/Text)의 대사를 이야기 코드가 부르는 순서대로 틀(script_page_template.html)에 넣는다.

대사 파일만 차례로 읽으면 순서가 틀린다. 덩어리(s.Lines)는 파일 순서대로지만, 주인공의 대답 같은 한 줄 대사는
파일 뒤쪽에 따로 있고 코드가 s.Say(Hero, T("…"))로 덩어리 사이에 끼워 넣는다. 그래서 장마다 Scripts/Story/<장>.cs를
위에서 아래로 읽으며 대사가 화면에 나오는 호출을 차례로 줍는다.
- s.Lines("열쇠"): 그 덩어리의 줄들(화자·말투는 파일에 있다)
- Say·SayUnseen·Shout·Think·Narrate·Notify·Choose의 글이 T("열쇠")이거나 그것을 돌려주는 속성: 화자·말투는 코드에서
- ("화자", T("열쇠")) 꼴의 짝: 말 걸면 차례로 하는 대사
- 위 호출의 글이 코드에 바로 적힌 문자열("......!" 같은): 코드 줄. 대사 파일에 없어서 페이지에서 고칠 수 없다
- 그 밖의 T("열쇠")(힌트, 막힌 길, 목표 같은 화면 안내): 안내 줄. 페이지는 처음에 숨긴다
코드 위치를 못 찾은 열쇠(이름표로만 쓰는 글)는 장 끝 「그 밖의 글」에 둔다. 2026-11-09에는 모든 열쇠를 자기 장 코드에서 찾았다.

페이지에서 고친 줄은 그 아티팩트의 db `edits`에 쌓이고(문서 id `파일~열쇠~순번`), Claude가 대사 파일에 넣은 뒤
이 도구로 페이지를 다시 만들어 같은 아티팩트에 올린다. 화자 이름과 색은 Data/Speakers에서 읽는다.

사용: python Tools/Story/script_page.py <프로젝트 루트> <만들 html 경로>
"""
import json
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
out = Path(sys.argv[2])
template = Path(__file__).with_name("script_page_template.html")
text_dir = root / "Assets/MiniPandoras/Data/Resources/Text"
story_dir = root / "Assets/MiniPandoras/Scripts/Story"
speaker_dir = root / "Assets/MiniPandoras/Data/Speakers"
HERO = "hero"
STRING = r'"(?:[^"\\\n]|\\.)*"'


def read_blocks(name):
    blocks, current = {}, None
    for raw in (text_dir / f"{name}.txt").read_text(encoding="utf-8").replace("\r\n", "\n").split("\n"):
        if raw.startswith("#") or not raw.strip():
            continue
        if raw.startswith("== "):
            current = blocks.setdefault(raw[3:].strip(), [])
            continue
        style, speaker, label, text = (raw.split("\t") + ["", "", "", ""])[:4]
        current.append({"s": style, "who": speaker, "name": label, "text": text})
    return blocks


# ── C# 읽기: 주석을 빈칸으로 바꾸고(자리는 그대로), 문자열 안인지 표시한다 ──

def blank_comments(src):
    code, inside = list(src), [False] * len(src)
    i, n = 0, len(src)
    while i < n:
        if src.startswith("//", i) or src.startswith("/*", i):
            end = src.find("\n", i) if src[i + 1] == "/" else src.find("*/", i + 2) + 2
            end = n if end < 2 else end
            for k in range(i, end):
                if src[k] != "\n":
                    code[k] = " "
            i = end
        elif src[i] in "\"'":
            quote, j = src[i], i + 1
            while j < n and src[j] != quote:
                j += 2 if src[j] == "\\" else 1
            for k in range(i, min(j + 1, n)):
                inside[k] = True
            i = j + 1
        else:
            i += 1
    return "".join(code), inside


class Source:
    def __init__(self, path):
        self.code, self.inside = blank_comments(path.read_text(encoding="utf-8"))
        self.consts = {m.group(1): m.group(2).strip() for m in re.finditer(r"const string (\w+) = ([^;]+);", self.code)}

    def enclosing_call(self, pos):
        """pos를 감싼 가장 안쪽 호출의 여는 괄호 자리. 배열 초기화 { }는 건너뛰고, 문장 끝이나 코드 블록에 닿으면 None."""
        depth, i = 0, pos - 1
        while i >= 0:
            ch = self.code[i]
            if not self.inside[i]:
                if ch in ")]}":
                    depth += 1
                elif ch in "([{":
                    if depth:
                        depth -= 1
                    elif ch == "(":
                        return i
                    elif ch == "{" and not re.search(r"(\]|new|,|\(|=)\s*$", self.code[max(0, i - 40):i]):
                        return None
                elif ch == ";" and not depth:
                    return None
            i -= 1
        return None

    def args(self, open_at):
        """괄호 안 인자들: [(시작, 끝, 글)]."""
        parts, depth, start, i = [], 0, open_at + 1, open_at + 1
        while i < len(self.code):
            ch = self.code[i]
            if not self.inside[i]:
                if ch in "([{":
                    depth += 1
                elif ch in ")]}":
                    if not depth:
                        parts.append((start, i, self.code[start:i].strip()))
                        return parts
                    depth -= 1
                elif ch == "," and not depth:
                    parts.append((start, i, self.code[start:i].strip()))
                    start = i + 1
            i += 1
        raise ValueError("괄호가 닫히지 않는다")

    def callee(self, open_at):
        m = re.search(r"(\w+)\s*$", self.code[:open_at])
        return m.group(1) if m else ""


SPEAK = {"Say", "SayUnseen", "Shout", "Think", "Narrate", "Notify", "Choose"}
TEXT_ARG = {"Say": 1, "SayUnseen": 1, "Shout": 1, "Think": 0, "Narrate": 0, "Notify": 0, "Choose": 1}
STYLE = {"Shout": "shout", "Thought": "think", "Narration": "narrate", "Say": "say"}


def literal(expr):
    return json.loads(expr) if re.fullmatch(STRING, expr) else None


def chapter_rows(name, blocks):
    """장 코드를 위에서 아래로 읽어 대사 줄을 차례로 돌려준다."""
    src = Source(story_dir / f"{name}.cs")
    aliases = {}  # private static string GoNorthLine => T("chapter2.goNorthLine");
    alias_at = set()
    for m in re.finditer(r"(\w+)\s*=>\s*T\(\s*(" + STRING + r")\s*\)", src.code):
        aliases[m.group(1)] = json.loads(m.group(2))
        alias_at.add(m.start(2))

    def value(expr):
        """화자·이름표 식의 값: 문자열, 상수, 주인공 id, T("열쇠")나 그 속성의 글."""
        expr = expr.strip()
        if expr in ("StoryDirector.HeroId", "HeroId", "GameState.Hero"):
            return HERO
        if literal(expr) is not None:
            return literal(expr)
        if expr in src.consts:
            return value(src.consts[expr])
        key = aliases.get(expr) or (re.fullmatch(r"T\(\s*(" + STRING + r")\s*\)", expr) or [None, None])[1]
        if key:
            key = json.loads(key) if key.startswith('"') else key
            return blocks[key][0]["text"]
        return None

    # 대사 배열: private static readonly string[] FairyLines = { T("…"), … }; 쓰는 곳 ("fairy", FairyLines[i])에서 화자를 안다.
    arrays, array_at = {}, set()
    for m in re.finditer(r"static readonly string\[\] (\w+)\s*=\s*\{", src.code):
        end = src.code.index("};", m.end())
        found = [(k.start(1), json.loads(k.group(1))) for k in re.finditer(r"\bT\(\s*(" + STRING + r")", src.code[:end]) if k.start() > m.end()]
        uses = [(u.start(), value(u.group(1))) for u in re.finditer(r"\(\s*([^,()]+?)\s*,\s*" + m.group(1) + r"\[", src.code)]
        if found and uses:
            arrays[m.group(1)] = ([k for _, k in found], uses)
            array_at.update(at for at, _ in found)
    # 대사를 받아 요정을 세우는 도우미: static void Folk(StoryDirector s, params string[] lines) { … ("fairy", lines[i]) … }
    helpers = {}
    for m in re.finditer(r"static \w+ (\w+)\(StoryDirector s, params string\[\] (\w+)\)", src.code):
        use = re.search(r"\(\s*([^,()]+?)\s*,\s*" + m.group(2) + r"\[", src.code[m.end():m.end() + 800])
        if use:
            helpers[m.group(1)] = use.group(1)

    def speech(call, args, index):
        """그 호출의 index번째 인자가 화면에 나오는 대사면 (말투, 화자, 이름표, 꼬리표)."""
        if call not in SPEAK:
            if call == "" and len(args) == 2 and index == 1 and value(args[0][2]):
                return "say", value(args[0][2]), "", ""
            if call == "ExplainInCutIn" and index >= 3 and literal(args[0][2]):
                return "say", literal(args[0][2]), "", "설명 컷인"
            if call == "Message" and index == 0:
                return "narrate", "", "", "전투 화면"
            if call in helpers and index >= 1:
                return "say", value(helpers[call]), "", ""
            return None
        positional = [a[2] for a in args if not re.match(r"\w+\s*:(?!:)", a[2])]
        named = dict(re.match(r"(\w+)\s*:\s*(.*)", a[2], re.S).groups() for a in args if re.match(r"\w+\s*:(?!:)", a[2]))
        style = next((STYLE[m] for a in args for m in re.findall(r"LineStyle\.(\w+)", a[2])), "say")
        if call == "Choose":
            who = value(positional[0]) or ""
            if index == 1:
                return ("think" if who == HERO else "say"), who, "", ""
            return "choice", "", "", ""
        if index != TEXT_ARG[call]:
            return None
        if call in ("Narrate", "Notify"):
            return ("narrate" if call == "Narrate" else "notify"), "", "", ""
        if call == "Think":
            return "think", value(positional[1]) if len(positional) > 1 else HERO, "", ""
        who = value(positional[0])
        label = named.get("name") or named.get("nameOverride")
        if call in ("Say", "Shout") and len(positional) > 2 and "LineStyle." not in positional[2]:
            label = positional[2]
        return ("shout" if call == "Shout" else style), who or "", value(label) if label else "", "화면 밖" if call == "SayUnseen" else ""

    # 대사가 나오는 자리를 모아 코드 순서로 늘어놓는다.
    events = []
    for m in re.finditer(r"\.Lines\(\s*(" + STRING + r")\s*\)", src.code):
        events.append((m.start(), "lines", json.loads(m.group(1))))
    for m in re.finditer(r"\bT\(\s*(" + STRING + r")", src.code):
        if m.start(1) not in alias_at and m.start(1) not in array_at:
            events.append((m.start(), "text", json.loads(m.group(1))))
    for keys, uses in arrays.values():
        at, who = uses[0]
        events += [(at, "said", (key, who)) for key in keys]
    if aliases:
        for m in re.finditer(r"\b(" + "|".join(aliases) + r")\b(?!\s*=>)", src.code):
            if not src.inside[m.start()]:
                events.append((m.start(), "text", aliases[m.group(1)]))
    for m in re.finditer(r"\.(" + "|".join(SPEAK) + r")\(", src.code):
        args = src.args(m.end() - 1)
        at = TEXT_ARG[m.group(1)]
        if at < len(args) and literal(args[at][2]) is not None:
            events.append((args[at][0], "code", literal(args[at][2])))
    events.sort(key=lambda e: e[0])

    rows, done = [], set()
    for pos, kind, payload in events:
        if kind == "said":
            key, who = payload
            if key not in done:
                done.add(key)
                rows.append({"s": "say", "who": who or "", "text": blocks[key][0]["text"], "key": key, "i": 0})
            continue
        if kind == "lines":
            if payload in done:
                continue
            done.add(payload)
            rows += [dict(ln, key=payload, i=i) for i, ln in enumerate(blocks[payload])]
            continue
        open_at = src.enclosing_call(pos)
        call = src.callee(open_at) if open_at is not None else None
        args = src.args(open_at) if open_at is not None else []
        index = next((n for n, (a, b, _) in enumerate(args) if a <= pos <= b), -1)
        said = speech(call, args, index) if call is not None else None
        if kind == "code":
            if said:
                rows.append({"s": said[0], "who": said[1], "name": said[2], "text": payload, "tag": said[3], "code": True})
            continue
        if payload in done:
            continue
        if said is None and call in ("Say", "Shout") and index == 2:
            continue  # 이름표로 쓰는 글(「병사들」)은 대사가 아니다. 장 끝에 따로 둔다.
        done.add(payload)
        ln = blocks[payload][0]
        branch = args and index >= 0 and re.search(r"\s\?\s", args[index][2])
        if said:
            row = {"s": said[0], "who": said[1], "name": said[2], "text": ln["text"], "key": payload, "i": 0}
            tag = " · ".join(t for t in (said[3], "갈래" if branch else "") if t)
            if tag:
                row["tag"] = tag
        else:
            row = {"s": "text", "who": "", "name": "", "text": ln["text"], "key": payload, "i": 0}
        rows.append(row)
    left = [k for k in blocks if k not in done]
    for key in left:
        rows += [dict(ln, s="text", key=key, i=i, scene="") for i, ln in enumerate(blocks[key])]
    return rows, left


def scenes_of(rows):
    """열쇠의 장면(prologue.twoPersonas.3 → prologue.twoPersonas)이 바뀔 때마다 묶음을 나눈다. 번호 없는 열쇠와 코드 줄은 앞 묶음에 붙는다."""
    scenes = []
    for row in rows:
        m = re.fullmatch(r"(.+)\.\d+", row.get("key", ""))
        scene = row.pop("scene", m.group(1) if m else None)
        if not scenes or (scene is not None and scene != scenes[-1]["key"]):
            scenes.append({"key": scene or "", "rows": []})
        scenes[-1]["rows"].append({k: v for k, v in row.items() if v is not None and v is not False and v != ""})
    return scenes


# 장 제목: chapterTitles.readonly.1·2가 0장 이름표와 제목, 3·4가 1장 …
titles = {}
for key, lines in read_blocks("ChapterTitles").items():
    m = re.fullmatch(r"chapterTitles\.readonly\.(\d+)", key)
    if m:
        titles[int(m.group(1))] = lines[0]["text"]

speakers = {}
for asset in speaker_dir.glob("*.asset"):
    body = asset.read_text(encoding="utf-8")
    sid = re.search(r"^  id: (.*)$", body, re.M).group(1).strip()
    name = re.search(r"^  displayName: (.*)$", body, re.M).group(1).strip()
    if name.startswith('"'):
        name = json.loads(name)
    color = re.search(r"^  nameColor: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)", body, re.M)
    hexcolor = "#%02x%02x%02x" % tuple(round(float(c) * 255) for c in color.groups()) if color else ""
    speakers[sid] = {"name": name or ("주인공" if sid == HERO else sid), "color": hexcolor}

chapters = [("prologue" if n == 0 else f"ch{n}", "Prologue" if n == 0 else f"Chapter{n}",
             titles.get(n * 2 + 1, f"{n}장"), titles.get(n * 2 + 2, "")) for n in range(12)]
chapters += [("adventurersGuild", "AdventurersGuild", "곁 이야기", "모험가 길드"), ("sideRoads", "SideRoads", "곁 이야기", "곁길"),
             ("giantTree", "GiantTree", "곁 이야기", "거대고목의 요정들")]

sections, report = [], []
for sid, name, label, title in chapters:
    blocks = read_blocks(name)
    rows, left = chapter_rows(name, blocks)
    lines_in_file = sum(len(v) for v in blocks.values())
    keyed = sum(1 for r in rows if "key" in r)
    assert keyed == lines_in_file, f"{name}: 파일 {lines_in_file}줄 중 {keyed}줄만 담았다"
    sections.append({"id": sid, "file": name, "label": label, "title": title, "scenes": scenes_of(rows)})
    report.append(f"{name} {sum(1 for r in rows if r['s'] != 'text')}/{sum(1 for r in rows if r['s'] == 'text')}" + (f" left{left}" if left else ""))

all_rows = [r for s in sections for sc in s["scenes"] for r in sc["rows"]]
used = {r["who"] for r in all_rows if r.get("who")}
missing = sorted(used - set(speakers))
data = {"speakers": {k: v for k, v in speakers.items() if k in used}, "sections": sections}
# 자료 속 "</"가 script 태그를 닫지 않게 JSON 이스케이프(\/)로 바꾼다.
payload = json.dumps(data, ensure_ascii=False, separators=(",", ":")).replace("</", "<\\/")
out.write_text(template.read_text(encoding="utf-8").replace("__LINES__", payload), encoding="utf-8")
print("rows", len(all_rows), "spoken", sum(1 for r in all_rows if r["s"] != "text"), "hint", sum(1 for r in all_rows if r["s"] == "text"),
      "code", sum(1 for r in all_rows if r.get("code")), "speakers", len(data["speakers"]), "missing", missing)
print(" ".join(report))
