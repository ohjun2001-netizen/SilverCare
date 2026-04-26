// Assets/Scripts/SongGuess/SongPlayer.cs
// 담당: 조성우
// TODO: AudioSource 연동, 페이드인/아웃, STT 서버 연동
using System.Collections;
using UnityEngine;
using SilverCare.Common;

namespace SilverCare.SongGuess
{
    public class SongPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private float fadeTime = 1.0f;

        private Coroutine _stopCoroutine;

        /// <summary>노래 재생 후 duration초 뒤 자동 정지</summary>
        public void PlaySong(string audioFile, float duration)
        {
            AudioClip clip = Resources.Load<AudioClip>($"Audio/Songs/{audioFile}");
            if (clip == null)
            {
                Debug.LogWarning($"[SongPlayer] 오디오 파일 없음: {audioFile}");
                return;
            }

            musicSource.clip = clip;
            musicSource.Play();

            if (_stopCoroutine != null) StopCoroutine(_stopCoroutine);
            _stopCoroutine = StartCoroutine(StopAfter(duration));
        }

        public void StopSong() => musicSource?.Stop();

        private IEnumerator StopAfter(float duration)
        {
            yield return new WaitForSeconds(duration - fadeTime);
            yield return FadeOut();
            musicSource?.Stop();
        }

        private IEnumerator FadeOut()
        {
            float startVol = musicSource.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVol, 0, t / fadeTime);
                yield return null;
            }
            musicSource.volume = startVol;
        }
    }
}
