using UnityEngine;
using UnityEngine.InputSystem;

namespace JuegoFPS
{
    /// <summary>
    /// Controlador en primera persona (FPS).
    /// Movimiento con las flechas del teclado (y WASD opcional) y apuntado con el ratón.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerControllerFPS : MonoBehaviour
    {
        [Header("Movimiento")]
        public float moveSpeed = 7.5f;
        public float jumpForce = 5.5f;
        public float gravity = -18f;

        [Header("Cámara / Ratón")]
        public Transform cameraTransform;
        public float mouseSensitivity = 0.16f;
        public float maxLookAngle = 80f;

        [Header("Supervivencia")]
        public float invulnerabilityDuration = 1.2f;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _pitch;
        private float _invulnTimer;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();

            if (cameraTransform == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }

            LockCursor(true);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                LockCursor(false);
                return;
            }

            if (_invulnTimer > 0f)
            {
                _invulnTimer -= Time.deltaTime;
            }

            HandleCursorToggle();

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                HandleMouseLook();
            }

            HandleMovement();
        }

        private void HandleCursorToggle()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                LockCursor(Cursor.lockState != CursorLockMode.Locked);
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }
        }

        private void HandleMouseLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || cameraTransform == null) return;

            Vector2 delta = mouse.delta.ReadValue();
            float yaw = delta.x * mouseSensitivity;
            float pitchDelta = delta.y * mouseSensitivity;

            transform.Rotate(Vector3.up * yaw);

            _pitch -= pitchDelta;
            _pitch = Mathf.Clamp(_pitch, -maxLookAngle, maxLookAngle);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            float moveX = 0f;
            float moveZ = 0f;

            // Flechas del teclado (requisito principal) + WASD como alternativa cómoda
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) moveX -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) moveX += 1f;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) moveZ += 1f;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) moveZ -= 1f;

            Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized;
            _controller.Move(move * (moveSpeed * Time.deltaTime));

            if (_controller.isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }

            if (_controller.isGrounded && kb.spaceKey.wasPressedThisFrame)
            {
                _velocity.y = jumpForce;
            }

            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider == null) return;
            EnemyController enemy = hit.collider.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                TakeDamage(1, enemy.enemyName);
            }
        }

        public void TakeDamage(int amount, string sourceName)
        {
            if (_invulnTimer > 0f) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            _invulnTimer = invulnerabilityDuration;

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.PlayPlayerHurt();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.DamagePlayer(amount, sourceName);
            }
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
