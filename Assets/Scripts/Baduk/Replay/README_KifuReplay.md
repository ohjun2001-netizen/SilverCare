# 바둑 복기 모드 (NPC 훈수꾼) — 설정 안내

기보를 자동/수동 재생하면서 NPC 노인 아바타가 옆에서 훈수를 두는 모드입니다.
교수님 1차 피드백 ("VR 탑골공원, 옆에서 훈수 두는 분위기") 반영.

## 1. 파일 구성

```
Assets/Scripts/Baduk/Replay/
├── KifuData.cs                # 데이터 모델 (Kifu, KifuMove, NpcCommentPool)
├── KifuLoader.cs              # JSON 로드
├── KifuReplayManager.cs       # 자동/수동 재생 로직
├── NpcCommentator.cs          # 수마다 코멘트 선택 + TTS
├── NpcAvatarSpawner.cs        # 아바타 3명 배치 (placeholder 또는 prefab)
├── KifuVRUI.cs                # World Space Canvas UI
└── KifuReplayGameManager.cs   # 전체 조율

Assets/Resources/Data/
├── kifu_sample.json           # 샘플 기보 1개 (9x9, 29수)
└── npc_comments.json          # 훈수 코멘트 풀
```

## 2. 씬 세팅 (Unity Editor)

**옵션 A — 새 씬 만들기 (권장)**

1. `File → New Scene` → 빈 씬 생성, 이름 `BadukReplay`로 저장 (`Assets/Scenes/`)
2. 빈 GameObject 생성, 이름 `BadukReplaySystem`
3. 다음 컴포넌트를 **모두 같은 GameObject**에 추가 (이름 그대로 검색):
   - `BadukBoard` (기존 사활문제용 재사용)
   - `KifuLoader`
   - `KifuReplayManager`
   - `NpcCommentator`
   - `NpcAvatarSpawner`
   - `KifuVRUI`
   - `KifuReplayGameManager`
   - `BadukVRBoardSetup` (XR Interaction Toolkit 설치된 경우)
4. XR Origin (또는 XR Rig) 추가 — 기존 Baduk 씬에서 복사 가능
5. Camera에 `MainCamera` 태그 확인
6. `File → Build Settings` 에 새 씬 추가
7. `GameSceneManager.cs` 의 씬 이름 상수 추가 (선택):
   ```csharp
   public const string SCENE_BADUK_REPLAY = "BadukReplay";
   ```

**옵션 B — 기존 Baduk 씬 복제 후 컴포넌트 교체**

1. `Baduk` 씬 복제 → `BadukReplay` 로 이름 변경
2. 기존 `BadukGameManager` 등 사활문제 전용 컴포넌트 제거
3. 위 컴포넌트 7개 추가

## 3. 동작 확인

씬 실행 → 카메라 앞에 **기보 선택 패널** 표시 → 기보 클릭 → 자동 재생 시작.
- 아바타 3명 (흑/백 대국자, 훈수꾼) 보드 주변에 배치됨
- 매 수마다 약 40% 확률로 NPC 코멘트 자막 + TTS 발화
- 컨트롤: ▶재생/⏸일시정지, ◀이전수, 다음수▶, ↺처음부터, 0.5x/1x/2x 속도

## 4. 콘텐츠 추가 방법

### 새 기보 추가
`Assets/Resources/Data/kifu_sample.json` 의 `kifus` 배열에 항목 추가:
```json
{
  "id": "lee_alphago_g4",
  "title": "이세돌 vs 알파고 4국",
  "black_player": "이세돌",
  "white_player": "알파고",
  "date": "2016-03-13",
  "board_size": 19,
  "view_area": [0, 0, 18, 18],
  "description": "이세돌 9단의 신의 한 수가 나온 4국입니다.",
  "moves": [
    { "row": 3, "col": 15, "color": "black", "comment": "" },
    ...
  ],
  "result": "흑 불계승"
}
```
`total` 값도 같이 갱신.

> SGF 파일을 JSON으로 변환하는 스크립트는 추후 추가 예정.
> 임시로 기보 사이트 (gokifu.com 등)에서 좌표 보고 수동 입력 가능.

### 코멘트 추가
`Assets/Resources/Data/npc_comments.json` 의 각 카테고리에 문자열 추가:
- `opening` / `midgame` / `endgame`: 게임 단계별
- `good_move` / `bad_move`: 평가성 멘트 (15% 확률로 우선 발화)
- `idle`: 추임새 폴백

### NPC 아바타 3D 모델 교체
`NpcAvatarSpawner` 컴포넌트의 Inspector:
- `Black Player Prefab`, `White Player Prefab`, `Commentator Prefab` 슬롯에 prefab 할당
- 비워두면 placeholder 캡슐+구체 사용
- 좌석 위치도 Inspector에서 조정 가능

## 5. 알려진 제약 / 다음 단계

- 아바타가 돌을 직접 잡고 놓는 손 모션 X (돌은 보드 위에 그냥 등장)
- 형세 평가 엔진 (KataGo) 미연동 — 코멘트는 단계/확률 기반 랜덤
- 호선/접바둑 구분 없음 (kifu의 첫 수가 흑이라고 가정)
- TTS 서버 (`localhost:8000`) 미실행 시 자막만 표시되고 음성 X (콘솔 로그로 폴백)
