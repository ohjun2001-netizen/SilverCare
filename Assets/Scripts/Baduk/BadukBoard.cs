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

        AudioSource        _clackSource;
        static AudioClip   _clackClip;

        void EnsureMaterials()
        {
            if (_matBlack != null) return;
            BuildMaterials();
        }

        // 돌 놓는 "탁" 소리 — 에셋/인스펙터 연결 없이 코드로 생성
        void EnsureClackAudio()
        {
            if (_clackSource != null) return;
            if (_clackClip == null) _clackClip = BuildClackClip();

            // 보드 transform을 건드리지 않도록 전용 자식 오브젝트에 부착
            var go = new GameObject("ClackAudio");
            go.transform.SetParent(transform, false);
            _clackSource = go.AddComponent<AudioSource>();
            _clackSource.clip          = _clackClip;
            _clackSource.playOnAwake   = false;
            _clackSource.volume        = 1f;
            _clackSource.spatialBlend  = 1f;      // 3D — 돌 위치에서 들림 (VR 대응)
            _clackSource.minDistance   = 2.5f;    // 가까이서 충분히 크게
            _clackSource.maxDistance   = 18f;
            _clackSource.dopplerLevel  = 0f;
        }

        /// <summary>나무 바둑판에 돌 놓는 "탁" 소리 합성 (짧은 트랜지언트 + 우디 레저넌스)</summary>
        static AudioClip BuildClackClip()
        {
            const int   rate     = 44100;
            const float duration = 0.10f;
            int   samples = (int)(rate * duration);
            var   data    = new float[samples];
            var   rng     = new System.Random(7);

            // 나무 + 돌 공명 주파수
            const float f1 = 1100f, f2 = 1800f, f3 = 2600f;
            for (int i = 0; i < samples; i++)
            {
                float t   = (float)i / rate;
                float env = Mathf.Exp(-t * 55f);                                   // 전체 감쇠
                float click = ((float)rng.NextDouble() * 2f - 1f)                  // 초기 클릭(노이즈 버스트)
                            * Mathf.Exp(-t * 450f) * 0.6f;
                float body = (Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.5f
                            + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.3f
                            + Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.2f) * env;
                data[i] = Mathf.Clamp(click + body, -1f, 1f) * 0.92f;
            }

            var clip = AudioClip.Create("BadukClack", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ── 공개 메서드 ──────────────────────────────────

        /// <summary>문제 데이터로 보드 초기화 (이전 보드 삭제 후 재생성)</summary>
        public void SetupBoard(BadukProblem problem)
        {
            EnsureMaterials();
            ClearBoard();
            // ApplyDeskLayout 호출 전까지 보드가 화면에 보이지 않도록 아래쪽에 숨긴다.
            // 이전 ApplyDeskLayout 의 스케일이 남을 수 있으므로 localScale 도 초기화한다.
            transform.SetPositionAndRotation(new Vector3(0f, -100f, 0f), Quaternion.identity);
            transform.localScale = Vector3.one;

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

            EnsureClackAudio();
            _clackSource.transform.localPosition = GridToWorld(row, col);  // 돌 위치에서 재생
            _clackSource.PlayOneShot(_clackClip, 1f);
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
            _hintMarker = new GameObject("HintMarker");
            _hintMarker.transform.SetParent(transform, false);
            _hintMarker.transform.localPosition = GridToWorld(row, col) + Vector3.up * 0.04f;

            var ringMaterial = MakeMat(new Color(1.0f, 0.82f, 0.18f), 0f, new Color(0.32f, 0.22f, 0.02f));
            const int segments = 12;
            float radius = STONE * 0.82f;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                segment.name = $"HintRing_{i}";
                segment.transform.SetParent(_hintMarker.transform, false);
                segment.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                segment.transform.localScale = new Vector3(0.05f, 0.015f, 0.05f);
                Destroy(segment.GetComponent<Collider>());
                segment.GetComponent<Renderer>().material = ringMaterial;
            }

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

        public bool TryWorldToIntersection(Vector3 worldPoint, out int row, out int col)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);

            int nearestCol = Mathf.RoundToInt(local.x / CELL) + _c0;
            int nearestRow = Mathf.RoundToInt(-local.z / CELL) + _r0;

            if (nearestRow < _r0 || nearestRow > _r1 || nearestCol < _c0 || nearestCol > _c1)
            {
                row = -1;
                col = -1;
                return false;
            }

            row = nearestRow;
            col = nearestCol;
            return true;
        }

        // ── 내부 ─────────────────────────────────────────

        System.Collections.IEnumerator BlinkMarker()
        {
            if (_hintMarker == null) yield break;
            bool visible = true;
            while (_hintMarker != null)
            {
                _hintMarker.SetActive(visible);
                visible = !visible;
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

        public void ClearBoard()
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
