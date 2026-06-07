using System.Collections;
using Baduk.Data;
using SilverCare.Common;
using UnityEngine;

namespace Baduk
{
    public class BadukHintSystem : MonoBehaviour
    {
        [Header("1단계 자동 힌트 시간 (초)")]
        [SerializeField] float easyAutoHintDelay = 20f;
        [SerializeField] float mediumAutoHintDelay = 30f;
        [SerializeField] float hardAutoHintDelay = 45f;

        BadukProblem _cur;
        BadukBoard _board;
        BadukAnswerChecker _checker;
        BadukTTSConnector _tts;
        Coroutine _timer;
        int _hintLevel; // 0=없음, 1=JSON 힌트, 2=방향 안내, 3=위치 표시

        public System.Action<string> OnHintTextReady;

        void Awake()
        {
            _tts = GetComponent<BadukTTSConnector>();
            _board = GetComponent<BadukBoard>();
            _checker = GetComponent<BadukAnswerChecker>();
        }

        public void SetProblem(BadukProblem problem)
        {
            _cur = problem;
            _hintLevel = 0;
            _board?.HideHintMarker();
            RestartTimer(GetAutoHintDelay(), 1);
        }

        public void ShowHint()
        {
            if (_cur == null)
                return;

            StopTimer();
            _hintLevel = Mathf.Clamp(_hintLevel + 1, 1, 3);
            ShowHintLevel(_hintLevel, speak: true);
        }

        public void ResetOnWrongAnswer()
        {
            _hintLevel = 0;
            _board?.HideHintMarker();
            RestartTimer(GetAutoHintDelay(), 1);
        }

        void ShowHintLevel(int level, bool speak)
        {
            switch (level)
            {
                case 1:
                    ShowJsonHint(speak);
                    break;
                case 2:
                    ShowContextualHint(speak);
                    break;
                default:
                    ShowMarkerHint(speak);
                    break;
            }
        }

        void ShowJsonHint(bool speak)
        {
            string displayText = string.IsNullOrWhiteSpace(_cur.hint)
                ? BuildGoalHint()
                : _cur.hint;
            string speechText = string.IsNullOrWhiteSpace(_cur.tts_hint)
                ? displayText
                : _cur.tts_hint;

            _board?.HideHintMarker();
            OnHintTextReady?.Invoke(displayText);
            if (speak)
                SpeakTTS(speechText);
        }

        void ShowContextualHint(bool speak)
        {
            string text = BuildContextualHint();
            _board?.HideHintMarker();
            OnHintTextReady?.Invoke(text);
            if (speak)
                SpeakTTS(text);
        }

        void ShowMarkerHint(bool speak)
        {
            string text = BuildMarkerHint();
            if (_checker != null && _checker.TryGetExpectedMove(out StonePosition expected))
                _board?.ShowHintMarker(expected.row, expected.col);

            OnHintTextReady?.Invoke(text);
            if (speak)
                SpeakTTS(text);
        }

        string BuildContextualHint()
        {
            if (_checker == null || !_checker.TryGetExpectedMove(out StonePosition expected))
                return "바둑판의 빈 교차점 중에서 돌의 연결과 활로가 가장 좋아지는 자리를 찾아보세요.";

            string stepText = GetStepText();
            string areaText = GetBoardAreaText(expected);
            string goalText = BuildGoalHint();
            return $"{stepText}{areaText} 쪽을 살펴보세요. {goalText}";
        }

        string BuildMarkerHint()
        {
            if (_checker == null || !_checker.TryGetExpectedMove(out StonePosition expected))
                return "막히면 빈 교차점을 천천히 다시 눌러보세요. 확인창이 뜨면 맞는지 한 번 더 살펴보면 됩니다.";

            return $"{GetStepText()}노란 원으로 표시된 자리를 눌러보세요. 좌표는 {expected.row + 1}행 {expected.col + 1}열입니다.";
        }

        string BuildGoalHint()
        {
            return _cur?.goal switch
            {
                "survive" => "두 눈을 만들거나 끊긴 돌을 이어서 살아나는 자리가 중요합니다.",
                "kill" => "상대 돌의 마지막 활로를 줄이는 자리를 먼저 살펴보세요.",
                "ko" => "바로 살기 어렵다면 패가 생기는 교환 자리를 찾아보세요.",
                _ => "돌의 연결, 끊김, 활로를 차분히 비교해보세요."
            };
        }

        string GetStepText()
        {
            if (_checker == null || _checker.TotalAnswerSteps <= 1)
                return "";

            int current = Mathf.Clamp(_checker.CurrentAnswerStep + 1, 1, _checker.TotalAnswerSteps);
            return $"{current}번째 수입니다. ";
        }

        string GetBoardAreaText(StonePosition expected)
        {
            int r0 = 0;
            int c0 = 0;
            int r1 = Mathf.Max(0, (_cur?.board_size ?? 19) - 1);
            int c1 = r1;

            if (_cur?.view_area != null && _cur.view_area.Length >= 4)
            {
                r0 = _cur.view_area[0];
                c0 = _cur.view_area[1];
                r1 = _cur.view_area[2];
                c1 = _cur.view_area[3];
            }

            float rowT = Mathf.InverseLerp(r0, r1, expected.row);
            float colT = Mathf.InverseLerp(c0, c1, expected.col);

            string vertical = rowT < 0.34f ? "위쪽" : rowT > 0.66f ? "아래쪽" : "가운데";
            string horizontal = colT < 0.34f ? "왼쪽" : colT > 0.66f ? "오른쪽" : "중앙";

            return vertical == "가운데" && horizontal == "중앙"
                ? "가운데"
                : $"{vertical} {horizontal}";
        }

        float GetAutoHintDelay()
        {
            if (_cur == null)
                return mediumAutoHintDelay;

            return _cur.difficulty switch
            {
                1 => easyAutoHintDelay,
                3 => hardAutoHintDelay,
                _ => mediumAutoHintDelay,
            };
        }

        void RestartTimer(float delay, int nextLevel)
        {
            StopTimer();
            _timer = StartCoroutine(AutoHintTimer(delay, nextLevel));
        }

        void StopTimer()
        {
            if (_timer == null)
                return;

            StopCoroutine(_timer);
            _timer = null;
        }

        IEnumerator AutoHintTimer(float delay, int nextLevel)
        {
            yield return new WaitForSeconds(delay);

            if (_cur == null)
                yield break;

            _hintLevel = Mathf.Max(_hintLevel, nextLevel);
            ShowHintLevel(nextLevel, speak: true);
            _timer = null;
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
    }
}
