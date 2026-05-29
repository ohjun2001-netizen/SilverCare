using UnityEngine;
using Baduk.Data;
using SilverCare.Common;

namespace Baduk
{
    public class BadukDesktopUI : MonoBehaviour, IBadukUI
    {
        public System.Action OnNext { get; set; }
        public System.Action OnPrev { get; set; }
        public System.Action OnHint { get; set; }
        public System.Action OnRetry { get; set; }
        public System.Action OnBack { get; set; }
        public System.Action OnConfirmPlacement { get; set; }
        public System.Action OnCancelPlacement { get; set; }
        public System.Action<int> OnDifficultySelected { get; set; }

        BadukProblem _cur;
        int _curIdx;
        int _total;
        string _msg = "바둑판의 교차점을 선택하세요.";
        Color _msgColor = new(0.08f, 0.38f, 0.42f);
        bool _answered;
        bool _showDifficultySelect = true;
        bool _showPlacementConfirm;
        string _confirmMessage = "여기에 바둑돌을 두시겠어요?";

        GUIStyle _title;
        GUIStyle _sub;
        GUIStyle _body;
        GUIStyle _message;
        GUIStyle _button;
        GUIStyle _largeButton;
        GUIStyle _confirmBody;
        bool _guiReady;

        readonly Color _bg = new(0.96f, 0.94f, 0.88f, 0.98f);
        readonly Color _panel = new(0.98f, 0.97f, 0.93f, 0.96f);
        readonly Color _accent = new(0.08f, 0.38f, 0.42f);
        readonly Color _ink = new(0.10f, 0.13f, 0.16f);

        public void ShowDifficultySelect()
        {
            _showDifficultySelect = true;
            _showPlacementConfirm = false;
            _cur = null;
        }

        public void ShowProblem(BadukProblem problem, int idx, int total)
        {
            _cur = problem;
            _curIdx = idx;
            _total = total;
            _msg = "바둑판에서 좋은 자리를 찾아 선택하세요.";
            _msgColor = _accent;
            _answered = false;
            _showDifficultySelect = false;
            _showPlacementConfirm = false;
        }

        public void ShowResult(ProblemResult result, string explanation = "")
        {
            _showPlacementConfirm = false;

            switch (result)
            {
                case ProblemResult.Correct:
                    _msg = string.IsNullOrWhiteSpace(explanation)
                        ? "정답입니다. 잘 찾으셨어요."
                        : $"정답입니다. {explanation}";
                    _msgColor = new Color(0.10f, 0.55f, 0.22f);
                    _answered = true;
                    break;

                case ProblemResult.Wrong:
                    _msg = "아쉽지만 틀렸습니다. 힌트를 보고 다시 시도해 보세요.";
                    _msgColor = new Color(0.78f, 0.20f, 0.16f);
                    break;

                case ProblemResult.PartialCorrect:
                    _msg = "좋습니다. 이어서 다음 수를 찾아보세요.";
                    _msgColor = new Color(0.08f, 0.42f, 0.64f);
                    break;
            }
        }

        public void ShowHintText(string hint)
        {
            _showPlacementConfirm = false;
            _msg = string.IsNullOrWhiteSpace(hint)
                ? "힌트가 없습니다. 빈 자리를 천천히 살펴보세요."
                : $"힌트: {hint}";
            _msgColor = new Color(0.58f, 0.38f, 0.05f);
        }

        public void ShowGuideMessage(string message)
        {
            _msg = string.IsNullOrWhiteSpace(message)
                ? "바둑돌을 둘 자리를 눌러보세요."
                : message;
            _msgColor = new Color(0.70f, 0.46f, 0.08f);
        }

        public void ShowPlacementConfirm(string message)
        {
            _confirmMessage = string.IsNullOrWhiteSpace(message) ? "여기에 바둑돌을 두시겠어요?" : message;
            _showPlacementConfirm = true;
        }

        public void HidePlacementConfirm()
        {
            _showPlacementConfirm = false;
        }

        void OnGUI()
        {
            if (!_guiReady)
                BuildStyles();

            if (_showDifficultySelect)
            {
                DrawDifficultySelect();
                return;
            }

            if (_cur == null)
                return;

            DrawGameUI();
            if (_showPlacementConfirm)
                DrawPlacementConfirm();
        }

        void DrawDifficultySelect()
        {
            DrawRect(0, 0, Screen.width, Screen.height, _bg);

            float panelW = Mathf.Min(760, Screen.width - 80);
            float panelH = Mathf.Min(560, Screen.height - 80);
            float x = (Screen.width - panelW) * 0.5f;
            float y = (Screen.height - panelH) * 0.5f;
            DrawRect(x, y, panelW, panelH, _panel);
            DrawRect(x + 34, y + 34, panelW - 68, 6, _accent);

            GUI.Label(new Rect(x, y + 58, panelW, 58), "바둑 사활문제", _title);
            GUI.Label(new Rect(x, y + 112, panelW, 44), "난이도를 고르고 좋은 자리를 찾아보세요.", _sub);

            float bw = panelW - 180;
            float bh = 70;
            float bx = x + 90;
            float by = y + 185;
            DrawDifficultyButton(new Rect(bx, by, bw, bh), "쉬움  -  처음 시작하는 문제", 1);
            DrawDifficultyButton(new Rect(bx, by + 84, bw, bh), "보통  -  조금 더 생각하는 문제", 2);
            DrawDifficultyButton(new Rect(bx, by + 168, bw, bh), "어려움  -  집중해서 푸는 문제", 3);
            DrawDifficultyButton(new Rect(bx, by + 252, bw, bh), "전체 문제", 0);

            if (GUI.Button(new Rect(bx, y + panelH - 82, bw, 58), "로비로 돌아가기", _largeButton))
                LoadLobby();
        }

