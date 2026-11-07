// 단죄참(斷罪斬): 빛의 칼날이 하늘에서 곧게 내리꽂혀 땅에 십자 금을 낸다. 한 칸마다 하나씩 터진다.
import { Particles, Lights, P, Rng, clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, colors } from "./holy-lib.js";

const DURATION = 0.7, DROP = 0.14;

export const meta = {
  name: "Holy smite",
  desc: "A blade of light drops straight down and cracks the ground in a cross.",
  tags: ["holy", "strike", "one-shot"],
  size: [64, 112],
  origin: [32, 96],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 3 },
};

export function create(o) {
  const C = colors(o), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 0, sparks: [C[6], C[5], C[4]] }), lights = new Lights();
  let t = 0, hit = false;
  return {
    update(dt) {
      t += dt;
      if (!hit && t > DROP) {
        hit = true;
        lights.add(0, -8, 38, C[4], 1, 0.3);
        for (let i = 0; i < 18; i++) ps.spawn(P.DUST, R.range(-6, 6), -1, R.sign() * R.range(20, 70), -R.range(10, 50), R.range(0.2, 0.4), R.chance(0.5) ? C[5] : C[3], R.range(1, 2.4));
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      const fall = ease.inCubic(clamp(t / DROP)), fade = 1 - clamp((t - DROP) / (DURATION - DROP));
      const tip = -92 + 92 * fall;
      if (fade > 0.05) {
        const w = 4.5 * fade + 0.6;
        p.poly([[-w, -92], [w, -92], [0, tip + 2]], C[5], 0.95);
        p.beam(0, -92, 0, tip, 1.6, C[6]);
        p.glowLine(0, -92, 0, tip, 9, C[4], 0.5 * fade);
      }
      if (t > DROP) {
        const k = clamp((t - DROP) / 0.2);
        p.line(-20 * k, 0, 20 * k, 0, C[5], fade);
        p.line(0, -7 * k, 0, 7 * k, C[5], fade);
        p.ellipse(0, 0, 13 * k, 5 * k, C[4], 0.45 * fade);
      }
      ps.draw(p);
      lights.draw(p);
    },
  };
}
