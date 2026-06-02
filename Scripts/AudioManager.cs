using UnityEngine;

namespace PP
{
    /// <summary>
    /// Place WAV files in Assets/Resources/Audio/
    /// music_loop.wav, bell.wav, stamp.wav, murmur.wav
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        AudioSource _music;
        AudioSource _sfx;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            // Music source - looping, quiet
            _music        = gameObject.AddComponent<AudioSource>();
            _music.loop   = true;
            _music.volume = 0.30f;
            _music.playOnAwake = false;

            // SFX source - one-shot
            _sfx        = gameObject.AddComponent<AudioSource>();
            _sfx.loop   = false;
            _sfx.volume = 0.85f;
            _sfx.playOnAwake = false;
        }

        void Start()
        {
            // Try loading music — WAV must be in Assets/Resources/Audio/
            var clip = Resources.Load<AudioClip>("Audio/music_loop");
            if (clip != null)
            {
                _music.clip = clip;
                _music.Play();
                Debug.Log("[AudioManager] Music started.");
            }
            else
            {
                Debug.LogWarning("[AudioManager] Could not find Assets/Resources/Audio/music_loop.wav");
                Debug.LogWarning("[AudioManager] Make sure the file is in Assets/Resources/Audio/ and is imported as AudioClip.");
            }
        }

        public void PlayBell()   => Play("Audio/bell");
        public void PlayStamp()  => Play("Audio/stamp");
        public void PlayMurmur() => Play("Audio/murmur");

        void Play(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip != null)
                _sfx.PlayOneShot(clip);
            else
                Debug.LogWarning("[AudioManager] Clip not found: Assets/Resources/" + path + ".wav");
        }

        public void SetMusicVolume(float v) => _music.volume = v;
    }
}
