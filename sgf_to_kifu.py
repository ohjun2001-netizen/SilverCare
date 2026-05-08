#!/usr/bin/env python3
"""
sgf_to_kifu.py: SGF 기보 파일 -> SilverCare kifu JSON 변환기
분기 시 첫 번째 변화도(실제 대국)를 따라갑니다.
Usage: python sgf_to_kifu.py [sgf_dir] [output_json]
"""

import re, json, sys, random
from pathlib import Path


# ── 토크나이저 ────────────────────────────────────────────────────────────
def tokenize(text):
    tokens = []
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c in '();':
            tokens.append(c)
            i += 1
        elif c.isupper():
            j = i
            while j < n and text[j].isupper():
                j += 1
            prop = text[i:j]
            i = j
            values = []
            while i < n and text[i] == '[':
                i += 1
                v = []
                while i < n and text[i] != ']':
                    if text[i] == '\\':
                        i += 1
                        if i < n: v.append(text[i])
                    else:
                        v.append(text[i])
                    i += 1
                values.append(''.join(v))
                if i < n: i += 1
            tokens.append((prop, values))
        else:
            i += 1
    return tokens


# ── 파서: 첫 번째 분기를 실제 대국으로 추적 ──────────────────────────────
def parse_game_tree(tokens):
    pos = [0]

    def parse_node():
        props = {}
        while pos[0] < len(tokens) and isinstance(tokens[pos[0]], tuple):
            prop, vals = tokens[pos[0]]
            props[prop] = vals[0] if vals else ''
            pos[0] += 1
        return props

    def skip_tree():
        depth = 1
        pos[0] += 1
        while pos[0] < len(tokens) and depth > 0:
            t = tokens[pos[0]]
            if t == '(':   depth += 1
            elif t == ')': depth -= 1
            pos[0] += 1

    def parse_tree():
        assert tokens[pos[0]] == '('
        pos[0] += 1
        nodes = []
        first_child_done = False
        while pos[0] < len(tokens) and tokens[pos[0]] != ')':
            t = tokens[pos[0]]
            if t == ';':
                pos[0] += 1
                nodes.append(parse_node())
            elif t == '(':
                if not first_child_done:
                    nodes.extend(parse_tree())
                    first_child_done = True
                else:
                    skip_tree()
        if pos[0] < len(tokens): pos[0] += 1
        return nodes

    return parse_tree()


# ── 예측 포인트 자동 생성 ────────────────────────────────────────────────
# 특정 기보의 하드코딩 예측포인트 (자동생성 후 병합)
SPECIAL_POINTS = {
    'alphago_lsd_g4': [
        {
            'move_index':   77,
            'candidates':   [
                {'row': 8, 'col': 10},
                {'row': 7, 'col': 10},
                {'row': 9, 'col': 10},
                {'row': 8, 'col':  9}
            ],
            'correct_index': 0,
            'question':     "백의 다음 수는? 이세돌의 '신의 한 수'를 맞혀보세요!",
            'explanation':  "백 78수 — 이세돌의 '신의 한 수'. 알파고의 수읽기를 무너뜨린 전설의 쐐기입니다."
        }
    ]
}


