// Assets/Scripts/Lobby/SimpleLobby.cs
// 씬 이름: "MainLobby"
// 빈 GameObject 하나에 이 스크립트만 붙이면 됩니다.
using UnityEngine;
using UnityEngine.SceneManagement;
using SilverCare.Common;

public class SimpleLobby : MonoBehaviour
{
    // ── 씬 이름 ──────────────────────────────────────────
    const string SCENE_BADUK      = "Baduk";
    const string SCENE_BADUK_VR   = "BadukVR";
    const string SCENE_CARD_MATCH = "CardMatch";
    const string SCENE_QUIZ       = "Quiz";
    const string SCENE_SONG_GUESS = "SongGuess";
    const string SCENE_GO_STOP    = "GoStop";
    const string SCENE_GOLF       = "Golf";

    GUIStyle _stTitle, _stBtn, _stSub;
    bool _guiReady;

    // ── GUI ──────────────────────────────────────────────
    void OnGUI()
    {
        if (!_guiReady) BuildStyles();

        // 배경
        DrawRect(0, 0, Screen.width, Screen.height, new Color(0.10f, 0.10f, 0.18f));

        // 타이틀
        GUI.Label(new Rect(0, 40, Screen.width, 60),
            "SilverCare - 어르신 미니게임", _stTitle);
        GUI.Label(new Rect(0, 100, Screen.width, 30),
            "즐기고 싶은 게임을 선택하세요", _stSub);

        // 게임 버튼 배치 (2열 3행)
        float bw = 200f, bh = 70f, gap = 20f;
        float totalW = bw * 2 + gap;
        float startX = (Screen.width - totalW) / 2f;
        float startY = 160f;

        (string scene, string label)[] games =
        {
            (SCENE_BADUK,      "바둑 사활문제"),
            (SCENE_BADUK_VR,   "바둑 (VR)"),
            (SCENE_CARD_MATCH, "카드 맞추기"),
            (SCENE_QUIZ,       "퀴즈"),
            (SCENE_SONG_GUESS, "노래 맞추기"),
            (SCENE_GO_STOP,    "고스톱"),
            (SCENE_GOLF,       "골프"),
        };

        for (int i = 0; i < games.Length; i++)
        {
            int row = i / 2;
            int col = i % 2;
            float x = startX + col * (bw + gap);
            float y = startY + row * (bh + gap);

            if (GUI.Button(new Rect(x, y, bw, bh), games[i].label, _stBtn))
                GoTo(games[i].scene);
        }
    }

    void GoTo(string sceneName)
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    void DrawRect(float x, float y, float w, float h, Color c)
    {
        var prev = GUI.color; GUI.color = c;
        GUI.DrawTexture(new UnityEngine.Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    void BuildStyles()
    {
        _stTitle = new GUIStyle
        {
            fontSize  = 28, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _stTitle.normal.textColor = Color.white;

        _stSub = new GUIStyle { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        _stSub.normal.textColor = new Color(0.78f, 0.78f, 0.78f);

        _stBtn = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        _stBtn.normal.textColor  = Color.white;
        _stBtn.padding = new RectOffset(10, 10, 15, 15);

        _guiReady = true;
    }
}
