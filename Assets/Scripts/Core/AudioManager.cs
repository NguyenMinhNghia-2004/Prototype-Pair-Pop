using UnityEngine;
using System.Collections.Generic;

namespace PairPop.Core {
    public class AudioManager : MonoBehaviour {
        public static AudioManager Instance { get; private set; }

        [System.Serializable]
        public struct SoundClip {
            public string id;
            public AudioClip clip;
            public bool isMusic;
            // Cho phép cấu hình default pitch/volume
            public float baseVolume; 
            public float basePitch;
        }

        [SerializeField] private SoundClip[] sounds;
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        private Dictionary<string, SoundClip> soundDic;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
                return;
            }

            soundDic = new Dictionary<string, SoundClip>();
            foreach (var s in sounds) {
                var copy = s;
                if(copy.baseVolume == 0) copy.baseVolume = 1f;
                if(copy.basePitch == 0) copy.basePitch = 1f;
                soundDic[copy.id] = copy;
            }
        }

        public void PlayMusic(string id) {
            if (soundDic.TryGetValue(id, out var sound)) {
                musicSource.clip = sound.clip;
                musicSource.volume = sound.baseVolume;
                musicSource.pitch = sound.basePitch;
                musicSource.loop = true;
                musicSource.Play();
            }
        }

        public void PlaySFX(string id, float pitchRandomness = 0f) {
            if (soundDic.TryGetValue(id, out var sound)) {
                sfxSource.pitch = sound.basePitch + Random.Range(-pitchRandomness, pitchRandomness);
                sfxSource.PlayOneShot(sound.clip, sound.baseVolume);
            }
        }
    }
}
