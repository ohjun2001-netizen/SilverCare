// Assets/Editor/HwatooCardGenerator.cs
// Unity 메뉴 → Tools → SilverCare → 화투 카드 생성
// 실행하면 Assets/Resources/GoStop/card_00.png ~ card_47.png 48장 자동 생성
using UnityEngine;
using UnityEditor;
using System.IO;

namespace SilverCare.GoStop
{
    public static class HwatooCardGenerator
    {
        const int W = 128, H = 192;

        // ── 월별 색상 팔레트 ────────────────────────────────────
        static readonly Color[] BgColors =
        {
            new(0.05f,0.16f,0.05f), // 1월 소나무 — 짙은 녹
            new(0.18f,0.06f,0.10f), // 2월 매화   — 짙은 다홍
            new(0.20f,0.09f,0.14f), // 3월 벚꽃   — 짙은 분홍
            new(0.12f,0.07f,0.20f), // 4월 등나무 — 짙은 보라
            new(0.06f,0.10f,0.22f), // 5월 난초   — 짙은 남
            new(0.20f,0.05f,0.15f), // 6월 모란   — 짙은 자주
            new(0.22f,0.05f,0.05f), // 7월 홍싸리 — 짙은 빨강
            new(0.16f,0.11f,0.03f), // 8월 공산   — 짙은 황토
            new(0.15f,0.15f,0.03f), // 9월 국화   — 짙은 노랑
            new(0.20f,0.05f,0.03f), // 10월 단풍  — 짙은 주황
            new(0.07f,0.05f,0.14f), // 11월 오동  — 짙은 남보라
            new(0.03f,0.07f,0.18f), // 12월 비    — 짙은 남색
        };

        static readonly Color[] PlantColors =
        {
            new(0.20f,0.80f,0.20f), // 1월
            new(1.00f,0.65f,0.75f), // 2월
            new(1.00f,0.78f,0.88f), // 3월
            new(0.78f,0.52f,0.96f), // 4월
            new(0.52f,0.62f,1.00f), // 5월
            new(1.00f,0.38f,0.70f), // 6월
            new(1.00f,0.38f,0.38f), // 7월
            new(0.95f,0.90f,0.65f), // 8월
            new(1.00f,0.92f,0.18f), // 9월
            new(0.95f,0.28f,0.08f), // 10월
            new(0.76f,0.55f,0.96f), // 11월
            new(0.58f,0.80f,1.00f), // 12월
        };

        static readonly Color Gold   = new(0.92f, 0.76f, 0.12f);
        static readonly Color Red    = new(0.80f, 0.05f, 0.05f);
        static readonly Color White  = Color.white;

        // ── 48장 카드 정의 ───────────────────────────────────────
        struct CardDef { public int month; public CardType type; }

        static readonly CardDef[] Defs =
        {
            new(){month=1,  type=CardType.Gwang},
            new(){month=1,  type=CardType.Yul},
            new(){month=1,  type=CardType.Tti},
            new(){month=1,  type=CardType.Pi},
            new(){month=2,  type=CardType.Yul},
            new(){month=2,  type=CardType.Yul},
            new(){month=2,  type=CardType.Tti},
            new(){month=2,  type=CardType.Pi},
            new(){month=3,  type=CardType.Gwang},
            new(){month=3,  type=CardType.Yul},
            new(){month=3,  type=CardType.Tti},
            new(){month=3,  type=CardType.Pi},
            new(){month=4,  type=CardType.Yul},
            new(){month=4,  type=CardType.Yul},
            new(){month=4,  type=CardType.Tti},
            new(){month=4,  type=CardType.Pi},
            new(){month=5,  type=CardType.Yul},
            new(){month=5,  type=CardType.Yul},
            new(){month=5,  type=CardType.Tti},
            new(){month=5,  type=CardType.Pi},
            new(){month=6,  type=CardType.Gwang},
            new(){month=6,  type=CardType.Yul},
            new(){month=6,  type=CardType.Tti},
            new(){month=6,  type=CardType.Pi},
            new(){month=7,  type=CardType.Yul},
            new(){month=7,  type=CardType.Yul},
            new(){month=7,  type=CardType.Tti},
            new(){month=7,  type=CardType.Pi},
            new(){month=8,  type=CardType.Gwang},
            new(){month=8,  type=CardType.Yul},
            new(){month=8,  type=CardType.Tti},
            new(){month=8,  type=CardType.Pi},
            new(){month=9,  type=CardType.Yul},
            new(){month=9,  type=CardType.Yul},
            new(){month=9,  type=CardType.Tti},
            new(){month=9,  type=CardType.Pi},
            new(){month=10, type=CardType.Yul},
            new(){month=10, type=CardType.Yul},
            new(){month=10, type=CardType.Tti},
            new(){month=10, type=CardType.Pi},
            new(){month=11, type=CardType.Pi},
            new(){month=11, type=CardType.Pi},
            new(){month=11, type=CardType.Pi},
            new(){month=11, type=CardType.Pi},
            new(){month=12, type=CardType.Gwang},
            new(){month=12, type=CardType.Yul},
            new(){month=12, type=CardType.Pi},
            new(){month=12, type=CardType.Pi},
        };

