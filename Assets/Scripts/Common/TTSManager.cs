// Assets/Scripts/Common/TTSManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SilverCare.Common
{
    public class TTSManager : MonoBehaviour
    {
        public static TTSManager Instance { get; private set; }

        // Android 빌드에서만 실제로 읽히므로 에디터 빌드에서는 CS0414 경고 억제
#pragma warning disable CS0414
        [Header("TTS 설정")]
        [SerializeField] [Range(0.6f, 1.4f)] float speechRate  = 0.9f;
        [SerializeField] [Range(0.8f, 1.2f)] float speechPitch = 1.0f;
#pragma warning restore CS0414

        const string ClipResourcePath = "Audio/TTS";

        bool _isSpeaking;
        Coroutine _waitCoroutine;
        volatile string _pendingText;
        volatile bool _pendingInterrupt;

        AudioSource _audioSource;
        Dictionary<string, AudioClip> _clipMap;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject _tts;
        volatile bool _ready;
        volatile bool _initFailed;
        float _retryTime = -1f;
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

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            _clipMap = new Dictionary<string, AudioClip>();
            foreach (var clip in Resources.LoadAll<AudioClip>(ClipResourcePath))
                _clipMap[clip.name] = clip;

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

        void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_ready && _pendingText != null)
                FlushPendingSpeech();

            if (_initFailed && _retryTime < 0f)
                _retryTime = Time.time + 3f;

            if (_retryTime > 0f && Time.time >= _retryTime)
            {
                _initFailed = false;
                _retryTime = -1f;
                InitTTS(isRetry: true);
            }
#endif
        }

        // ── 공개 API ─────────────────────────────────────────

        public void SpeakClip(string key, bool interruptCurrent = false)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_isSpeaking)
            {
                if (!interruptCurrent) return;
                StopSpeaking();
            }
            if (_clipMap.TryGetValue(key, out var clip))
                PlayAudioClip(clip);
        }

        public void Speak(string text, bool interruptCurrent = false)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_isSpeaking)
            {
                if (!interruptCurrent) return;
                StopSpeaking();
            }

            // 오디오 클립 매칭 우선 (Quest TTS 대체)
            if (_clipMap.TryGetValue(text, out var clip))
            {
                PlayAudioClip(clip);
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_ready) SpeakInternal(text);
            else
            {
                _pendingText = text;
                _pendingInterrupt = interruptCurrent;
                Debug.Log($"[TTS] 초기화 대기 중: {text}");
            }
#elif UNITY_EDITOR_WIN
            EditorSpeak(text);
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
            _audioSource?.Stop();
        }

        // ── 내부 ─────────────────────────────────────────────

        void PlayAudioClip(AudioClip clip)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
            _isSpeaking = true;
            if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
            _waitCoroutine = StartCoroutine(WaitAudioClip(clip.length));
        }

        IEnumerator WaitAudioClip(float duration)
        {
            yield return new WaitForSeconds(duration);
            _isSpeaking = false;
            _waitCoroutine = null;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        void InitTTS(bool isRetry = false)
        {
            try
            {
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                                   .GetStatic<AndroidJavaObject>("currentActivity");

                AndroidJavaObject ttsRef = null;
                var listener = new InitListener(success =>
                {
                    if (!success)
                    {
                        if (!isRetry)
                        {
                            Debug.LogWarning("[TTS] Google TTS 초기화 실패 — 엔진 없이 재시도");
                            _initFailed = true;
                        }
                        else
                        {
                            Debug.LogWarning("[TTS] 초기화 재시도도 실패 — TTS 사용 불가");
                        }
                        return;
                    }

                    var tts = ttsRef ?? _tts;
                    if (tts == null) { Debug.LogWarning("[TTS] TTS 객체 null (레이스 컨디션)"); return; }

                    var locale = new AndroidJavaClass("java.util.Locale")
                                     .GetStatic<AndroidJavaObject>("KOREA");
                    int result = tts.Call<int>("setLanguage", locale);
                    if (result < 0)
                    {
                        Debug.LogWarning("[TTS] 한국어 언어팩 없음 — 시스템 기본 언어 사용");
                        var defLocale = new AndroidJavaClass("java.util.Locale")
                                            .CallStatic<AndroidJavaObject>("getDefault");
                        tts.Call<int>("setLanguage", defLocale);
                    }

                    tts.Call<int>("setSpeechRate", speechRate);
                    tts.Call<int>("setPitch", speechPitch);
                    _tts = tts;
                    _ready = true;
                    Debug.Log("[TTS] 초기화 완료");
                });

                // 3인자 생성자로 Google TTS 엔진을 명시 (Quest에 기본 엔진 없음)
                // isRetry 때는 엔진 지정 없이 시스템 기본으로 폴백
                if (!isRetry)
                    ttsRef = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, listener, "com.google.android.tts");
                else
                    ttsRef = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, listener);
                if (!_ready) _tts = ttsRef;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TTS] Android TTS 생성 오류: {e.Message}");
            }
        }

        void SpeakInternal(string text)
        {
            try
            {
                // QUEUE_FLUSH(0) = 현재 발화 즉시 중단 후 새 텍스트 시작. null Bundle 대신 빈 Bundle 사용(일부 기기 호환)
                using var bundle = new AndroidJavaObject("android.os.Bundle");
                _tts.Call<int>("speak", text, 0, bundle, "utterance");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TTS] speak 호출 오류: {e.Message}");
                return;
            }
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

#if UNITY_EDITOR_WIN
        // Windows 에디터 전용: PowerShell의 System.Speech로 실제 음성 출력
        // 이전 프로세스를 종료한 뒤 새 프로세스를 시작해 중복 재생 방지
        static System.Diagnostics.Process _editorSpeechProcess;

        static void EditorSpeak(string text)
        {
            try { _editorSpeechProcess?.Kill(); } catch { }
            _editorSpeechProcess?.Dispose();
            _editorSpeechProcess = null;

            var safe = System.Text.RegularExpressions.Regex.Replace(text, @"[""'`\\]", " ");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"" +
                            $"Add-Type -AssemblyName System.Speech; " +
                            $"$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                            $"$s.Speak('{safe}')\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            _editorSpeechProcess = System.Diagnostics.Process.Start(psi);
        }
#endif
    }
}
