// 레벨 업: 발밑에서 금빛 고리가 번지고, 몸 둘레로 빛줄기와 별이 솟아오른 뒤 머리 위에서 흩어진다.
// 빛만으로는 무슨 일인지 모르니 머리 위에 「LEVEL UP!」 글자를 띄운다. 빛에 가리지 않게 글자를 맨 나중에 그린다.
// 인물을 가리지 않도록 몸 가운데는 비워 둔다.
import { Particles, Lights, P, Rng, TAU, hex, fit, clamp, ease } from "../../../ThirdParty/pixelfx/src/core/index.js";

const PALETTE = ["#5a3a10", "#a8741c", "#e6b23c", "#ffdf70", "#fff3b0", "#7fe8e0", "#d8fff4", "#ffffff"];
const DURATION = 1.8, RISE = 0.9, HEIGHT = 56, LIGHT_END = 1.4;
const TEXT = "LEVEL UP!", FONT = { font: "big", scale: 2 }, TEXT_TOP = -74, TEXT_FLOAT = 7;
const POP = 0.18, STAGGER = 0.04, SHINE_AT = 0.6, SHINE_TIME = 0.3, TEXT_FADE = 1.45;
const TEXT_LIGHT = hex("#fff6c8"), TEXT_GOLD = hex("#ffd24a"), TEXT_DEEP = hex("#f09a24"), TEXT_SHINE = hex("#ffffff"), TEXT_EDGE = hex("#4a2408");

export const meta = {
  name: "Level up",
  desc: "A golden ring spreads from the feet, light and stars rise around the body, and LEVEL UP! pops up overhead.",
  tags: ["level", "buff", "one-shot"],
  size: [132, 128],
  origin: [66, 104],
  duration: DURATION,
  loop: false,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 11 },
};

const star = (p, x, y, r, c, a) => {
  p.line(x - r, y, x + r, y, c, a);
  p.line(x, y - r, x, y + r, c, a);
  p.dot(x, y, 0xffffffff, a);
};

// 글자가 하나씩 튀어 오르고, 빛이 한 번 훑고 지나간 뒤 위로 떠오르며 사라진다.
const levelUpText = (p, t) => {
  const total = p.measure(TEXT, FONT)[0], left = -total / 2, fade = 1 - clamp((t - TEXT_FADE) / (DURATION - TEXT_FADE));
  const top = TEXT_TOP - TEXT_FLOAT * clamp((t - SHINE_AT) / (DURATION - SHINE_AT)), shine = left - 8 + clamp((t - SHINE_AT) / SHINE_TIME) * (total + 16);
  for (let i = 0; i < TEXT.length; i++) {
    const k = clamp((t - i * STAGGER) / POP);
    if (k <= 0 || TEXT[i] === " ") continue;
    const x = left + p.measure(TEXT.slice(0, i), FONT)[0], y = top + (1 - ease.outBack(k, 3)) * 14, a = fade * clamp(k * 3);
    const colour = (gx, gy) => (Math.abs(x + gx * FONT.scale - shine) < 3 && t < SHINE_AT + SHINE_TIME ? TEXT_SHINE : gy < 2 ? TEXT_LIGHT : gy < 5 ? TEXT_GOLD : TEXT_DEEP);
    p.text(TEXT[i], x, y + 2, TEXT_EDGE, { ...FONT, outline: TEXT_EDGE, a });
    p.text(TEXT[i], x, y, colour, { ...FONT, outline: TEXT_EDGE, a });
  }
};

export function create(o) {
  const C = fit(o.palette ?? PALETTE, 8), R = new Rng(o.seed);
  const ps = new Particles({ rng: R, ground: () => 8, sparks: [C[7], C[4], C[5]] }), lights = new Lights();
  const stars = Array.from({ length: 16 }, (_, i) => ({ a: (i / 16) * TAU * 3, start: i * 0.05, speed: R.range(0.75, 1.1), size: i % 3 === 0 ? 3 : 2 }));
  let t = 0, burst = false;
  return {
    update(dt) {
      t += dt;
      if (t < RISE && R.chance(0.9)) {
        const s = R.sign(), x = s * R.range(15, 25);
        ps.spawn(P.STREAK, x, -R.range(0, 14), 0, -R.range(70, 130), R.range(0.3, 0.5), R.chance(0.6) ? C[3] : C[5]);
      }
      if (!burst && t > RISE * 0.7) {
        burst = true;
        lights.add(0, -HEIGHT, 40, C[3], 0.8, 0.35);
        for (let i = 0; i < 26; i++) {
          const a = R.range(0, TAU), v = R.range(20, 80);
          ps.spawn(P.DUST, 0, -HEIGHT, Math.cos(a) * v, Math.sin(a) * v * 0.7 - 10, R.range(0.3, 0.6), R.chance(0.5) ? C[4] : C[6], R.range(1, 2.2));
        }
      }
      ps.update(dt);
      lights.update(dt);
    },
    draw(p) {
      const spread = ease.outCubic(clamp(t / 0.5)), fade = 1 - clamp((t - 0.8) / (LIGHT_END - 0.8));
      const rx = 12 + 22 * spread, ry = rx * 0.42;
      for (let i = 0; i < 80; i++) {
        const a = (i / 80) * TAU + t * 2, wave = i % 8 < 4;
        p.dot(Math.cos(a) * rx, Math.sin(a) * ry, wave ? C[4] : C[2], fade);
        p.dot(Math.cos(a) * rx * 0.72, Math.sin(a) * ry * 0.72, C[3], 0.7 * fade);
      }
      p.glow(0, 0, rx, C[3], 0.3 * fade);
      // 몸 둘레로 빛이 차오른다. 양쪽 기둥이 위로 갈수록 가늘어진다.
      const top = -HEIGHT * ease.outCubic(clamp(t / 0.6));
      for (const side of [-1, 1]) {
        p.glowLine(side * 20, 0, side * 12, top, 13, C[3], 0.5 * fade);
        p.beam(side * 20, 0, side * 13, top * 0.8, 1.6, C[4], 0.55 * fade);
      }
      if (t < 0.25) p.glow(0, -30, 36, C[4], 0.7 * (1 - t / 0.25));
      for (const s of stars) {
        const k = clamp((t - s.start) / (RISE * s.speed));
        if (k <= 0 || k >= 1) continue;
        const a = s.a + k * 3, r = 24 - 8 * k;
        const near = Math.sin(a) > 0;
        star(p, Math.cos(a) * r, -k * HEIGHT + Math.sin(a) * 7, s.size, near ? C[7] : C[5], (near ? 1 : 0.6) * (1 - k * 0.4));
      }
      ps.draw(p);
      lights.draw(p);
      levelUpText(p, t);
    },
  };
}