def generate_prediction_points(moves, interval=25):
    total = len(moves)
    if total < interval:
        return []

    # interval 수마다 1개 생성 (첫 interval수부터 마지막 10수 전까지)
    indices = list(range(interval, total - 10, interval))
    points = []

    for idx in indices:
        correct = moves[idx]
        cr, cc = correct['row'], correct['col']
        used = {(cr, cc)}

        # 대안 후보: 게임의 다른 지점 수들 (이미 둔 자리지만 시각적으로 자연스러움)
        alts = []
        for offset in [total // 5, total // 3, total * 2 // 3, total * 4 // 5]:
            j = (idx + offset) % total
            if j == idx: continue
            m = moves[j]
            pos_key = (m['row'], m['col'])
            if pos_key not in used:
                alts.append({'row': m['row'], 'col': m['col']})
                used.add(pos_key)
            if len(alts) == 3: break

        # 부족하면 오프셋 위치로 채움
        for dr, dc in [(5,5), (-5,5), (5,-5), (-5,-5), (0,7), (7,0)]:
            if len(alts) >= 3: break
            nr, nc = max(0, min(18, cr+dr)), max(0, min(18, cc+dc))
            if (nr, nc) not in used:
                alts.append({'row': nr, 'col': nc})
                used.add((nr, nc))

        candidates = [{'row': cr, 'col': cc}] + alts[:3]
        color = correct['color']
        question = '흑의 다음 수, 어디일까요?' if color == 'black' else '백의 다음 수, 어디일까요?'
        explanation = f'{"흑" if color == "black" else "백"}의 {idx+1}번째 수입니다. 바둑판 전체 흐름을 읽어보세요.'

        points.append({
            'move_index':   idx,
            'candidates':   candidates,
            'correct_index': 0,
            'question':     question,
            'explanation':  explanation
        })

    return points


# ── SGF 파일 처리 ─────────────────────────────────────────────────────────
def parse_sgf_file(path):
    text = Path(path).read_text(encoding='utf-8', errors='replace')
    tokens = tokenize(text)
    nodes = parse_game_tree(tokens)

    root = nodes[0] if nodes else {}
    moves = []
    for node in nodes[1:]:
        color, coord = None, None
        if 'B' in node:   color, coord = 'black', node['B']
        elif 'W' in node: color, coord = 'white', node['W']
        if color and coord and len(coord) == 2:
            col = ord(coord[0]) - ord('a')
            row = ord(coord[1]) - ord('a')
            if 0 <= row <= 18 and 0 <= col <= 18:
                moves.append({'row': row, 'col': col, 'color': color, 'comment': ''})

    return root, moves


def determine_winner(re_str):
    s = (re_str or '').upper()
    if s.startswith('B'): return 'black'
    if s.startswith('W'): return 'white'
    return ''


GAME_INFO = {
    '2016.03.09': (1, '알파고 vs 이세돌 1국', '알파고(백) 불계승'),
    '2016.03.10': (2, '이세돌 vs 알파고 2국', '알파고(백) 불계승'),
    '2016.03.12': (3, '알파고 vs 이세돌 3국', '알파고(백) 불계승'),
    '2016.03.13': (4, '이세돌 vs 알파고 4국 — 신의 한 수', '이세돌(백) 불계승'),
    '2016.03.15': (5, '알파고 vs 이세돌 5국', '알파고(흑) 불계승'),
}

DESCRIPTIONS = {
    1: '2016년 3월, 구글 딥마인드의 알파고와 이세돌 9단의 세기의 5번기 1국. 알파고가 백을 잡아 불계승했습니다.',
    2: '5번기 2국. 알파고가 두 번째 승리를 거두며 전 세계를 놀라게 했습니다.',
    3: '5번기 3국. 알파고의 연승, 이세돌은 기로에 섭니다.',
    4: '5번기 4국. 이세돌 9단의 역사적인 "신의 한 수"(78수)로 유일한 인간의 승리를 거뒀습니다.',
    5: '5번기 최종국. 알파고가 흑을 잡아 최종 4승 1패로 대국을 마무리했습니다.',
}


def convert_directory(sgf_dir, output_path):
    sgf_files = sorted(Path(sgf_dir).glob('*.sgf'))
    kifus = []
    for sgf_path in sgf_files:
        print(f'변환 중: {sgf_path.name}')
        root, moves = parse_sgf_file(sgf_path)

        game_num, title, result_ko = next(
            ((n, t, r) for date, (n, t, r) in GAME_INFO.items() if date in sgf_path.name),
            (0, sgf_path.stem, '')
        )

        re_str = root.get('RE', '')
        pp = generate_prediction_points(moves, interval=25)
        # 특수 포인트 병합 (중복 move_index 제거)
        specials = SPECIAL_POINTS.get(kifu['id'] if False else f'alphago_lsd_g{game_num}', [])
        existing_indices = {p['move_index'] for p in pp}
        for sp in specials:
            if sp['move_index'] not in existing_indices:
                pp.append(sp)
        pp.sort(key=lambda p: p['move_index'])
        print(f'  -> {len(moves)}수, 예측포인트 {len(pp)}개')

        kifu = {
            'id':                f'alphago_lsd_g{game_num}',
            'title':             title,
            'black_player':      root.get('PB', 'Black'),
            'white_player':      root.get('PW', 'White'),
            'date':              root.get('DT', ''),
            'board_size':        19,
            'view_area':         [0, 0, 18, 18],
            'description':       DESCRIPTIONS.get(game_num, ''),
            'moves':             moves,
            'result':            result_ko or re_str,
            'winner':            determine_winner(re_str),
            'prediction_points': pp
        }
        kifus.append(kifu)

    output = {'version': '0.4', 'total': len(kifus), 'kifus': kifus}
    Path(output_path).write_text(
        json.dumps(output, ensure_ascii=False, indent=2),
        encoding='utf-8'
    )
    print(f'\n저장 완료: {output_path}  (총 {len(kifus)}개 기보)')


if __name__ == '__main__':
    sgf_dir = sys.argv[1] if len(sys.argv) > 1 else r'C:\Users\ohjun\Downloads\leesedol'
    output  = sys.argv[2] if len(sys.argv) > 2 else r'E:\Projects\SilverCare\Assets\Resources\Data\kifu_alphago_lsd.json'
    convert_directory(sgf_dir, output)
