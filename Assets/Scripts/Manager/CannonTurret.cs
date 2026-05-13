using UnityEngine;
using TowerDefense.Common;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Canhão fixo (base estática) com cano rotativo (barrelTransform) que aponta
    /// pro inimigo mais próximo dentro do range. Atira projéteis com cooldown.
    ///
    /// Setup do prefab:
    ///   - Root: SpriteRenderer da BASE + este script
    ///   - Filho 'Barrel': empty Transform na junta (ponto de rotação)
    ///     - Filho 'CanoSprite': SpriteRenderer do cano (posicionado com o "mount"
    ///       coincidindo com o pivot do Barrel)
    ///     - Filho 'FirePoint': empty Transform no bico do cano
    ///   - 'barrelTransform' aponta pro filho Barrel
    ///   - 'firePoint' aponta pro FirePoint (filho de Barrel pra acompanhar a rotação)
    ///
    /// barrelAngleOffset compensa a orientação default do sprite:
    ///   - Se o sprite do cano "olha pra direita" no editor: offset = 0°
    ///   - Se o sprite olha pra esquerda: offset = 180°
    /// </summary>
    public class CannonTurret : MonoBehaviour
    {
        [Header("Detecção")]
        [SerializeField] private float range = 8f;
        [SerializeField] private string targetTag = "Enemy";

        [Header("Cano (rotaciona)")]
        [Tooltip("Transform do cano que rotaciona pra apontar no inimigo. Pivot deve estar na junta com a base.")]
        [SerializeField] private Transform barrelTransform;
        [Tooltip("Ângulo (graus) somado à rotação. Use 180 se o sprite do cano olha pra esquerda por padrão.")]
        [SerializeField] private float barrelAngleOffset = 180f;
        [Tooltip("Velocidade de rotação (graus/seg). 0 = instantâneo.")]
        [SerializeField] private float barrelTurnSpeed = 720f;

        [Header("Tiro")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireCooldown = 1.2f;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private int projectileDamage = 20;

        private float lastFireTime = -999f;
        private Transform currentTarget;

        private void Update()
        {
            currentTarget = FindClosestEnemy();
            if (currentTarget != null) AimAt(currentTarget);

            if (Time.time - lastFireTime < fireCooldown) return;
            if (currentTarget == null) return;
            Fire(currentTarget);
            lastFireTime = Time.time;
        }

        private void AimAt(Transform target)
        {
            if (barrelTransform == null) return;
            Vector2 dir = (Vector2)(target.position - barrelTransform.position);
            if (dir.sqrMagnitude < 0.0001f) return;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + barrelAngleOffset;
            Quaternion desired = Quaternion.Euler(0, 0, targetAngle);
            if (barrelTurnSpeed <= 0f)
            {
                barrelTransform.rotation = desired;
            }
            else
            {
                barrelTransform.rotation = Quaternion.RotateTowards(barrelTransform.rotation, desired, barrelTurnSpeed * Time.deltaTime);
            }
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
