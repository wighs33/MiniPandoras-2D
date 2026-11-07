# Mini Pandoras

직접 만든 웹게임 판도라즈 1부를 Unity로 옮긴 탑뷰 2D 액션 RPG입니다. 휴대폰 가로 화면을 기준으로 하며, 0장부터 11장과 엔딩까지 끝까지 플레이할 수 있습니다.

- 포트폴리오 페이지: https://hyeonsik-game-portfolio.vercel.app/projects/minipandoras/
- 환경: Unity 6000.3.25f1, URP 2D Renderer, Input System, Test Framework
- 개발: 1인 개발. 코드는 Claude Code와 함께 작성했습니다.

## 저장소에 있는 것

이 저장소에는 코드, 게임 데이터, 도구, 문서가 있습니다. 그림, 맵, 씬, 프리팹, 외부 에셋은 올리지 않아서 이 저장소만으로는 게임을 실행할 수 없습니다.

| 경로 | 내용 |
|---|---|
| `Assets/MiniPandoras/Scripts` | 게임 코드. 폴더가 곧 네임스페이스입니다 |
| `Assets/MiniPandoras/Data` | 대사, 수치, 표 같은 게임 내용 |
| `Assets/MiniPandoras/Editor` | 맵과 그림을 들여오는 임포터 |
| `Assets/MiniPandoras/Tests` | EditMode, PlayMode 테스트 |
| `Tools/PixelGen` | 맵, 소품, 몬스터, 컷인을 코드로 그리는 Python 생성기 |
| `Tools/PixelFx` | 코드로 그리는 도트 이펙트 |
| `Tools/WebGame` | 웹게임에서 데이터와 그림을 뽑는 도구 |
| `Docs/ARCHITECTURE.md` | 코드 구조와 결정 기록 |
| `Docs/ROADMAP.md` | 단계별 계획 |

## 코드 구조

네임스페이스는 층을 이룹니다. 아래 층은 위 층을 모르고, `NamespaceLayerTests`가 이 규칙을 검사합니다.

```text
Dialogue
  → Input · World · Progress
  → Characters
  → Combat
  → Player · Minigames
  → Field · UI
  → Controls
  → Story · App
```

## 게임 내용

코드에는 규칙과 연출만 있고, 내용은 데이터로 둡니다. 내용을 고치는 데 컴파일이 필요 없습니다.

| 내용 | 위치 |
|---|---|
| 대사와 문구 | `Assets/MiniPandoras/Data/Resources/Text/*.txt` (말투, 화자, 이름표, 글) |
| 스킬, 적, 판도라, 인물 능력치, 보스 패턴, 몬스터 서식지 | `Assets/MiniPandoras/Data/Battle` |
| 물건 | `Assets/MiniPandoras/Data/Items` |
| 전역 밸런스, 의뢰 | `Assets/MiniPandoras/Data/Resources` |

에셋은 컴파일러가 검사하지 못하므로 `GameDataTests`와 `GameTextTests`가 빠진 칸과 끊긴 id를 잡습니다.

## 테스트

Unity Test Runner로 실행합니다. EditMode 80개는 공식과 규칙을, PlayMode 114개는 Field 장면에서 장별 이야기와 전투, 메뉴를 확인합니다.

## 외부 자료

사용한 외부 자료와 라이선스는 `Docs/THIRD_PARTY.md`에 있습니다.
