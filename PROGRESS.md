# SilverCare 개발 진행 현황

## 1. 바둑 기보 (BadukReplay / BadukPrediction)

### sgf_to_kifu.py (신규)
- AlphaGo vs 이세돌 5번기 SGF 파일 → `kifu_alphago_lsd.json` 변환
- 분기(variation) 있는 SGF에서 첫 번째 변화도(실제 대국)를 추적하는 파서 구현
- 25수마다 예측 포인트 자동 생성
- 4국 78수 "신의 한 수" 하드코딩 특수 포인트 (`SPECIAL_POINTS`)
- 출력: 1국 186수/7포인트, 2국 211수/8포인트, 3국 176수/6포인트, 4국 180수/7포인트, 5국 280수/10포인트

### KifuLoader.cs
- 단일 파일 로드 → `Resources.LoadAll`로 `kifu_*` 파일 전부 자동 로드로 변경

### PredictionVRUI.cs
- `BuildKifuButtons`에서 `i < 4` 하드코딩 → 동적 레이아웃 (최대 8개)으로 수정 (5국 안 보이던 버그 수정)

---

## 2. 바둑 사활 (BadukVRInput.cs)

### 보드 배치 방식 변경
- 기존: 모든 씬에서 테이블 위 수평 배치
- 변경: 씬 이름으로 분기
  - `BadukReplay` / `BadukPrediction` → 수평 배치 (기존 유지)
  - 그 외 (사활) → 수직 배치 (`Quaternion.LookRotation(Vector3.up, -flatForward)`)
- 사활 보드 거리: `0.85f` → `1.5f` (뒤로 이동)
- `#if UNITY_XR_INTERACTION_TOOLKIT` / `#else` 두 블록 모두 동일하게 적용

---

## 3. 카드 짝맞추기 (CardMatch)

### 전체 재작성 (CardController.cs, CardMatchGameManager.cs)

**CardController.cs**
- 카드 앞면/뒷면 `SetActive`로 뒤집기 처리
- `Init()`: `Unlit/Texture` → `Sprites/Default` → `Universal Render Pipeline/Unlit` → `Standard` 순서로 셰이더 탐색
- `EnsureLabel()`: 앞면에 TextMesh로 카드 번호 표시
- `SetMatched()`: 초록 하이라이트 + 라벨 색 변경

**CardMatchGameManager.cs**
- 입력: `IPointerClickHandler` 제거 → `Update() + Physics.Raycast` (PC/VR 공용)
- 카드 스폰: `Camera.main` 기준으로 정면에 그리드 배치
  - 카드 회전: `Quaternion.LookRotation(-forward)` (카메라를 향하도록)
  - 뒷면 Quad: `localRotation = Euler(0, 180, 0)` 추가 (양면 가시성)
  - 머티리얼: `MakeColorMat()` — `Unlit/Color` 우선, 여러 셰이더 폴백
- World Space Canvas: 카드 그리드 위에 점수/상태 표시
  - Canvas 회전: `LookRotation(forward)` (텍스트 올바르게 보임)
- 기본값: spacingX=0.85, spacingY=1.0, spawnDist=1.8, heightOffset=-0.2

**현재 상태**: 카드 클릭(콜라이더) 동작 확인 완료 / 뒷면 색상·가시성 작업 중

---

## 남은 작업

| 항목 | 상태 |
|------|------|
| 카드 짝맞추기 — 카드 가시성 최종 확인 | 진행 중 |
| 퀴즈 맞추기 | 미시작 |
| 노래 맞추기 | 미시작 (오디오 파일 필요) |
| 고스톱 | 미시작 (규칙 복잡) |
| 바둑 사활 JSON 수정 | 미시작 |
| 골프 VR 테스트 | 실기기 필요 |
