using System.Collections;
using UnityEngine;

namespace TowerDefense.Common
{
    [RequireComponent(typeof(Health))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private float flashDuration = 0.08f;
        [SerializeField] private SpriteRenderer targetRenderer;
        [Tooltip("Material que pinta o sprite inteiro de uma cor sólida (usa o alpha da textura).")]
        [SerializeField] private Material flashMaterial;

        private Health health;
        private Material originalMaterial;
        private Coroutine flashRoutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>();
            if (targetRenderer != null) originalMaterial = targetRenderer.sharedMaterial;
        }

        private void OnEnable() { if (health != null) health.Damaged += OnDamaged; }
        private void OnDisable() { if (health != null) health.Damaged -= OnDamaged; }

        private void OnDamaged(int current, int max)
        {
            if (targetRenderer == null || flashMaterial == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            targetRenderer.material = flashMaterial;
            yield return new WaitForSeconds(flashDuration);
            if (targetRenderer != null) targetRenderer.material = originalMaterial;
            flashRoutine = null;
        }
    }
}
