// 열사폭풍: 오른쪽으로 쓸고 나가는 모래 벽. 앞머리가 말려 들고 알갱이가 수평으로 흐른다. 한 바퀴를 이어 되풀이한다.
import { TAU } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors } from "./sand-lib.js";

const PERIOD = 0.6, W = 40, H = 36;

export const meta = {
  name: "Sand wave",
  desc: "A wall of sand rolling to the right; grains stream sideways.",
  tags: ["sand", "wave", "loop"],
  size: [80, 56],
  origin: [40, 46],
  duration: PERIOD,
  loop: true,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 3 },
};

export function create(o) {
  const C = colors(o);
  let t = 0;
  return {
    update(dt) {
      t += dt;
    },
    draw(p) {
      const ph = (t / PERIOD) * TAU;
      for (let x = -W + 6; x < W - 12; x++) {
        const k = (x + W) / (W * 2), crest = H * Math.pow(k, 0.7) * (0.7 + 0.12 * Math.sin(x * 0.2 - ph) + 0.06 * Math.sin(x * 0.45 + ph * 2));
        for (let y = 0; y < crest; y++) {
          const band = Math.sin(y * 0.55 + x * 0.25 - ph * 2);
          p.rect(x, -y, 1, 1, band > 0.45 ? C[5] : band > -0.2 ? C[4] : band > -0.7 ? C[3] : C[2]);
        }
        p.rect(x, -crest, 1, 1, C[6]);
      }
      for (let i = 0; i < 44; i++) {
        const row = (i * 17) % 34, speed = 2 + (i % 3), x = -W + (((i * 13) % 80) + (t / PERIOD) * 80 * speed) % 80;
        p.line(x, -row, x + 3, -row, i % 4 ? C[5] : C[6], 0.85);
      }
      p.glowLine(W - 16, 0, W - 16, -H * 0.7, 10, C[5], 0.25);
    },
  };
}
