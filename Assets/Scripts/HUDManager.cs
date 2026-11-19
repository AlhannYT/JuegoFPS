using UnityEngine;
using UnityEngine.UI;

namespace JuegoFPS
{
    /// <summary>
    /// Administra la interfaz en pantalla (HUD): mira, puntuación, vidas, munición,
    /// arma equipada, barra de vida del Boss y menú final de victoria/derrota.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("HUD Principal")]
        public Text scoreText;
        public Text livesText;
        public Text ammoText;
        public Text weaponText;
        public Text levelText;
        public Text feedbackText;

        [Header("Barra de Boss")]
        public GameObject bossPanel;
        public Text bossNameText;
        public Slider bossHealthSlider;

        [Header("Panel Final (Victoria / Derrota)")]
        public GameObject endScreenPanel;
        public Text endTitleText;
        public Text endDetailsText;
        public Button restartButton;
        public Button menuButton;

        private float _feedbackTimer;

        private void Awake()
        {
            Instance = this;

            if (bossPanel != null) bossPanel.SetActive(false);
            if (endScreenPanel != null) endScreenPanel.SetActive(false);
            if (feedbackText != null) feedbackText.text = string.Empty;

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.RestartGame();
                });
            }

            if (menuButton != null)
            {
                menuButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
                });
            }
        }

        private void Update()
        {
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f && feedbackText != null)
                {
                    feedbackText.text = string.Empty;
                }
            }
        }

        public void UpdateHUD(int score, int lives, int ammo, string levelName, string objLabel, int objectivesLeft)
        {
            if (scoreText != null) scoreText.text = $"PUNTUACIÓN: {score}";
            if (livesText != null) livesText.text = $"VIDAS: {lives}";
            if (ammoText != null) ammoText.text = $"MUNICIÓN: {ammo}";
            if (levelText != null) levelText.text = $"{levelName}  |  {objLabel} restantes: {objectivesLeft}";
        }

        public void UpdateWeaponName(string weaponName)
        {
            if (weaponText != null) weaponText.text = $"ARMA: {weaponName} [1 / 2 / 3]";
        }

        public void ShowHitFeedback(string message)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            _feedbackTimer = 2.0f;
        }

        public void ShowBossHealth(string bossName, int current, int max)
        {
            if (bossPanel == null) return;
            bossPanel.SetActive(true);
            if (bossNameText != null) bossNameText.text = bossName;
            if (bossHealthSlider != null)
            {
                bossHealthSlider.maxValue = max;
                bossHealthSlider.value = current;
            }
        }

        public void UpdateBossHealth(int current, int max)
        {
            if (bossHealthSlider == null) return;
            bossHealthSlider.maxValue = max;
            bossHealthSlider.value = Mathf.Max(0, current);
        }

        public void HideBossHealth()
        {
            if (bossPanel != null) bossPanel.SetActive(false);
        }

        public void ShowEndScreen(bool victory, string reason, int finalScore, int livesRemaining)
        {
            if (endScreenPanel == null) return;
            endScreenPanel.SetActive(true);

            if (endTitleText != null)
            {
                endTitleText.text = victory ? "¡HAS GANADO!" : "FIN DEL JUEGO";
                endTitleText.color = victory ? new Color(0.3f, 1f, 0.45f) : new Color(1f, 0.35f, 0.35f);
            }

            if (endDetailsText != null)
            {
                endDetailsText.text = $"{reason}\n\nPuntuación Final: {finalScore}\nVidas Restantes: {livesRemaining}";
            }
        }
    }
}
