using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace TowerDefense.Manager
{
    /// <summary>
    /// "Jogo de câmera": leva a câmera suavemente até um ponto do mundo,
    /// segura um tempo, e volta acompanhando o player (que pode ter se movido).
    /// Usa a CinemachineCamera trocando o alvo de Follow por um dummy temporário.
    /// </summary>
    public class CameraFocusPan : MonoBehaviour
    {
        public static CameraFocusPan Instance { get; private set; }

        [SerializeField] private CinemachineCamera vcam;
        [SerializeField] private float toDuration = 1.1f;
        [SerializeField] private float holdDuration = 0.9f;
        [SerializeField] private float backDuration = 1.1f;

        public bool IsBusy { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (vcam == null) vcam = FindObjectOfType<CinemachineCamera>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void FocusOn(Vector3 worldTarget)
        {
            if (IsBusy || vcam == null) return;
            StartCoroutine(Routine(worldTarget));
        }

        private IEnumerator Routine(Vector3 target)
        {
            IsBusy = true;

            var originalFollow = vcam.Follow;
            Vector3 start = originalFollow != null ? originalFollow.position : vcam.transform.position;

            var dummyGo = new GameObject("CamFocusDummy");
            var dummy = dummyGo.transform;
            dummy.position = start;
            vcam.Follow = dummy;

            // ida: start -> target
            float t = 0f;
            while (t < toDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / toDuration));
                dummy.position = Vector3.Lerp(start, target, k);
                yield return null;
            }
            dummy.position = target;

            yield return new WaitForSeconds(holdDuration);

            // volta: target -> posição ATUAL do player (acompanha se ele se moveu)
            t = 0f;
            while (t < backDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / backDuration));
                Vector3 live = originalFollow != null ? originalFollow.position : start;
                dummy.position = Vector3.Lerp(target, live, k);
                yield return null;
            }

            // devolve o controle pro player sem corte
            vcam.Follow = originalFollow;
            Destroy(dummyGo);
            IsBusy = false;
        }
    }
}
