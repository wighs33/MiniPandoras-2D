// 광귀각성(狂鬼覺醒): 몸 둘레에서 붉은 기운이 터져 오르고, 톱니 같은 충격이 바닥으로 퍼진다.
import { Particles, Lights, P, Rng, TAU, clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors } from "./berserk-lib.js";

const DURATION = 0.9, BURST = 0.12;

export const meta = {
  name: "Berserk awaken",
  desc: "Red fury bursts upward around the body; a jagged shock spreads over the floor.",
  tags: ["berserk", "buff", "one-shot"],
  size: [112, 112],
  origin: [56, 88],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 9 },
};

export function create(o) {
  const C = colors(o), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 0, sparks: [C[7], C[6], C[5]] }), lights = new Lights();
  let t = 0, hit = false;
  return {
    update(dt) {
      t += dt;
      if (!hit && t > BURST) {
        hit = true;
        lights.add(0, -30, 56, C[4], 1, 0.3);
      }
      if (t < 0.6) {
        for (let i = 0; i < 3; i++) ps.spawn(P.DUST, R.range(-18, 18), -R.range(0, 12), R.range(-10, 10), -R.range(50, 130), R.range(0.25, 0.5), R.chance(0.6) ? C[4] : C[3], R.range(1.4, 3));
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      const k = ease.outCubic(clamp((t - BURST) / 0.4)), fade = 1 - clamp((t - 0.45) / 0.45);
      if (t > BURST) {
        const r = 12 + 40 * k;
        for (let i = 0; i < 28; i++) {
          const a0 = (i / 28) * TAU, a1 = ((i + 1) / 28) * TAU, s0 = i % 2 ? 1 : 0.8, s1 = i % 2 ? 0.8 : 1;
          p.line(Math.cos(a0) * r * s0, Math.sin(a0) * r * 0.42 * s0, Math.cos(a1) * r * s1, Math.sin(a1) * r * 0.42 * s1, C[4], fade);
        }
        p.ellipse(0, 0, r * 0.7, r * 0.3, C[2], 0.4 * fade);
      }
      p.glow(0, -30, 30, C[3], 0.4 * clamp(t / 0.1) * fade);
      ps.draw(p);
      lights.draw(p);
    },
  };
}
