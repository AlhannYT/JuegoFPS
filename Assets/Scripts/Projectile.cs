using UnityEngine;

namespace JuegoFPS
{
    /// <summary>
    /// Proyectil físico disparado por el arma del jugador.
    /// Al impactar contra una diana o enemigo, se destruye y aplica el efecto correspondiente.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public float speed = 55f;
        public float lifeTime = 3.5f;
        public int damage = 15;

        private Rigidbody _rb;
        private bool _hasHit;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }
            _rb.useGravity = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterActiveProjectile();
            }

            _rb.linearVelocity = transform.forward * speed;
            Destroy(gameObject, lifeTime);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterActiveProjectile();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.collider);
        }

        private void HandleHit(Collider col)
        {
            if (_hasHit || col == null) return;
            if (col.CompareTag("Player") || col.GetComponent<Projectile>() != null) return;

            _hasHit = true;

            // 1. ¿Impactó una Diana / Objetivo?
            TargetObject target = col.GetComponentInParent<TargetObject>();
            if (target != null)
            {
                target.OnHitByProjectile();
                Destroy(gameObject);
                return;
            }

            // 2. ¿Impactó un Enemigo o Boss?
            EnemyController enemy = col.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            // 3. Impactó contra pared o suelo
            Destroy(gameObject);
        }
    }
}
