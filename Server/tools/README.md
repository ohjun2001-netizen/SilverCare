# Server/tools

SilverCare 보조 스크립트.

## sgf_to_json.py

SGF (바둑 표준 기보 포맷) 파일을 SilverCare 바둑 복기/내기 모드에서 사용하는 KifuDatabase JSON으로 변환합니다.

### 사용 예

```bash
# 단일 파일 → stdout
python sgf_to_json.py mygame.sgf

# 단일 파일 → 출력 파일
python sgf_to_json.py mygame.sgf -o ../../Assets/Resources/Data/kifu_sample.json

# 여러 파일 묶어서 하나의 DB로
python sgf_to_json.py game1.sgf game2.sgf game3.sgf -o ../../Assets/Resources/Data/kifu_sample.json
```

### 지원 범위 / 한계

- **지원**: 메인 라인 수순(`;B[xy]`, `;W[xy]`), 보드 크기(`SZ`), 플레이어(`PB`/`PW`), 날짜(`DT`), 결과(`RE`), 이벤트/게임명(`EV`/`GN`)
- **무시**: 변화도(branch), 셋업 돌(`AB`/`AW`), 주석(`C[]`), 패(`PASS`)
- **결과 파싱**: `B+...` → winner=black, `W+...` → winner=white, `0`/`Draw`/`Jigo` → draw, 그 외 → 빈 문자열

### 기보 데이터 구하기

- gokifu.com / go4go.net 등에서 SGF 다운로드
- KGS / OGS 본인 대국 기록 export
- 직접 작성한 SGF

저작권에 유의하여 학술/데모 목적으로만 사용하세요.

### 출력 파일을 Unity에서 사용하려면

1. 위 명령으로 `Assets/Resources/Data/kifu_sample.json` 덮어쓰기 (또는 새 파일명 사용)
2. 새 파일명을 사용한 경우 `Assets/Scripts/Baduk/Replay/KifuLoader.cs` 의 `KIFU_PATH` 상수 수정
3. Unity 에디터에서 Resources 폴더 자동 갱신 (필요시 우클릭 → Reimport)