        void DrawDifficultyButton(Rect rect, string label, int difficulty)
        {
            if (GUI.Button(rect, label, _largeButton))
                OnDifficultySelected?.Invoke(difficulty);
        }

        void DrawGameUI()
        {
            DrawRect(0, 0, Screen.width, 132, _panel);
            DrawRect(32, 20, Screen.width - 64, 5, _accent);

            GUI.Label(new Rect(24, 28, Screen.width - 220, 42),
                $"{_cur.title}  ({GetDifficultyLabel(_cur.difficulty)})", _title);
            GUI.Label(new Rect(Screen.width - 170, 35, 140, 35), $"{_curIdx} / {_total}", _sub);
            GUI.Label(new Rect(32, 78, Screen.width - 64, 46), _cur.description, _body);

            float bottomH = 150;
            DrawRect(0, Screen.height - bottomH, Screen.width, bottomH, _panel);

            _message.normal.textColor = _msgColor;
            GUI.Label(new Rect(28, Screen.height - bottomH + 16, Screen.width - 56, 46), _msg, _message);

            float btnW = 132;
            float btnH = 56;
            float gap = 12;
            float y = Screen.height - btnH - 18;
            float x = 28;

            // 확인창이 떠 있는 동안에는 하단 버튼이 함께 눌리지 않도록 클릭을 막는다.
            bool prevEnabled = GUI.enabled;
            GUI.enabled = !_showPlacementConfirm;

            if (_answered && GUI.Button(new Rect(x, y, btnW, btnH), "다시 풀기", _button))
                OnRetry?.Invoke();

            x += _answered ? btnW + gap : 0;
            if (GUI.Button(new Rect(x, y, btnW, btnH), "이전", _button)) OnPrev?.Invoke();
            if (GUI.Button(new Rect(x + (btnW + gap), y, btnW, btnH), "힌트", _button)) OnHint?.Invoke();
            if (GUI.Button(new Rect(x + (btnW + gap) * 2, y, btnW, btnH), "다음", _button)) OnNext?.Invoke();
            if (GUI.Button(new Rect(Screen.width - 220, y, 192, btnH), "난이도 선택", _button)) OnBack?.Invoke();

            GUI.enabled = prevEnabled;
        }

        void DrawPlacementConfirm()
        {
            DrawRect(0, 0, Screen.width, Screen.height, new Color(0f, 0f, 0f, 0.42f));

            float panelW = Mathf.Min(560, Screen.width - 80);
            float panelH = 200;
            float x = (Screen.width - panelW) * 0.5f;
            float y = 16f;   // 화면 상단(설명창 위)에 고정 — 하단 버튼과 겹치지 않게
            DrawRect(x, y, panelW, panelH, new Color(0.12f, 0.14f, 0.18f, 0.96f));
            DrawRect(x + 24, y + 20, panelW - 48, 4, new Color(0.90f, 0.72f, 0.22f));

            GUI.Label(new Rect(x + 30, y + 38, panelW - 60, 52), _confirmMessage, _title);
            GUI.Label(new Rect(x + 30, y + 92, panelW - 60, 34), "확인을 누르면 착수하고, 다시 선택을 누르면 취소됩니다.", _confirmBody);

            if (GUI.Button(new Rect(x + 72, y + 138, 160, 48), "확인", _button))
                OnConfirmPlacement?.Invoke();

            if (GUI.Button(new Rect(x + panelW - 232, y + 138, 160, 48), "다시 선택", _button))
                OnCancelPlacement?.Invoke();
        }

        static void LoadLobby()
        {
            if (GameSceneManager.Instance != null)
                GameSceneManager.Instance.LoadScene(GameSceneManager.SCENE_LOBBY);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainLobby");
        }

        static string GetDifficultyLabel(int difficulty)
        {
            return difficulty switch
            {
                1 => "쉬움",
                2 => "보통",
                3 => "어려움",
                _ => "전체",
            };
        }

        static void DrawRect(float x, float y, float w, float h, Color color)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        void BuildStyles()
        {
            _title = new GUIStyle
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _title.normal.textColor = _accent;

            _sub = new GUIStyle
            {
                fontSize = 21,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _sub.normal.textColor = new Color(0.34f, 0.39f, 0.43f);

            _body = new GUIStyle
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _body.normal.textColor = _ink;

            _message = new GUIStyle
            {
                fontSize = 23,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            _confirmBody = new GUIStyle
            {
                fontSize = 19,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _confirmBody.normal.textColor = new Color(0.90f, 0.92f, 0.94f);

            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 21,
                fontStyle = FontStyle.Bold
            };
            _button.normal.textColor = Color.white;
            _button.padding = new RectOffset(12, 12, 10, 10);

            _largeButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            _largeButton.normal.textColor = Color.white;
            _largeButton.padding = new RectOffset(16, 16, 14, 14);

            _guiReady = true;
        }
    }
}
