// Assets/Scripts/Baduk/BadukBoard.cs
// 순수 시각 컴포넌트 - 입력은 IBadukInput 구현체가 처리
using System.Collections.Generic;
using UnityEngine;
using Baduk.Data;

namespace Baduk
{
    public class BadukBoard : MonoBehaviour
    {
        public const float CELL  = 1.0f;
        public const float STONE = 0.44f;

        int _r0, _c0, _r1, _c1;

        // 외부에서 view_area 참조용
        public int R0 => _r0;
        public int C0 => _c0;
        public int R1 => _r1;
        public int C1 => _c1;

        List<GameObject> _objects       = new();
        List<GameObject> _playerStones  = new();
        GameObject       _lastStone;
        GameObject       _hintMarker;
        Coroutine        _hintBlink;

        Material _matBlack, _matWhite, _matBoard, _matLine;

        void EnsureMaterials()
        {
            if (_matBlack != null) return;
            BuildMaterials();
        }

        // ── 공개 메서드 ──────────────────────────────────

        /// <summary>문제 데이터로 보드 초기화 (이전 보드 삭제 후 재생성)</summary>
        public void SetupBoard(BadukProblem problem)
        {
            EnsureMaterials();
            ClearBoard();
            // OnBoardReady에서 회전 전 반드시 초기화 (2번째 문제부터 로컬 좌표 틀어짐 방지)
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            _r0 = problem.view_area[0]; _c0 = problem.view_area[1];
            _r1 = problem.view_area[2]; _c1 = problem.view_area[3];

            BuildBoardVisuals();
            PlaceInitialStones(problem.stones);
        }

        /// <summary>플레이어 돌 놓기</summary>
        public bool PlaceStone(int row, int col, StoneType type)
        {
            _lastStone = SpawnStone(row, col, type == StoneType.Black);
            _playerStones.Add(_lastStone);
            return true;
        }

        /// <summary>정답/오답 피드백: 마지막 놓은 돌 색 변경</summary>
        public void HighlightPlayerStone(Color color)
        {
            if (_lastStone == null) return;
            _lastStone.GetComponent<Renderer>().material = MakeMat(color);
        }

        /// <summary>마지막 플레이어 돌 제거</summary>
        public void RemoveLastPlayerStone()
        {
            if (_playerStones.Count == 0) return;
            var last = _playerStones[_playerStones.Count - 1];
            _playerStones.RemoveAt(_playerStones.Count - 1);
            if (last) Destroy(last);
            _lastStone = _playerStones.Count > 0 ? _playerStones[_playerStones.Count - 1] : null;
        }

        /// <summary>모든 플레이어 돌 제거 (다중 수 오답 시)</summary>
        public void RemoveAllPlayerStones()
        {
            foreach (var s in _playerStones) if (s) Destroy(s);
            _playerStones.Clear();
            _lastStone = null;
        }

        /// <summary>정답 위치에 깜빡이는 힌트 마커 표시</summary>
        public void ShowHintMarker(int row, int col)
        {
            HideHintMarker();
            EnsureMaterials();
            _hintMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _hintMarker.name = "HintMarker";
            _hintMarker.transform.SetParent(transform, false);
            _hintMarker.transform.localPosition = GridToWorld(row, col) + Vector3.up * 0.2f;
            _hintMarker.transform.localScale    = Vector3.one * STONE * 2.2f;
            Destroy(_hintMarker.GetComponent<Collider>());
            _hintMarker.GetComponent<Renderer>().material = MakeMat(new Color(0.2f, 1f, 0.2f, 0.5f));
            _hintBlink = StartCoroutine(BlinkMarker());
        }

        /// <summary>힌트 마커 제거</summary>
        public void HideHintMarker()
        {
            if (_hintBlink != null) { StopCoroutine(_hintBlink); _hintBlink = null; }
            if (_hintMarker != null) { Destroy(_hintMarker); _hintMarker = null; }
        }

        /// <summary>그리드 좌표 → 월드 좌표 변환 (외부에서도 사용)</summary>
        public Vector3 GridToWorld(int row, int col)
            => new Vector3((col - _c0) * CELL, 0f, -(row - _r0) * CELL);

        // ── 내부 ─────────────────────────────────────────

        System.Collections.IEnumerator BlinkMarker()
        {
            if (_hintMarker == null) yield break;
            var rend = _hintMarker.GetComponent<Renderer>();
            while (_hintMarker != null)
            {
                rend.enabled = !rend.enabled;
                yield return new WaitForSeconds(0.5f);
            }
        }

