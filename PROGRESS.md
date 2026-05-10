# SilverCare 개발 진행 현황

_최종 업데이트: 2026-05-09_

---

## 완료

### 바둑 기보 재생 (BadukReplay)
- [x] `sgf_to_kifu.py` — AlphaGo vs 이세돌 5번기 SGF → kifu JSON 변환
  - 분기 SGF 파싱 (첫 번째 variation 추적)
  - 25수마다 예측 포인트 자동 생성
  - 4국 78수 "신의 한 수" 특수 포인트 하드코딩
- [x] `KifuLoader.cs` — `kifu_*` 파일 전부 자동 로드 (`Resources.LoadAll`)
- [x] `PredictionVRUI.cs` — 5국 안 보이던 버그 수정 (`i < 4` → 동적 레이아웃)

### 바둑 사활
- [x] `BadukVRInput.cs` — 씬별 보드 배치 분기
  - `BadukReplay` / `BadukPrediction`: 수평 배치 (기존 유지)
  - 그 외 (사활): 수직 배치, 거리 1.5f

---

## 진행 중

### 카드 짝맞추기 (CardMatch)
- [x] 전체 재작성 (입력: Physics.Raycast, World Space Canvas UI)
- [x] 카드 클릭 동작 확인 (콜라이더 정상)
- [x] Canvas 방향 수정 (텍스트 뒤집힘 해결)
- [x] 뒷면 Quad 180도 회전 추가
- [ ] **카드 가시성 — 아직 미해결**
  - 카드 위치·방향·콜라이더 모두 정상 확인 (Debug.Log)
  - 셰이더 탐색 순서: `Unlit/Color` → `Sprites/Default` → `URP/Unlit` → `Standard`
  - 현재 뒷면 색: 주황-빨강 (하늘과 대비용)
  - 원인 추정: 프로젝트 내 사용 가능한 셰이더 확인 필요

---

## 미시작

| 항목 | 비고 |
|------|------|
| 퀴즈 맞추기 | 전체 재작성 필요 |
| 노래 맞추기 | 오디오 파일 준비 필요 |
| 고스톱 | 규칙 복잡, 설계 필요 |
| 바둑 사활 JSON 수정 | 문제 내용 오류 수정 |
| 골프 VR 테스트 | 실기기 필요 |
