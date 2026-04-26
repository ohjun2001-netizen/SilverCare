// Assets/Scripts/Baduk/BadukHintSystem.cs
using System.Collections;
using UnityEngine;
using Baduk.Data;

namespace Baduk
{
    public class BadukHintSystem : MonoBehaviour
    {
        [Header("난이도별 자동 힌트 시간 (초)")]
        [SerializeField] float easyAutoHintDelay   = 20f;
        [SerializeField] float mediumAutoHintDelay = 30f;
        [SerializeField] float hardAutoHintDelay   = 45f;

        [Header("2단계 힌트 추가 대기 (초)")]
        [SerializeField] float visualHintExtraDelay = 15f;

        BadukProblem      _cur;
        BadukBoard        _board;
        BadukTTSConnector _tts;
        Coroutine         _timer;
        int               _hintLevel;   // 0=없음, 1=텍스트, 2=시각

        // GameManager가 구독
        public System.Action<string> OnHintTextReady;

        void Awake()
        {
            _tts   = GetComponent<BadukTTSConnector>();
            _board = GetComponent<BadukBoard>();
        }

        public void SetProblem(BadukProblem problem)
        {
            _cur = problem;
            _hintLevel = 0;
            _board.HideHintMarker();
            RestartTimer();
        }

        /// <summary>힌트 버튼 클릭 시 (BadukGameManager에서 호출)</summary>
        public void ShowHint()
        {
            if (_cur == null) return;
            _hintLevel++;

            if (_hintLevel == 1)
            {
                // 1단계: 텍스트 + 음성
                string text = !string.IsNullOrEmpty(_cur.tts_hint) ? _cur.tts_hint : _cur.hint;
                _tts?.Speak(text);
                OnHintTextReady?.Invoke(_cur.hint);
                RestartTimerForVisualHint();
            }
            else
            {
                // 2단계: 정답 위치 시각 표시
                ShowVisualHint();
            }
        }

        /// <summary>오답 시 타이머 리셋 (다시 힌트 대기)</summary>
        public void ResetOnWrongAnswer()
        {
            _hintLevel = 0;
            _board.HideHintMarker();
            RestartTimer();
        }

        void ShowVisualHint()
        {
            if (_cur?.answer == null || _cur.answer.Count == 0) return;

            // 현재 풀어야 할 첫 번째 정답 위치 표시
            var target = _cur.answer[0];
            _board.ShowHintMarker(target.row, target.col);

            string text = "정답 위치를 표시했습니다. 깜빡이는 곳에 두세요.";
            _tts?.Speak(text);
            OnHintTextReady?.Invoke(text);

            StopTimer();
        }

        // ── 타이머 ────────────────────────────────────────
        float GetAutoHintDelay()
        {
            if (_cur == null) return mediumAutoHintDelay;
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

        void RestartTimerForVisualHint()
        {
            StopTimer();
            _timer = StartCoroutine(AutoHintTimer(visualHintExtraDelay, true));
        }

        void StopTimer()
        {
            if (_timer != null) { StopCoroutine(_timer); _timer = null; }
        }

        IEnumerator AutoHintTimer(float delay, bool isVisualStage)
        {
            yield return new WaitForSeconds(delay);

            if (isVisualStage)
            {
                _hintLevel = 2;
                ShowVisualHint();
            }
            else
            {
                _hintLevel = 1;
                string text = !string.IsNullOrEmpty(_cur.tts_hint) ? _cur.tts_hint : _cur.hint;
                _tts?.Speak(text);
                OnHintTextReady?.Invoke(_cur.hint);
                RestartTimerForVisualHint();
            }
        }
    }
}
