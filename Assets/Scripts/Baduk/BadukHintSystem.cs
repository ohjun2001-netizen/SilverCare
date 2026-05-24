using System.Collections;
using UnityEngine;
using Baduk.Data;

namespace Baduk
{
    public class BadukHintSystem : MonoBehaviour
    {
        [Header("1단계 자동 힌트 시간 (초)")]
        [SerializeField] float easyAutoHintDelay = 20f;
        [SerializeField] float mediumAutoHintDelay = 30f;
        [SerializeField] float hardAutoHintDelay = 45f;

        [Header("2단계 추가 안내 대기 시간 (초)")]
        [SerializeField] float visualHintExtraDelay = 15f;

        BadukProblem _cur;
        BadukBoard _board;
        BadukAnswerChecker _checker;
        BadukTTSConnector _tts;
        Coroutine _timer;
        int _hintLevel; // 0=없음, 1=텍스트, 2=추가 안내

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
            _board.HideHintMarker();
            RestartTimer();
        }

        public void ShowHint()
        {
            if (_cur == null)
                return;

            _hintLevel++;

            if (_hintLevel == 1)
            {
                string text = !string.IsNullOrEmpty(_cur.tts_hint) ? _cur.tts_hint : _cur.hint;
                _tts?.Speak(text);
                OnHintTextReady?.Invoke(_cur.hint);
                RestartTimerForPlacementGuide();
                return;
            }

            ShowPlacementGuideHint();
        }

        public void ResetOnWrongAnswer()
        {
            _hintLevel = 0;
            _board.HideHintMarker();
            RestartTimer();
        }

        void ShowPlacementGuideHint()
        {
            if (_checker == null || !_checker.TryGetExpectedMove(out _))
                return;

            _board.HideHintMarker();

            const string text = "바둑판에서 생각한 자리를 눌러보세요. 확인창이 열리면 '확인'을 눌러 돌을 두고, 아니면 '다시 선택'을 누르세요.";
            _tts?.Speak(text);
            OnHintTextReady?.Invoke(text);

            StopTimer();
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

        void RestartTimer()
        {
            StopTimer();
            _timer = StartCoroutine(AutoHintTimer(GetAutoHintDelay(), false));
        }

        void RestartTimerForPlacementGuide()
        {
            StopTimer();
            _timer = StartCoroutine(AutoHintTimer(visualHintExtraDelay, true));
        }

        void StopTimer()
        {
            if (_timer == null)
                return;

            StopCoroutine(_timer);
            _timer = null;
        }

        IEnumerator AutoHintTimer(float delay, bool isPlacementGuideStage)
        {
            yield return new WaitForSeconds(delay);

            if (isPlacementGuideStage)
            {
                _hintLevel = 2;
                ShowPlacementGuideHint();
                yield break;
            }

            _hintLevel = 1;
            string text = !string.IsNullOrEmpty(_cur.tts_hint) ? _cur.tts_hint : _cur.hint;
            _tts?.Speak(text);
            OnHintTextReady?.Invoke(_cur.hint);
            RestartTimerForPlacementGuide();
        }
    }
}
