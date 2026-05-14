using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace TowerDefense.Common
{
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }

        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Presets de intensidade")]
        [SerializeField] private float lightAmplitude = 0.15f;
        [SerializeField] private float mediumAmplitude = 0.35f;
        [SerializeField] private float heavyAmplitude = 0.7f;

        [Header("Rumble (charge)")]
        [SerializeField] private float rumbleInterval = 0.06f;
        [SerializeField] private float rumbleAmplitude = 0.08f;

        private Coroutine rumbleRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShakeLight() => ShakeWithAmplitude(lightAmplitude);
        public void ShakeMedium() => ShakeWithAmplitude(mediumAmplitude);
        public void ShakeHeavy() => ShakeWithAmplitude(heavyAmplitude);

        public void ShakeWithAmplitude(float amplitude)
        {
            if (impulseSource == null) return;
            var v = Random.insideUnitCircle.normalized * amplitude;
            impulseSource.GenerateImpulse(new Vector3(v.x, v.y, 0f));
        }

        public void StartRumble()
        {
            if (rumbleRoutine != null) return;
            rumbleRoutine = StartCoroutine(RumbleLoop());
        }

        public void StopRumble()
        {
            if (rumbleRoutine == null) return;
            StopCoroutine(rumbleRoutine);
            rumbleRoutine = null;
        }

        private IEnumerator RumbleLoop()
        {
            var wait = new WaitForSeconds(rumbleInterval);
            while (true)
            {
                ShakeWithAmplitude(rumbleAmplitude);
                yield return wait;
            }
        }
    }
}
