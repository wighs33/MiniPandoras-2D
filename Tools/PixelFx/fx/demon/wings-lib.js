// 컷인에서 펼쳐지는 악마의 날개 그리기(사용자 요청 2026-11-05: 웹게임 날개 그림은 가운데 등뼈가 징그럽다). 필드 날개는 wings-field.js에 따로 있다.
// 좌우 날개가 양 어깨에서 따로 뻗는 박쥐 날개다. 몸 쪽 가운데에는 아무것도 그리지 않는다.
// spread 0은 등에 접힌 모습, 1은 다 편 모습이다. 팔과 손가락뼈의 각도만 바뀌고 길이는 그대로라 펴지는 도중도 같은 날개로 보인다.
import { fit, hex } from "../../../ThirdParty/pixelfx/src/core/index.js";

export const PALETTE = ["#07040c", "#140a1f", "#231033", "#35174b", "#4e2068", "#7a2f8e", "#b04aa8", "#ec9ad8"];
const OUTLINE = hex("#050308");

export const colors = (o) => fit(o.palette ?? PALETTE, 8);

const mix = (a, b, t) => a + (b - a) * t;
const toRad = (d) => (d * Math.PI) / 180;

// 각도는 바깥쪽 가로가 0도, 위가 -90도, 아래가 +90도다. [접힘, 편 모습]
// 다 펴면 낮고 넓게 벌어진다. 컷인에서 날개 너머의 병사들을 가리지 않아야 한다.
const UPPER_ARM = { angle: [-82, -18], length: 42 };
const FOREARM = { angle: [-104, -36], length: 56 };
const FINGERS = [
  { angle: [72, 12], length: 108 },
  { angle: [84, 40], length: 118 },
  { angle: [93, 68], length: 112 },
];
const HIP_DROP = 14; // 날개막이 몸에 붙는 자리(어깨에서 아래로). 몸통을 덮지 않게 어깨뼈 언저리에서 끝난다.
const SCALLOP = 0.2; // 손가락 끝 사이의 막이 안쪽으로 파이는 정도
const BACK_SCALLOP = 0.55; // 몸 쪽 막은 깊이 파서 등으로 갈수록 좁아진다

const along = (from, degrees, length, side, k) => ({
  x: from.x + side * Math.cos(toRad(degrees)) * length * k,
  y: from.y + Math.sin(toRad(degrees)) * length * k,
});

/** 뼈대 자리: 어깨, 팔꿈치, 손목, 손가락 끝들, 막이 몸에 붙는 자리. */
function skeleton(shoulder, side, spread, k) {
  const elbow = along(shoulder, mix(UPPER_ARM.angle[0], UPPER_ARM.angle[1], spread), UPPER_ARM.length, side, k);
  const wrist = along(elbow, mix(FOREARM.angle[0], FOREARM.angle[1], spread), FOREARM.length, side, k);
  const tips = FINGERS.map((f) => along(wrist, mix(f.angle[0], f.angle[1], spread), f.length, side, k));
  const hip = { x: shoulder.x, y: shoulder.y + HIP_DROP * k };
  return { shoulder, elbow, wrist, tips, hip };
}

/** 두 점 사이의 막 끝: 손목 쪽으로 파인 곡선. */
function scallop(a, b, wrist, depth = SCALLOP) {
  const middle = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
  const pull = { x: mix(middle.x, wrist.x, depth), y: mix(middle.y, wrist.y, depth) };
  const points = [];
  for (let i = 1; i < 8; i++) {
    const t = i / 8, u = 1 - t;
    points.push({ x: u * u * a.x + 2 * u * t * pull.x + t * t * b.x, y: u * u * a.y + 2 * u * t * pull.y + t * t * b.y });
  }
  return points;
}

/** 손목에서 뻗은 막 한 칸. amount만큼 손목 쪽으로 줄인 모양이라 겹쳐 칠하면 띠가 진다. */
const shrink = (points, center, amount) => points.map((q) => ({ x: mix(center.x, q.x, amount), y: mix(center.y, q.y, amount) }));
const flat = (points) => points.map((q) => [q.x, q.y]);

function membrane(p, C, points, center, base) {
  p.poly(flat(points), C[base]);
  p.poly(flat(shrink(points, center, 0.74)), C[base - 1]);
  p.poly(flat(shrink(points, center, 0.46)), C[base - 2]);
}

function bone(p, from, to, width, color) {
  p.beam(from.x, from.y, to.x, to.y, width, color);
}

/** 날개 한쪽. side는 오른쪽 +1, 왼쪽 -1이다. */
function wing(p, C, side, spread, origin, k) {
  const shoulder = { x: origin.x + side * 11 * k, y: origin.y };
  const s = skeleton(shoulder, side, spread, k);

  // 막은 몸 쪽 칸부터 칠한다. 바깥 칸이 위에 겹친다.
  const inner = [s.wrist, s.tips[2], ...scallop(s.tips[2], s.hip, s.wrist, BACK_SCALLOP), s.hip, s.shoulder, s.elbow];
  membrane(p, C, inner, s.wrist, 3);
  for (let i = 1; i >= 0; i--) {
    membrane(p, C, [s.wrist, s.tips[i], ...scallop(s.tips[i], s.tips[i + 1], s.wrist), s.tips[i + 1]], s.wrist, i === 0 ? 5 : 4);
  }

  // 위 칸: 팔과 첫 손가락 사이가 비지 않게 잇는다.
  p.poly(flat([s.shoulder, s.elbow, s.wrist, s.hip]), C[2]);

  for (const tip of s.tips) bone(p, s.wrist, tip, Math.max(1, 2 * k), C[6]);
  bone(p, s.shoulder, s.elbow, Math.max(2, 3 * k), C[5]);
  bone(p, s.elbow, s.wrist, Math.max(2, 3 * k), C[6]);
  p.line(s.elbow.x, s.elbow.y - 1, s.wrist.x, s.wrist.y - 1, C[7]);
  p.line(s.wrist.x, s.wrist.y, s.tips[0].x, s.tips[0].y, C[7]);

  // 손목의 작은 갈고리.
  const claw = along(s.wrist, mix(-60, -110, spread), 10, side, k);
  p.line(s.wrist.x, s.wrist.y, claw.x, claw.y, C[7]);
}

/** 양쪽 날개. origin은 등 한가운데(어깨 높이)이고 k는 크기 배율이다. */
export function drawWings(p, C, spread, origin, k = 1) {
  wing(p, C, -1, spread, origin, k);
  wing(p, C, 1, spread, origin, k);
}
