// 차원문(닫힘): 되풀이 한 바퀴가 끝난 자리에서 시작한다. 틈이 잠깐 부풀었다가 휙 감기며 오므라들고, 펑 소리처럼
// 빛과 불꽃이 튄 뒤 바닥에 빛 부스러기가 반짝이다 사라진다. 마지막 프레임은 비어 있다.
import { hole, lip, floorGlow, swirl, seeded, colors, PALETTE, SIZE, ORIGIN, RX } from "./rift-lib.js";
import { Particles, Lights, P, Rng, clamp, WHITE } from "../../../ThirdParty/pixelfx/src/core/index.js";

const SHUT = 0.5;
const DURATION = 1.6;

export const meta = {
  name: "Dimension rift (close)",
  desc: "The rift swells for a breath, whips round and collapses, pulling the light in hard; a pop of light and sparks, then specks twinkle out where it was.",
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

export function create(o) {
  const C = colors(o), R = new Rng(o.seed + 3), floor = () => 0;
  const ps = new Particles({ rng: R, ground: floor, sparks: [C[7], C[6], C[5], C[3]] }), lights = new Lights();
  let t = 0, popped = false, ring = null;
  return {
    update(dt) {
      t += dt;
      if (t < SHUT && R.chance(0.85)) {
        const s = R.sign(), D = R.range(RX * 1.3, RX * 2.4), T = R.range(0.14, 0.24), v = (D * 3) / (Math.exp(3 * T) - 1);
        ps.spawn(P.STREAK, s * D, -R.range(0, 3), -s * v, R.range(-2, 2), T, R.chance(0.5) ? C[6] : C[5]);
      }
      if (!popped && t >= SHUT) {
        popped = true;
        ring = { t0: t, life: 0.35 };
        lights.add(0, -4, 44, C[4], 0.95, 0.45);
        lights.add(0, -2, 24, WHITE, 0.9, 0.12);
        for (let i = 0; i < 16; i++) {
          const a = R.range(-Math.PI, 0), sp = R.range(80, 230);
          ps.spawn(P.SPARK, 0, -1, Math.cos(a) * sp, Math.sin(a) * sp * 0.8, R.range(0.25, 0.55));
        }
        for (let i = 0; i < 12; i++) ps.spawn(P.STAR, R.range(-1, 1) * RX * 0.9, -R.range(0.5, 2), R.range(-4, 4), -R.range(0, 4), R.range(0.5, 0.95), R.pick([C[6], C[5], C[5]]), R.range(0.4, 0.9), R.range(0, 0.2));
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      const k = clamp(t / SHUT);
      if (k < 1) {
        // 잠깐 부풀었다가(10%) 빠르게 오므라든다. 오므라드는 동안 팔이 휙 감긴다.
        const r = RX * (k < 0.35 ? 1 + 0.1 * Math.sin((k / 0.35) * Math.PI * 0.5) : 1.1 * (1 - Math.pow((k - 0.35) / 0.65, 1.6)));
        floorGlow(p, C, Math.max(r, 2), 1 + k * 0.4);
        hole(p, C, r, t, 1 + k * 0.4, 10 * Math.pow(k, 2.2));
        lip(p, C, r, t, 1 + k * 0.4);
        swirl(p, C, RX, t, SWIRL, 14, 1 - k);
      }
      if (ring) {
        const q = clamp((t - ring.t0) / ring.life);
        // 펑과 함께 바닥을 따라 퍼지는 빛 고리
        if (q < 1) p.glowRing(0, 0, 2 + q * RX * 1.6, 2.5, C[5], 0.5 * (1 - q));
      }
      ps.draw(p);
      lights.draw(p);
    },
  };
}
