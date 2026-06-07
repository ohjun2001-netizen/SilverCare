using UnityEngine;
using Baduk.Data;
using SilverCare.Common;
using Unity.XR.CoreUtils;

namespace Baduk.Replay
{
    public class KifuReplayGameManager : MonoBehaviour
    {
        [Header("종료 시 이동할 씬")]
        [SerializeField] string lobbySceneName = "MainLobby";

        [Header("기보 선택 후 자동 재생 시작")]
        [SerializeField] bool autoPlayOnLoad = true;

        KifuLoader _loader;
        KifuReplayManager _replay;
        NpcCommentator _commentator;
        KifuVRUI _ui;
        BadukVRBoardSetup _vrBoardSetup;

        bool _inReplayView;

        Vector3 _originXRPos;
        Quaternion _originXRRot;
        bool _originXRSaved;

        Vector3 _originCamPos;
        Quaternion _originCamRot;
        bool _originCamSaved;

        void Awake()
        {
            _loader = GetComponent<KifuLoader>();
            _replay = GetComponent<KifuReplayManager>();
            _commentator = GetComponent<NpcCommentator>();
            _ui = GetComponent<KifuVRUI>();
            _vrBoardSetup = GetComponent<BadukVRBoardSetup>();

            if (_loader == null) Debug.LogError("[KifuReplayGameManager] KifuLoader missing.");
            if (_replay == null) Debug.LogError("[KifuReplayGameManager] KifuReplayManager missing.");
            if (_ui == null) Debug.LogError("[KifuReplayGameManager] KifuVRUI missing.");
        }

        void Start()
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null)
            {
                _originXRPos = xrOrigin.transform.position;
                _originXRRot = xrOrigin.transform.rotation;
                _originXRSaved = true;
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                _originCamPos = cam.transform.position;
                _originCamRot = cam.transform.rotation;
                _originCamSaved = true;
            }

            _ui.OnKifuSelected = HandleKifuSelected;
            _ui.OnPlayPause = () => _replay.TogglePlayPause();
            _ui.OnNext = () => _replay.Next();
            _ui.OnPrev = () => _replay.Prev();
            _ui.OnRestart = HandleRestart;
            _ui.OnSpeedChanged = s => _replay.SetSpeed(s);
            _ui.OnBack = HandleBack;

            _replay.OnMoveAdvanced = (cur, total) => _ui.UpdateProgress(cur, total);
            _replay.OnPlaybackStateChanged = () => _ui.UpdatePlayPauseLabel(_replay.IsPlaying);
            _replay.OnReplayEnded = HandleReplayEnded;

            if (_commentator != null)
                _commentator.OnComment = text => _ui.ShowComment(text);

            _ui.ShowKifuSelect(_loader.AllKifus);
        }

        void HandleKifuSelected(Kifu kifu)
        {
            var board = GetComponent<BadukBoard>();
            if (board != null && !board.gameObject.activeSelf)
                board.gameObject.SetActive(true);

            _replay.LoadKifu(kifu, _loader.Comments);
            SetupBoardAndRoom();
            _ui.ShowReplay(kifu);
            _inReplayView = true;

            if (autoPlayOnLoad)
                _replay.Play();
        }

        void SetupBoardAndRoom()
        {
            var board = GetComponent<BadukBoard>();
            if (board == null)
                return;

            ResetOrigin();

            Camera cam = Camera.main;
            float cx = (board.C1 - board.C0) * BadukBoard.CELL / 2f;
            float cy = (board.R1 - board.R0) * BadukBoard.CELL / 2f;
            string sceneKey = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            BadukDeskLayoutUtility.UpdateSceneAnchor(sceneKey, cam);

            BadukDeskLayoutUtility.ApplyDeskLayout(
                board.transform,
                cx,
                cy,
                0.92f,
                0.20f,
                0.62f,
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
        }

        void HandleRestart()
        {
            var current = _replay.CurrentKifu;
            if (current == null)
                return;

            HandleKifuSelected(current);
        }

        void HandleReplayEnded()
        {
            bool firstClear = StoryProgressManager.Instance != null &&
                              StoryProgressManager.Instance.TryMarkActivityCleared(
                                  StoryProgressManager.StoryActivity.BadukReplay);
            if (firstClear)
                StoryProgressManager.Instance?.SpeakClearNarration(StoryProgressManager.StoryActivity.BadukReplay);
        }

        void HandleBack()
        {
            if (_inReplayView)
            {
                _replay.Pause();
                ClearReplayEnvironment();
                SelectionBackdropUtility.ClearAllBackdrops();
                BadukDeskLayoutUtility.ClearSceneAnchor(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                ResetOrigin();
                _ui.ShowKifuSelect(_loader.AllKifus);
                _inReplayView = false;
                return;
            }

            LoadLobby();
        }

        void ResetOrigin()
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin != null && _originXRSaved)
            {
                xrOrigin.transform.position = _originXRPos;
                xrOrigin.transform.rotation = _originXRRot;
            }

            Camera cam = Camera.main;
            if (cam == null || !_originCamSaved)
                return;

            cam.transform.position = _originCamPos;
            cam.transform.rotation = _originCamRot;
        }

        void ClearReplayEnvironment()
        {
            GetComponent<NpcAvatarSpawner>()?.Despawn();
            BadukRoomEnvironment.Cleanup();
            SelectionBackdropUtility.ClearAllBackdrops();

            var board = GetComponent<BadukBoard>();
            if (board != null)
            {
                board.RemoveAllPlayerStones();
                board.transform.position = new Vector3(0f, -100f, 0f);
                board.gameObject.SetActive(false);
            }
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
