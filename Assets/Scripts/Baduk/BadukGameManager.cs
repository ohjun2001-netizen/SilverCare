// Assets/Scripts/Baduk/BadukGameManager.cs
// Desktop/VR 공용 - IBadukInput, IBadukUI 인터페이스로 입력/UI 추상화
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Baduk.Data;
using SilverCare.Common;

namespace Baduk
{
    public class BadukGameManager : MonoBehaviour
    {
        [Header("씬 이름")]
        [SerializeField] string lobbySceneName = "MainLobby";

        // 공유 컴포넌트 (자동 탐색)
        BadukBoard          _board;
        BadukProblemLoader  _loader;
        BadukAnswerChecker  _checker;
        BadukHintSystem     _hint;
        BadukTTSConnector   _tts;

        // 인터페이스 (Desktop 또는 VR 구현체)
        IBadukInput _input;
        IBadukUI    _ui;

        // VR 전용
        BadukVRBoardSetup _vrBoardSetup;

        BadukProblem _cur;
        int          _idx = 0;
        HashSet<(int r, int c)> _placedStones = new();

        List<BadukProblem> _filteredProblems = new();
        int _filteredIdx = 0;

        // ────────────────────────────────────────────────
        void Awake()
        {
            _board   = GetComponent<BadukBoard>();
            _loader  = GetComponent<BadukProblemLoader>();
            _checker = GetComponent<BadukAnswerChecker>();
            _hint    = GetComponent<BadukHintSystem>();
            _tts     = GetComponent<BadukTTSConnector>();

            // Desktop 또는 VR 입력 자동 감지
            _input = GetComponent<IBadukInput>();
            _ui    = GetComponent<IBadukUI>();

            // VR 보드 셋업 (있으면)
            _vrBoardSetup = GetComponent<BadukVRBoardSetup>();

            if (_input == null) Debug.LogError("[BadukGameManager] IBadukInput 구현체 없음 (BadukDesktopInput 또는 BadukVRInput 필요)");
            if (_ui == null) Debug.LogError("[BadukGameManager] IBadukUI 구현체 없음 (BadukDesktopUI 또는 BadukVRUI 필요)");
        }

        void Start()
        {
            // 입력 이벤트 연결
            _input.OnIntersectionClicked += HandlePlayerMove;

            // UI 버튼 이벤트 연결
            _ui.OnNext   = NextProblem;
            _ui.OnPrev   = PrevProblem;
            _ui.OnHint   = ShowHint;
            _ui.OnRetry  = () => LoadFilteredProblem(_filteredIdx);
            _ui.OnBack   = () => _ui.ShowDifficultySelect();  // 나가기 → 난이도 선택
            _ui.OnDifficultySelected = OnDifficultySelected;

            // 힌트 시스템 콜백
            _hint.OnHintTextReady = (text) => _ui.ShowHintText(text);

            // 난이도 선택 화면부터 시작
            _ui.ShowDifficultySelect();
        }

        void OnDestroy()
        {
            if (_input != null) _input.OnIntersectionClicked -= HandlePlayerMove;
        }

        // ── 난이도 선택 ───────────────────────────────────
        void OnDifficultySelected(int difficulty)
        {
            if (difficulty == 0)
            {
                _filteredProblems = new List<BadukProblem>();
                for (int i = 1; i <= _loader.TotalProblems; i++)
                {
                    var p = _loader.GetProblemById(i);
                    if (p != null) _filteredProblems.Add(p);
                }
            }
            else
            {
                _filteredProblems = _loader.GetProblemsByDifficulty(difficulty);
            }

            if (_filteredProblems.Count == 0)
            {
                Debug.LogWarning($"난이도 {difficulty} 문제 없음");
                return;
            }

            _filteredIdx = 0;
            LoadFilteredProblem(0);
        }

        // ── 문제 로드 ─────────────────────────────────────
        void LoadFilteredProblem(int idx)
        {
            if (_filteredProblems.Count == 0) return;
            _filteredIdx = idx;
            _cur = _filteredProblems[idx];

            _placedStones.Clear();
            _board.SetupBoard(_cur);
            _checker.SetProblem(_cur);
            _hint.SetProblem(_cur);
            _ui.ShowProblem(_cur, idx + 1, _filteredProblems.Count);

            // 입력 컴포넌트에 보드 준비 알림 (카메라/위치 조정)
            _input.OnBoardReady(_board.R0, _board.C0, _board.R1, _board.C1);
            _input.EnableInput();

            // VR: 교차점에 XR Interactable 부착
            _vrBoardSetup?.AttachInteractables();

            // 공통 TTSManager 우선, 없으면 자체 BadukTTSConnector 사용
            if (TTSManager.Instance != null)
                TTSManager.Instance.Speak(_cur.description, interruptCurrent: true);
            else
                _tts?.Speak(_cur.description);
        }

        void NextProblem()
        {
            int next = _filteredIdx + 1;
            if (next >= _filteredProblems.Count) next = 0;
            LoadFilteredProblem(next);
        }

        void PrevProblem()
        {
            int prev = _filteredIdx - 1;
            if (prev < 0) prev = _filteredProblems.Count - 1;
            LoadFilteredProblem(prev);
        }

        // ── 클릭 처리 ─────────────────────────────────────
        void HandlePlayerMove(int row, int col)
        {
            if (IsOccupied(row, col)) return;

            StoneType playerStone = _cur.player == "black" ? StoneType.Black : StoneType.White;
            _board.PlaceStone(row, col, playerStone);
            _placedStones.Add((row, col));

            ProblemResult result = _checker.CheckMove(row, col);

            switch (result)
            {
                case ProblemResult.Correct:
                    _ui.ShowResult(result, _cur.explanation);
                    _input.DisableInput();
                    _board.HideHintMarker();
                    SpeakTTS(_cur.tts_correct);
                    break;

                case ProblemResult.Wrong:
                    _ui.ShowResult(result);
                    _board.HighlightPlayerStone(Color.red);
                    SpeakTTS(_cur.tts_wrong);
                    _hint.ResetOnWrongAnswer();
                    StartCoroutine(RemoveWrongStoneAfterDelay(1.2f));
                    break;

                case ProblemResult.PartialCorrect:
                    _ui.ShowResult(result);
                    break;
            }
        }

        IEnumerator RemoveWrongStoneAfterDelay(float delay)
        {
            _input.DisableInput();
            yield return new WaitForSeconds(delay);
            _board.RemoveAllPlayerStones();
            _placedStones.Clear();
            _input.EnableInput();
        }

        void ShowHint()
        {
            _hint.ShowHint();
        }

        // 공통 TTSManager 우선, 없으면 자체 BadukTTSConnector 폴백
        void SpeakTTS(string text)
        {
            if (TTSManager.Instance != null)
                TTSManager.Instance.Speak(text, interruptCurrent: true);
            else
                _tts?.Speak(text);
        }

        bool IsOccupied(int row, int col)
        {
            if (_placedStones.Contains((row, col))) return true;
            if (_cur.stones?.black != null)
                foreach (var s in _cur.stones.black)
                    if (s.row == row && s.col == col) return true;
            if (_cur.stones?.white != null)
                foreach (var s in _cur.stones.white)
                    if (s.row == row && s.col == col) return true;
            return false;
        }
    }
}
