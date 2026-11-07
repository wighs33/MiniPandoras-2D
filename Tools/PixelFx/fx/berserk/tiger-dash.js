// 맹호돌격(猛虎突擊): 몸을 던져 오른쪽으로 내달린다. 붉은 속도선 끝에서 쌍검이 호랑이 발톱처럼 내리찍힌다.
import { Particles, Lights, P, Rng, clamp } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors, slash } from "./berserk-lib.js";

const DURATION = 0.55, RUSH = 0.16;

export const meta = {
  name: "Tiger dash",
  desc: "A rushing red streak ends in a tiger-claw slam of twin blades.",
  tags: ["berserk", "dash", "one-shot"],
  size: [128, 72],
  origin: [64, 48],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 5 },
};

export function create(o) {
  const C = colors(o), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 8, sparks: [C[7], C[6], C[5]] }), lights = new Lights();
  let t = 0, hit = false;
  return {
    update(dt) {
      t += dt;
      if (!hit && t > RUSH) {
        hit = true;
        lights.add(36, -16, 34, C[4], 0.9, 0.2);
        for (let i = 0; i < 16; i++) ps.spawn(P.DUST, 36, -R.range(2, 30), R.range(0, 90), R.range(-40, 30), R.range(0.14, 0.3), R.chance(0.5) ? C[6] : C[4], R.range(1, 2.2));
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      const k = clamp(t / RUSH), fade = 1 - clamp((t - RUSH) / (DURATION - RUSH));
      for (let i = 0; i < 9; i++) {
        const y = -4 - i * 4, x1 = -52 + 88 * k, x0 = x1 - 34 - ((i * 17) % 40);
        p.line(x0, y, x1, y, i % 3 ? C[4] : C[6], 0.9 * fade);
      }
      if (t > RUSH) {
        const c = clamp((t - RUSH) / 0.08);
        for (const dx of [0, 11, 22]) slash(p, C, 26 + dx, -34, 34 + dx, 2, 5, c, fade);
      }
      ps.draw(p);
      lights.draw(p);
    },
  };
}
