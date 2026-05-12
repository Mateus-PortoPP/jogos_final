using UnityEngine;
using TowerDefense.Common;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Canhão fixo que detecta o inimigo mais próximo dentro do range e atira projéteis.
    /// Cooldown entre tiros configurável. Não persegue — só atira na direção do alvo
    /// no momento do tiro.
    ///
    /// Setup do prefab:
    ///   - Sprite Renderer com a arte do canhão
    ///   - (opcional) Animator
    ///   - este script
    ///   - referência ao projectilePrefab (que tem CannonProjectile + Rigidbody2D + Collider2D trigger)
    ///   - firePoint = transform empty no "bico" do canhão (de onde o projétil sai)
    /// </summary>
    public class CannonTurret : MonoBehaviour
    {
        [Header("Detecção")]
        [SerializeField] private float range = 8f;
        [SerializeField] private string targetTag = "Enemy";

        [Header("Tiro")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireCooldown = 1.2f;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private int projectileDamage = 20;

        private float lastFireTime = -999f;

        private void Update()
        {
            if (Time.time - lastFireTime < fireCooldown) return;

            var target = FindClosestEnemy();
            if (target == null) return;

            Fire(target);
            lastFireTime = Time.time;
        }

        private Transform FindClosestEnemy()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, range);
            Transform closest = null;
            float closestDist = float.MaxValue;
            foreach (var hit in hits)
            {
                if (!hit.CompareTag(targetTag)) continue;
                var dmg = hit.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsDead) continue;
                float d = Vector2.Distance(transform.position, hit.transform.position);
                if (d < closestDist) { closestDist = d; closest = hit.transform; }
            }
            return closest;
        }

        private void Fire(Transform target)
        {
            if (projectilePrefab == null) return;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Vector2 dir = ((Vector2)(target.position - spawnPos)).normalized;
            var cp = proj.GetComponent<CannonProjectile>();
            if (cp != null) cp.Launch(dir, projectileSpeed, projectileDamage, targetTag);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
