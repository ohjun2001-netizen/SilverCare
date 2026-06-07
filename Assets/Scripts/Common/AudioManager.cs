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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null || FindObjectOfType<AudioManager>() != null)
                return;

            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
            EnsureFallbackSfx();
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

        void EnsureSources()
        {
            if (bgmSource == null)
            {
                var bgm = new GameObject("BGMSource");
                bgm.transform.SetParent(transform, false);
                bgmSource = bgm.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
            }

            if (sfxSource == null)
            {
                var sfx = new GameObject("SFXSource");
                sfx.transform.SetParent(transform, false);
                sfxSource = sfx.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        void EnsureFallbackSfx()
        {
            correctSFX ??= BuildTone("CorrectSFX", 880f, 0.12f, 0.35f);
            wrongSFX ??= BuildTone("WrongSFX", 220f, 0.16f, 0.30f);
            buttonClickSFX ??= BuildTone("ButtonClickSFX", 660f, 0.06f, 0.22f);
            gameStartSFX ??= BuildTone("GameStartSFX", 520f, 0.10f, 0.28f);
            gameClearSFX ??= BuildTone("GameClearSFX", 1040f, 0.18f, 0.30f);
        }

        static AudioClip BuildTone(string name, float frequency, float duration, float volume)
        {
            const int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(rate * duration));
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float fade = Mathf.Sin(Mathf.PI * i / Mathf.Max(1, samples - 1));
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * fade;
            }

            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
