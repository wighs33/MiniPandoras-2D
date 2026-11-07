// 필드에서 카일의 등에 붙는 천사의 날개. 첫 장은 접은 모습, 나머지 여덟 장은 천천히 날갯짓하는 반복이다. 왼쪽을 보는 몸 기준이라 등 쪽(오른쪽)으로 뻗는다.
import { colors, drawSideWings, outline, PALETTE } from "./wings-lib.js";

const WIDTH = 460, HEIGHT = 420, BODY_X = 70;
const SHOULDER_FROM_TOP = 240; // CharacterAnimator.AngelWingRoot(논리 px)의 네 배와 같아야 한다.
const BACK_OFFSET = 14; // 몸 가운데에서 등까지
const SCALE = 0.78;
const FPS = 8, FLAP_FRAMES = 8;

export const meta = {
  name: "Angel wings (field)",
  desc: "Side-view feathered wings for the field sprite: one folded frame, then a slow flap loop.",
  tags: ["angel", "wings", "sprite"],
  size: [WIDTH, HEIGHT],
  origin: [BODY_X, HEIGHT],
  duration: FLAP_FRAMES / FPS,
  loop: false,
  fps: FPS,
  bg: "ground",
  options: { palette: PALETTE, seed: 3 },
};

export function create(o) {
  const C = colors(o);
  const origin = { x: BACK_OFFSET, y: SHOULDER_FROM_TOP - HEIGHT };
  let t = 0;
  return {
    update(dt) {
      t += dt;
    },
    draw(p) {
      const frame = Math.round(t * FPS);
      if (frame === 0) {
        drawSideWings(p, C, 0, 0, origin, SCALE);
      } else {
        const phase = ((frame - 1) / FLAP_FRAMES) * Math.PI * 2;
        drawSideWings(p, C, 0.95 + 0.05 * Math.sin(phase), 7 * Math.sin(phase - 1), origin, SCALE);
      }
      outline(p, 2);
    },
  };
}
