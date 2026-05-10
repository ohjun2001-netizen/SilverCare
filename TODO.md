# SilverCare TODO 리스트

_업데이트: 2026-05-10_

---

## 박건영 — 카드짝맞추기 + 세대공감퀴즈

### 카드짝맞추기
- [ ] 카드 앞면 이미지 8종 교체 (현재 색+숫자 폴백으로 동작 중 — 선택사항)

### 세대공감퀴즈

**기능**
- [ ] `quiz_questions.json` 최소 30문제 작성 (카테고리: 옛날 노래·역사·속담·생활상식, 난이도 1~3 균등)
- [ ] Whisper 한국어 인식 테스트 (5문장 이상)
- [ ] TTS 발화 속도 조절 테스트 (노인 대상 느린 속도)
- [ ] 서버 에러 시 Unity 측 폴백 처리 확인

**UI (QuizUIManager.cs — 전부 stub)**
- [ ] World Space Canvas 생성
- [ ] 문제 텍스트 표시 영역
- [ ] 4지선다 선택지 버튼 (큰 버튼, 노인 친화)
- [ ] 정답 시 초록 하이라이트 + 설명 표시
- [ ] 오답 시 빨강 하이라이트 + 정답 표시
- [ ] 진행도 표시 (현재 문제 / 전체 문제)
- [ ] 최종 결과 패널 (맞춘 수, 점수)
- [ ] 나가기/다시하기 버튼
- [ ] 선택지 버튼에 XR 클릭 이벤트 연결

---

## 조성우 — 노래맞추기

**기능**
- [ ] 트로트 30곡 MP3 수집 → `Assets/Resources/Audio/Songs/`
- [ ] `song_list.json` 30곡 입력
- [ ] Unity에서 마이크 녹음 → WAV 저장
- [ ] WAV 파일을 `POST /stt/recognize`로 전송
- [ ] 응답 텍스트로 정답 판정

**UI (SongGuessUIManager.cs — 전부 stub)**
- [ ] World Space Canvas 생성
- [ ] 라운드 표시 (현재 / 전체)
- [ ] 재생 중 표시 (♪ 아이콘 또는 텍스트)
- [ ] 정답 입력 방식 결정: 방법A(4지선다) 또는 방법B(STT 음성)
- [ ] 정답/오답 결과 표시 (곡명 + 가수)
- [ ] 최종 결과 패널 (맞춘 수, 점수)
- [ ] 나가기/다시하기 버튼

---

## 양석원 — 골프 + 메인 로비

### 골프
- [ ] Quest 실기기 VR 스윙 테스트 (기능/UI/코스 환경 모두 완성, 실기기 검증만 남음)

### 메인 로비

**기능**
- [ ] `PlayerDataManager.cs` — GameRecord 리스트 JSON 직렬화 저장 (SaveRecords)
- [ ] `PlayerDataManager.cs` — LoadAll()에서 GameRecord 복원 (앱 재시작 시 기록 유지)
- [ ] `VRLobby.cs:71` — 하이스코어가 바둑만 하드코딩 → 전체 게임으로 확장

---

## 권오준 — 바둑

**기능**
- [ ] 사활문제 추가 확보 (현재 15개 → 목표 40개)
- [ ] TTS 음성 힌트 동작 테스트
- [ ] 돌 놓기 확인 단계 — 클릭 시 ghost 돌 표시 후 확정 (`BadukVRInput.cs`, 수전증 대비)
- [ ] 아바타 2인 스폰 로직 (`BadukRoomEnvironment.cs`) — 자세한 계획은 `아바타.md` 참고

**UI**
- [ ] 바둑 UI 글씨 크기 확대 (노인 친화)
- [ ] 바둑판-UI 겹침 최종 확인
- [ ] 아바타 Animator 연결 — 흑/백 턴 전환 시 Head Nod 트리거

---

## 배경/환경 (나중에 — 기능 완성 후)

- [ ] 고스톱 — 배경 환경 없음 (현재 빈 씬)
- [ ] 카드짝맞추기 — 배경 환경 없음
- [ ] 바둑 사활 — 배경 환경 없음
- [ ] 바둑 기보/예측 — 테이블 큐브 하나만 있음 (`BadukRoomEnvironment`)
- [ ] 세대공감퀴즈 — 배경 환경 없음
- [ ] 노래맞추기 — 배경 환경 없음
- [ ] 골프 — 코스는 프리미티브로 구현됨, 실제 배경(하늘/나무 등) 없음
- [ ] 메인 로비 — Canvas만 있음, 배경 씬 없음

---

## 통합 테스트 (전원 공통)

- [ ] 로비 → 8개 게임 → 로비 왕복 테스트
- [ ] 게임별 점수 저장 → 로비 하이스코어 반영 확인
- [ ] PlayerDataManager GameRecord 직렬화 완료 후 재시작 점수 복원 확인
- [ ] Quest 빌드 → 실기기 동작 확인
- [ ] 멀미 방지 확인 (이동 속도, 회전 속도)
- [ ] 전체 TTS 음성 출력 확인
- [ ] 메모리 누수 점검
- [ ] 데모 시나리오 작성
- [ ] 발표 자료 준비

---

## 완료 ✅

### 카드짝맞추기 (박건영)
- [x] 카드 가시성 문제 해결 (`Sprites/Default` 셰이더)
- [x] 카드 뒷면 단색 처리
- [x] 카드 폴백 텍스처 시스템 (색+숫자, 실제 이미지 없어도 동작)
- [x] 게임 로직 완성 (짝 맞추기, 점수, 난이도 3단계, 뒤집기)

### 고스톱 (조성우)
- [x] `GoStopDeck.cs` — 48장 카드 데이터 정의, 셔플 (매판 랜덤)
- [x] `GoStopGameManager.cs` — DealCards, 매칭 로직, CPU AI, 고/스톱 판정, 결과 화면
- [x] `GoStopScoreCalculator.cs` — 광/열끗/띠(홍단·청단·초단)/피 점수 계산
- [x] 화투패 48장 이미지 크롭 → `Assets/Resources/GoStop/card_00~47.png`

### 바둑 기보 재생 / 다음 수 맞추기
- [x] `sgf_to_kifu.py` — AlphaGo vs 이세돌 5번기 SGF → kifu JSON 변환
- [x] `KifuLoader.cs` — `kifu_*` 파일 전부 자동 로드
- [x] `PredictionVRUI.cs` — 옵션 5국으로 제한
- [x] `KifuVRUI.cs` — 기보 복기 5국으로 제한, 동적 레이아웃

### 바둑 사활
- [x] `BadukVRInput.cs` — 씬별 보드 배치 분기 (사활: 수직, 기보: 수평)
- [x] `BadukBoard.cs` — `ClearBoard()` public 노출
- [x] `BadukGameManager.cs` — `OnBack` 콜백에 `ClearBoard()` 추가 (보드 잔상 버그 수정)
- [x] `baduk_problems.json` — 문제 11 중복 돌·포위 오류 수정
