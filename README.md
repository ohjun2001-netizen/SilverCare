# SilverCare — 노인 대상 VR 인지 게임

<p align="center">
  <strong>Unity 2022.3 · XR Interaction Toolkit · OpenXR · Meta Quest</strong>
</p>

노인의 인지능력 향상과 여가활동을 위한 VR 게임 플랫폼입니다.
7가지 미니게임을 통해 기억력, 판단력, 반응속도를 훈련합니다.

---

## 게임 목록

| 게임 | 설명 | 인지 영역 |
|------|------|----------|
| 바둑 사활문제 | 19×19 바둑판에서 사활 문제 풀기 (3단계 난이도) | 전략적 사고 |
| 카드 짝맞추기 | 뒤집힌 카드의 짝을 찾는 기억력 게임 | 단기 기억력 |
| 세대공감 퀴즈 | 세대를 아우르는 교양/상식 퀴즈 | 회상·지식 |
| 노래 맞추기 | 트로트 음악을 듣고 제목 맞추기 | 회상·청각 인지 |
| 고스톱 | 화투패 고스톱 게임 (AI 상대) | 판단력·계산 |
| 골프 | VR 컨트롤러로 퍼팅하는 미니골프 | 공간 감각·운동 |
| 메인 로비 | 프로필 관리, 게임 선택, 추천 시스템 | — |

---

## 환경 설정

### 필수 요구사항

- **Unity 2022.3.62f3** (Unity Hub에서 설치)
- **Python 3.10+** (AI 서버용)
- **Git**

### 프로젝트 클론 및 실행

```bash
git clone https://github.com/ohjun2001-netizen/SilverCare.git
```

1. Unity Hub → **Open** → 클론한 폴더 선택
2. 첫 실행 시 Library 생성에 5~10분 소요 (정상)
3. 상단 메뉴 **SilverCare → Build Scenes (씬 자동 생성)** 실행
4. MainLobby 씬에서 **Play** 버튼 클릭

### AI 서버 실행 (TTS/STT)

```bash
cd Server
pip install -r requirements.txt
python main.py
```

서버가 `http://localhost:8000`에서 실행됩니다.

| 엔드포인트 | 기능 | 방식 |
|-----------|------|------|
| `POST /tts/speak` | 텍스트 → 음성 변환 | Edge TTS (무료) |
| `POST /stt/recognize` | 음성 → 텍스트 변환 | Whisper 로컬 (무료) |
| `GET /health` | 서버 상태 확인 | — |

---

## 조작법

### PC (XR Device Simulator)

| 키 | 동작 |
|----|------|
| W / A / S / D | 앞 / 좌 / 뒤 / 우 이동 |
| R / F | 위 / 아래 이동 |
| Q / E | 좌 / 우 회전 |
| 마우스 좌클릭 | 선택 / 돌 놓기 |
| 마우스 우클릭 + 드래그 | 시점 회전 |

### Meta Quest (VR)

| 입력 | 동작 |
|------|------|
| 왼쪽 조이스틱 | 이동 |
| 오른쪽 조이스틱 | 회전 |
| 트리거 버튼 | 선택 / 돌 놓기 |

---

## 프로젝트 구조

```
SilverCare/
├── Assets/
│   ├── Scripts/
│   │   ├── Common/         # 공용 매니저 (TTS, Audio, Scene, Player)
│   │   ├── Baduk/          # 바둑 사활문제
│   │   ├── CardMatch/      # 카드 짝맞추기
│   │   ├── Quiz/           # 세대공감 퀴즈
│   │   ├── SongGuess/      # 노래 맞추기
│   │   ├── GoStop/         # 고스톱
│   │   ├── Golf/           # 골프
│   │   └── Lobby/          # 메인 로비
│   ├── Resources/Data/     # 게임 데이터 (JSON)
│   ├── Scenes/             # 게임 씬
│   └── Editor/             # SceneBuilder (씬 자동 생성 도구)
├── Server/                 # FastAPI AI 서버 (TTS/STT)
├── Packages/               # Unity 패키지 설정
└── ProjectSettings/        # Unity 프로젝트 설정
```

---

## 기술 스택

| 분류 | 기술 |
|------|------|
| 엔진 | Unity 2022.3 |
| VR | XR Interaction Toolkit 2.6.5 + OpenXR |
| 타겟 기기 | Meta Quest 2 / 3 / Pro |
| 언어 | C# (Unity), Python (서버) |
| AI 서버 | FastAPI + Edge TTS + Whisper |
| 데이터 | JSON |

---

## 팀 역할 분담

| 이름 | 담당 |
|------|------|
| 박건영 | 카드 짝맞추기, 세대공감 퀴즈, VR 기반 개발, AI 음성 서버 |
| 조성우 | 노래 맞추기, 고스톱, 트로트 DB, 화투 에셋 |
| 권오준 | 바둑 사활문제, DB 설계, JSON 데이터 |
| 양석원 | 골프, 메인 로비, 전체 UI/UX, 씬 매니저 |

---

## Git 작업 가이드

```bash
# 본인 브랜치 생성
git checkout -b feature/본인이름

# 작업 후 커밋
git add .
git commit -m "작업 내용"
git push -u origin feature/본인이름

# GitHub에서 Pull Request 생성
```

### 주의사항

- `Scripts/Common/` 수정 시 팀원에게 공유
- `.gitignore`에 의해 Library, Logs, obj 등은 자동 제외됨
- Meta Quest 빌드: **File → Build Settings → Android** 전환 후 Build

---

## 라이선스

이 프로젝트는 학술 목적으로 제작되었습니다.
