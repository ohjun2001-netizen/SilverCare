// Assets/Scripts/Common/TTSManager.cs
// 전체 프로젝트 공통 TTS 싱글턴 - 박건영 FastAPI 서버 연동
// 개별 게임에서 TTSManager.Instance.Speak("텍스트") 로 호출
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SilverCare.Common
{
    public class TTSManager : MonoBehaviour
    {
        public static TTSManager Instance { get; private set; }

        [Header("Server")]
        [SerializeField] private string serverUrl = "http://localhost:8000";
        [SerializeField] private string ttsEndpoint = "/tts/speak";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private bool _isSpeaking;
        private Coroutine _currentCoroutine;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>텍스트 음성 변환 재생. 이전 발화 중이면 중단 후 새로 시작.</summary>
        public void Speak(string text, bool interruptCurrent = false)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_isSpeaking)
            {
                if (!interruptCurrent) return;
                StopSpeaking();
            }

            _currentCoroutine = StartCoroutine(RequestTTS(text));
        }

        public void StopSpeaking()
        {
            if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
            audioSource?.Stop();
            _isSpeaking = false;
        }

        private IEnumerator RequestTTS(string text)
        {
            _isSpeaking = true;
            string json = $"{{\"text\":\"{Escape(text)}\"}}";

            using var req = new UnityWebRequest($"{serverUrl}{ttsEndpoint}", "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerAudioClip(req.url, AudioType.MPEG);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip != null && audioSource != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    yield return new WaitForSeconds(clip.length);
                }
            }
            else
            {
                Debug.LogWarning($"[TTS] 서버 연결 실패: {req.error} → 텍스트: {text}");
            }

            _isSpeaking = false;
        }

        private static string Escape(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
