// 차원문(되풀이): 바닥에 열린 틈이 천천히 돌며 빛 점을 빨아들이고, 틈에서 옅은 빛 알갱이가 피어오른다.
// 한 바퀴(PERIOD)가 끝나면 처음과 같은 모습이라 끝없이 이어 틀 수 있다. 그리기 재료는 rift-lib.js.
import { hole, lip, floorGlow, swirl, seeded, colors, phaseAt, PALETTE, SIZE, ORIGIN, PERIOD, RX } from "./rift-lib.js";
import { TAU } from "../../../ThirdParty/pixelfx/src/core/index.js";

export const meta = {
  name: "Dimension rift (loop)",
  desc: "A rift lies open on the floor, its three arms of light turning slowly into the dark; specks of light spiral in and faint motes rise out of it.",
  tags: ["portal", "rift", "arcane", "ground", "loop"],
  size: SIZE,
  origin: ORIGIN,
  duration: PERIOD,
  loop: true,
  fps: 30,
  bg: "ground",
  options: { palette: PALETTE, seed: 7 },
};

const SWIRL = seeded(14 * 3, 7);
const RISE = seeded(8 * 3, 19);

export function draw(p, t, o) {
  const C = colors(o);
  floorGlow(p, C, RX, 1 + 0.15 * Math.sin(phaseAt(t)));
  hole(p, C, RX, t);
  lip(p, C, RX, t);
  swirl(p, C, RX, t, SWIRL);
  // 틈에서 피어오르는 빛 알갱이: 한 바퀴에 한 번씩 오르며 흐려진다.
  for (let i = 0; i < 8; i++) {
    const f = ((t / PERIOD + RISE[i * 3]) % 1 + 1) % 1, x0 = (RISE[i * 3 + 1] - 0.5) * RX * 1.2;
    const x = x0 + Math.sin(f * TAU + i) * 2, y = -2 - f * 26;
    p.dot(x, y, f < 0.4 ? C[6] : C[5], 1 - f);
    p.light(x, y, C[4], 0.3 * (1 - f));
  }
}
