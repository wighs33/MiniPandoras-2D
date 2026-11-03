// 웹게임 맵의 칸 격자(world/maps.ts의 rows)를 칸마다 타일 이름으로 풀어 JSON으로 뽑는다.
// 바닥 그림을 코드로 다시 그리는 도구(Tools/PixelGen)가 어느 칸이 풀·길·물·절벽인지 알려고 쓴다.
// maps.ts는 타입만 가져오는 모듈이라 Node(23.6 이상)가 타입을 지우고 그대로 불러온다(export_battle.mjs와 같다).
//
// 사용: node Tools/WebGame/export_tiles.mjs <웹게임 games/pandoras 폴더> <프로젝트 루트>
//   Assets/MiniPandoras/Art/WebGame/Maps/<id>/tiles.json   { id, width, height, tiles: [줄마다 타일 이름 배열] }
import { existsSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";
import { pathToFileURL } from "node:url";

const [gameDir, projectDir] = process.argv.slice(2).map((p) => resolve(p));
if (!gameDir || !projectDir) {
  console.error("사용: node export_tiles.mjs <games/pandoras> <프로젝트 루트>");
  process.exit(1);
}

const { MAPS, THEMES } = await import(pathToFileURL(join(gameDir, "world/maps.ts")).href);
const mapsDir = join(projectDir, "Assets/MiniPandoras/Art/WebGame/Maps");

for (const [id, def] of Object.entries(MAPS)) {
  const folder = join(mapsDir, id);
  if (!existsSync(folder)) continue;
  const legend = THEMES[def.theme];
  // 지점 표시 문자(marks)는 그 자리를 적어 둔 뒤 정해 둔 타일 문자로 바뀐다(웹게임과 같다).
  const tiles = def.rows.map((row) =>
    [...row].map((ch) => {
      const plain = def.marks?.[ch]?.tile ?? ch;
      const entry = legend[plain];
      if (entry === undefined) return "void";
      return typeof entry === "string" ? entry : entry.tile;
    }),
  );
  const width = Math.max(...def.rows.map((row) => row.length));
  writeFileSync(join(folder, "tiles.json"), JSON.stringify({ id, width, height: def.rows.length, tiles }));
  console.log(`${id} ${width}x${def.rows.length}`);
}
