// Assets/Scripts/Baduk/Prediction/PredictionGameManager.cs
// 다음 수 맞히기 모드 전체 조율
// 흐름: 기보 선택 → 자동 재생 → 예측 포인트 도달 시 일시정지 → 후보 선택 → 정/오답 → 재생 재개 → 종료/결과
using System.Collections;
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
        [SerializeField] float resumeDelayAfterAnswer = 2.0f;

        // 컴포넌트 (자동 탐색)
        KifuLoader        _loader;
        KifuReplayManager _replay;
        NpcAvatarSpawner  _avatars;
        PredictionVRUI    _ui;

        // 상태
        Kifu              _currentKifu;
        PredictionPoint   _activePoint;
        HashSet<int>      _consumedPoints = new();
        int               _correctCount;
        int               _totalAsked;
        bool              _manuallyPaused;

        // 씬 시작 시점 XR Origin 상태 — 그만 보기 시 원위치 복원용
        Vector3    _originXRPos;
        Quaternion _originXRRot;
        bool       _originSaved;

        // PC 폴백용 카메라 상태
        Vector3    _originCamPos;
        Quaternion _originCamRot;
        bool       _originCamSaved;

        void Awake()
        {
            _loader  = GetComponent<KifuLoader>();
            _replay  = GetComponent<KifuReplayManager>();
            _avatars = GetComponent<NpcAvatarSpawner>();
            _ui      = GetComponent<PredictionVRUI>();

            if (_loader == null) Debug.LogError("[PredictionGameManager] KifuLoader 없음");
            if (_replay == null) Debug.LogError("[PredictionGameManager] KifuReplayManager 없음");
            if (_ui == null)     Debug.LogError("[PredictionGameManager] PredictionVRUI 없음");
        }

        void Start()
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                _originXRPos = xrOrigin.transform.position;
                _originXRRot = xrOrigin.transform.rotation;
                _originSaved = true;
            }
            Camera cam0 = Camera.main;
            if (cam0 != null)
            {
                _originCamPos   = cam0.transform.position;
                _originCamRot   = cam0.transform.rotation;
                _originCamSaved = true;
            }

            _ui.OnKifuSelected     = HandleKifuSelected;
            _ui.OnPlayPause        = () => {
                if (_replay.IsPlaying) _manuallyPaused = true;
                else                   _manuallyPaused = false;
                _replay.TogglePlayPause();
            };
            _ui.OnSpeedChanged     = (s) => _replay.SetSpeed(s);
            _ui.OnPredictionSubmit = HandlePredictionSubmitted;
            _ui.OnRestart          = HandleRestart;
            _ui.OnBack             = HandleBack;
            _ui.OnBackToSelect     = HandleBackToSelect;

            _replay.OnMoveAdvanced         = HandleMoveAdvanced;
            _replay.OnPlaybackStateChanged = () => _ui.UpdatePlayPauseLabel(_replay.IsPlaying);
            _replay.OnReplayEnded          = HandleReplayEnded;

            ShowKifuSelect();
        }

        // ── 화면 흐름 ────────────────────────────────────

        void ShowKifuSelect()
        {
            _currentKifu = null;
            _activePoint = null;
            _consumedPoints.Clear();
            _correctCount = 0;
            _totalAsked   = 0;
            _ui.ShowKifuSelect(_loader.AllKifus);
        }

        void HandleKifuSelected(Kifu kifu)
        {
            _currentKifu    = kifu;
            _consumedPoints.Clear();
            _correctCount   = 0;
            _totalAsked     = 0;
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
            Camera cam = Camera.main;
            if (cam != null && _originCamSaved)
            {
                cam.transform.position = _originCamPos;
                cam.transform.rotation = _originCamRot;
            }
        }

        void SetupBoardAndRoom()
        {
            var board = GetComponent<BadukBoard>();
            if (board == null) return;

            Camera cam = Camera.main;

            float cx = (board.C1 - board.C0) * BadukBoard.CELL / 2f;
            float cy = (board.R1 - board.R0) * BadukBoard.CELL / 2f;

            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0, 1f, 0);
            Vector3 fwd = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (fwd == Vector3.zero) fwd = Vector3.forward;

            float maxSize = 0.75f;
            float boardWorldMax = Mathf.Max(cx * 2f, cy * 2f);
            float scale = boardWorldMax > 0f ? Mathf.Min(1f, maxSize / boardWorldMax) : 1f;

            board.transform.localScale = Vector3.one * scale;
            board.transform.rotation   = Quaternion.LookRotation(fwd, Vector3.up);

            float tableY = camPos.y - 0.35f;
            Vector3 boardCenter = camPos + fwd * 0.65f;
            boardCenter.y = tableY;
            board.transform.position =
                boardCenter - board.transform.rotation * new Vector3(cx, 0f, -cy) * scale;

            BadukRoomEnvironment.Spawn(boardCenter, cx * scale, cy * scale, tableY,
                                       board.transform.rotation);

            _avatars?.Spawn(board.transform);
        }

        void HandleMoveAdvanced(int cur, int total)
        {
            _ui.UpdateProgress(cur, total);

            var pp = FindPredictionPoint(cur);
            if (pp != null && !_consumedPoints.Contains(cur))
            {
                _consumedPoints.Add(cur);
                _activePoint = pp;
                _replay.Pause();
                _totalAsked++;
                _ui.ShowPredictionOverlay(pp);
            }
        }

        PredictionPoint FindPredictionPoint(int moveIndex)
        {
            if (_currentKifu?.prediction_points == null) return null;
            foreach (var p in _currentKifu.prediction_points)
                if (p.move_index == moveIndex) return p;
            return null;
        }

        void HandlePredictionSubmitted(int chosenCandidateIndex)
        {
            if (_activePoint == null) return;

            bool correct = chosenCandidateIndex == _activePoint.correct_index;
            if (correct) _correctCount++;

            _ui.ShowPredictionResult(correct, _activePoint, chosenCandidateIndex);
            StartCoroutine(ResumeAfterAnswer());
        }

        IEnumerator ResumeAfterAnswer()
        {
            yield return new WaitForSeconds(resumeDelayAfterAnswer);
            _ui.HidePredictionOverlay();
            _activePoint = null;
            if (_currentKifu != null && _replay.MoveIndex < _replay.TotalMoves && !_manuallyPaused)
                _replay.Play();
        }

        void HandleReplayEnded()
        {
            _ui.ShowResult(_correctCount, _totalAsked);
        }

        void HandleRestart()
        {
            if (_currentKifu == null) { ShowKifuSelect(); return; }
            HandleKifuSelected(_currentKifu);
        }

        void HandleBackToSelect()
        {
            _replay.Pause();
            BadukRoomEnvironment.Cleanup();
            _avatars?.Despawn();
            var board = GetComponent<BadukBoard>();
            if (board != null)
            {
                board.RemoveAllPlayerStones();
                board.transform.position = new Vector3(0f, -100f, 0f);
            }
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
