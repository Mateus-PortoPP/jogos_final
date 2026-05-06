using UnityEngine;

namespace TowerDefense.Common
{
    /// <summary>
    /// Som em loop com configuração simples. Pode ser usado pra:
    /// - Música de fundo (Spatial Blend = 0)
    /// - Ambiente sem posição (vento, noite) (Spatial Blend = 0)
    /// - Som posicional que varia com distância (fogueira) (Spatial Blend = 1)
    ///
    /// Anexa-se ao GameObject que deve emitir o som — adiciona um AudioSource
    /// configurado e dá Play.
    /// </summary>
    public class AmbientSound : MonoBehaviour
    {
        [Header("Áudio")]
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnAwake = true;

        [Header("Espacialização")]
        [Tooltip("0 = som 2D (volume constante), 1 = som 3D (varia com distância do listener).")]
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f;
        [Tooltip("Distância em que o som está no volume cheio (apenas Spatial Blend > 0).")]
        [SerializeField] private float minDistance = 2f;
        [Tooltip("Distância máxima em que o som ainda é audível.")]
        [SerializeField] private float maxDistance = 12f;

        private AudioSource source;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            if (playOnAwake && clip != null) source.Play();
        }

        public void Play() { if (source != null) source.Play(); }
        public void Stop() { if (source != null) source.Stop(); }
    }
}
