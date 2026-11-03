// 웹게임(개발 서버 페이지)에서 맵과 인물 그림을 뽑아 export_server.py로 보낸다.
// 웹게임 엔진이 그린 결과를 그대로 쓰므로, 타일 이어 그리기·지형 윤곽 같은 그리기 코드를 Unity에서 다시 짜지 않는다.
//
// 사용(브라우저 콘솔 또는 자동화):
//   const m = await import("http://127.0.0.1:4330/export_webgame.js");
//   await m.exportWebGame(window.__pandoras, { maps: ["hillia", "castle"], characters: ["hero"] });
//
// 결과(서버 root 기준):
//   Maps/<id>/ground.png   바닥 그림. 타일 한 칸 = 논리 16px = 그림 64px(ART 4배)
//   Maps/<id>/map.json     크기, 충돌 격자, 소품 자리, 지점(marks), 포탈(warps), 세운 것, 움직이는 타일, 빛, 분위기
//   Maps/<id>/upright_<n>.png  바닥 위에 세워 그리는 것(성문·벽·문·창문·세운 타일 묶음). 인물과 발밑 y로 앞뒤를 가린다
//   Maps/<id>/gate_<n>.png     닫힌 성문(이야기에서 성문을 닫을 때 바꿔 낀다)
//   Props/<id>.png, Props/props.json   소품 그림과 기준점(발밑, 논리 px)
//   Characters/<id>/<dir>_<n>.png, Characters/<id>/sheet.json   인물 걷기 그림(down·right·up 3장씩)
//   Images/<name>.png      이야기가 맵에 놓는 물건(상자, 보석 …)
//   Effects/flame_<size>_<n>.png   횃불·모닥불 타일의 불꽃(세 모양이 번갈아 흔들린다)

const TILE = 16;
const ART = 4;
// 충돌은 웹게임처럼 점 단위(지형 윤곽)로 판정하므로 한 칸을 4×4로 쪼개 표본을 뜬다.
const COLLISION_PER_TILE = 4;
// 웹게임 FieldScene.renderSorted가 바닥 그림 위에 따로 세워 그리는 타일. 웹게임 목록과 어긋나면 개수 검사가 멈춘다.
const UPRIGHT_TILES = ["door", "inWall", "oldWall", "inWindow", "dayWindow", "shelf", "fireplace"];
const VIEW_W = 384;
const VIEW_H = 216;
// 웹게임 drawFlame에 쓰는 크기: 횃불 1, 모닥불은 두 불꽃 1.3·1.1
const FLAME_SIZES = [1, 1.3, 1.1];

// 감정 아이콘 키는 파일 이름에 쓸 수 없는 글자가 있어 이름을 바꿔 저장한다.
const EMOTE_FILES = { "!": "exclaim", "?": "question", "…": "dots", anger: "anger", sweat: "sweat", heart: "heart" };

