// 유사지옥: 바닥의 모래가 가운데로 빨려 드는 소용돌이. 팔이 셋이라 1/3바퀴 돌면 같은 모습이어서 끊기지 않고 되풀이한다.
import { TAU } from "../../../ThirdParty/pixelfx/src/core/index.js";
import { PALETTE, SHADOW, colors } from "./sand-lib.js";

const PERIOD = 1.2, RX = 40, SY = 0.46;

export const meta = {
  name: "Quicksand",
  desc: "A sand whirlpool on the ground pulling inward; three spiral arms.",
  tags: ["sand", "zone", "loop"],
  size: [96, 56],
  origin: [48, 28],
  duration: PERIOD,
  loop: true,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 4 },
};

export function create(o) {
  const C = colors(o);
  let t = 0;
  return {
    update(dt) {
      t += dt;
    },
    draw(p) {
      const spin = (t / PERIOD) * (TAU / 3), ry = RX * SY;
      p.ellipse(0, 0, RX + 2, ry + 2, C[1], 0.7);
      p.ellipse(0, 0, RX, ry, C[2], 0.95);
      p.ellipse(0, 0, RX * 0.72, ry * 0.72, C[3], 0.95);
      p.ellipse(0, 0, RX * 0.36, ry * 0.36, C[1]);
      p.ellipse(0, 0, RX * 0.2, ry * 0.2, SHADOW);
      for (let i = 0; i < 3; i++) {
        for (let j = 0; j <= 170; j++) {
          const q = 0.16 + 0.82 * (j / 170), th = -spin + (i * TAU) / 3 + 2.4 * Math.log(q);
          const x = Math.cos(th) * q * RX, y = Math.sin(th) * q * ry;
          p.dot(x, y, q > 0.72 ? C[6] : q > 0.45 ? C[5] : C[4]);
          p.dot(x, y + 1, q > 0.45 ? C[4] : C[2], 0.8);
        }
      }
      for (let i = 0; i < 26; i++) {
        const q = 1 - ((i / 26 + t / PERIOD) % 1) * 0.9, th = -spin * 2 + i * 2.4 + 2.4 * Math.log(Math.max(q, 0.1));
        p.dot(Math.cos(th) * q * RX, Math.sin(th) * q * ry, C[6]);
      }
    },
  };
}
