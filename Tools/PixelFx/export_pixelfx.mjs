// pixelfx(https://github.com/patch-gg/pixelfx, MIT) 효과를 Unity가 읽을 스프라이트 시트로 뽑는다.
//
// pixelfx의 시트 내보내기(render.mjs --sheet)는 빛 층을 색 층에 섞어 한 장으로 만든다. 그러면 빛이 반투명한 색으로 바뀌어
// 약해지고, 회상 색 위에서 빛만 원래 색으로 그릴 수도 없다. 그래서 프레임마다 색 버퍼와 빛 버퍼를 따로 받아 두 장으로 쓴다.
// 게임은 색 시트를 보통 스프라이트로, 빛 시트를 더해 그리는 재질(MiniPandoras/Sprite Additive)로 겹쳐 그린다.
//
// 뽑을 효과는 같은 폴더의 effects.json에 적는다: { name, id, options(팔레트·seed …), impact(빛이 닿는 때, 초), dir, colorOnly, pixelsPerUnit }.
// colorOnly면 빛 시트를 쓰지 않는다(빛을 그리지 않는 그림). pixelsPerUnit은 한 칸에 든 점 수다(비우면 pixelfx대로 16, 인물 그림과 같은 고운 점이면 64).
// dir이 있으면 pixelfx 저장소가 아니라 이 폴더 아래(fx)의 우리 효과다(pixelfx 그리기 재료로 직접 만든 것).
//
// 사용: node Tools/PixelFx/export_pixelfx.mjs <pixelfx 폴더> <프로젝트 루트> [이름 …]
//   Assets/MiniPandoras/Art/PixelFx/<name>/color.png   색 층(프레임 격자)
//   Assets/MiniPandoras/Art/PixelFx/<name>/light.png   빛 층(같은 격자, 알파는 빛이 있는 곳만 255)
//   Assets/MiniPandoras/Art/PixelFx/<name>/effect.json 프레임 크기·기준점·fps·반복·빛이 닿는 때
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const [pixelfxDir, projectDir, ...only] = process.argv.slice(2);
if (!pixelfxDir || !projectDir) {
  console.error("사용: node export_pixelfx.mjs <pixelfx 폴더> <프로젝트 루트> [이름 …]");
  process.exit(1);
}

const lib = (file) => import(pathToFileURL(join(resolve(pixelfxDir), file)).href);
const { loadEffect, renderFrames } = await lib("tools/lib/fxlib.mjs");
const { encodePNG } = await lib("tools/lib/png.mjs");

const here = dirname(fileURLToPath(import.meta.url));
const list = JSON.parse(readFileSync(join(here, "effects.json"), "utf8"));
const outRoot = join(resolve(projectDir), "Assets/MiniPandoras/Art/PixelFx");

for (const entry of list.filter((e) => only.length === 0 || only.includes(e.name))) {
  const fx = await loadEffect(entry.id, ...(entry.dir ? [join(here, entry.dir)] : []));
  // 배경 없이(bg none) 효과만. 프레임 가장자리의 빛은 pixelfx가 부드럽게 줄여 준다.
  const r = renderFrames(fx, { bg: "none", actor: false, options: entry.options ?? {} });
  const { w, h, n, fps, frames } = r;
  const columns = Math.min(n, Math.max(1, Math.ceil(Math.sqrt((n * h) / w))));
  const rows = Math.ceil(n / columns);
  const color = new Uint8ClampedArray(columns * w * rows * h * 4);
  const light = new Uint8ClampedArray(color.length);
  frames.forEach((frame, index) => {
    const left = (index % columns) * w;
    const top = Math.floor(index / columns) * h;
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        const from = y * w + x;
        const to = ((top + y) * columns * w + left + x) * 4;
        // pixelfx 버퍼는 한 칸이 32비트 ABGR이다.
        const c = frame.raw[from];
        color[to] = c & 255;
        color[to + 1] = (c >>> 8) & 255;
        color[to + 2] = (c >>> 16) & 255;
        color[to + 3] = c >>> 24;
        const l = frame.light[from] & 0xffffff;
        light[to] = l & 255;
        light[to + 1] = (l >>> 8) & 255;
        light[to + 2] = (l >>> 16) & 255;
        light[to + 3] = l ? 255 : 0;
      }
    }
  });

  const dir = join(outRoot, entry.name);
  mkdirSync(dir, { recursive: true });
  writeFileSync(join(dir, "color.png"), encodePNG(columns * w, rows * h, color));
  if (!entry.colorOnly) writeFileSync(join(dir, "light.png"), encodePNG(columns * w, rows * h, light));
  const info = {
    id: entry.id,
    frameSize: [w, h],
    // 기준점: 프레임 왼쪽 위에서 잰 px(효과의 원점, 보통 발밑).
    pivot: fx.meta.origin,
    fps,
    loop: fx.meta.loop,
    frames: n,
    columns,
    impact: entry.impact ?? 0,
    pixelsPerUnit: entry.pixelsPerUnit ?? 16,
  };
  writeFileSync(join(dir, "effect.json"), JSON.stringify(info, null, 2));
  console.log(`${entry.name}  ${entry.id}  ${n} frames ${w}x${h} @${fps}  -> ${dir}`);
}
