using UnityEngine;

namespace TowerDefense.Common
{
    [RequireComponent(typeof(Health))]
    public class CameraShakeOnDamage : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.35f;
        private Health health;

        private void Awake() { health = GetComponent<Health>(); }
        private void OnEnable() { if (health != null) health.Damaged += OnDamaged; }
        private void OnDisable() { if (health != null) health.Damaged -= OnDamaged; }

        private void OnDamaged(int current, int max)
        {
            if (CameraShaker.Instance == null) return;
            CameraShaker.Instance.ShakeWithAmplitude(amplitude);
        }
    }
}
