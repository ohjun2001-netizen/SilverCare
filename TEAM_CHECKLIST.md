# SilverCare 팀원 체크리스트

## 0. 공통 — 프로젝트 셋업 (모든 팀원)

- [ ] Git 클론: `git clone https://github.com/ohjun2001-netizen/SilverCare.git`
- [ ] Unity Hub에서 프로젝트 열기 (Unity **2022.3.62f3** 필요)
- [ ] 첫 실행 시 Library 폴더 자동 생성됨 (5~10분 소요, 정상임)
- [ ] 상단 메뉴 **SilverCare → Build Scenes** 실행하여 전체 씬 생성
- [ ] Play 버튼 눌러 MainLobby 정상 실행 확인
- [ ] WASD로 이동, 마우스로 시점 변경 확인 (XR Device Simulator)
- [ ] 본인 브랜치 생성: `git checkout -b feature/본인이름`

---

## 1. 박건영 — 카드짝맞추기 + 세대공감퀴즈

### 카드짝맞추기 (CardMatch)

**현재 완료된 것:**
- 카드 뒤집기 상태 관리, 매칭 판정 로직, 점수 계산, 오디오/TTS 피드백

**해야 할 것:**
- [ ] 카드 스프라이트 에셋 제작/수집 (16장 이상, `Resources/` 폴더에 배치)
- [ ] `CardMatchGameManager.cs` → `InitGame()` 카드 로딩 및 셔플 구현
- [ ] `CardController.cs` → 카드 뒤집기 애니메이션 구현 (DOTween 또는 코루틴)
- [ ] 매칭 성공 시 이펙트 (글로우, 페이드 등)
- [ ] 난이도별 카드 수 조절 (초급 8장, 중급 12장, 고급 16장)
- [ ] VR 컨트롤러 카드 선택 연동 (XRRayInteractor 활용)
- [ ] 결과 화면 UI 구현

### 세대공감퀴즈 (Quiz)

**현재 완료된 것:**
- JSON에서 문제 로딩, 셔플, 정답 판정, 점수 계산, TTS 피드백

**해야 할 것:**
- [ ] `Resources/Data/quiz_questions.json` 문제 데이터 보강 (현재 기본 데이터만 있음)
- [ ] `QuizUIManager.cs` → 모든 메서드가 `Debug.Log`만 있음, **실제 UI 구현 필요**
  - [ ] 문제/선택지 표시 UI (World Space Canvas)
  - [ ] 정답/오답 하이라이팅
  - [ ] 최종 결과 패널
- [ ] VR에서 선택지 버튼 클릭 연동
- [ ] (선택) GPT-4o 동적 문제 생성

### AI 음성 서버

- [ ] Python FastAPI 서버 구축 (`localhost:8000`)
- [ ] Whisper STT 연동
- [ ] GPT-4o 연동
- [ ] TTS 엔드포인트: `POST /tts/speak` (TTSManager.cs에서 호출)

---

## 2. 조성우 — 노래맞추기 + 고스톱

### 노래맞추기 (SongGuess)

**현재 완료된 것:**
- JSON 노래 DB 로딩, 키워드 매칭 정답 판정, 오디오 재생/페이드아웃, 라운드 관리

**해야 할 것:**
- [ ] 트로트 노래 30곡 수집 → `Resources/Audio/Songs/` 폴더에 MP3로 배치
- [ ] `Resources/Data/song_list.json` 노래 데이터 보강 (30곡, 제목/가수/키워드)
- [ ] `SongGuessUIManager.cs` → 모든 메서드가 `Debug.Log`만 있음, **실제 UI 구현 필요**
  - [ ] 라운드 시작 화면
  - [ ] 정답 입력 UI (텍스트 입력 또는 선택지)
  - [ ] 결과 표시 패널
- [ ] 음성 AI 서버 연동 (박건영의 FastAPI 서버 재활용)
- [ ] (선택) STT 음성 인식으로 정답 입력

### 고스톱 (GoStop)

**현재 완료된 것:**
- 게임 상태 관리, 고/스톱 판정, 고 배율 적용, 기본 UI 프레임워크

