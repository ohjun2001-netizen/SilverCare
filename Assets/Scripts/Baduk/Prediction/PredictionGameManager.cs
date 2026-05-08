// Assets/Scripts/Baduk/Prediction/PredictionGameManager.cs
// 다음 수 맞히기 모드 전체 조율
// 흐름: 기보 선택 → 자동 재생 → 예측 포인트 도달 시 일시정지 → 후보 선택 → 정/오답 → 재생 재개 → 종료/결과
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Baduk.Data;
using Baduk.Replay;
using SilverCare.Common;

namespace Baduk.Prediction
{
    public class PredictionGameManager : MonoBehaviour
    {
        [SerializeField] string lobbySceneName = "MainLobby";
        [SerializeField] int rewardPerCorrect = 50;
        [SerializeField] float resumeDelayAfterAnswer = 2.0f;

        // 컴포넌트 (자동 탐색)
        KifuLoader        _loader;
        KifuReplayManager _replay;
        NpcAvatarSpawner  _avatars;
        PredictionVRUI    _ui;

        // 상태
        Kifu              _currentKifu;
        PredictionPoint   _activePoint;
        HashSet<int>      _consumedPoints = new();   // 이미 발동된 move_index 들
        int               _correctCount;
        int               _totalAsked;
        int               _coinsEarned;

        // 씬 시작 시점 카메라 상태 — 매번 동일한 기준으로 보드/룸 배치
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
            // 시나리오 시작 시점 카메라 상태 캡쳐
            Camera cam0 = Camera.main;
            if (cam0 != null)
            {
                _originCamPos = cam0.transform.position;
                _originCamRot = cam0.transform.rotation;
                _originCamSaved = true;
            }

            _ui.OnKifuSelected      = HandleKifuSelected;
            _ui.OnPlayPause         = () => _replay.TogglePlayPause();
            _ui.OnSpeedChanged      = (s) => _replay.SetSpeed(s);
            _ui.OnPredictionSubmit  = HandlePredictionSubmitted;
            _ui.OnRestart           = HandleRestart;
            _ui.OnBack              = HandleBack;

            _replay.OnMoveAdvanced         = HandleMoveAdvanced;
            _replay.OnPlaybackStateChanged = () => _ui.UpdatePlayPauseLabel(_replay.IsPlaying);
            _replay.OnReplayEnded          = HandleReplayEnded;

            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.OnCoinsChanged += OnCoinsChanged;

            ShowKifuSelect();
        }

        void OnDestroy()
        {
            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.OnCoinsChanged -= OnCoinsChanged;
        }

        void OnCoinsChanged(int coins) => _ui.UpdateCoins(coins);

        // ── 화면 흐름 ────────────────────────────────────

        void ShowKifuSelect()
        {
            _currentKifu = null;
            _activePoint = null;
            _consumedPoints.Clear();
            _correctCount = 0;
            _totalAsked = 0;
            _coinsEarned = 0;
            _ui.ShowKifuSelect(_loader.AllKifus, GetCoins());
        }

        void HandleKifuSelected(Kifu kifu)
        {
            _currentKifu = kifu;
            _consumedPoints.Clear();
            _correctCount = 0;
            _totalAsked = 0;
            _coinsEarned = 0;

            _replay.LoadKifu(kifu, _loader.Comments);
            // BadukBoard.SetupBoard 안에서 transform이 원점으로 리셋되므로 사용자 앞에 다시 배치
            SetupBoardAndRoom();
            _ui.ShowReplay(kifu, GetCoins());
            _replay.Play();
        }

        void SetupBoardAndRoom()
        {
            var board = GetComponent<BadukBoard>();
            if (board == null) return;

            // 매번 카메라를 씬 시작 시점 상태로 리셋 → 보드 배치가 결정론적
            Camera cam = Camera.main;
            if (cam != null && _originCamSaved)
            {
                cam.transform.position = _originCamPos;
                cam.transform.rotation = _originCamRot;
            }

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

            // 보드 위치 확정 후 아바타 재배치 (LoadKifu의 첫 spawn은 보드가 원점일 때라 위치 어긋남)
            _avatars?.Spawn(board.transform);
        }

        void HandleMoveAdvanced(int cur, int total)
        {
            _ui.UpdateProgress(cur, total);

            // cur = 다음에 둘 수의 인덱스. 이 인덱스를 가진 prediction_point 가 있으면 발동.
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
            if (correct)
            {
                _correctCount++;
                _coinsEarned += rewardPerCorrect;
                PlayerDataManager.Instance?.AddCoins(rewardPerCorrect);
            }

            _ui.ShowPredictionResult(correct, _activePoint, chosenCandidateIndex, rewardPerCorrect);
            StartCoroutine(ResumeAfterAnswer());
        }

        IEnumerator ResumeAfterAnswer()
        {
            yield return new WaitForSeconds(resumeDelayAfterAnswer);
            _ui.HidePredictionOverlay();
            _activePoint = null;
            if (_currentKifu != null && _replay.MoveIndex < _replay.TotalMoves)
                _replay.Play();
        }

        void HandleReplayEnded()
        {
            _ui.ShowResult(_correctCount, _totalAsked, _coinsEarned, GetCoins());
        }

        void HandleRestart()
        {
            if (_currentKifu == null) { ShowKifuSelect(); return; }
            HandleKifuSelected(_currentKifu);  // 같은 기보 재시작
        }

        void HandleBack()
        {
            _replay.Pause();
            if (_currentKifu == null) { LoadLobby(); return; }

            // select 패널 매번 동일 위치에 표시되도록 카메라 리셋
            Camera cam = Camera.main;
            if (cam != null && _originCamSaved)
            {
                cam.transform.position = _originCamPos;
                cam.transform.rotation = _originCamRot;
            }
            ShowKifuSelect();
        }

        // ── 헬퍼 ─────────────────────────────────────────
        static int GetCoins() => PlayerDataManager.Instance?.Coins ?? 0;

        void LoadLobby()
        {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(GameSceneManager.SCENE_LOBBY);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }
    }
}