        // ── 메뉴 진입점 ──────────────────────────────────────────
        [MenuItem("Tools/SilverCare/화투 카드 생성 (48장)")]
        static void Generate()
        {
            string dir = "Assets/Resources/GoStop";
            Directory.CreateDirectory(dir);

            for (int i = 0; i < Defs.Length; i++)
            {
                var tex  = DrawCard(Defs[i], i);
                byte[] png = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);
                File.WriteAllBytes($"{dir}/card_{i:D2}.png", png);
            }

            AssetDatabase.Refresh();

            // Sprite 타입으로 일괄 설정
            for (int i = 0; i < Defs.Length; i++)
            {
                string path = $"{dir}/card_{i:D2}.png";
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.filterMode  = FilterMode.Bilinear;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }

            Debug.Log("[화투] 48장 생성 완료 → Assets/Resources/GoStop/");
            EditorUtility.DisplayDialog("완료", "화투 카드 48장 생성 완료!", "확인");
        }

        // ── 카드 1장 그리기 ──────────────────────────────────────
        static Texture2D DrawCard(CardDef def, int idx)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            Color bg    = BgColors   [def.month - 1];
            Color plant = PlantColors[def.month - 1];

            // 전체 배경
            Fill(tex, bg);

            // 빨간 테두리 (6px)
            FillRect(tex, 0, 0, W, H, Red);
            // 내부 배경
            FillRect(tex, 6, 6, W - 12, H - 12, bg);
            // 금색 내부 테두리
            DrawBorder1px(tex, 6, 6, W - 12, H - 12, Gold);

            // 월별 식물 일러스트 (중앙 영역)
            DrawPlant(tex, def.month, plant, bg);

            // 타입 오버레이
            DrawTypeOverlay(tex, def.type, def.month);

            // 좌상단 월 번호
            FillRect(tex, 8, H - 26, 24, 18, new Color(0, 0, 0, 0.6f));
            DrawSmallNum(tex, def.month, 10, H - 24, White);

