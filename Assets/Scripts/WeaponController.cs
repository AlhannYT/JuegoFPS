using UnityEngine;
using UnityEngine.InputSystem;

namespace JuegoFPS
{
    /// <summary>
    /// Controla el disparo con clic izquierdo, el cambio entre Manos (1), M1911 (2) y SCAR (3),
    /// el retroceso visual y la generación de proyectiles.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("Referencias")]
        public Camera playerCamera;
        public Transform weaponHolder;
        public Transform muzzlePoint;
        public GameObject handsModel;
        public GameObject pistolModel;
        public GameObject rifleModel;
        public Light muzzleFlashLight;

        [Header("Estadísticas Pistola (M1911)")]
        public float pistolFireRate = 0.25f;
        public int pistolDamage = 15;
        public float pistolProjectileSpeed = 55f;

        [Header("Estadísticas Rifle (SCAR)")]
        public float rifleFireRate = 0.13f;
        public int rifleDamage = 25;
        public float rifleProjectileSpeed = 70f;

        // 0 = Manos (1), 1 = M1911 (2), 2 = SCAR (3)
        private int _currentWeaponIndex = 1;
        private float _nextFireTime;
        private float _flashTimer;
        private Vector3 _holderBasePos;

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (weaponHolder != null)
            {
                _holderBasePos = weaponHolder.localPosition;
            }

            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.enabled = false;
            }

            SelectWeapon(1);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            UpdateVisualEffects();

            if (Cursor.lockState != CursorLockMode.Locked) return;

            HandleWeaponSwitch();
            HandleFireInput();
        }

        private void UpdateVisualEffects()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && muzzleFlashLight != null)
                {
                    muzzleFlashLight.enabled = false;
                }
            }

            if (weaponHolder != null)
            {
                weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, _holderBasePos, Time.deltaTime * 14f);
            }
        }

        private void HandleWeaponSwitch()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) SelectWeapon(0);
                else if (kb.digit2Key.wasPressedThisFrame) SelectWeapon(1);
                else if (kb.digit3Key.wasPressedThisFrame) SelectWeapon(2);
            }

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f)
                {
                    SelectWeapon((_currentWeaponIndex + 1) % 3);
                }
                else if (scroll < -0.1f)
                {
                    SelectWeapon((_currentWeaponIndex + 2) % 3);
                }
            }
        }

        public void SelectWeapon(int index)
        {
            _currentWeaponIndex = Mathf.Clamp(index, 0, 2);

            if (handsModel != null) handsModel.SetActive(_currentWeaponIndex == 0);
            if (pistolModel != null) pistolModel.SetActive(_currentWeaponIndex == 1);
            if (rifleModel != null) rifleModel.SetActive(_currentWeaponIndex == 2);

            if (HUDManager.Instance != null)
            {
                string name = _currentWeaponIndex switch
                {
                    0 => "Manos",
                    1 => "M1911 (Pistola)",
                    _ => "SCAR (Rifle)"
                };
                HUDManager.Instance.UpdateWeaponName(name);
            }
        }

        private void HandleFireInput()
        {
            // Con las manos (0) no se dispara
            if (_currentWeaponIndex == 0) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            bool wantsFire = _currentWeaponIndex == 1
                ? mouse.leftButton.wasPressedThisFrame
                : mouse.leftButton.isPressed;

            if (!wantsFire || Time.time < _nextFireTime) return;

            if (GameManager.Instance != null && !GameManager.Instance.TryConsumeAmmo())
            {
                return;
            }

            float rate = _currentWeaponIndex == 1 ? pistolFireRate : rifleFireRate;
            _nextFireTime = Time.time + rate;

            FireProjectile();
        }

        private void FireProjectile()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint = ray.GetPoint(100f);
            if (Physics.Raycast(ray, out RaycastHit hit, 120f))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    targetPoint = hit.point;
                }
            }

            Vector3 spawnPos = muzzlePoint != null
                ? muzzlePoint.position
                : playerCamera.transform.position + playerCamera.transform.forward * 0.8f;

            Vector3 direction = (targetPoint - spawnPos).normalized;

            GameObject projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projObj.name = "Proyectil";
            projObj.transform.position = spawnPos;
            projObj.transform.rotation = Quaternion.LookRotation(direction);
            projObj.transform.localScale = new Vector3(0.11f, 0.11f, 0.24f);

            SphereCollider col = projObj.GetComponent<SphereCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.6f;
            }

            Renderer rend = projObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = _currentWeaponIndex == 1
                    ? new Color(1f, 0.85f, 0.2f)
                    : new Color(0.3f, 0.9f, 1f);
            }

            Projectile proj = projObj.AddComponent<Projectile>();
            proj.damage = _currentWeaponIndex == 1 ? pistolDamage : rifleDamage;
            proj.speed = _currentWeaponIndex == 1 ? pistolProjectileSpeed : rifleProjectileSpeed;

            if (weaponHolder != null)
            {
                weaponHolder.localPosition = _holderBasePos - new Vector3(0f, 0f, 0.08f);
            }

            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.enabled = true;
                _flashTimer = 0.05f;
            }

            if (ProceduralAudio.Instance != null)
            {
                if (_currentWeaponIndex == 1) ProceduralAudio.Instance.PlayPistolShot();
                else ProceduralAudio.Instance.PlayRifleShot();
            }
        }
    }
}