        void BuildBoardVisuals()
        {
            int rows = _r1 - _r0 + 1;
            int cols = _c1 - _c0 + 1;
            float w  = (cols - 1) * CELL;
            float h  = (rows - 1) * CELL;

            // 배경 (로컬 좌표로 배치 — 보드 회전 후에도 올바른 위치 유지)
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Board_BG";
            Destroy(bg.GetComponent<Collider>());
            bg.transform.SetParent(transform, false);
            bg.transform.localPosition = new Vector3(w / 2f, -0.02f, -h / 2f);
            bg.transform.localRotation = Quaternion.Euler(90, 0, 0);
            bg.transform.localScale    = new Vector3(w + CELL * 0.8f, h + CELL * 0.8f, 1f);
            bg.GetComponent<Renderer>().material = _matBoard;
            _objects.Add(bg);

            // 격자선
            for (int r = 0; r < rows; r++)
            {
                var line = MakeLine(new Vector3(0, 0, -r * CELL), new Vector3(w, 0, -r * CELL));
                line.transform.SetParent(transform, false);
                _objects.Add(line);
            }
            for (int c = 0; c < cols; c++)
            {
                var line = MakeLine(new Vector3(c * CELL, 0, 0), new Vector3(c * CELL, 0, -h));
                line.transform.SetParent(transform, false);
                _objects.Add(line);
            }

            // 클릭용 투명 구체 (Collider 유지 - 입력 컴포넌트가 Raycast로 감지)
            for (int r = _r0; r <= _r1; r++)
            for (int c = _c0; c <= _c1; c++)
            {
                var sp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sp.name = $"I_{r}_{c}";
                sp.transform.SetParent(transform, false);
                sp.transform.localPosition = GridToWorld(r, c);
                sp.transform.localScale    = Vector3.one * CELL * 0.85f;
                sp.GetComponent<Renderer>().enabled = false;
                _objects.Add(sp);
            }
        }

        GameObject MakeLine(Vector3 a, Vector3 b)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "Line";
            Destroy(obj.GetComponent<Collider>());
            obj.transform.localPosition = (a + b) / 2f + Vector3.up * 0.01f;
            bool  horiz = Mathf.Abs(a.x - b.x) > Mathf.Abs(a.z - b.z);
            float len   = Vector3.Distance(a, b);
            obj.transform.localScale = horiz
                ? new Vector3(len, 0.04f, 0.04f)
                : new Vector3(0.04f, 0.04f, len);
            obj.GetComponent<Renderer>().material = _matLine;
            return obj;
        }

        void PlaceInitialStones(BoardStones stones)
        {
            if (stones?.black != null)
                foreach (var p in stones.black)
                    _objects.Add(SpawnStone(p.row, p.col, true));

            if (stones?.white != null)
                foreach (var p in stones.white)
                    _objects.Add(SpawnStone(p.row, p.col, false));
        }

        GameObject SpawnStone(int row, int col, bool isBlack)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = $"Stone_{row}_{col}";
            s.transform.SetParent(transform, false);
            s.transform.localPosition = GridToWorld(row, col) + Vector3.up * 0.15f;
            s.transform.localScale    = Vector3.one * STONE * 2f;
            s.GetComponent<Renderer>().material = isBlack ? _matBlack : _matWhite;
            Destroy(s.GetComponent<Collider>());
            return s;
        }

        void ClearBoard()
        {
            HideHintMarker();
            foreach (var o in _objects) if (o) Destroy(o);
            _objects.Clear();
            foreach (var s in _playerStones) if (s) Destroy(s);
            _playerStones.Clear();
            _lastStone = null;
        }

        // ── 머티리얼 ─────────────────────────────────────
        void BuildMaterials()
        {
            _matBlack = MakeMat(Color.black);
            _matWhite = MakeMat(new Color(0.93f, 0.93f, 0.93f));
            _matBoard = MakeUnlitMat(new Color(0.78f, 0.66f, 0.42f));
            _matLine  = MakeMat(new Color(0.20f, 0.12f, 0.04f));
        }

        Material MakeMat(Color color, float glossiness = 0f, Color emission = default)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mat = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
            DestroyImmediate(tmp);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (glossiness > 0f && mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", glossiness);
            if (emission != default)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
            return mat;
        }

        // 조명 무관하게 정확한 색상 표시 (Unlit)
        static Material MakeUnlitMat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color")
                      ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            return mat;
        }
    }
}
