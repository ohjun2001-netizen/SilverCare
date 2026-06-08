using System.Collections.Generic;
using UnityEngine;
using Baduk.Data;
using Baduk.Replay;
using SilverCare.Common;
using Unity.XR.CoreUtils;

namespace Baduk.Prediction
{
    public class PredictionGameManager : MonoBehaviour
    {
        [SerializeField] string lobbySceneName = "MainLobby";

        KifuLoader _loader;
        KifuReplayManager _replay;
        PredictionVRUI _ui;
        BadukVRBoardSetup _vrBoardSetup;

        Kifu _currentKifu;
        PredictionPoint _activePoint;
        HashSet<int> _consumedPoints = new();
        int _correctCount;
        int _totalAsked;
        bool _manuallyPaused;
        bool _answeredCurrentPoint;

        Vector3 _originXRPos;
        Quaternion _originXRRot;
        bool _originSaved;

        void Awake()
        {
            _loader = GetComponent<KifuLoader>();
            _replay = GetComponent<KifuReplayManager>();
            _ui = GetComponent<PredictionVRUI>();
            _vrBoardSetup = GetComponent<BadukVRBoardSetup>();

            if (_loader == null) Debug.LogError("[PredictionGameManager] KifuLoader missing.");
            if (_replay == null) Debug.LogError("[PredictionGameManager] KifuReplayManager missing.");
            if (_ui == null) Debug.LogError("[PredictionGameManager] PredictionVRUI missing.");
        }

        void Start()
        {
            _ui.OnKifuSelected = HandleKifuSelected;
            _ui.OnPlayPause = () =>
            {
                _manuallyPaused = _replay.IsPlaying;
                _replay.TogglePlayPause();
            };
            _ui.OnSpeedChanged = s => _replay.SetSpeed(s);
            _ui.OnPredictionSubmit = HandlePredictionSubmitted;
            _ui.OnPredictionContinue = HandlePredictionContinue;
            _ui.OnRestart = HandleRestart;
            _ui.OnBack = HandleBack;
            _ui.OnBackToSelect = HandleBackToSelect;

            _replay.OnMoveAdvanced = HandleMoveAdvanced;
            _replay.OnPlaybackStateChanged = () => _ui.UpdatePlayPauseLabel(_replay.IsPlaying);
            _replay.OnReplayEnded = HandleReplayEnded;

            StartCoroutine(DelayedInit());
        }

        System.Collections.IEnumerator DelayedInit()
        {
            yield return null;
            yield return null;

            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                _originXRPos = xrOrigin.transform.position;
                _originXRRot = xrOrigin.transform.rotation;
                _originSaved = true;
            }

            ShowKifuSelect();
        }

        void ShowKifuSelect()
        {
            _currentKifu = null;
            _activePoint = null;
            _consumedPoints.Clear();
            _correctCount = 0;
            _totalAsked = 0;
            _ui.ShowKifuSelect(_loader.AllKifus);
        }

        void HandleKifuSelected(Kifu kifu)
        {
            _currentKifu = kifu;
            _consumedPoints.Clear();
            _correctCount = 0;
            _totalAsked = 0;
            _manuallyPaused = false;

            _replay.LoadKifu(kifu, _loader.Comments);
            SetupBoardAndRoom();
            _ui.ShowReplay(kifu);
            _replay.Play();
        }

        void RestoreOrigin()
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null && _originSaved)
            {
                xrOrigin.transform.position = _originXRPos;
                xrOrigin.transform.rotation = _originXRRot;
            }
        }

        void SetupBoardAndRoom()
        {
            var board = GetComponent<BadukBoard>();
            if (board == null)
                return;

            RestoreOrigin();

            Camera cam = Camera.main;
            string sceneKey = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            BadukDeskLayoutUtility.UpdateSceneAnchor(sceneKey, cam);
            float cx = (board.C1 - board.C0) * BadukBoard.CELL / 2f;
            float cy = (board.R1 - board.R0) * BadukBoard.CELL / 2f;

            BadukDeskLayoutUtility.ApplyDeskLayout(
                board.transform,
                cx,
                cy,
                0.92f,
                2.5f,
                0.85f,
                sceneKey,
                cam,
                out Vector3 boardCenter,
                out float tableY);

            float scale = board.transform.localScale.x;
            BadukRoomEnvironment.Spawn(
                boardCenter,
                cx * scale,
                cy * scale,
                tableY,
                board.transform.rotation,
                true,
                BadukRoomEnvironment.SceneStyle.Practice,
                spawnSpectators: true);
            _vrBoardSetup?.AttachInteractables();
            XRUIUtility.StepPlayerBack(1.5f);
        }

        void HandleMoveAdvanced(int cur, int total)
        {
            _ui.UpdateProgress(cur, total);

            var point = FindPredictionPoint(cur);
            if (point == null || _consumedPoints.Contains(cur))
                return;

            _consumedPoints.Add(cur);
            _activePoint = point;
            _answeredCurrentPoint = false;
            _replay.Pause();
            _totalAsked++;
            _ui.ShowPredictionOverlay(point);
        }

        PredictionPoint FindPredictionPoint(int moveIndex)
        {
            if (_currentKifu?.prediction_points == null)
                return null;

            foreach (var point in _currentKifu.prediction_points)
            {
                if (point.move_index == moveIndex)
                    return point;
            }

            return null;
        }

        void HandlePredictionSubmitted(int chosenCandidateIndex)
        {
            if (_activePoint == null)
                return;
            if (_answeredCurrentPoint)
                return;

            bool correct = chosenCandidateIndex == _activePoint.correct_index;
            if (correct)
            {
                _answeredCurrentPoint = true;
                _correctCount++;
            }

            _ui.ShowPredictionResult(correct, _activePoint, chosenCandidateIndex);
        }

        void HandlePredictionContinue()
        {
            if (_activePoint == null || !_answeredCurrentPoint)
                return;

            _ui.HidePredictionOverlay();
            _activePoint = null;
            _answeredCurrentPoint = false;

            if (_currentKifu != null && _replay.MoveIndex < _replay.TotalMoves && !_manuallyPaused)
                _replay.Play();
        }

        void HandleReplayEnded()
        {
            _ui.ShowResult(_correctCount, _totalAsked);
            bool firstClear = StoryProgressManager.Instance != null &&
                              StoryProgressManager.Instance.TryMarkActivityCleared(
                                  StoryProgressManager.StoryActivity.BadukPrediction);
            if (firstClear)
                StoryProgressManager.Instance?.SpeakClearNarration(StoryProgressManager.StoryActivity.BadukPrediction);
        }

        void HandleRestart()
        {
            if (_currentKifu == null)
            {
                ShowKifuSelect();
                return;
            }

            HandleKifuSelected(_currentKifu);
        }

        void HandleBackToSelect()
        {
            _replay.Pause();
            GetComponent<NpcAvatarSpawner>()?.Despawn();
            BadukRoomEnvironment.Cleanup();
            SelectionBackdropUtility.ClearAllBackdrops();

            var board = GetComponent<BadukBoard>();
            if (board != null)
            {
                board.RemoveAllPlayerStones();
                board.transform.position = new Vector3(0f, -100f, 0f);
            }

            BadukDeskLayoutUtility.ClearSceneAnchor(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            RestoreOrigin();
            ShowKifuSelect();
        }

        void HandleBack()
        {
            _replay.Pause();
            LoadLobby();
        }

        void LoadLobby()
        {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(GameSceneManager.SCENE_LOBBY);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }
    }
}