export async function exportWebGame(
  game,
  { maps = [], characters = [], images = [], emotes = false, panels = [], stagePanels = [], flames = false, server = "http://127.0.0.1:4330" } = {},
) {
  const save = async (path, body, type) => {
    const response = await fetch(`${server}/save?path=${encodeURIComponent(path)}`, {
      method: "POST",
      body,
      headers: { "Content-Type": type },
    });
    if (!response.ok) throw new Error(`저장 실패 ${path}: ${response.status}`);
  };
  const png = (canvas) => new Promise((resolve) => canvas.toBlob(resolve, "image/png"));
  const report = { maps: {}, props: [], characters: [] };

  const usedProps = new Set();
  for (const id of maps) {
    game.field.load(id);
    const map = game.field.map;
    await save(`Maps/${id}/ground.png`, await png(groundWithStaircases(game.field)), "image/png");
    const standing = standingPieces(game.field);
    for (const [i, piece] of standing.uprights.entries())
      await save(`Maps/${id}/upright_${i}.png`, await png(piece.canvas), "image/png");
    for (const [i, gate] of standing.gates.entries())
      await save(`Maps/${id}/gate_${i}.png`, await png(gate.canvas), "image/png");

    const collision = [];
    const step = TILE / COLLISION_PER_TILE;
    for (let y = 0; y < map.h * COLLISION_PER_TILE; y++) {
      let row = "";
      for (let x = 0; x < map.w * COLLISION_PER_TILE; x++)
        row += map.isSolidAt(x * step + step / 2, y * step + step / 2) ? "1" : "0";
      collision.push(row);
    }

    for (const prop of map.renderProps) usedProps.add(prop.id);
    const data = {
      id,
      name: map.def.name,
      theme: map.def.theme,
      music: map.def.music ?? null,
      width: map.w,
      height: map.h,
      tilePixels: TILE,
      artScale: ART,
      collisionPerTile: COLLISION_PER_TILE,
      collision,
      props: map.renderProps.map((p) => ({ id: p.id, x: p.x, y: p.y })),
      points: Object.entries(map.points).map(([name, cells]) => ({ name, cells: cells.map(([x, y]) => ({ x, y })) })),
      warps: Object.entries(map.def.warps ?? {}).map(([point, w]) => ({ point, map: w.map, target: w.point, dir: w.dir })),
      uprights: standing.uprights.map(({ canvas, ...rect }, i) => ({ image: `upright_${i}`, ...rect })),
      gates: standing.gates.map(({ canvas, ...rect }, i) => ({ image: `gate_${i}`, ...rect })),
      animated: map.animated(),
      glints: waterGlints(map),
      lights: game.field.lights.map(({ x, y, radius, color, flicker }) => ({ x, y, radius, color, flicker })),
      background: map.def.background ?? null,
      ambient: map.def.ambient ?? null,
      weather: map.def.weather ?? null,
    };
    await save(`Maps/${id}/map.json`, JSON.stringify(data, null, 1), "application/json");
    report.maps[id] = {
      width: map.w,
      height: map.h,
      props: data.props.length,
      warps: data.warps.length,
      uprights: data.uprights.length,
      gates: data.gates.length,
    };
  }

  const propMeta = [];
  for (const id of usedProps) {
    const prop = game.assets.props[id];
    await save(`Props/${id}.png`, await png(prop.image), "image/png");
    propMeta.push({ id, anchorX: prop.ax, anchorY: prop.ay, width: prop.image.width / ART, height: prop.image.height / ART });
  }
  if (propMeta.length) await save("Props/props.json", JSON.stringify({ props: propMeta }, null, 1), "application/json");
  report.props = propMeta.map((p) => p.id);

  for (const id of characters) {
    const sheet = game.assets.sheets[id];
    const frames = {};
    for (const dir of ["down", "right", "up"]) {
      frames[dir] = sheet.frames[dir].length;
      for (let i = 0; i < sheet.frames[dir].length; i++)
        await save(`Characters/${id}/${dir}_${i}.png`, await png(sheet.frames[dir][i]), "image/png");
    }
    // 대화창 안의 도트 얼굴. 도트 해상도(ART배) 캔버스다.
    if (sheet.portrait) await save(`Characters/${id}/portrait.png`, await png(sheet.portrait), "image/png");
    const meta = {
      id,
      width: sheet.w,
      height: sheet.h,
      anchorX: sheet.ax,
      anchorY: sheet.ay,
      framesDown: frames.down,
      framesRight: frames.right,
      framesUp: frames.up,
      hasPortrait: Boolean(sheet.portrait),
    };
    await save(`Characters/${id}/sheet.json`, JSON.stringify(meta, null, 1), "application/json");
    report.characters.push(id);
  }

  if (images.length) {
    report.images = [];
    for (const name of images) {
      const image = game.assets.images[name];
      if (!image) throw new Error(`그림이 없다: ${name}`);
      await save(`Images/${name}.png`, await png(image), "image/png");
      report.images.push(name);
    }
  }

  // 연출 그림(story/stage-fx.ts의 …Panel)은 코드로 그리므로 모듈을 불러와 한 장면(90프레임째)을 그린다.
  if (stagePanels.length) {
    const fx = await webModule("story/stage-fx.ts");
    report.stagePanels = [];
    for (const name of stagePanels) {
      const canvas = document.createElement("canvas");
      canvas.width = VIEW_W * ART;
      canvas.height = VIEW_H * ART;
      const ctx = canvas.getContext("2d");
      ctx.setTransform(ART, 0, 0, ART, 0, 0);
      fx[name](ctx, 90);
      await save(`Panels/${name}.png`, await png(canvas), "image/png");
      report.stagePanels.push(name);
    }
  }

  if (flames) {
    report.flames = [];
    for (const size of FLAME_SIZES)
      for (let frame = 0; frame < 3; frame++) {
        await save(`Effects/flame_${size}_${frame}.png`, await png(flameFrame(game.field, size, frame)), "image/png");
        report.flames.push(`${size}_${frame}`);
      }
  }

  if (emotes) {
    report.emotes = [];
    for (const [key, file] of Object.entries(EMOTE_FILES)) {
      const image = game.assets.images[`emote:${key}`];
      if (!image) continue;
      await save(`Emotes/${file}.png`, await png(image), "image/png");
      report.emotes.push(file);
    }
  }

  // 설명 컷씬 그림은 코드로 그려지므로, 개발 도구로 화면에 띄우고 잠시 움직인 뒤의 화면을 그대로 찍는다.
  if (panels.length) {
    const dev = window.__pandorasDev;
    report.panels = [];
    for (const name of panels) {
      await dev.explain(name);
      await dev.step(90);
      await save(`Panels/${name}.png`, await png(game.ctx.canvas), "image/png");
      report.panels.push(name);
    }
    await dev.explain();
  }
  return report;
}

