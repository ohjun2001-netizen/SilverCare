# SilverCare TODO 리스트

## 박건영 — 카드짝맞추기 + 세대공감퀴즈

### 카드짝맞추기 (Assets/Scripts/CardMatch/)

**카드 에셋 준비**
- [ ] 카드 앞면 스프라이트 8종 제작/수집 (PNG, 256x256 권장)
- [ ] `Assets/Resources/Cards/` 폴더 생성 후 배치
- [ ] 카드 뒷면 스프라이트 1종 제작

**CardMatchGameManager.cs — InitGame() 구현**
- [ ] Resources에서 카드 스프라이트 로딩 (`Resources.LoadAll<Sprite>("Cards")`)
- [ ] 난이도별 카드 수 결정 (초급 4쌍=8장, 중급 6쌍=12장, 고급 8쌍=16장)
- [ ] 카드 쌍 생성 후 셔플 (Fisher-Yates)
- [ ] 그리드 배치 (4x2, 4x3, 4x4)
- [ ] 각 카드에 CardController 연결

**CardController.cs — 뒤집기 구현**
- [ ] 뒷면 → 앞면 회전 애니메이션 (코루틴으로 Y축 180도 회전)
- [ ] 앞면 → 뒷면 되돌리기 애니메이션
- [ ] 매칭 성공 시 색상 변경 또는 페이드아웃

**VR UI 구현 (새 파일: CardMatchVRUI.cs)**
- [ ] World Space Canvas 생성 (BadukVRUI.cs 패턴 참고)
- [ ] 난이도 선택 패널 (초급/중급/고급 버튼)
- [ ] 상단바: 점수, 남은 쌍 수 표시
- [ ] 하단바: 다시하기, 나가기 버튼
- [ ] 결과 패널 (점수, 걸린 시간)

**VR 연동**
- [ ] 카드 오브젝트에 XRSimpleInteractable 추가
- [ ] XRRayInteractor로 카드 선택 처리
- [ ] SceneBuilder.cs에 CardMatch 씬 컴포넌트 추가

---

### 세대공감 퀴즈 (Assets/Scripts/Quiz/)

**QuizUIManager.cs — VR UI 구현**
- [ ] World Space Canvas 생성
- [ ] 문제 텍스트 표시 영역
- [ ] 4지선다 선택지 버튼 (큰 버튼, 노인 친화)
- [ ] 정답 시 초록 하이라이트 + 설명 표시
- [ ] 오답 시 빨강 하이라이트 + 정답 표시
- [ ] 진행도 표시 (현재 문제 / 전체 문제)
- [ ] 최종 결과 패널 (맞춘 수, 점수)
- [ ] 나가기/다시하기 버튼

**퀴즈 데이터 보강 (Assets/Resources/Data/quiz_questions.json)**
- [ ] 최소 30문제 추가 (현재 기본 데이터만)
- [ ] 카테고리: 옛날 노래, 역사, 속담, 생활상식
- [ ] 난이도 1~3 균등 배분

**VR 연동**
- [ ] 선택지 버튼에 XR 클릭 이벤트 연결
- [ ] SceneBuilder.cs에 Quiz 씬 UI 컴포넌트 추가

---

### AI 서버 안정화 (Server/)
- [ ] Whisper 한국어 인식 테스트 (5문장 이상)
- [ ] TTS 발화 속도 조절 테스트 (노인 대상 느린 속도)
- [ ] 서버 에러 시 Unity 측 폴백 처리 확인

---

## 조성우 — 노래맞추기 + 고스톱

### 노래맞추기 (Assets/Scripts/SongGuess/)

**노래 데이터 준비**
- [ ] 트로트 30곡 MP3 수집
- [ ] `Assets/Resources/Audio/Songs/` 폴더 생성 후 배치
- [ ] `Assets/Resources/Data/song_list.json` 30곡 입력
  ```json
  {"title": "곡명", "artist": "가수", "audioFile": "파일명", "keywords": ["키워드1","키워드2"]}
  ```

**SongGuessUIManager.cs — VR UI 구현**
- [ ] World Space Canvas 생성
- [ ] 라운드 표시 (현재 / 전체)
- [ ] 재생 중 표시 (♪ 아이콘 또는 텍스트)
- [ ] 정답 입력 방식 선택:
  - [ ] 방법A: 4지선다 버튼 (쉬움)
  - [ ] 방법B: STT 음성 입력 (고급)
- [ ] 정답/오답 결과 표시 (곡명 + 가수)
- [ ] 최종 결과 패널 (맞춘 수, 점수)
- [ ] 나가기/다시하기 버튼

**STT 연동**
- [ ] Unity에서 마이크 녹음 → WAV 저장
- [ ] WAV 파일을 `POST /stt/recognize`로 전송
- [ ] 응답 텍스트로 정답 판정

---

### 고스톱 (Assets/Scripts/GoStop/)

**화투 에셋 준비**
- [ ] 화투패 48장 스프라이트 제작/수집 (12월 × 4장)
- [ ] `Assets/Resources/GoStop/` 폴더 생성 후 배치
- [ ] 카드 뒷면 스프라이트 1종

**GoStopDeck.cs — BuildDeck() 구현**
- [ ] 48장 카드 데이터 정의
  ```
  1월: 송학(광), 소나무(띠-홍단), 소나무(띠), 소나무(피)
  2월: 매조(띠-홍단), 매화(띠), 매화(피), 매화(피)
  ... (12월까지)
  ```
- [ ] 카드 구조체 정의 (월, 타입: 광/열끗/띠/피, 스프라이트)

