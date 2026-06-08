using System.Collections;
using System.Collections.Generic;
using Baduk.Data;
using SilverCare.Common;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Baduk
{
    public class BadukGameManager : MonoBehaviour
    {
        BadukBoard _board;
        BadukProblemLoader _loader;
        BadukAnswerChecker _checker;
        BadukHintSystem _hint;
        BadukTTSConnector _tts;

        IBadukInput _input;
        IBadukUI _ui;
        BadukVRBoardSetup _vrBoardSetup;

        BadukProblem _currentProblem;
        readonly HashSet<(int row, int col)> _placedStones = new();
        (int row, int col)? _selectedPlacement;
        bool _awaitingPlacementConfirm;

        List<BadukProblem> _filteredProblems = new();
        int _filteredIndex;

        Vector3 _originXRPos;
        Quaternion _originXRRot;
        bool _originXRSaved;

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
            _ui.OnRetry = () =>
            {
                if (_awaitingPlacementConfirm)
                    return;

                LoadFilteredProblem(_filteredIndex);
            };
            _ui.OnBack = () =>
            {
                if (_awaitingPlacementConfirm)
                    return;

                ReturnToDifficultySelect();
            };
            _ui.OnConfirmPlacement = ConfirmPlacement;
            _ui.OnCancelPlacement = CancelPlacement;
            _ui.OnDifficultySelected = OnDifficultySelected;

            _hint.OnHintTextReady = text => _ui.ShowHintText(text);

            StartCoroutine(DelayedStart());
        }

        IEnumerator DelayedStart()
        {
            yield return null;
            yield return null;

            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                _originXRPos = xrOrigin.transform.position;
                _originXRRot = xrOrigin.transform.rotation;
                _originXRSaved = true;
            }

            _ui.ShowDifficultySelect();
        }

        void OnDestroy()
        {
            if (_input != null)
                _input.OnIntersectionClicked -= HandlePlayerMove;

            BadukRoomEnvironment.Cleanup();
        }

        void RestoreOrigin()
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null && _originXRSaved)
            {
                xrOrigin.transform.position = _originXRPos;
                xrOrigin.transform.rotation = _originXRRot;
            }
        }

        void ReturnToDifficultySelect()
        {
            _board.ClearBoard();
            _board.HideHintMarker();
            _board.transform.position = new Vector3(0f, -100f, 0f);
            _placedStones.Clear();
            _selectedPlacement = null;
            _awaitingPlacementConfirm = false;

            BadukRoomEnvironment.Cleanup();
            BadukDeskLayoutUtility.ClearSceneAnchor(SceneManager.GetActiveScene().path);
            RestoreOrigin();
            _ui.ShowDifficultySelect();
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

            _filteredIndex = 0;
            BadukDeskLayoutUtility.ClearSceneAnchor(SceneManager.GetActiveScene().path);
            LoadFilteredProblem(0);
        }

        void LoadFilteredProblem(int index)
        {
            if (_filteredProblems.Count == 0)
                return;

            RestoreOrigin();

            _filteredIndex = index;
            _currentProblem = _filteredProblems[index];
            _placedStones.Clear();
            _selectedPlacement = null;
            _awaitingPlacementConfirm = false;

            _board.SetupBoard(_currentProblem);
            _checker.SetProblem(_currentProblem);
            _hint.SetProblem(_currentProblem);
            _ui.ShowProblem(_currentProblem, index + 1, _filteredProblems.Count);
            RefreshPlacementGuide();

            _input.OnBoardReady(_board.R0, _board.C0, _board.R1, _board.C1);
            _input.EnableInput();
            _vrBoardSetup?.AttachInteractables();

            SpeakTTS(_currentProblem.description);
        }

        void NextProblem()
        {
            if (_awaitingPlacementConfirm)
                return;

            int next = _filteredIndex + 1;
            if (next >= _filteredProblems.Count)
                next = 0;

            LoadFilteredProblem(next);
        }

        void PrevProblem()
        {
            if (_awaitingPlacementConfirm)
                return;

            int prev = _filteredIndex - 1;
            if (prev < 0)
                prev = _filteredProblems.Count - 1;

            LoadFilteredProblem(prev);
        }

        void HandlePlayerMove(int row, int col)
        {
            if (_awaitingPlacementConfirm || IsOccupied(row, col))
                return;

            _selectedPlacement = (row, col);
            _awaitingPlacementConfirm = true;
            _board.ShowHintMarker(row, col);
            _ui.ShowPlacementConfirm("선택하신 자리가 여기 맞을까요?");
            _input.DisableInput();
            SpeakTTS("선택하신 자리가 여기 맞을까요? 맞으면 확인, 아니면 다시 선택을 눌러 주세요.");
        }

        void ConfirmPlacement()
        {
            if (!_selectedPlacement.HasValue)
                return;

            var placement = _selectedPlacement.Value;
            StoneType playerStone = _currentProblem.player == "black" ? StoneType.Black : StoneType.White;
            _board.PlaceStone(placement.row, placement.col, playerStone);
            _placedStones.Add((placement.row, placement.col));

            _awaitingPlacementConfirm = false;
            _selectedPlacement = null;
            _ui.HidePlacementConfirm();
            _board.HideHintMarker();

            ProblemResult result = _checker.CheckMove(placement.row, placement.col);
            switch (result)
            {
                case ProblemResult.Correct:
                    HandleCorrectAnswer();
                    break;

                case ProblemResult.Wrong:
                    _ui.ShowResult(result);
                    _board.HighlightPlayerStone(Color.red);
                    SpeakTTS(_currentProblem.tts_wrong);
                    _hint.ResetOnWrongAnswer();
                    StartCoroutine(RemoveWrongStoneAfterDelay(1.2f));
                    break;

                case ProblemResult.PartialCorrect:
                    _ui.ShowResult(result);
                    RefreshPlacementGuide();
                    _input.EnableInput();
                    break;
            }
        }

        void HandleCorrectAnswer()
        {
            _ui.ShowResult(ProblemResult.Correct, _currentProblem.explanation);
            _input.DisableInput();

            bool firstProblemClear = StoryProgressManager.Instance != null &&
                                     StoryProgressManager.Instance.TryMarkActivityCleared(
                                         StoryProgressManager.StoryActivity.BadukProblem);

            string clearMessage = _currentProblem.tts_correct;
            if (StoryProgressManager.Instance != null)
            {
                if (firstProblemClear)
                {
                    string storyMessage = StoryProgressManager.Instance.GetClearNarration(
                        StoryProgressManager.StoryActivity.BadukProblem);
                    StoryProgressManager.Instance.ShowStoryPanel("바둑 사활 완료", storyMessage, 6.5f);
                    clearMessage = $"{_currentProblem.tts_correct} {storyMessage}";
                }
                else
                {
                    StoryProgressManager.Instance.ShowStoryPanel(
                        "정답입니다",
                        "좋습니다. 이 문제를 잘 해결하셨습니다. 다음 문제도 천천히 이어가 보세요.",
                        4.5f);
                }
            }

            SpeakTTS(clearMessage);
        }

        void CancelPlacement()
        {
            _awaitingPlacementConfirm = false;
            _selectedPlacement = null;
            _board.HideHintMarker();
            _ui.HidePlacementConfirm();
            _input.EnableInput();
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

            if (_currentProblem.stones?.black != null)
            {
                foreach (var stone in _currentProblem.stones.black)
                {
                    if (stone.row == row && stone.col == col)
                        return true;
                }
            }

            if (_currentProblem.stones?.white != null)
            {
                foreach (var stone in _currentProblem.stones.white)
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
