"""SGF → SilverCare KifuDatabase JSON 변환기.

사용법:
    python sgf_to_json.py file1.sgf [file2.sgf ...] [-o output.json]

기본 동작:
    - 파일이 1개면 stdout으로 출력 (또는 -o)
    - 파일이 여러 개면 모두 하나의 KifuDatabase로 묶어 출력
    - 변화도/주석 등 SGF 부가정보는 무시 (메인 라인만)

KifuDatabase 스키마와 일치해야 합니다 (Assets/Scripts/Baduk/Replay/KifuData.cs 참고).

외부 라이브러리 의존성 없음 (정규식 파싱).
"""

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Optional


# SGF 좌표 'a' = 0, 'b' = 1, ... 's' = 18
def _sgf_coord_to_idx(c: str) -> int:
    if not c or len(c) != 1:
        raise ValueError(f"잘못된 SGF 좌표: '{c}'")
    return ord(c) - ord("a")


def _parse_property(sgf: str, key: str) -> Optional[str]:
    """SGF에서 PB, PW, RE, SZ, DT 등 단일 값 속성 추출."""
    m = re.search(rf"\b{key}\[([^\]]*)\]", sgf)
    return m.group(1) if m else None


def _parse_winner(re_str: Optional[str]) -> str:
    """SGF RE[] 값 → winner 코드."""
    if not re_str:
        return ""
    s = re_str.strip().upper()
    if s.startswith("B+"):
        return "black"
    if s.startswith("W+"):
        return "white"
    if s in ("0", "DRAW", "JIGO"):
        return "draw"
    return ""


def _extract_moves(sgf: str) -> list:
    """;B[xy] 또는 ;W[xy] 패턴 순서대로 추출."""
    pattern = re.compile(r";\s*([BW])\s*\[([a-z]{0,2})\]")
    moves = []
    for m in pattern.finditer(sgf):
        color = "black" if m.group(1) == "B" else "white"
        coord = m.group(2)
        if not coord:
            # 패스 — 일단 무시
            continue
        col = _sgf_coord_to_idx(coord[0])
        row = _sgf_coord_to_idx(coord[1])
        moves.append({"row": row, "col": col, "color": color, "comment": ""})
    return moves


def parse_sgf(sgf_text: str, kifu_id: str, fallback_title: str) -> dict:
    size_str = _parse_property(sgf_text, "SZ")
    size = int(size_str) if size_str and size_str.isdigit() else 19

    pb = _parse_property(sgf_text, "PB") or ""
    pw = _parse_property(sgf_text, "PW") or ""
    dt = _parse_property(sgf_text, "DT") or ""
    re_str = _parse_property(sgf_text, "RE") or ""
    ev = _parse_property(sgf_text, "EV") or ""
    gn = _parse_property(sgf_text, "GN") or ""

    title_parts = [p for p in (gn, ev, f"{pb} vs {pw}".strip(" vs")) if p]
    title = next((p for p in title_parts if p), fallback_title)

    moves = _extract_moves(sgf_text)

    return {
        "id": kifu_id,
        "title": title,
        "black_player": pb or "흑",
        "white_player": pw or "백",
        "date": dt,
        "board_size": size,
        "view_area": [0, 0, size - 1, size - 1],
        "description": f"{title} ({len(moves)}수)",
        "moves": moves,
        "result": re_str,
        "winner": _parse_winner(re_str),
    }


def convert_files(paths: list) -> dict:
    kifus = []
    for p in paths:
        path = Path(p)
        text = path.read_text(encoding="utf-8", errors="replace")
        kifu = parse_sgf(text, kifu_id=path.stem, fallback_title=path.stem)
        kifus.append(kifu)

    return {
        "version": "0.1",
        "total": len(kifus),
        "kifus": kifus,
    }


def main():
    ap = argparse.ArgumentParser(description="SGF → KifuDatabase JSON 변환")
    ap.add_argument("inputs", nargs="+", help="SGF 파일 경로 (1개 이상)")
    ap.add_argument("-o", "--output", help="출력 JSON 경로 (기본: stdout)")
    args = ap.parse_args()

    db = convert_files(args.inputs)
    text = json.dumps(db, ensure_ascii=False, indent=2)

    if args.output:
        Path(args.output).write_text(text, encoding="utf-8")
        print(f"[sgf_to_json] {len(db['kifus'])}개 기보 → {args.output}", file=sys.stderr)
    else:
        print(text)


if __name__ == "__main__":
    main()