// ── 세운 것 ───────────────────────────────────────────────
// 웹게임은 성문·벽·문 같은 것을 바닥 그림에 넣지 않고 그때그때 인물과 함께 발밑 y 순서로 그린다(renderSorted).
// 그 그리기 함수는 모듈 안에 있어 밖에서 부를 수 없으므로, renderSorted가 모은 그리기 목록을 정렬 직전에 가로채
// 하나씩 따로 그린다. 그리기 함수가 받은 ctx를 붙잡아 두므로, 그리는 곳은 대리 ctx로 나중에 바꾼다.

function retargetableContext() {
  const holder = { target: null };
  const proxy = new Proxy(
    {},
    {
      get: (_, key) => {
        const value = holder.target[key];
        return typeof value === "function" ? value.bind(holder.target) : value;
      },
      set: (_, key, value) => {
        holder.target[key] = value;
        return true;
      },
    },
  );
  return { proxy, holder };
}

function captureSorted(field, ctx, camX, camY) {
  const original = Array.prototype.sort;
  let captured = [];
  Array.prototype.sort = function (compare) {
    if (this.length && this.every((item) => typeof item?.draw === "function" && typeof item.y === "number")) {
      captured = this.slice();
      this.length = 0;
      return this;
    }
    return original.call(this, compare);
  };
  try {
    field.renderSorted(ctx, camX, camY);
  } finally {
    Array.prototype.sort = original;
  }
  return captured;
}

function artCanvas(box) {
  const canvas = document.createElement("canvas");
  canvas.width = Math.ceil(box.w * ART);
  canvas.height = Math.ceil(box.h * ART);
  const ctx = canvas.getContext("2d");
  ctx.imageSmoothingEnabled = false;
  return { canvas, ctx };
}

// 그린 것을 감싸는 가장 작은 상자로 자른다. 결과 자리는 맵 왼쪽 위 기준 그림 px(ART배)다.
function cropped(canvas, box) {
  const { width, height } = canvas;
  const pixels = canvas.getContext("2d").getImageData(0, 0, width, height).data;
  let left = width;
  let top = height;
  let right = -1;
  let bottom = -1;
  for (let y = 0; y < height; y++)
    for (let x = 0; x < width; x++)
      if (pixels[(y * width + x) * 4 + 3] > 0) {
        if (x < left) left = x;
        if (x > right) right = x;
        if (y < top) top = y;
        if (y > bottom) bottom = y;
      }
  if (right < 0) return null;
  const out = document.createElement("canvas");
  out.width = right - left + 1;
  out.height = bottom - top + 1;
  out.getContext("2d").drawImage(canvas, left, top, out.width, out.height, 0, 0, out.width, out.height);
  return { canvas: out, left: Math.round(box.x * ART) + left, top: Math.round(box.y * ART) + top };
}

// 그리기 목록을 box 안에 그린다. 그리기 함수는 (월드 − 카메라) 자리에 그리므로 그만큼 옮겨 둔다.
function drawPieces(holder, pieces, box) {
  const { canvas, ctx } = artCanvas(box);
  for (const { item, camX, camY } of pieces) {
    ctx.setTransform(ART, 0, 0, ART, (camX - box.x) * ART, (camY - box.y) * ART);
    holder.target = ctx;
    item.draw();
  }
  return cropped(canvas, box);
}

