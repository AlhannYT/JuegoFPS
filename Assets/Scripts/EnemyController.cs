using UnityEngine;

namespace JuegoFPS
{
    /// <summary>
    /// Controla el comportamiento de los enemigos (Esqueletos) y del Boss final.
    /// Persigue al jugador, le resta una vida al chocar con él y muere al recibir disparos.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        [Header("Estadísticas")]
        public string enemyName = "Esqueleto";
        public bool isBoss = false;
        public int maxHealth = 25;
        public int pointsValue = 150;
        public bool countsForLevelCompletion = true;

        [Header("Movimiento y Combate")]
        public float moveSpeed = 3.2f;
        public float detectionRange = 35f;
        public float contactDamageDistance = 1.35f;

        private int _currentHealth;
        private Transform _player;
        private float _bobTimer;
        private float _baseY;
        private bool _isDead;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHealth;

        private void Start()
        {
            _currentHealth = maxHealth;
            _baseY = transform.position.y;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }

            if (countsForLevelCompletion && GameManager.Instance != null)
            {
                GameManager.Instance.RegisterEnemy();
            }

            if (isBoss && HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowBossHealth(enemyName, _currentHealth, maxHealth);
            }
        }

        private void Update()
        {
            if (_isDead) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            if (_player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) _player = playerObj.transform;
                else return;
            }

            Vector3 toPlayer = _player.position - transform.position;
            Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
            float dist = flatToPlayer.magnitude;

            if (dist <= detectionRange && dist > 0.1f)
            {
                Vector3 dir = flatToPlayer.normalized;
                transform.position += dir * (moveSpeed * Time.deltaTime);

                // Rotar suavemente hacia el jugador
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);

                // Pequeño balanceo al caminar
                _bobTimer += Time.deltaTime * (isBoss ? 6f : 10f);
                Vector3 pos = transform.position;
                pos.y = _baseY + Mathf.Abs(Mathf.Sin(_bobTimer)) * 0.08f;
                transform.position = pos;
            }

            // Daño por proximidad / choque con el jugador
            if (dist <= contactDamageDistance)
            {
                TryDamagePlayer();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isDead) return;
            if (collision.gameObject.CompareTag("Player"))
            {
                TryDamagePlayer();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isDead) return;
            if (other.CompareTag("Player"))
            {
                TryDamagePlayer();
            }
        }

        private void TryDamagePlayer()
        {
            if (_player == null) return;
            PlayerControllerFPS pc = _player.GetComponent<PlayerControllerFPS>();
            if (pc != null)
            {
                pc.TakeDamage(1, enemyName);
            }
        }

        /// <summary>
        /// Recibe daño de un proyectil.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHealth -= damage;

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.PlayHitEnemy();
            }

            if (isBoss && HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateBossHealth(_currentHealth, maxHealth);
            }

            if (_currentHealth <= 0)
            {
                Die();
            }
            else if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowHitFeedback($"{enemyName} ({_currentHealth}/{maxHealth} HP)");
            }
        }

        private void Die()
        {
            _isDead = true;

            if (isBoss && HUDManager.Instance != null)
            {
                HUDManager.Instance.HideBossHealth();
            }

            SpawnDeathParticles();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyDestroyed(enemyName, pointsValue, countsForLevelCompletion, isBoss);
            }
            else
            {
                Debug.Log($"[Enemigo] {enemyName} eliminado (+{pointsValue} pts).");
            }

            Destroy(gameObject);
        }

        private void SpawnDeathParticles()
        {
            int count = isBoss ? 14 : 7;
            Color c = isBoss ? new Color(0.9f, 0.2f, 0.9f) : new Color(0.9f, 0.25f, 0.2f);
            for (int i = 0; i < count; i++)
            {
                GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.transform.position = transform.position + Vector3.up * 1f;
                part.transform.localScale = Vector3.one * (isBoss ? 0.28f : 0.16f);
                Destroy(part.GetComponent<Collider>());

                Renderer r = part.GetComponent<Renderer>();
                if (r != null) r.material.color = c;

                Rigidbody rb = part.AddComponent<Rigidbody>();
                rb.AddForce(Random.onUnitSphere * Random.Range(4f, 9f), ForceMode.Impulse);
                Destroy(part, 0.8f);
            }
        }
    }
}
