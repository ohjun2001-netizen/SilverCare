using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Baduk.Data;
using SilverCare.Common;

namespace Baduk
{
    public class BadukGameManager : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] string lobbySceneName = "MainLobby";

        BadukBoard _board;
        BadukProblemLoader _loader;
        BadukAnswerChecker _checker;
        BadukHintSystem _hint;
        BadukTTSConnector _tts;

        IBadukInput _input;
        IBadukUI _ui;
        BadukVRBoardSetup _vrBoardSetup;

        BadukProblem _cur;
        readonly HashSet<(int r, int c)> _placedStones = new();
        (int r, int c)? _selectedPlacement;
        bool _awaitingPlacementConfirm;

        List<BadukProblem> _filteredProblems = new();
        int _filteredIdx;

        void Awake()
        {
            _board = GetComponent<BadukBoard>();
            _loader = GetComponent<BadukProblemLoader>();
            _checker = GetComponent<BadukAnswerChecker>();
            _hint = GetComponent<BadukHintSystem>();
            _tts = GetComponent<BadukTTSConnector>();
            _input = GetComponent<IBadukInput>();
            _ui = GetComponent<IBadukUI>();
            _vrBoardSetup = GetComponent<BadukVRBoardSetup>();

            if (_input == null)
                Debug.LogError("[BadukGameManager] Missing IBadukInput implementation.");
            if (_ui == null)
                Debug.LogError("[BadukGameManager] Missing IBadukUI implementation.");
        }

        void Start()
        {
            _input.OnIntersectionClicked += HandlePlayerMove;

            _ui.OnNext = NextProblem;
            _ui.OnPrev = PrevProblem;
            _ui.OnHint = ShowHint;
            _ui.OnRetry = () => LoadFilteredProblem(_filteredIdx);
            _ui.OnBack = () =>
            {
                _board.ClearBoard();
                _ui.ShowDifficultySelect();
            };
            _ui.OnConfirmPlacement = ConfirmPlacement;
            _ui.OnCancelPlacement = CancelPlacement;
            _ui.OnDifficultySelected = OnDifficultySelected;

            _hint.OnHintTextReady = text => _ui.ShowHintText(text);
            _ui.ShowDifficultySelect();
        }

        void OnDestroy()
        {
            if (_input != null)
                _input.OnIntersectionClicked -= HandlePlayerMove;
        }

        void OnDifficultySelected(int difficulty)
        {
            if (difficulty == 0)
            {
                _filteredProblems = new List<BadukProblem>();
                for (int i = 1; i <= _loader.TotalProblems; i++)
                {
                    var problem = _loader.GetProblemById(i);
                    if (problem != null)
                        _filteredProblems.Add(problem);
                }
            }
            else
            {
                _filteredProblems = _loader.GetProblemsByDifficulty(difficulty);
            }

            if (_filteredProblems.Count == 0)
            {
                Debug.LogWarning($"No problems found for difficulty {difficulty}.");
                return;
            }

            _filteredIdx = 0;
            LoadFilteredProblem(0);
        }

        void LoadFilteredProblem(int idx)
        {
            if (_filteredProblems.Count == 0)
                return;

            _filteredIdx = idx;
            _cur = _filteredProblems[idx];
            _placedStones.Clear();
            _selectedPlacement = null;
            _awaitingPlacementConfirm = false;

            _board.SetupBoard(_cur);
            _checker.SetProblem(_cur);
            _hint.SetProblem(_cur);
            _ui.ShowProblem(_cur, idx + 1, _filteredProblems.Count);
            RefreshPlacementGuide();

            _input.OnBoardReady(_board.R0, _board.C0, _board.R1, _board.C1);
            _input.EnableInput();
            _vrBoardSetup?.AttachInteractables();

            if (TTSManager.Instance != null)
                TTSManager.Instance.Speak(_cur.description, interruptCurrent: true);
            else
                _tts?.Speak(_cur.description);
        }

        void NextProblem()
        {
            int next = _filteredIdx + 1;
            if (next >= _filteredProblems.Count)
                next = 0;

            LoadFilteredProblem(next);
        }

        void PrevProblem()
        {
            int prev = _filteredIdx - 1;
            if (prev < 0)
                prev = _filteredProblems.Count - 1;

            LoadFilteredProblem(prev);
        }

        void HandlePlayerMove(int row, int col)
        {
            if (_awaitingPlacementConfirm)
                return;

            if (IsOccupied(row, col))
                return;

            _selectedPlacement = (row, col);
            _awaitingPlacementConfirm = true;
            _board.ShowHintMarker(row, col);
            _ui.ShowPlacementConfirm("선택하신 자리가 여기 맞을까요?");
            // 확인창이 떠 있는 동안에는 보드 입력을 꺼서, 레이저가 확인창 뒤 보드로 새는 오작동을 막는다.
            _input.DisableInput();
            SpeakTTS("선택하신 자리가 여기 맞을까요? 맞으면 확인, 아니면 다시 선택해 주세요.");
        }

        void ConfirmPlacement()
        {
            if (!_selectedPlacement.HasValue)
                return;

            var placement = _selectedPlacement.Value;
            StoneType playerStone = _cur.player == "black" ? StoneType.Black : StoneType.White;
            _board.PlaceStone(placement.r, placement.c, playerStone);
            _placedStones.Add((placement.r, placement.c));

            _awaitingPlacementConfirm = false;
            _selectedPlacement = null;
            _ui.HidePlacementConfirm();
            _board.HideHintMarker();

            ProblemResult result = _checker.CheckMove(placement.r, placement.c);
            switch (result)
            {
                case ProblemResult.Correct:
                    _ui.ShowResult(result, _cur.explanation);
                    _input.DisableInput();
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
                    RefreshPlacementGuide();
                    _input.EnableInput();   // 다음 수를 두도록 보드 입력 재개
                    break;
            }
        }

        void CancelPlacement()
        {
            _awaitingPlacementConfirm = false;
            _selectedPlacement = null;
            _board.HideHintMarker();
            _ui.HidePlacementConfirm();
            _input.EnableInput();   // 확인창을 닫았으니 다시 자리를 고를 수 있게 보드 입력 재개
            _ui.ShowGuideMessage("괜찮습니다. 다시 바둑돌을 둘 자리를 눌러보세요.");
            SpeakTTS("괜찮습니다. 다시 고르시면 됩니다.");
        }

        IEnumerator RemoveWrongStoneAfterDelay(float delay)
        {
            _input.DisableInput();
            yield return new WaitForSeconds(delay);
            _board.RemoveAllPlayerStones();
            _placedStones.Clear();
            _selectedPlacement = null;
            _awaitingPlacementConfirm = false;
            _ui.HidePlacementConfirm();
            RefreshPlacementGuide();
            _input.EnableInput();
        }

        void ShowHint()
        {
            _hint.ShowHint();
        }

        void SpeakTTS(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (TTSManager.Instance != null)
                TTSManager.Instance.Speak(text, interruptCurrent: true);
            else
                _tts?.Speak(text);
        }

        bool IsOccupied(int row, int col)
        {
            if (_placedStones.Contains((row, col)))
                return true;

            if (_cur.stones?.black != null)
            {
                foreach (var stone in _cur.stones.black)
                {
                    if (stone.row == row && stone.col == col)
                        return true;
                }
            }

            if (_cur.stones?.white != null)
            {
                foreach (var stone in _cur.stones.white)
                {
                    if (stone.row == row && stone.col == col)
                        return true;
                }
            }

            return false;
        }

        void RefreshPlacementGuide()
        {
            if (_checker == null || _board == null || _ui == null)
                return;

            _selectedPlacement = null;
            _awaitingPlacementConfirm = false;
            _board.HideHintMarker();
            _ui.HidePlacementConfirm();
            _ui.ShowGuideMessage("바둑돌을 둘 자리를 누르면 확인창이 열립니다. 맞으면 확인을 눌러 진행하세요.");
        }
    }
}
