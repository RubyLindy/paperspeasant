using UnityEngine;

namespace PP
{
    /// <summary>
    /// Handles all game audio. Place on any persistent GameObject.
    /// Loads WAV clips from Resources/Audio/.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        AudioSource _music;
        AudioSource _sfx;

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop   = true;
            _music.volume = 0.35f;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.loop   = false;
            _sfx.volume = 0.8f;
        }

        void Start()
        {
            var musicClip = Resources.Load<AudioClip>("Audio/music_loop");
            if (musicClip != null) { _music.clip = musicClip; _music.Play(); }
            else Debug.LogWarning("[AudioManager] music_loop not found in Resources/Audio/");
        }

        public void PlayBell()    => PlaySFX("Audio/bell");
        public void PlayStamp()   => PlaySFX("Audio/stamp");
        public void PlayMurmur()  => PlaySFX("Audio/murmur");

        void PlaySFX(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null) _sfx.PlayOneShot(clip);
            else Debug.LogWarning($"[AudioManager] Clip not found: {path}");
        }

        public void SetMusicVolume(float v) => _music.volume = v;
    }
}
