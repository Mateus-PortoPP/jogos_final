using UnityEngine;
using TowerDefense.Common;

namespace TowerDefense.Manager
{
    /// <summary>
    /// Projétil disparado por um CannonTurret. Voa em linha reta na direção
    /// passada por Launch() e some ao bater num inimigo (causando dano) ou
    /// após um tempo limite.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class CannonProjectile : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 3f;

        private int damage;
        private string targetTag = "Enemy";
        private Rigidbody2D rb;
        private bool launched;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void Launch(Vector2 direction, float speed, int dmg, string enemyTag)
        {
            damage = dmg;
            targetTag = enemyTag;
            rb.linearVelocity = direction.normalized * speed;
            transform.right = direction;
            launched = true;
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!launched) return;
            if (!other.CompareTag(targetTag)) return;

            var dmg = other.GetComponent<IDamageable>();
            if (dmg != null && !dmg.IsDead)
            {
                dmg.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
