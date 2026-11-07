// 라의 심판: 모래 기둥이 땅을 찢고 치솟았다가 터져 흩어진다.
import { Particles, Lights, P, Rng, ease, clamp } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors, ring } from "./sand-lib.js";

const DURATION = 1.3, RISE = 0.28, BLAST = 0.75;

export const meta = {
  name: "Sand pillar",
  desc: "A column of sand tears out of the ground, then blasts apart.",
  tags: ["sand", "pillar", "one-shot"],
  size: [96, 128],
  origin: [48, 112],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 9 },
};

export function create(o) {
  const C = colors(o), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 0, sparks: [C[7], C[6], C[5]] }), lights = new Lights();
  let t = 0, risen = false, blasted = false;
  return {
    update(dt) {
      t += dt;
      if (!risen && t > RISE * 0.6) {
        risen = true;
        lights.add(0, -30, 60, C[5], 0.8, 0.35);
        for (let i = 0; i < 24; i++) ps.spawn(P.DUST, R.range(-16, 16), -1, R.sign() * R.range(20, 70), -R.range(4, 30), R.range(0.3, 0.5), C[4], R.range(1.5, 3));
      }
      if (!blasted && t > BLAST) {
        blasted = true;
        lights.add(0, -60, 70, C[6], 1, 0.3);
        for (let i = 0; i < 70; i++) {
          const y = -R.range(0, 100);
          ps.spawn(P.DUST, R.range(-14, 14), y, R.sign() * R.range(20, 120), -R.range(-20, 50), R.range(0.25, 0.5), R.chance(0.5) ? C[4] : C[5], R.range(1, 3));
        }
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      ring(p, C, 30 * ease.outCubic(clamp(t / RISE)), t * 8, 1 - clamp((t - BLAST) / 0.5));
      const h = 104 * ease.outCubic(clamp(t / RISE)), fade = clamp((t - BLAST) / 0.2);
      if (fade < 1) {
        const top = h * (1 - fade * 0.2), wide = 1 - fade;
        for (let y = 0; y < top; y++) {
          const k = y / Math.max(1, top), w = (15 - 6 * k + Math.sin(y * 0.35 - t * 26) * 3) * wide;
          const band = Math.floor((y + t * 90) / 7) % 3;
          p.rect(-w, -y, w * 2, 1, band === 0 ? C[3] : band === 1 ? C[4] : C[2]);
          p.rect(-w + w * 0.4, -y, w * 0.5, 1, C[5]);
          p.rect(w - 1.5, -y, 1.5, 1, C[1]);
        }
        p.glow(0, -top * 0.5, 34, C[5], 0.35 * wide);
      }
      ps.draw(p);
      lights.draw(p);
    },
  };
}
