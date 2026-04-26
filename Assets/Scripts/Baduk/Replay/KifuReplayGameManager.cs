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
            _replay.LoadKifu(kifu, _loader.Comments);
            _ui.ShowReplay(kifu);

            // 교차점 Interactable은 사활문제용. 복기에서는 사용자 입력 막아야 하므로 부착하지 않음.
            // (혹시 입력을 허용하고 싶으면 _vrBoardSetup?.AttachInteractables() 호출)

            if (autoPlayOnLoad) _replay.Play();
        }

        void HandleRestart()
        {
            _replay.Restart();
            if (autoPlayOnLoad) _replay.Play();
        }

        void HandleBack()
        {
            // 복기 중이면 → 기보 선택으로, 선택 화면이면 → 로비로
            if (_replay.CurrentKifu != null)
            {
                _replay.Pause();
                _ui.ShowKifuSelect(_loader.AllKifus);
            }
            else
            {
                LoadLobby();
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
