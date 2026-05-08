// Assets/Scripts/Baduk/Replay/KifuReplayGameManager.cs
// 기보 복기 모드 전체 조율 (BadukGameManager 패턴)
using UnityEngine;
using Baduk.Data;
using SilverCare.Common;

namespace Baduk.Replay
{
    public class KifuReplayGameManager : MonoBehaviour
    {
        [Header("종료 시 이동할 씬")]
        [SerializeField] string lobbySceneName = "MainLobby";

        [Header("기보 선택 후 자동 재생 시작")]
        [SerializeField] bool autoPlayOnLoad = true;

        KifuLoader        _loader;
        KifuReplayManager _replay;
        NpcCommentator    _commentator;
        KifuVRUI          _ui;
        BadukVRBoardSetup _vrBoardSetup;

        // UI 상태 — 무한 루프 방지: 어느 화면이 보이는지 기준으로 분기
        bool _inReplayView;

        // 씬 시작 시점 카메라 상태 — 매번 동일한 기준으로 보드/룸 배치(여러 번 진입해도 첫 진입과 동일하게)
        Vector3    _originCamPos;
        Quaternion _originCamRot;
        bool       _originCamSaved;

        void Awake()
        {
            _loader       = GetComponent<KifuLoader>();
            _replay       = GetComponent<KifuReplayManager>();
            _commentator  = GetComponent<NpcCommentator>();
            _ui           = GetComponent<KifuVRUI>();
            _vrBoardSetup = GetComponent<BadukVRBoardSetup>();

            if (_loader == null) Debug.LogError("[KifuReplayGameManager] KifuLoader 없음");
            if (_replay == null) Debug.LogError("[KifuReplayGameManager] KifuReplayManager 없음");
            if (_ui == null)     Debug.LogError("[KifuReplayGameManager] KifuVRUI 없음");
        }

        void Start()
        {
            // 시나리오 시작 시점 카메라 상태 캡쳐 (이후 모든 SetupBoardAndRoom의 기준)
            Camera cam = Camera.main;
            if (cam != null)
            {
                _originCamPos = cam.transform.position;
                _originCamRot = cam.transform.rotation;
                _originCamSaved = true;
            }

            // UI → 매니저
            _ui.OnKifuSelected = HandleKifuSelected;
            _ui.OnPlayPause    = () => _replay.TogglePlayPause();
            _ui.OnNext         = () => _replay.Next();
            _ui.OnPrev         = () => _replay.Prev();
            _ui.OnRestart      = () => HandleRestart();
            _ui.OnSpeedChanged = (s) => _replay.SetSpeed(s);
            _ui.OnBack         = HandleBack;

            // 매니저 → UI
            _replay.OnMoveAdvanced         = (cur, total) => _ui.UpdateProgress(cur, total);
            _replay.OnPlaybackStateChanged = ()           => _ui.UpdatePlayPauseLabel(_replay.IsPlaying);

            // 코멘트 자막
            if (_commentator != null) _commentator.OnComment = (text) => _ui.ShowComment(text);

            // 시작은 기보 선택 화면
            _ui.ShowKifuSelect(_loader.AllKifus);
        }

        void HandleKifuSelected(Kifu kifu)
        {
            // 이전 진입에서 ClearReplayEnvironment로 보드를 숨겨놨을 수 있음 → 다시 활성화
            var board = GetComponent<BadukBoard>();
            if (board != null && !board.gameObject.activeSelf) board.gameObject.SetActive(true);

            _replay.LoadKifu(kifu, _loader.Comments);

            // 순서 중요: SetupBoardAndRoom이 카메라를 시작 시점으로 리셋하고 보드를 놓고
            // AdjustCamera로 카메라를 보드 보러 이동시킴 → 이후 UI를 그 안정화된 카메라 기준으로 배치
            // (이게 거꾸로면 매 사이클 직전 카메라 위치를 누적해서 UI가 점점 내려가고 가까워짐)
            SetupBoardAndRoom();
            _ui.ShowReplay(kifu);
            _inReplayView = true;

            if (autoPlayOnLoad) _replay.Play();
        }

        void SetupBoardAndRoom()
        {
            var board = GetComponent<BadukBoard>();
            if (board == null) return;

            // 매 호출 시 카메라를 씬 시작 시점 상태로 리셋 → 보드 배치가 결정론적
            Camera cam = Camera.main;
            if (cam != null && _originCamSaved)
            {
                cam.transform.position = _originCamPos;
                cam.transform.rotation = _originCamRot;
            }

            // SetupBoard 후 보드는 world origin에 identity 상태
            float cx = (board.C1 - board.C0) * BadukBoard.CELL / 2f;
            float cy = (board.R1 - board.R0) * BadukBoard.CELL / 2f;

            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0, 1f, 0);
            Vector3 fwd = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
                : Vector3.forward;
            if (fwd == Vector3.zero) fwd = Vector3.forward;

            // 복기 씬은 더 큰 판 (0.75m)
            float maxSize = 0.75f;
            float boardWorldMax = Mathf.Max(cx * 2f, cy * 2f);
            float scale = boardWorldMax > 0f ? Mathf.Min(1f, maxSize / boardWorldMax) : 1f;

            board.transform.localScale = Vector3.one * scale;
            board.transform.rotation   = Quaternion.LookRotation(fwd, Vector3.up);

            float tableY     = camPos.y - 0.35f;
            Vector3 boardCenter = camPos + fwd * 0.65f;
            boardCenter.y    = tableY;
            board.transform.position =
                boardCenter - board.transform.rotation * new Vector3(cx, 0f, -cy) * scale;

            BadukRoomEnvironment.Spawn(boardCenter, cx * scale, cy * scale, tableY,
                                       board.transform.rotation);

            // 보드가 사용자 앞으로 옮겨진 직후 — 아바타를 옮긴 보드 위치 기준으로 재배치
            // (LoadKifu에서 한 번 spawn했지만 그 시점엔 보드가 원점에 있어 위치가 어긋남)
            GetComponent<NpcAvatarSpawner>()?.Spawn(board.transform);
        }

        void HandleRestart()
        {
            // 처음 진입과 100% 동일한 흐름으로 위임 → 잔여 상태(룸·아바타·보드)가 깔끔히 재구성됨
            var current = _replay.CurrentKifu;
            if (current == null) return;
            HandleKifuSelected(current);
        }

        void HandleBack()
        {
            // 복기 화면이면 → 기보 선택으로, 선택 화면이면 → 로비로
            if (_inReplayView)
            {
                _replay.Pause();
                ClearReplayEnvironment();
                ResetCameraToOrigin();   // select 패널이 매번 동일 위치에 표시되도록
                _ui.ShowKifuSelect(_loader.AllKifus);
                _inReplayView = false;
            }
            else
            {
                LoadLobby();
            }
        }

        void ResetCameraToOrigin()
        {
            Camera cam = Camera.main;
            if (cam != null && _originCamSaved)
            {
                cam.transform.position = _originCamPos;
                cam.transform.rotation = _originCamRot;
            }
        }

        void ClearReplayEnvironment()
        {
            // 바둑방 환경(테이블·기통·다다미·벽) 제거
            var room = GameObject.Find("BadukRoom");
            if (room != null) Destroy(room);
            // 아바타 제거
            GetComponent<NpcAvatarSpawner>()?.Despawn();
            // 보드는 숨김(다음 LoadKifu에서 다시 활성화 + 빈 보드로 재구성)
            var board = GetComponent<BadukBoard>();
            if (board != null) board.gameObject.SetActive(false);
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
