// 웹게임 전투 공식이 낸 값을 JSON으로 뽑는다. 전투 수치의 정본은 Data/Battle의 에셋이고, 이 값은 테스트가 대조하는 기대값이다.
// battle/data.ts는 TypeScript 모듈이라 Node(23.6 이상)가 타입만 지우고 그대로 불러온다. 수치를 손으로 옮기지 않기 위해서다.
//
// 사용: node Tools/WebGame/export_battle.mjs <웹게임 games/pandoras 폴더> <프로젝트 루트>
//   Assets/MiniPandoras/Tests/EditMode/Fixtures/battle_formulas.json   공식 테스트가 대조하는 웹게임 계산 결과
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { pathToFileURL } from "node:url";

const [gameDir, projectDir] = process.argv.slice(2).map((p) => resolve(p));
if (!gameDir || !projectDir) {
  console.error("사용: node export_battle.mjs <games/pandoras> <프로젝트 루트>");
  process.exit(1);
}

const load = (file) => import(pathToFileURL(join(gameDir, file)).href);
const data = await load("battle/data.ts");

const MEMBERS = ["hero", "rang", "diel", "dielYoung", "largo", "buddy", "isabel", "aurora", "erus", "hass"];

// 공식 대조용: 동료마다 장(레벨) 1~8, 깨어남 전후의 능력치와, 정해 둔 난수로 낸 피해량.
const memberStats = [];
for (const id of MEMBERS)
  for (let level = 1; level <= 8; level++)
    for (const awakened of [false, true])
      memberStats.push({ id, level, awakened, ...data.memberStats(id, level, awakened) });

const damage = [];
for (const [atk, def, power] of [
  [10, 16, 1],
  [11, 6, 1.25],
  [12, 7, 1.45],
  [16, 8, 2.3],
  [25, 80, 1],
  [46, 10, 2.2],
  [30, 17, 0],
])
  for (const roll of [0, 0.25, 0.5, 0.75, 0.999])
    damage.push({ atk, def, power, roll, result: data.computeDamage(atk, def, power, () => roll) });

writeJson(join(projectDir, "Assets/MiniPandoras/Tests/EditMode/Fixtures/battle_formulas.json"), {
  memberStats,
  damage,
});

function writeJson(path, value) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, JSON.stringify(value, null, 2) + "\n", "utf8");
  console.log("wrote", path);
}