            tex.Apply();
            return tex;
        }

        // ── 월별 식물 일러스트 ────────────────────────────────────
        static void DrawPlant(Texture2D tex, int month, Color plant, Color bg)
        {
            int cx = W / 2, cy = H / 2;

            switch (month)
            {
                case 1: // 소나무 — 삼각형 층 3개
                    FillRect(tex, cx-2, 40, 4, 50, new Color(0.35f,0.20f,0.08f));
                    FillTriangle(tex, cx, 32, 24, plant);
                    FillTriangle(tex, cx, 52, 32, plant);
                    FillTriangle(tex, cx, 72, 40, plant);
                    break;

                case 2: // 매화 — 원형 꽃 5송이
                    Circle(tex, cx,      cy,     16, plant);
                    Circle(tex, cx - 18, cy - 12, 11, plant);
                    Circle(tex, cx + 18, cy - 12, 11, plant);
                    Circle(tex, cx - 14, cy + 20, 10, plant);
                    Circle(tex, cx + 14, cy + 20, 10, plant);
                    Circle(tex, cx,      cy,       6, Gold);
                    Circle(tex, cx - 18, cy - 12,  4, Gold);
                    Circle(tex, cx + 18, cy - 12,  4, Gold);
                    break;

                case 3: // 벚꽃 — 꽃잎 4방향 + 중심
                    Circle(tex, cx,      cy,     14, plant);
                    Circle(tex, cx - 16, cy,     11, plant);
                    Circle(tex, cx + 16, cy,     11, plant);
                    Circle(tex, cx,      cy - 18, 11, plant);
                    Circle(tex, cx,      cy + 18, 11, plant);
                    Circle(tex, cx,      cy,       7, new Color(1f,0.5f,0.7f));
                    break;

                case 4: // 등나무 — 세로로 늘어진 타원
                    for (int i = -2; i <= 2; i++)
                    {
                        int bx = cx + i * 12;
                        Ellipse(tex, bx, cy, 5, 22, plant);
                        Circle(tex, bx, cy + 22, 5, Color.Lerp(plant, White, 0.3f));
                    }
                    break;

                case 5: // 난초 — 붓꽃 형태
                    Ellipse(tex, cx,      cy,     22, 10, plant);
                    Ellipse(tex, cx - 14, cy - 22, 10, 20, plant);
                    Ellipse(tex, cx + 14, cy - 22, 10, 20, plant);
                    Circle (tex, cx,      cy - 8,   8, new Color(0.85f,0.65f,1f));
                    break;

                case 6: // 모란 — 겹꽃
                    Circle(tex, cx, cy, 26, plant);
                    Circle(tex, cx, cy, 20, Color.Lerp(plant, White, 0.25f));
                    Circle(tex, cx, cy, 14, plant);
                    Circle(tex, cx, cy,  8, Color.Lerp(plant, White, 0.4f));
                    Circle(tex, cx, cy,  4, Gold);
                    break;

                case 7: // 홍싸리 — 방사형 점 클러스터
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i * Mathf.PI / 6f;
                        int bx = cx + (int)(Mathf.Cos(a) * 22);
                        int by = cy + (int)(Mathf.Sin(a) * 26);
                        Circle(tex, bx, by, 5, plant);
                    }
                    Circle(tex, cx, cy, 8, plant);
                    break;

                case 8: // 억새 — 비스듬한 이삭 줄기
                    FillRect(tex, cx - 28, cy + 18, 56, 4, new Color(0.28f,0.18f,0.04f));
                    for (int i = -3; i <= 3; i++)
                        Line(tex, cx + i * 8, cy + 18, cx + i * 8 + 14, cy - 28, plant, 3);
                    break;

                case 9: // 국화 — 방사선 꽃잎
                    for (int i = 0; i < 16; i++)
                    {
                        float a = i * Mathf.PI / 8f;
                        int ex = cx + (int)(Mathf.Cos(a) * 26);
                        int ey = cy + (int)(Mathf.Sin(a) * 26);
                        Line(tex, cx, cy, ex, ey, plant, 5);
                    }
                    Circle(tex, cx, cy, 10, new Color(1f, 0.75f, 0.1f));
                    Circle(tex, cx, cy,  5, Gold);
                    break;

                case 10: // 단풍 — 가지 + 잎
                    FillRect(tex, cx-2, cy+5, 4, 40, new Color(0.40f,0.12f,0.04f));
                    Line(tex, cx, cy+5, cx-22, cy-20, plant, 4);
                    Line(tex, cx, cy+5, cx+22, cy-20, plant, 4);
                    Line(tex, cx, cy+5, cx-12, cy-32, plant, 4);
                    Line(tex, cx, cy+5, cx+12, cy-32, plant, 4);
                    Line(tex, cx, cy+5, cx,    cy-36, plant, 4);
                    Circle(tex, cx-22, cy-20, 8, plant);
                    Circle(tex, cx+22, cy-20, 8, plant);
                    Circle(tex, cx-12, cy-32, 7, plant);
                    Circle(tex, cx+12, cy-32, 7, plant);
                    Circle(tex, cx,    cy-36, 8, plant);
                    break;

                case 11: // 오동 — 꽃봉오리 클러스터
                    Circle(tex, cx,      cy - 20, 11, plant);
                    Circle(tex, cx - 20, cy - 2,  10, plant);
                    Circle(tex, cx + 20, cy - 2,  10, plant);
                    Circle(tex, cx - 12, cy + 18,  9, plant);
                    Circle(tex, cx + 12, cy + 18,  9, plant);
                    break;

                case 12: // 비 — 빗줄기 + 우산
                    for (int i = 0; i < 8; i++)
                        Line(tex, 10 + i * 15, 42, 6 + i * 15, 95, plant, 2);
                    // 우산 (반원)
                    for (int x2 = cx - 20; x2 <= cx + 20; x2++)
                    {
                        int dist = (int)Mathf.Sqrt(Mathf.Max(0, 400 - (x2 - cx) * (x2 - cx)));
                        if (dist == 0) continue;
                        int y2 = 105 - dist;
                        if (y2 >= 0 && y2 < H) tex.SetPixel(x2, y2, new Color(0.7f,0.5f,0.2f));
                    }
                    FillRect(tex, cx-1, 85, 2, 24, new Color(0.7f,0.5f,0.2f));
                    break;
            }
        }

        // ── 타입 오버레이 ─────────────────────────────────────────
        static void DrawTypeOverlay(Texture2D tex, CardType type, int month)
        {
            int cx = W / 2;

            switch (type)
            {
                case CardType.Gwang:
                    // 금빛 후광 + 光 글자 영역
                    Circle(tex, cx, H/2, 22, new Color(1f, 0.85f, 0.1f, 0.5f));
                    Circle(tex, cx, H/2,  9, new Color(1f, 1f, 0.9f));
                    FillRect(tex, 8, 10, W - 16, 20, Gold);
                    DrawBorder1px(tex, 8, 10, W - 16, 20, White);
                    // 光 픽셀 문자
                    DrawKanji光(tex, cx - 12, 14);
                    break;

                case CardType.Tti:
                    Color rc = month is 1 or 2 or 3  ? new Color(0.90f,0.08f,0.08f)
                             : month is 6 or 9 or 10 ? new Color(0.08f,0.25f,0.90f)
                             :                          new Color(0.10f,0.65f,0.10f);
                    FillRect(tex, 8,  H - 40, W - 16, 18, rc);
                    DrawBorder1px(tex, 8, H - 40, W - 16, 18, White);
                    // 리본 무늬 선
                    FillRect(tex, 8, H - 34, W - 16, 2, Color.Lerp(rc, White, 0.4f));
                    break;

                case CardType.Pi:
                    // 피 표시: 하단 작은 회색 원
                    Circle(tex, cx, H - 22, 7, new Color(0.55f, 0.55f, 0.55f));
                    Circle(tex, cx, H - 22, 4, new Color(0.70f, 0.70f, 0.70f));
                    break;

                case CardType.Yul:
                    // 열끗: 양 끝 금색 원
                    Circle(tex, 18,   H - 22, 6, Gold);
                    Circle(tex, W-18, H - 22, 6, Gold);
                    Circle(tex, 18,   H - 22, 3, White);
                    Circle(tex, W-18, H - 22, 3, White);
                    break;
            }
        }

        // ── 光 한자 픽셀 (24×16) ─────────────────────────────────
        static void DrawKanji光(Texture2D tex, int x, int y)
        {
            Color c = White;
            // 가로획 3개
            FillRect(tex, x,      y,     24, 2, c);
            FillRect(tex, x + 5,  y + 4, 14, 2, c);
            FillRect(tex, x,      y + 9, 24, 2, c);
            // 세로획
            FillRect(tex, x + 11, y,      2, 12, c);
            // 아래 펼침
            FillRect(tex, x + 4,  y + 12,  6, 3, c);
            FillRect(tex, x + 14, y + 12,  6, 3, c);
        }

        // ── 작은 월 숫자 (7세그먼트) ─────────────────────────────
        static void DrawSmallNum(Texture2D tex, int n, int x, int y, Color col)
        {
            bool[][] seg = {
                new[]{true,true,true,false,true,true,true},
                new[]{false,false,true,false,false,true,false},
                new[]{true,false,true,true,true,false,true},
                new[]{true,false,true,true,false,true,true},
                new[]{false,true,true,true,false,true,false},
                new[]{true,true,false,true,false,true,true},
                new[]{true,true,false,true,true,true,true},
                new[]{true,false,true,false,false,true,false},
                new[]{true,true,true,true,true,true,true},
                new[]{true,true,true,true,false,true,true},
            };
            string s = n.ToString();
            for (int i = 0; i < s.Length; i++)
            {
                int d = s[i] - '0';
                int ox = x + i * 9;
                bool[] g = seg[d];
                int t = 1;
                if (g[0]) FillRect(tex, ox,     y + 12, 7, t, col);
                if (g[1]) FillRect(tex, ox,     y + 7,  t, 5, col);
                if (g[2]) FillRect(tex, ox + 6, y + 7,  t, 5, col);
                if (g[3]) FillRect(tex, ox,     y + 6,  7, t, col);
                if (g[4]) FillRect(tex, ox,     y + 1,  t, 5, col);
                if (g[5]) FillRect(tex, ox + 6, y + 1,  t, 5, col);
                if (g[6]) FillRect(tex, ox,     y,      7, t, col);
            }
        }

        // ── 드로잉 유틸 ──────────────────────────────────────────
        static void Fill(Texture2D tex, Color c)
        {
            var px = new Color[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            tex.SetPixels(px);
        }

        static void FillRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            for (int yy = Mathf.Max(0,y); yy < Mathf.Min(H,y+h); yy++)
                for (int xx = Mathf.Max(0,x); xx < Mathf.Min(W,x+w); xx++)
                    tex.SetPixel(xx, yy, c);
        }

        static void DrawBorder1px(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            FillRect(tex, x,       y,       w, 1, c);
            FillRect(tex, x,       y+h-1,   w, 1, c);
            FillRect(tex, x,       y,       1, h, c);
            FillRect(tex, x+w-1,   y,       1, h, c);
        }

        static void Circle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            for (int y = cy-r; y <= cy+r; y++)
                for (int x = cx-r; x <= cx+r; x++)
                {
                    if (x < 0 || x >= W || y < 0 || y >= H) continue;
                    if ((x-cx)*(x-cx) + (y-cy)*(y-cy) <= r*r)
                    {
                        Color existing = tex.GetPixel(x, y);
                        tex.SetPixel(x, y, c.a >= 1f ? c : Color.Lerp(existing, c, c.a));
                    }
                }
        }

        static void Ellipse(Texture2D tex, int cx, int cy, int rx, int ry, Color c)
        {
            for (int y = cy-ry; y <= cy+ry; y++)
                for (int x = cx-rx; x <= cx+rx; x++)
                {
                    if (x < 0 || x >= W || y < 0 || y >= H) continue;
                    float fx = (float)(x-cx)/rx, fy = (float)(y-cy)/ry;
                    if (fx*fx + fy*fy <= 1f) tex.SetPixel(x, y, c);
                }
        }

        static void FillTriangle(Texture2D tex, int cx, int tipY, int baseW, Color c)
        {
            int baseY = tipY + baseW / 2;
            for (int y = tipY; y <= baseY; y++)
            {
                float t  = (float)(y - tipY) / (baseY - tipY);
                int   hw = (int)(t * baseW / 2);
                FillRect(tex, cx - hw, y, hw * 2 + 1, 1, c);
            }
        }

        static void Line(Texture2D tex, int x0, int y0, int x1, int y1, Color c, int thick=1)
        {
            int dx = Mathf.Abs(x1-x0), dy = Mathf.Abs(y1-y0);
            int sx = x0<x1?1:-1, sy = y0<y1?1:-1, err = dx-dy;
            int x = x0, y = y0;
            while (true)
            {
                FillRect(tex, x-thick/2, y-thick/2, thick, thick, c);
                if (x==x1 && y==y1) break;
                int e2 = 2*err;
                if (e2 > -dy){err-=dy; x+=sx;}
                if (e2 <  dx){err+=dx; y+=sy;}
            }
        }
    }
}
