using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Quando TODAS as ondas da noite terminam, faz a câmera ir até um alvo
    /// (ex: a porta de saída) e voltar — guia o jogador sem precisar de texto.
    /// </summary>
    public class CameraCueOnWavesCompleted : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Se true, só dispara uma vez (não repete a cada reload da cena na mesma noite).")]
        [SerializeField] private bool once = true;

        private bool fired;

        private void OnEnable()
        {
            if (WaveManager.Instance != null) Subscribe();
            else Invoke(nameof(Subscribe), 0.05f);
        }

        private void Subscribe()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted += Handle;
        }

        private void OnDisable()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted -= Handle;
        }

        private void Handle()
        {
            if (once && fired) return;
            fired = true;
            if (target != null && CameraFocusPan.Instance != null)
                CameraFocusPan.Instance.FocusOn(target.position);
        }
    }
}
