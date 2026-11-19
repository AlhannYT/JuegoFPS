using UnityEngine;

namespace JuegoFPS
{
    /// <summary>
    /// Representa una diana u objetivo destructible con puntuación diferenciada.
    /// Al recibir un impacto, otorga sus puntos correspondientes y se destruye junto con el proyectil.
    /// </summary>
    public class TargetObject : MonoBehaviour
    {
        [Header("Identificación y Puntuación")]
        public string targetName = "Diana Normal";
        public int pointsValue = 50;
        public bool countsForLevelCompletion = true;
        public Color hitEffectColor = new Color(1f, 0.8f, 0.2f);

        [Header("Movimiento Opcional")]
        public bool isMoving = false;
        public Vector3 moveAxis = Vector3.right;
        public float moveDistance = 2.5f;
        public float moveSpeed = 2f;

        private Vector3 _startPos;
        private float _timeOffset;
        private bool _isDestroyed;

        private void Start()
        {
            _startPos = transform.position;
            _timeOffset = Random.Range(0f, Mathf.PI * 2f);

            if (countsForLevelCompletion && GameManager.Instance != null)
            {
                GameManager.Instance.RegisterTarget();
            }
        }

        private void Update()
        {
            if (isMoving && !_isDestroyed)
            {
                float offset = Mathf.Sin((Time.time + _timeOffset) * moveSpeed) * moveDistance;
                transform.position = _startPos + moveAxis.normalized * offset;
            }
        }

        /// <summary>
        /// Llamado por el proyectil al impactar contra este objetivo.
        /// </summary>
        public void OnHitByProjectile()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.PlayHitTarget();
            }

            SpawnHitParticles();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTargetDestroyed(targetName, pointsValue, countsForLevelCompletion);
            }
            else
            {
                Debug.Log($"[Impacto] {targetName} destruido (+{pointsValue} pts).");
            }

            Destroy(gameObject);
        }

        private void SpawnHitParticles()
        {
            for (int i = 0; i < 6; i++)
            {
                GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.transform.position = transform.position;
                spark.transform.localScale = Vector3.one * 0.12f;
                Destroy(spark.GetComponent<Collider>());

                Renderer r = spark.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = hitEffectColor;
                }

                Rigidbody rb = spark.AddComponent<Rigidbody>();
                rb.AddForce(Random.onUnitSphere * Random.Range(3f, 7f), ForceMode.Impulse);
                Destroy(spark, 0.6f);
            }
        }
    }
}
