using UnityEngine;
using TowerDefense.Common;
using TowerDefense.Manager;

namespace TowerDefense.Enemies
{
    public class BossSpawnImpact : MonoBehaviour
    {
        [Header("AoE")]
        [SerializeField] private float radius = 5f;
        [SerializeField] private int damage = 8;
        [SerializeField] private float knockbackForce = 14f;
        [SerializeField] private float knockbackUp = 0.5f;
        [SerializeField] private float knockbackStun = 0.4f;
        [SerializeField] private string playerTag = "Player";

        [Header("Camera Shake")]
        [SerializeField] private bool shakeCameraHeavy = true;

        [Header("VFX")]
        [Tooltip("Prefab instanciado no ponto de spawn (ex: nuvem de poeira, explosão).")]
        [SerializeField] private GameObject impactVfxPrefab;
        [SerializeField] private float vfxLifetime = 1.5f;

        private void Start()
        {
            if (shakeCameraHeavy)
                CameraShaker.Instance?.ShakeHeavy();

            BossAlertHUD.Instance?.ShowAlert();

            if (impactVfxPrefab != null)
            {
                var vfx = Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, vfxLifetime);
            }

            var hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var h in hits)
            {
                if (h.gameObject == gameObject) continue;
                if (!h.CompareTag(playerTag)) continue;

                var dmg = h.GetComponent<IDamageable>();
                if (dmg != null && !dmg.IsDead) dmg.TakeDamage(damage);

                var kb = h.GetComponent<Knockback>();
                if (kb != null)
                {
                    float dirX = transform.position.x < h.transform.position.x ? 1f : -1f;
                    kb.Apply(new Vector2(dirX, knockbackUp), knockbackForce, knockbackStun);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