function standingPieces(field) {
  const map = field.map;
  const { proxy, holder } = retargetableContext();
  const renderProps = map.renderProps;
  map.renderProps = [];
  try {
    // 성문과 세운 타일 묶음은 카메라와 상관없이 매번 목록 앞에 들어온다(웹게임 renderSorted 순서).
    const gateways = map.structures.filter((s) => s.kind === "gateway");
    const raised = map.assemblies.filter((a) => a.raised);
    const fixed = gateways.length + raised.length;
    const first = captureSorted(field, proxy, 0, 0);
    const uprights = [];
    const gates = [];
    gateways.forEach((s, i) => {
      const box = { x: s.x, y: s.y, w: s.width, h: s.height };
      const piece = drawPieces(holder, [{ item: first[i], camX: 0, camY: 0 }], box);
      if (piece) uprights.push({ ...piece, foot: s.bottom, gate: s.point });
    });
    raised.forEach((a, i) => {
      const bottom = (a.y + a.height) * TILE;
      const box = { x: (a.x - 1) * TILE, y: bottom - (a.height + 3) * TILE, w: (a.width + 2) * TILE, h: (a.height + 3) * TILE + 4 };
      const piece = drawPieces(holder, [{ item: first[gateways.length + i], camX: 0, camY: 0 }], box);
      if (piece) uprights.push({ ...piece, foot: bottom });
    });

    // 세운 타일은 카메라에 보이는 칸만 목록에 들어오므로, 화면 크기로 맵을 훑으며 모은다.
    // 훑는 칸이 서로 겹치므로 칸 자리로 한 번씩만 남기고, 같은 줄의 타일은 그림 한 장으로 합친다.
    const rows = new Map();
    for (let camY = 0; camY < map.h * TILE; camY += VIEW_H)
      for (let camX = 0; camX < map.w * TILE; camX += VIEW_W) {
        const items = captureSorted(field, proxy, camX, camY).slice(fixed);
        const cells = uprightCellsInView(map, camX, camY);
        if (items.length !== cells.length)
          throw new Error(`${map.id}: 세운 타일 수가 다르다(${items.length} ≠ ${cells.length}). UPRIGHT_TILES를 웹게임과 맞춘다`);
        cells.forEach(([x, y], k) => {
          const row = rows.get(y) ?? new Map();
          rows.set(y, row);
          if (!row.has(x)) row.set(x, { item: items[k], camX, camY });
        });
      }
    for (const [y, row] of [...rows.entries()].sort((a, b) => a[0] - b[0])) {
      const xs = [...row.keys()];
      const box = { x: (Math.min(...xs) - 1) * TILE, y: (y + 1) * TILE - 40, w: (Math.max(...xs) - Math.min(...xs) + 3) * TILE, h: 40 };
      const piece = drawPieces(holder, [...row.values()], box);
      if (piece) uprights.push({ ...piece, foot: (y + 1) * TILE });
    }

    // 닫힌 성문: 성문 칸이 gate 타일이면 웹게임이 낙하문을 내려 그린다. 그린 뒤 타일을 되돌린다.
    gateways.forEach((s, i) => {
      const before = s.cells.map(([x, y]) => map.tileAt(x, y));
      for (const [x, y] of s.cells) map.tiles[y * map.w + x] = "gate";
      const items = captureSorted(field, proxy, 0, 0);
      s.cells.forEach(([x, y], k) => (map.tiles[y * map.w + x] = before[k]));
      const box = { x: s.x, y: s.y, w: s.width, h: s.height };
      const piece = drawPieces(holder, [{ item: items[i], camX: 0, camY: 0 }], box);
      if (piece) gates.push({ ...piece, foot: s.bottom, point: s.point });
    });
    return { uprights, gates };
  } finally {
    map.renderProps = renderProps;
  }
}

// 웹게임 renderSorted의 세운 타일 고르기와 같은 순서·조건(그림이 있는지는 밖에서 알 수 없어 개수로 대조한다).
function uprightCellsInView(map, camX, camY) {
  const cells = [];
  for (let y = Math.max(0, Math.floor(camY / TILE)); y < Math.min(map.h, Math.ceil((camY + VIEW_H + 40) / TILE)); y++)
    for (let x = Math.max(0, Math.floor(camX / TILE) - 1); x < Math.min(map.w, Math.ceil((camX + VIEW_W) / TILE) + 1); x++) {
      const id = map.tileAt(x, y);
      if (map.isAssembled(x, y)) continue;
      if (!id || !UPRIGHT_TILES.includes(id)) continue;
      cells.push([x, y]);
    }
  return cells;
}

