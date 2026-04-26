// Assets/Scripts/Common/AudioManager.cs
// BGM·SFX 공통 관리 싱글턴
using UnityEngine;

namespace SilverCare.Common
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Clips - BGM")]
        [SerializeField] private AudioClip lobbyBGM;
        [SerializeField] private AudioClip gameBGM;

        [Header("Clips - SFX")]
        [SerializeField] private AudioClip correctSFX;
        [SerializeField] private AudioClip wrongSFX;
        [SerializeField] private AudioClip buttonClickSFX;
        [SerializeField] private AudioClip gameStartSFX;
        [SerializeField] private AudioClip gameClearSFX;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── BGM ────────────────────────────────────────────────
        public void PlayLobbyBGM()  => PlayBGM(lobbyBGM);
        public void PlayGameBGM()   => PlayBGM(gameBGM);
        public void StopBGM()       => bgmSource?.Stop();

        private void PlayBGM(AudioClip clip)
        {
            if (clip == null || bgmSource == null) return;
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // ── SFX ────────────────────────────────────────────────
        public void PlayCorrect()    => PlaySFX(correctSFX);
        public void PlayWrong()      => PlaySFX(wrongSFX);
        public void PlayButtonClick() => PlaySFX(buttonClickSFX);
        public void PlayGameStart()  => PlaySFX(gameStartSFX);
        public void PlayGameClear()  => PlaySFX(gameClearSFX);

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

        // ── 볼륨 ───────────────────────────────────────────────
        public void SetBGMVolume(float v)  { if (bgmSource)  bgmSource.volume  = v; }
        public void SetSFXVolume(float v)  { if (sfxSource)  sfxSource.volume  = v; }
    }
}
