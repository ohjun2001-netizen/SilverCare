### 토큰 절약해서 사용하기

---

### 씬 전환/패널 전환 시 3D 오브젝트 정리 규칙

- UI 패널만 껐다 켜는 `Show*` 메서드는 **3D 오브젝트를 건드리지 않는다**
- 게임 플레이 → 메뉴/로비 복귀 시에는 반드시 보드/카드/3D 씬 오브젝트를 `ClearBoard()` / `Destroy()` 등으로 명시적으로 제거해야 함
- 예: `OnBack = () => { _board.ClearBoard(); _ui.ShowDifficultySelect(); }` — 보드 정리와 UI 전환을 동시에
- `ShowDifficultySelect()` 같은 UI 전환 메서드 안에서 3D 정리까지 하지 말 것 (단일 책임 원칙)

---

### CardMatch 카드 그리드 배치 규칙

- 그리드 중심 Y = `camPos.y` (눈높이 고정, topRow 기준 아님)
- 세로 간격: `effSpacingY = Min(spacingY, 2.2f / (rows-1))` — 행 수 늘어도 전체 높이 2.2m 이내
- 카드 높이: `cardH = Min(1.2f, effSpacingY * 0.88f)` — 간격에 비례
- UI 패널 Y: `center.y + totH/2 + cardH/2 + 0.3f` — 카드 상단 끝 기준으로 띄움
  - `totH/2 + 0.5f` 같은 고정 오프셋은 cardH를 무시해서 겹침 발생하므로 금지