// 계단 구조물은 인물보다 늘 아래에 그리므로(웹게임 render) 바닥 그림에 그려 넣는다.
// 계단 그림도 모듈 안에 있어서, render를 한 번 돌려 그 자리에 그려지는 drawImage를 붙잡는다.
function groundWithStaircases(field) {
  const map = field.map;
  const ground = document.createElement("canvas");
  ground.width = map.canvas.width;
  ground.height = map.canvas.height;
  const gctx = ground.getContext("2d");
  gctx.imageSmoothingEnabled = false;
  gctx.drawImage(map.canvas, 0, 0);
  const stairs = map.structures.filter((s) => s.kind === "staircase");
  if (!stairs.length) return ground;

  const scratch = document.createElement("canvas");
  scratch.width = VIEW_W;
  scratch.height = VIEW_H;
  const real = scratch.getContext("2d");
  const found = new Map();
  const recorder = new Proxy(
    {},
    {
      get: (_, key) => {
        if (key === "drawImage")
          return (...args) => {
            if (args.length === 5)
              for (const s of stairs)
                if (args[1] === s.x && args[2] === s.y && args[3] === s.width && args[4] === s.height) found.set(s, args[0]);
            return real.drawImage(...args);
          };
        const value = real[key];
        return typeof value === "function" ? value.bind(real) : value;
      },
      set: (_, key, value) => {
        real[key] = value;
        return true;
      },
    },
  );
  const saved = { camX: field.camX, camY: field.camY, shakeX: field.shakeX, shakeY: field.shakeY };
  Object.assign(field, { camX: 0, camY: 0, shakeX: 0, shakeY: 0 });
  try {
    field.render(recorder);
  } finally {
    Object.assign(field, saved);
  }
  for (const s of stairs) {
    const image = found.get(s);
    if (!image) throw new Error(`${map.id}: 계단 그림을 찾지 못했다(${s.point})`);
    gctx.drawImage(image, s.x * ART, s.y * ART, s.width * ART, s.height * ART);
  }
  return ground;
}

// ── 움직이는 타일 ─────────────────────────────────────────

// 웹게임 core/math.ts의 hash2
function hash2(x, y, salt = 0) {
  let h = (x * 374761393 + y * 668265263 + salt * 2147483647) >>> 0;
  h = Math.imul(h ^ (h >>> 13), 1274126177);
  return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
}

// 물 타일의 반짝임 자리(웹게임 renderAnimatedTiles). 지형 윤곽 밖으로 삐져나오는 반짝임은 웹게임처럼 뺀다.
function waterGlints(map) {
  const glints = [];
  for (const a of map.animated()) {
    if (a.kind !== "water") continue;
    const h = hash2(a.x, a.y);
    const px = Math.floor(h * 10) + 2;
    const py = Math.floor(hash2(a.y, a.x) * 10) + 3;
    const wx = a.x * TILE + px;
    const wy = a.y * TILE + py;
    if (!map.waterVisibleAt(wx, wy) || !map.waterVisibleAt(wx + 3, wy)) continue;
    glints.push({ x: wx, y: wy, phase: h * 6 });
  }
  return glints;
}

// 불꽃 한 장. 웹게임 drawFlame은 (시간/5 + x)로 모양을 고르므로 x를 0에 두고 시간을 맞춰 원하는 모양을 그린다.
function flameFrame(field, size, frame) {
  const box = { x: -8, y: -16, w: 16, h: 20 };
  const { canvas, ctx } = artCanvas(box);
  ctx.setTransform(ART, 0, 0, ART, -box.x * ART, -box.y * ART);
  const time = field.time;
  field.time = frame * 5;
  try {
    field.drawFlame(ctx, 0, 0, size);
  } finally {
    field.time = time;
  }
  return canvas;
}

// ── 웹게임 모듈 ───────────────────────────────────────────

// 웹게임 모듈 하나를 불러온다(경로는 games/pandoras 기준). 웹게임 코드는 고치지 않고, 개발 서버의 Turbopack 런타임에
// 작은 모듈을 하나 등록해 그 안에서 import한다. 개발 서버 페이지에서만 된다.
export async function webModule(path) {
  const id = `export-probe:${path}:${Date.now()}`;
  let loaded = null;
  globalThis.TURBOPACK.push([
    `static/chunks/export-probe-${Date.now()}.js`,
    id,
    (context) => {
      loaded = context.i(`[project]/games/pandoras/${path} [app-client] (ecmascript)`);
    },
  ]);
  await globalThis.TURBOPACK.push({ otherChunks: [], runtimeModuleIds: [id] });
  if (!loaded) throw new Error(`웹게임 모듈을 불러오지 못했다: ${path}`);
  return loaded;
}
