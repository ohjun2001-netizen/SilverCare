// Assets/Scripts/Baduk/BadukDesktopUI.cs
// Desktop 전용 - OnGUI 방식 (노인 친화 UI)
using UnityEngine;
using Baduk.Data;

namespace Baduk
{
    public class BadukDesktopUI : MonoBehaviour, IBadukUI
    {
        public System.Action OnNext  { get; set; }
        public System.Action OnPrev  { get; set; }
        public System.Action OnHint  { get; set; }
        public System.Action OnRetry { get; set; }
        public System.Action OnBack  { get; set; }
        public System.Action<int> OnDifficultySelected { get; set; }

        BadukProblem _cur;
        int    _curIdx, _total;
        string _msg      = "바둑판을 클릭해서 돌을 놓으세요.";
        Color  _msgColor = Color.white;
        bool   _answered;
        bool   _showDifficultySelect = true;

        GUIStyle _stTitle, _stDesc, _stMsg, _stBtn, _stBtnLarge;
        bool     _guiReady;

        // ── IBadukUI 구현 ────────────────────────────────

        public void ShowDifficultySelect()
        {
            _showDifficultySelect = true;
            _cur = null;
        }

        public void ShowProblem(BadukProblem problem, int idx, int total)
        {
            _cur      = problem;
            _curIdx   = idx;
            _total    = total;
            _msg      = "바둑판을 클릭해서 돌을 놓으세요.";
            _msgColor = Color.white;
            _answered = false;
            _showDifficultySelect = false;
        }

        public void ShowResult(ProblemResult result, string explanation = "")
        {
            switch (result)
            {
                case ProblemResult.Correct:
                    _msg      = $"정답!  {explanation}";
                    _msgColor = new Color(0.2f, 1f, 0.2f);
                    _answered = true;
                    break;
                case ProblemResult.Wrong:
                    _msg      = "틀렸습니다. 힌트 버튼을 눌러보세요.";
                    _msgColor = new Color(1f, 0.4f, 0.4f);
                    break;
                case ProblemResult.PartialCorrect:
                    _msg      = "잘했어요! 계속 두세요.";
                    _msgColor = new Color(0.4f, 1f, 1f);
                    break;
            }
        }

        public void ShowHintText(string hint)
        {
            _msg      = $"힌트: {hint}";
            _msgColor = new Color(1f, 1f, 0.3f);
        }

        // ── OnGUI ────────────────────────────────────────
        void OnGUI()
        {
            if (!_guiReady) BuildStyles();

            if (_showDifficultySelect)
            {
                DrawDifficultySelect();
                return;
            }

            if (_cur == null) return;

            DrawRect(0, 0, Screen.width, 110, new Color(0, 0, 0, 0.82f));
            string stars = new string('★', _cur.difficulty) + new string('☆', 3 - _cur.difficulty);
            GUI.Label(new Rect(16, 8, Screen.width - 30, 44),
                $"#{_cur.id}  {_cur.title}    {stars} {_cur.difficulty_name}", _stTitle);
            GUI.Label(new Rect(16, 56, Screen.width - 30, 48), _cur.description, _stDesc);

            float bottomH = 140;
            DrawRect(0, Screen.height - bottomH, Screen.width, bottomH, new Color(0, 0, 0, 0.82f));
            _stMsg.normal.textColor = _msgColor;
            GUI.Label(new Rect(16, Screen.height - bottomH + 6, Screen.width - 140, 60), _msg, _stMsg);

            float btnW = 110, btnH = 50, gap = 10;
            float y = Screen.height - btnH - 14;

            if (GUI.Button(new Rect(16, y, btnW, btnH), "◀ 이전", _stBtn)) OnPrev?.Invoke();
            if (GUI.Button(new Rect(16 + (btnW + gap), y, btnW, btnH), "힌트", _stBtn)) OnHint?.Invoke();
            if (GUI.Button(new Rect(16 + (btnW + gap) * 2, y, btnW, btnH), "다음 ▶", _stBtn)) OnNext?.Invoke();
            if (_answered && GUI.Button(new Rect(16 + (btnW + gap) * 3, y, btnW, btnH), "다시 풀기", _stBtn))
                OnRetry?.Invoke();

            if (GUI.Button(new Rect(Screen.width - btnW - 16, y, btnW, btnH), "나가기", _stBtn))
                OnBack?.Invoke();

            GUI.Label(new Rect(Screen.width - btnW - 16, y - 30, btnW, 28),
                $"{_curIdx} / {_total}", _stDesc);
        }

        void DrawDifficultySelect()
        {
            DrawRect(0, 0, Screen.width, Screen.height, new Color(0.10f, 0.10f, 0.18f));

            GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 60),
                "바둑 사활문제", _stTitle);

            GUIStyle sub = new GUIStyle(_stDesc) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, Screen.height * 0.12f + 60, Screen.width, 40),
                "난이도를 선택하세요", sub);

            float bw = 280, bh = 70, bGap = 20;
            float startX = (Screen.width - bw) / 2f;
            float startY = Screen.height * 0.35f;

            (int diff, string label)[] options =
            {
                (1, "초급  ★☆☆"),
                (2, "중급  ★★☆"),
                (3, "고급  ★★★"),
                (0, "전체 문제"),
            };

            for (int i = 0; i < options.Length; i++)
            {
                float y = startY + i * (bh + bGap);
                if (GUI.Button(new Rect(startX, y, bw, bh), options[i].label, _stBtnLarge))
                    OnDifficultySelected?.Invoke(options[i].diff);
            }

            if (GUI.Button(new Rect(startX, startY + options.Length * (bh + bGap) + 10, bw, bh), "로비로 돌아가기", _stBtnLarge))
                LoadLobby();
        }

        static void LoadLobby()
        {
            if (SilverCare.Common.GameSceneManager.Instance != null)
                SilverCare.Common.GameSceneManager.Instance.LoadScene(SilverCare.Common.GameSceneManager.SCENE_LOBBY);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainLobby");
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
                fontSize = 26, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _stTitle.normal.textColor = Color.white;

            _stDesc = new GUIStyle
            {
                fontSize = 20, wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            _stDesc.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

            _stMsg = new GUIStyle
            {
                fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true
            };
            _stMsg.normal.textColor = Color.white;

            _stBtn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20, fontStyle = FontStyle.Bold
            };
            _stBtn.normal.textColor = Color.white;
            _stBtn.padding = new RectOffset(8, 8, 10, 10);

            _stBtnLarge = new GUIStyle(GUI.skin.button)
            {
                fontSize = 24, fontStyle = FontStyle.Bold
            };
            _stBtnLarge.normal.textColor = Color.white;
            _stBtnLarge.padding = new RectOffset(12, 12, 14, 14);

            _guiReady = true;
        }
    }
}
