# 내기 바둑 모드 — 설정 안내

기보 결과(흑/백 승)에 코인을 베팅하고, 자동 재생 후 정산받는 모드입니다.
교수님 1차 피드백 ("내기 바둑 두는 사람처럼 아바타 심어 놓는") 반영.

## 1. 파일 구성

```
Assets/Scripts/Baduk/Betting/
├── BettingData.cs              # BettingChoice, BettingTicket, BettingResult, BettingPayout
├── BettingCommentPool.cs       # JSON 직렬화용 (win/lose 카테고리 추가)
├── BettingSpectatorSpawner.cs  # 보드 옆 구경꾼 N명 (placeholder)
├── BettingNpcCommentator.cs    # 떠들썩한 톤 코멘트 + 베팅 결과 멘트
├── BettingVRUI.cs              # 4단계 UI (선택→베팅→재생→결과)
└── BettingGameManager.cs       # 전체 흐름 + 코인 정산

Assets/Resources/Data/
└── betting_comments.json        # 떠들썩한 코멘트 풀 (opening/midgame/endgame/good/bad/idle/win/lose)
```

복기 모듈 자산도 같이 사용:
- `Baduk.Replay.KifuLoader` (기보 로드)
- `Baduk.Replay.KifuReplayManager` (자동 재생 로직)
- `Baduk.Replay.NpcAvatarSpawner` (대국자 흑/백 + 훈수꾼)

## 2. 코인 시스템

`Assets/Scripts/Common/PlayerDataManager.cs` 에 코인 시스템 추가됨:
- 초기 코인: 1000
- `PlayerDataManager.Instance.Coins`, `TrySpendCoins(n)`, `AddCoins(n)`, `ResetCoins()`
- `OnCoinsChanged` 이벤트로 UI 자동 갱신
- PlayerPrefs(`player_coins`)에 저장

베팅 정산 비율: 적중 시 `1.8x` (BettingPayout.WinMultiplier), 무승부면 환급.
조정하려면 `BettingData.cs`의 상수 수정.

## 3. 씬 세팅 (Unity Editor)

1. `File → New Scene` → `BadukBetting` 으로 저장 (`Assets/Scenes/`)
2. 빈 GameObject `BadukBettingSystem` 생성
3. 다음 컴포넌트 **모두 같은 GameObject**에 부착:
   - `BadukBoard` (재사용)
   - `KifuLoader` (Replay 폴더)
   - `KifuReplayManager` (Replay 폴더)
   - `NpcAvatarSpawner` (Replay 폴더 — 흑/백/훈수꾼 3명)
   - `BettingSpectatorSpawner` (구경꾼 N명)
   - `BettingNpcCommentator`
   - `BettingVRUI`
   - `BettingGameManager`
   - `BadukVRBoardSetup` (XR Toolkit 사용 시)
4. XR Origin 추가 (기존 Baduk 씬에서 복사)
5. 메인 카메라에 `MainCamera` 태그 확인
6. `File → Build Settings` 에 `BadukBetting` 씬 추가
7. `MainLobby` 씬에서 "내기 바둑" 버튼 클릭 → 이 씬으로 진입

## 4. 동작 확인

1. 로비에서 `내기 바둑` 클릭 → 기보 선택 화면 (3개: 9로흑승, 9로백승, 19로백승)
2. 기보 클릭 → 베팅 화면 (흑/백 + 100/300/500/1000 + 베팅 확정)
3. 확정 시 코인 차감 → 자동 재생 시작 (스킵 불가, NPC 코멘트 + 자막)
4. 재생 끝나면 결과 화면 (적중 +코인 / 실패 -코인)
5. "한판 더" → 같은 기보로 베팅 다시 / "로비로" → 메인 로비

## 5. 콘텐츠 추가/조정

### 새 기보 추가
`kifu_sample.json`에 `winner` 필드 명시 필수 (`"black"` | `"white"` | `"draw"`).
SGF 변환 사용 시 `Server/tools/sgf_to_json.py` 가 자동으로 winner 채움.

### 코멘트 풀 조정
`betting_comments.json` 의 각 카테고리에 문자열 추가/수정.

### 정산 비율 변경
`BettingData.cs` 의 `BettingPayout.WinMultiplier` 수정 (현재 1.8).

### 구경꾼 수/위치 변경
`BettingSpectatorSpawner` Inspector:
- `Spectator Count` (1~6)
- `Seat Positions` 배열 (각 좌석 로컬 좌표)
- 진짜 3D 모델 있으면 `Spectator Prefab` 슬롯에 할당

## 6. 알려진 제약 / 다음 단계

- 베팅 중 일시정지는 가능하나 이전/다음 수 스킵은 막혀 있음 (결과 미리 알면 안 되므로)
- 무승부 처리는 환급만 함 (수수료 없음)
- 코인 잔고가 베팅 금액보다 적으면 베팅 확정 무시 (피드백 UI 없음 — TODO)
- 진짜 형세 평가 (KataGo) 미연동 — 결과는 기보 데이터의 `winner` 필드에 의존
