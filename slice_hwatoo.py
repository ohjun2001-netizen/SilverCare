#!/usr/bin/env python3
"""
화투 48장 이미지 슬라이서 — 정밀 좌표 버전
행: 빨간 테두리 피크 분석으로 정확한 경계 결정
열: 쌍 테두리 중간값 사용
"""
from PIL import Image
import os

SRC = r"C:\Users\ohjun\Downloads\ChatGPT Image 2026년 5월 10일 오전 03_37_41.png"
DST = r"E:\Projects\SilverCare\Assets\Resources\GoStop"

img = Image.open(SRC).convert("RGB")
W, H = img.size
print(f"이미지: {W}x{H}")
os.makedirs(DST, exist_ok=True)

# ── 열 슬롯 (쌍 테두리 중간값 사용) ──────────────────────
# 좌측 테이블: 외부 x=103, 쌍 테두리 256(249/264), 415(407/423), 577(570/585), 734
LEFT_SLOTS = [
    (103, 256),  # col0: 1번(피)
    (256, 415),  # col1: 2번(띠)
    (415, 577),  # col2: 3번(광/열끗)
    (577, 734),  # col3: 4번(열꽃)
]

# 우측 테이블: 외부 x=872, 쌍 테두리 1026(1019/1033), 1188(1180/1196), 1350(1343/1358), 1508
RIGHT_SLOTS = [
    ( 872, 1026),  # col0: 1번(피)
    (1026, 1188),  # col1: 2번(띠)
    (1188, 1350),  # col2: 3번(광/열끗)
    (1350, 1508),  # col3: 4번(열꽃)
]

# ── 행 슬롯 (내부 카드 경계 기준) ────────────────────────
# 외부 그리드 경계: y=75-77 (상단), y=1009-1010 (하단)
# 행 간 구분 (외부 테두리): y=219-221, 377-393, 534-552, 692-709, 849-865
# Row0는 외부 그리드 경계 사용, Row1-5는 내부 카드 경계(232, 391, 550, 707, 863) 사용
ROW_SLOTS = [
    ( 75,  221),  # row0: 1월/7월  (146px, 외부 경계 공유)
    (232,  379),  # row1: 2월/8월  (147px, 내부 카드 경계)
    (391,  535),  # row2: 3월/9월  (144px)
    (550,  693),  # row3: 4월/10월 (143px)
    (707,  851),  # row4: 5월/11월 (144px)
    (863, 1010),  # row5: 6월/12월 (147px)
]

PAD = 5  # 빨간 테두리 건너뜀

# ── BuildDeck 매핑 ────────────────────────────────────────
# 이미지 열: col0=1번(피), col1=2번(띠), col2=3번(광/열끗), col3=4번(열꽃)
COL_ORDER = {
    1:  [2,3,1,0],  # 솔광(c2), 솔학(c3), 솔홍띠(c1), 솔피(c0)
    2:  [2,3,1,0],  # 매조(c2), 매열끗(c3), 매홍띠(c1), 매피(c0)
    3:  [2,3,1,0],  # 벚광(c2), 벚조(c3), 벚홍띠(c1), 벚피(c0)
    4:  [2,3,1,0],  # 등두견(c2), 등열끗(c3), 등초단(c1), 등피(c0)
    5:  [2,3,1,0],  # 난조(c2), 난열끗(c3), 난초단(c1), 난피(c0)
    6:  [2,3,1,0],  # 모란광(c2), 모란나비(c3), 모란청단(c1), 모란피(c0)
    7:  [2,3,1,0],  # 홍돼지(c2), 홍열끗(c3), 홍초단(c1), 홍피(c0)
    8:  [2,3,1,0],  # 공산광(c2), 공산기러기(c3), 공산띠(c1), 공산피(c0)
    9:  [2,3,1,0],  # 국화술(c2), 국화열끗(c3), 국화청단(c1), 국화피(c0)
    10: [2,3,1,0],  # 단풍사슴(c2), 단풍열끗(c3), 단풍청단(c1), 단풍피(c0)
    11: [0,1,2,3],  # 오동피1(c0), 오동피2(c1), 오동피3(c2), 오동쌍피(c3)
    12: [2,3,0,1],  # 비광(c2), 비열끗(c3), 비피1(c0), 비피2(c1)
}

def crop_card(col_slots, row_slots, img_col, row):
    x0, x1 = col_slots[img_col]
    y0, y1 = row_slots[row]
    return img.crop((x0+PAD, y0+PAD, x1-PAD, y1-PAD)).resize((128, 192), Image.LANCZOS)

idx = 0
for month in range(1, 13):
    row   = (month - 1) % 6
    right = month > 6
    slots = RIGHT_SLOTS if right else LEFT_SLOTS
    for img_col in COL_ORDER[month]:
        card = crop_card(slots, ROW_SLOTS, img_col, row)
        path = os.path.join(DST, f"card_{idx:02d}.png")
        card.save(path)
        print(f"card_{idx:02d}.png ← 월{month} col{img_col} row{row}")
        idx += 1

print(f"\n완료: {idx}장 → {DST}")
