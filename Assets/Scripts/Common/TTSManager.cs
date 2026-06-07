// Assets/Scripts/Common/TTSManager.cs
// Android TTS 전용 — 서버 불필요, Quest 오프라인 완전 동작
using System.Collections;
using UnityEngine;

namespace SilverCare.Common
{
    public class TTSManager : MonoBehaviour
    {
        public static TTSManager Instance { get; private set; }

        [Header("TTS 설정")]
        [SerializeField] [Range(0.6f, 1.4f)] float speechRate  = 0.9f;
        [SerializeField] [Range(0.8f, 1.2f)] float speechPitch = 1.0f;

        bool _isSpeaking;
        Coroutine _waitCoroutine;
        string _pendingText;
        bool _pendingInterrupt;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject _tts;
        bool _ready;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null || FindObjectOfType<TTSManager>() != null)
                return;

            var go = new GameObject("TTSManager");
            go.AddComponent<TTSManager>();
        }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !UNITY_EDITOR
            InitTTS();
#endif
        }

        void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { _tts?.Call<int>("stop"); _tts?.Call("shutdown"); } catch { }
#endif
        }

        // ── 공개 API ─────────────────────────────────────────

        public void Speak(string text, bool interruptCurrent = false)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_isSpeaking)
            {
                if (!interruptCurrent) return;
                StopSpeaking();
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_ready) SpeakInternal(text);
            else
            {
                _pendingText = text;
                _pendingInterrupt = interruptCurrent;
                Debug.Log($"[TTS] 초기화 대기 중: {text}");
            }
#else
            Debug.Log($"[TTS] {text}");
#endif
        }

        public void StopSpeaking()
        {
            if (_waitCoroutine != null) { StopCoroutine(_waitCoroutine); _waitCoroutine = null; }
            _isSpeaking = false;
            _pendingText = null;
            _pendingInterrupt = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            _tts?.Call<int>("stop");
#endif
        }

        // ── 내부 ─────────────────────────────────────────────

#if UNITY_ANDROID && !UNITY_EDITOR
        void InitTTS()
        {
            try
            {
                var listener = new InitListener(success =>
                {
                    if (!success) { Debug.LogWarning("[TTS] 초기화 실패"); return; }

                    var locale = new AndroidJavaClass("java.util.Locale")
                                     .GetStatic<AndroidJavaObject>("KOREA");
                    int result = _tts.Call<int>("setLanguage", locale);
                    if (result < 0)
                        Debug.LogWarning("[TTS] 한국어 언어팩 없음 — 기본 언어로 대체");

                    _tts.Call<int>("setSpeechRate", speechRate);
                    _tts.Call<int>("setPitch", speechPitch);
                    _ready = true;
                    FlushPendingSpeech();
                });

                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                                   .GetStatic<AndroidJavaObject>("currentActivity");
                _tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, listener);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TTS] Android TTS 생성 오류: {e.Message}");
            }
        }

        void SpeakInternal(string text)
        {
            // QUEUE_FLUSH(0) = 현재 발화 즉시 중단 후 새 텍스트 시작
            _tts.Call<int>("speak", text, 0, null, "utterance");
            _isSpeaking = true;
            if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
            _waitCoroutine = StartCoroutine(WaitSpeech(text.Length));
        }

        void FlushPendingSpeech()
        {
            if (string.IsNullOrWhiteSpace(_pendingText))
                return;

            string text = _pendingText;
            bool interrupt = _pendingInterrupt;
            _pendingText = null;
            _pendingInterrupt = false;
            Speak(text, interrupt);
        }

        IEnumerator WaitSpeech(int charCount)
        {
            // 글자 수 기반 발화 시간 추정 (80ms/글자 + 0.5초 여유)
            yield return new WaitForSeconds(charCount * 0.08f + 0.5f);
            _isSpeaking = false;
            _waitCoroutine = null;
        }

        class InitListener : AndroidJavaProxy
        {
            readonly System.Action<bool> _cb;
            public InitListener(System.Action<bool> cb)
                : base("android.speech.tts.TextToSpeech$OnInitListener") => _cb = cb;
            void onInit(int status) => _cb?.Invoke(status == 0);
        }
#endif
    }
}
