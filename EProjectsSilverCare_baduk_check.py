import json, sys

with open(r'E:\Projects\SilverCare\Assets\Resources\Data\baduk_problems.json', encoding='utf-8') as f:
    data = json.load(f)

def build(prob):
    b = {}  # (r,c) -> 'B'/'W'
    for s in prob['stones']['black']:
        b[(s['row'], s['col'])] = 'B'
    for s in prob['stones']['white']:
        b[(s['row'], s['col'])] = 'W'
    return b

def neighbors(r,c,size):
    for dr,dc in ((1,0),(-1,0),(0,1),(0,-1)):
        nr,nc=r+dr,c+dc
        if 0<=nr<size and 0<=nc<size:
            yield nr,nc

def group_and_libs(board, start, size):
    color = board[start]
    stack=[start]; seen={start}; libs=set()
    while stack:
        r,c=stack.pop()
        for nr,nc in neighbors(r,c,size):
            if (nr,nc) not in board:
                libs.add((nr,nc))
            elif board[(nr,nc)]==color and (nr,nc) not in seen:
                seen.add((nr,nc)); stack.append((nr,nc))
    return seen, libs

def play(board, r, c, color, size):
    """Play a stone, resolve captures of opponent. Return (new_board, captured_count) or None if illegal (suicide/occupied)."""
    if (r,c) in board:
        return None, 'occupied'
    board = dict(board)
    board[(r,c)] = color
    opp = 'W' if color=='B' else 'B'
    captured = 0
    # remove opponent groups with no libs
    checked=set()
    for nr,nc in neighbors(r,c,size):
        if board.get((nr,nc))==opp and (nr,nc) not in checked:
            grp, libs = group_and_libs(board,(nr,nc),size)
            checked|=grp
            if not libs:
                for g in grp: del board[g]
                captured += len(grp)
    # suicide check
    grp, libs = group_and_libs(board,(r,c),size)
    if not libs:
        return None, 'suicide'
    return board, captured

def is_real_eye(board, r, c, color, size):
    """Empty point fully surrounded orthogonally by color, with diagonal majority."""
    for nr,nc in neighbors(r,c,size):
        if board.get((nr,nc)) != color:
            return False
    # diagonals
    diag=[]
    on_edge=False
    for dr,dc in ((1,1),(1,-1),(-1,1),(-1,-1)):
        nr,nc=r+dr,c+dc
        if 0<=nr<size and 0<=nc<size:
            diag.append(board.get((nr,nc)))
        else:
            on_edge=True
    enemy_diag = sum(1 for d in diag if d is not None and d!=color)
    if on_edge:
        return enemy_diag==0
    return enemy_diag<=1

def count_eyes_of_group(board, grp_seed, size):
    color = board[grp_seed]
    grp,_ = group_and_libs(board, grp_seed, size)
    eyes=set()
    for r,c in grp:
        for nr,nc in neighbors(r,c,size):
            if (nr,nc) not in board and is_real_eye(board,nr,nc,color,size):
                eyes.add((nr,nc))
    return eyes, grp

print("="*70)
for prob in data['problems']:
    size = prob['board_size']
    board = build(prob)
    pid = prob['id']
    title = prob['title']
    goal = prob['goal']
    player = prob['player']
    pc = 'B' if player=='black' else 'W'
    opp = 'W' if pc=='B' else 'B'
    issues=[]

    # 1) overlap check
    bset = {(s['row'],s['col']) for s in prob['stones']['black']}
    wset = {(s['row'],s['col']) for s in prob['stones']['white']}
    overlap = bset & wset
    if overlap: issues.append(f"OVERLAP stones at {overlap}")

    # 2) answer occupancy / bounds
    ans = [(a['row'],a['col']) for a in prob['answer']]
    for (r,c) in ans:
        if not (0<=r<size and 0<=c<size): issues.append(f"answer {(r,c)} out of bounds")
        if (r,c) in board: issues.append(f"answer {(r,c)} on occupied point")

    # 3) check any side already in atari pre-move
    pre_black_atari=[]
    seen=set()
    for pt,col in list(board.items()):
        if pt in seen: continue
        grp,libs=group_and_libs(board,pt,size)
        seen|=grp
        if len(libs)==1:
            pre_black_atari.append((col,sorted(grp)[0],len(grp),next(iter(libs))))

    # apply player's answer moves sequentially (consecutive same-color, no opp response modeled)
    bd = dict(board)
    total_cap=0
    legal=True
    for (r,c) in ans:
        res, info = play(bd, r, c, pc, size)
        if res is None:
            issues.append(f"answer move {(r,c)} ILLEGAL ({info})")
            legal=False
            break
        bd = res
        if isinstance(info,int): total_cap+=info

    # evaluate by goal
    note=""
    if goal=='kill' and legal:
        # is any opponent group captured / now dead?
        # check remaining opponent groups' min libs
        seen=set(); minlib=99; opp_present=False
        for pt,col in list(bd.items()):
            if col!=opp: continue
            opp_present=True
            if pt in seen: continue
            grp,libs=group_and_libs(bd,pt,size); seen|=grp
            minlib=min(minlib,len(libs))
        note=f"captured={total_cap}, opp_present={opp_present}, opp_min_libs={minlib if opp_present else '-'}"
        if total_cap==0 and opp_present and minlib>1:
            issues.append(f"KILL goal but after answer: no capture, opp still has {minlib} libs")
    elif goal in ('survive','ko') and legal:
        eyes, grp = count_eyes_of_group(bd, ans[0] if ans[0] in bd else next(iter([p for p,c in bd.items() if c==pc])), size)
        # find player's main group containing answer
        # check player's group libs
        # locate the group that includes the last answer move
        seed = ans[-1]
        if seed in bd:
            g,libs = group_and_libs(bd, seed, size)
            ge = set()
            for (r,c) in g:
                for nr,nc in neighbors(r,c,size):
                    if (nr,nc) not in bd and is_real_eye(bd,nr,nc,pc,size):
                        ge.add((nr,nc))
            note=f"player group libs={len(libs)}, real_eyes={sorted(ge)} (count={len(ge)})"
            if goal=='survive' and len(ge)<2:
                issues.append(f"SURVIVE goal but player group has only {len(ge)} real eye(s): {sorted(ge)}")

    status = "  !! ISSUES" if issues else "  ok"
    print(f"[{pid:2}] {title} | goal={goal} | ans={ans}{status}")
    if note: print(f"      {note}")
    if pre_black_atari:
        print(f"      pre-move atari groups: {pre_black_atari}")
    for i in issues:
        print(f"      >>> {i}")
print("="*70)