**해야 할 것 (작업량 많음):**
- [ ] 화투패 48장 스프라이트 에셋 제작/수집
- [ ] `GoStopDeck.cs` → `BuildDeck()` 48장 카드 정의 (월/타입 매핑)
- [ ] `GoStopDeck.cs` → `DealCards()` 패 분배 구현 (플레이어 10장, CPU 10장, 바닥 8장)
- [ ] `GoStopGameManager.cs` → `OnPlayerPlayCard()` 카드 매칭 로직 구현
- [ ] `GoStopScoreCalculator.cs` → 점수 계산 규칙 완성
  - [ ] 광(3광/4광/5광/비광) 점수
  - [ ] 열끗(5점 이상) 점수
  - [ ] 띠(홍단/청단/초단) 점수
  - [ ] 피(10장 이상) 점수
- [ ] CPU AI 턴 구현
- [ ] `GoStopUIManager.cs` → `RefreshAll()` 실제 UI 렌더링 구현
- [ ] 패 집기 인터랙션 (VR 컨트롤러)
- [ ] 고스톱 음성 인식 연동

---

## 3. 양석원 — 골프 배팅 + 메인 로비

### 골프 (Golf)

**현재 완료된 것:**
- 공 물리 (Rigidbody), 스윙 힘 적용, 홀인 감지, 코스 진행, 스트로크 카운트

**해야 할 것:**
- [ ] 3개 코스 프리팹 제작 (지형, 홀, 장애물)
- [ ] `BallController.cs` → VR 컨트롤러 스윙 감지 구현 (컨트롤러 속도/방향 추출)
- [ ] `GolfCourseManager.cs` → 코스 프리팹 연결 및 시작 위치 설정
- [ ] `GolfUIManager.cs` → 모든 메서드가 `Debug.Log`만 있음, **실제 UI 구현 필요**
  - [ ] 코스 정보 표시
  - [ ] 스트로크 카운트 UI
  - [ ] 최종 스코어 패널
- [ ] 골프 스코어 UI 디자인

### 메인 로비 (Lobby)

**현재 완료된 것:**
- VRLobby: World Space Canvas 자동 생성, 7개 게임 버튼, 씬 전환 동작
- SimpleLobby: OnGUI 기반 백업 로비

**해야 할 것:**
- [ ] `ProfilePanel.cs` → 프로필 입력 UI 연결 (이름/나이/성별/난이도 입력 필드)
- [ ] `GameSelectUI.cs` → 하이스코어 표시 연동
- [ ] 프로필 기반 게임 추천 로직 구현
- [ ] 로비 ↔ 게임 씬 전환 흐름 점검
- [ ] 전체 씬 매니저 동작 확인 (7개 게임 모두 왕복 테스트)
- [ ] 게임 기록 저장/불러오기 확인
- [ ] 로비 UI 디자인 개선 (노인 친화 — 큰 글씨, 높은 대비)

---

## 공통 참고사항

### 프로젝트 구조
```
Assets/
├── Scripts/
│   ├── Common/        ← 공용 매니저 (수정 시 팀원 공유)
│   ├── Baduk/         ← 권오준 담당 (완료)
│   ├── CardMatch/     ← 박건영
│   ├── Quiz/          ← 박건영
│   ├── SongGuess/     ← 조성우
│   ├── GoStop/        ← 조성우
│   ├── Golf/          ← 양석원
│   └── Lobby/         ← 양석원
├── Resources/Data/    ← 게임 데이터 JSON
├── Scenes/            ← 게임 씬 (Build Scenes로 자동 생성)
└── Editor/            ← SceneBuilder.cs (씬 자동 생성 도구)
```

### 씬 재생성
코드를 수정한 후 씬에 새 컴포넌트를 추가해야 하면:
**SilverCare → Build Scenes** 메뉴 실행

### Git 작업 흐름
```bash
git checkout -b feature/본인이름
# 작업 후
git add .
git commit -m "작업 내용"
git push -u origin feature/본인이름
# GitHub에서 Pull Request 생성
```

### 주의사항
- `Common/` 폴더 스크립트 수정 시 반드시 팀원에게 공유
- `.gitignore`에 의해 Library, Logs, obj 등은 Git에 안 올라감 (정상)
- Meta Quest 빌드: File → Build Settings → Android로 Switch 후 Build