**GoStopDeck.cs — DealCards() 구현**
- [ ] 48장 셔플
- [ ] 플레이어 10장 배분
- [ ] CPU 10장 배분
- [ ] 바닥 8장 오픈
- [ ] 나머지 20장 뒷면으로 쌓기

**GoStopGameManager.cs — OnPlayerPlayCard() 구현**
- [ ] 플레이어 패에서 카드 선택
- [ ] 바닥에 같은 월 카드 있는지 매칭 확인
- [ ] 매칭 0장: 바닥에 내려놓기
- [ ] 매칭 1장: 가져오기
- [ ] 매칭 2장: 하나 선택 후 가져오기
- [ ] 뒷면 더미에서 1장 뒤집기 (2차 매칭)
- [ ] 먹은 카드 분류 (광/열끗/띠/피)

**GoStopScoreCalculator.cs — 점수 계산 완성**
- [ ] 광 점수: 3광=3점, 비광=2점, 4광=4점, 5광=15점
- [ ] 열끗 점수: 5장=1점, 이후 +1장당 +1점
- [ ] 띠 점수: 3장=1점, 이후 +1장당 +1점
- [ ] 보너스: 홍단=3점, 청단=3점, 초단=3점
- [ ] 피 점수: 10장=1점, 이후 +1장당 +1점
- [ ] 고 배율 적용 (1.5배/고)

**CPU AI 구현**
- [ ] 기본 전략: 매칭 가능한 카드 우선
- [ ] 광/열끗 우선 수집
- [ ] 고/스톱 판단 (점수 3점 이상이면 스톱 고려)

**GoStopUIManager.cs — VR UI 구현**
- [ ] 플레이어 패 표시 (화면 하단, 부채꼴)
- [ ] 바닥 카드 표시 (중앙)
- [ ] CPU 패 표시 (화면 상단, 뒷면)
- [ ] 먹은 카드 영역 (광/열끗/띠/피 분류)
- [ ] 점수 표시
- [ ] 고/스톱 선택 버튼
- [ ] 결과 패널

**VR 연동**
- [ ] 카드에 XRSimpleInteractable 추가
- [ ] 카드 선택 → OnPlayerPlayCard 연결

---

## 양석원 — 골프 + 메인 로비

### 골프 (Assets/Scripts/Golf/)

**코스 프리팹 제작**
- [ ] 코스 1: 직선 홀 (초급, 장애물 없음)
- [ ] 코스 2: 커브 홀 (중급, 벽 장애물)
- [ ] 코스 3: 언덕 홀 (고급, 경사면)
- [ ] 각 코스에 홀컵 오브젝트 배치 (Trigger Collider)
- [ ] `Assets/Resources/Golf/` 폴더에 프리팹 저장

**BallController.cs — VR 스윙 감지**
- [ ] 오른쪽 컨트롤러 속도 추적 (ActionBasedController velocity)
- [ ] 스윙 감지 조건 (속도 임계값 초과 시)
- [ ] 스윙 방향 + 세기 → ApplySwing() 호출
- [ ] PC 테스트: 마우스 클릭 + 드래그로 방향/세기 폴백

**GolfCourseManager.cs — 코스 연결**
- [ ] coursePrefabs 배열에 3개 프리팹 연결
- [ ] ballStartPositions 각 코스별 시작 위치 설정
- [ ] 코스 전환 시 이전 코스 제거 + 새 코스 생성

**GolfUIManager.cs — VR UI 구현**
- [ ] World Space Canvas 생성
- [ ] 코스 정보 (코스 번호, 파)
- [ ] 스트로크 카운트 실시간 표시
- [ ] 홀인 시 축하 메시지
- [ ] 최종 스코어 패널 (3코스 합산)
- [ ] 나가기/다시하기 버튼

--

### 메인 로비 (Assets/Scripts/Lobby/)

**ProfilePanel.cs — 프로필 입력 구현**
- [ ] 이름 입력 필드 (VR 키보드 또는 프리셋 선택)
- [ ] 나이 선택 (60대/70대/80대 버튼)
- [ ] 성별 선택 (남/여 버튼)
- [ ] 난이도 선택 (쉬움/보통/어려움)
- [ ] SaveProfile()에 실제 입력값 연결

**GameSelectUI.cs — 보강**
- [ ] 각 게임 버튼에 최고점수 표시
- [ ] ApplyRecommendation() — 나이/난이도 기반 게임 추천
- [ ] 추천 게임 하이라이트 표시

**PlayerDataManager.cs — 저장 완성**
- [ ] GameRecord 리스트 JSON 직렬화
- [ ] LoadAll() 구현 (앱 재시작 시 기록 복원)

**로비 UI 개선**
- [ ] 글씨 크기 확대 (최소 28pt)
- [ ] 버튼 크기 확대 (최소 150x60)
- [ ] 고대비 색상 적용

---

## 권오준 — 바둑 사활문제

- [ ] 사활문제 추가 확보 (현재 20개 → 목표 40개)
- [ ] 바둑 UI 글씨 크기 확대 (노인 친화)
- [ ] TTS 음성 힌트 동작 테스트
- [ ] 바둑판-UI 겹침 최종 확인

---

## 통합 테스트 (전원 공통, 7주차~)

- [ ] 로비 → 7개 게임 → 로비 왕복 테스트
- [ ] 게임별 점수 저장 → 로비 하이스코어 반영 확인
- [ ] Quest 빌드 → 실기기 동작 확인
- [ ] 멀미 방지 확인 (이동 속도, 회전 속도)
- [ ] 전체 TTS 음성 출력 확인
- [ ] 메모리 누수 점검
- [ ] 데모 시나리오 작성
- [ ] 발표 자료 준비





