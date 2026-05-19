using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Toca a música de fundo em loop durante todo o jogo, persistindo entre cenas.
    /// Coloque um GameObject com este componente apenas na primeira cena (Menu_Inicial).
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [SerializeField] private AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.028f;

        private AudioSource source;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            source = gameObject.AddComponent<AudioSource>();
            source.clip = musicClip;
            source.volume = volume;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            if (musicClip != null) source.Play();
        }

        public void SetVolume(float v) { if (source != null) source.volume = v; }
        public void Stop() { if (source != null) source.Stop(); }
        public void Play() { if (source != null && !source.isPlaying) source.Play(); }
    }
}
