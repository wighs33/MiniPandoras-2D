// 차원문(열림): 바닥이 한 줄로 갈라졌다가 둥글게 확 벌어진다(넘쳤다가 자리 잡는다). 둘레의 빛과 먼지가 빨려 들고,
// 다 벌어지는 순간 빛이 번쩍이며 흙먼지가 튄다. 끝나는 순간의 모습이 되풀이(rift-loop)의 첫 모습과 같다.
import { hole, lip, floorGlow, swirl, seeded, colors, PALETTE, SIZE, ORIGIN, RX } from "./rift-lib.js";
import { Particles, Lights, P, Rng, ease, clamp, hex } from "../../../ThirdParty/pixelfx/src/core/index.js";

const DURATION = 0.9;
const OPEN = 0.6;

export const meta = {
  name: "Dimension rift (open)",
  desc: "The floor splits in a line, then tears open past round and settles, pulling light and dust in; a flash and a kick of dust as it opens.",
  tags: ["portal", "rift", "arcane", "ground", "one-shot"],
  size: SIZE,
  origin: ORIGIN,
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 7 },
};

const SWIRL = seeded(14 * 3, 7);
const DUST = hex("#5a5070");

export function create(o) {
  const C = colors(o), R = new Rng(o.seed), floor = () => 0;
  const ps = new Particles({ rng: R, ground: floor, sparks: [C[7], C[6], C[5], C[3]] }), lights = new Lights();
  let t = 0, flashed = false;
  return {
    update(dt) {
      t += dt;
      // 갈라지는 동안 둘레의 빛이 빨려 든다(수명이 짧아 끝날 때는 남지 않는다).
      if (t < OPEN * 0.8 && R.chance(0.6)) {
        const s = R.sign(), D = R.range(RX * 1.3, RX * 2.4), T = R.range(0.16, 0.26), v = (D * 3) / (Math.exp(3 * T) - 1);
        ps.spawn(P.STREAK, s * D, -R.range(0, 3), -s * v, R.range(-2, 2), T, R.chance(0.5) ? C[6] : C[5]);
      }
      if (!flashed && t > OPEN * 0.6) {
        flashed = true;
        lights.add(0, -6, 46, C[4], 0.8, 0.35);
        for (let i = 0; i < 8; i++) ps.spawn(P.DUST, R.sign() * R.range(0.7, 1.1) * RX, -1, R.sign() * R.range(10, 40), -R.range(4, 12), R.range(0.25, 0.32), DUST, R.range(1.5, 2.6));
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      // 되풀이 시간으로는 열림이 끝나는 순간이 0이다(돌기·흔들림이 이어지도록).
      const loopTime = t - DURATION;
      const open = clamp(t / OPEN);
      const rx = RX * (open < 0.08 ? 0.1 : ease.outBack(open, 2.2));
      floorGlow(p, C, rx, open);
      if (open < 0.08) p.line(-RX * 0.6, 0, RX * 0.6, 0, C[6]);
      else {
        hole(p, C, rx, loopTime, 1 + (1 - open) * 0.3);
        lip(p, C, rx, loopTime, 1 + (1 - open) * 0.3);
      }
      if (t > OPEN * 0.5) swirl(p, C, RX, loopTime, SWIRL, 14, clamp((t - OPEN * 0.5) / (DURATION - OPEN * 0.5)));
      ps.draw(p);
      lights.draw(p);
    },
  };
}
