// Unity 복사 경로: Assets/Scripts/Baduk/BadukTTSConnector.cs
// FastAPI 서버(박건영 담당)의 TTS 엔드포인트를 호출합니다.
// Inspector에서 serverUrl 설정 필요 (예: http://192.168.x.x:8000)
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Baduk
{
    public class BadukTTSConnector : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private string serverUrl = "http://localhost:8000";
        [SerializeField] private string ttsEndpoint = "/tts/speak";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private bool _isSpeaking = false;

        /// <summary>텍스트를 TTS 서버로 전송하여 음성 재생</summary>
        public void Speak(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (_isSpeaking) StopSpeaking();   // 기존 발화 중단 후 새 발화

            StartCoroutine(RequestTTS(text));
        }

        /// <summary>현재 재생 중지</summary>
        public void StopSpeaking()
        {
            audioSource?.Stop();
            _isSpeaking = false;
            StopAllCoroutines();
        }

        private IEnumerator RequestTTS(string text)
        {
            _isSpeaking = true;

            string json = $"{{\"text\": \"{EscapeJson(text)}\"}}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using UnityWebRequest req = new UnityWebRequest($"{serverUrl}{ttsEndpoint}", "POST");
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerAudioClip(req.url, AudioType.MPEG);
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 5;  // 서버 없을 때 빠르게 실패

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
                Debug.LogWarning($"[BadukTTS] TTS 요청 실패: {req.error}");
                // 서버 없을 때 폴백: Debug.Log로 텍스트 출력
                Debug.Log($"[BadukTTS] 음성 출력(폴백): {text}");
            }

            _isSpeaking = false;
        }

        private static string EscapeJson(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
