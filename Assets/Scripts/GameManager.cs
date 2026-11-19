using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JuegoFPS
{
    /// <summary>
    /// Gestor central del juego: puntuación, vidas, munición, progresión de niveles
    /// y menú de victoria / derrota.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const string SceneMenu = "00 Menu Principal";
        public const string SceneLevel1 = "01 Galeria de disparos";
        public const string SceneLevel2 = "02 Nivel Enemigos";
        public const string SceneLevel3 = "03 Nivel Boss";

        [Header("Configuración Inicial")]
        public int startingLives = 3;
        public int level1Ammo = 18;
        public int level2Ammo = 35;
        public int level3Ammo = 55;

        public int Score { get; private set; }
        public int Lives { get; private set; }
        public int Ammo { get; private set; }
        public bool IsGameOver { get; private set; }

        private int _targetsRemaining;
        private int _enemiesRemaining;
        private int _activeProjectiles;
        private bool _isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (ProceduralAudio.Instance == null)
            {
                gameObject.AddComponent<ProceduralAudio>();
            }

            ResetRun();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void ResetRun()
        {
            Score = 0;
            Lives = startingLives;
            IsGameOver = false;
            _isTransitioning = false;
            SetupAmmoForScene(SceneManager.GetActiveScene().name);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;
            _targetsRemaining = 0;
            _enemiesRemaining = 0;
            _activeProjectiles = 0;
            _isTransitioning = false;

            if (scene.name == SceneMenu)
            {
                PlayerControllerFPS.LockCursor(false);
                return;
            }

            if (IsGameOver)
            {
                ResetRun();
            }
            else
            {
                SetupAmmoForScene(scene.name);
            }

            StartCoroutine(DelayedHudRefresh(scene.name));
        }

        private IEnumerator DelayedHudRefresh(string sceneName)
        {
            yield return null;
            RefreshHud(sceneName);
        }

        private void SetupAmmoForScene(string sceneName)
        {
            if (sceneName == SceneLevel1) Ammo = level1Ammo;
            else if (sceneName == SceneLevel2) Ammo = level2Ammo;
            else if (sceneName == SceneLevel3) Ammo = level3Ammo;
            else Ammo = level1Ammo;
        }

        public void RegisterTarget()
        {
            _targetsRemaining++;
            RefreshHud(SceneManager.GetActiveScene().name);
        }

        public void RegisterEnemy()
        {
            _enemiesRemaining++;
            RefreshHud(SceneManager.GetActiveScene().name);
        }

        public void RegisterActiveProjectile()
        {
            _activeProjectiles++;
        }

        public void UnregisterActiveProjectile()
        {
            _activeProjectiles = Mathf.Max(0, _activeProjectiles - 1);
            CheckOutOfAmmoCondition();
        }

        public bool TryConsumeAmmo()
        {
            if (IsGameOver || _isTransitioning) return false;

            if (Ammo <= 0)
            {
                CheckOutOfAmmoCondition();
                return false;
            }

            Ammo--;
            RefreshHud(SceneManager.GetActiveScene().name);
            return true;
        }

        public void OnTargetDestroyed(string targetName, int points, bool countsForLevel)
        {
            if (IsGameOver) return;

            Score += points;
            if (countsForLevel)
            {
                _targetsRemaining = Mathf.Max(0, _targetsRemaining - 1);
            }

            Debug.Log($"[Impacto] {targetName} destruida | +{points} pts | Puntuación total: {Score} | Dianas restantes: {_targetsRemaining}");

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowHitFeedback($"+{points} pts ({targetName})");
            }

            RefreshHud(SceneManager.GetActiveScene().name);
            CheckLevelCompletion();
        }

        public void OnEnemyDestroyed(string enemyName, int points, bool countsForLevel, bool isBoss)
        {
            if (IsGameOver) return;

            Score += points;
            if (countsForLevel)
            {
                _enemiesRemaining = Mathf.Max(0, _enemiesRemaining - 1);
            }

            Debug.Log($"[Combate] {enemyName} eliminado | +{points} pts | Puntuación total: {Score}");

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowHitFeedback($"+{points} pts ({enemyName} eliminado)");
            }

            RefreshHud(SceneManager.GetActiveScene().name);

            if (isBoss)
            {
                EndGame(true, "¡Has derrotado al Boss Final!");
            }
            else
            {
                CheckLevelCompletion();
            }
        }

        public void DamagePlayer(int amount, string sourceName)
        {
            if (IsGameOver || _isTransitioning) return;

            Lives = Mathf.Max(0, Lives - amount);
            Debug.Log($"[Daño] Choque con {sourceName}. Vidas restantes: {Lives}");

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowHitFeedback($"¡Golpe de {sourceName}! (-1 Vida)");
            }

            RefreshHud(SceneManager.GetActiveScene().name);

            if (Lives <= 0)
            {
                EndGame(false, $"Has perdido todas tus vidas al chocar con {sourceName}.");
            }
        }

        private void CheckOutOfAmmoCondition()
        {
            if (IsGameOver || _isTransitioning) return;
            if (SceneManager.GetActiveScene().name == SceneMenu) return;

            if (Ammo <= 0 && _activeProjectiles <= 0 && !AreLevelObjectivesMet())
            {
                EndGame(false, "Te has quedado sin munición.");
            }
        }

        private bool AreLevelObjectivesMet()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == SceneLevel1) return _targetsRemaining <= 0;
            if (sceneName == SceneLevel2) return _enemiesRemaining <= 0;
            if (sceneName == SceneLevel3) return _enemiesRemaining <= 0;
            return false;
        }

        private void CheckLevelCompletion()
        {
            if (IsGameOver || _isTransitioning) return;

            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == SceneLevel1 && _targetsRemaining <= 0)
            {
                StartCoroutine(TransitionToNextLevel(SceneLevel2, "¡Nivel 1 Completado! Pasando a Nivel 2: Enemigos..."));
            }
            else if (sceneName == SceneLevel2 && _enemiesRemaining <= 0)
            {
                StartCoroutine(TransitionToNextLevel(SceneLevel3, "¡Nivel 2 Completado! Prepárate para el Boss Final..."));
            }
            else if (sceneName == SceneLevel3 && _enemiesRemaining <= 0)
            {
                EndGame(true, "¡Has completado todos los niveles y derrotado al Boss!");
            }
        }

        private IEnumerator TransitionToNextLevel(string nextScene, string message)
        {
            _isTransitioning = true;
            Debug.Log($"[Nivel] {message} | Puntuación actual: {Score} | Vidas restantes: {Lives}");

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowHitFeedback(message);
            }

            yield return new WaitForSeconds(1.8f);
            SceneManager.LoadScene(nextScene);
        }

        private void EndGame(bool victory, string reason)
        {
            if (IsGameOver) return;
            IsGameOver = true;
            PlayerControllerFPS.LockCursor(false);

            string resultTitle = victory ? "¡VICTORIA!" : "FIN DEL JUEGO";
            Debug.Log($"=== {resultTitle} === {reason} | Puntuación Final: {Score} | Vidas Restantes: {Lives}");

            if (ProceduralAudio.Instance != null)
            {
                if (victory) ProceduralAudio.Instance.PlayVictory();
                else ProceduralAudio.Instance.PlayGameOver();
            }

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowEndScreen(victory, reason, Score, Lives);
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            ResetRun();
            SceneManager.LoadScene(SceneLevel1);
        }

        public void RestartCurrentLevel()
        {
            Time.timeScale = 1f;
            IsGameOver = false;
            _isTransitioning = false;
            string curScene = SceneManager.GetActiveScene().name;
            SetupAmmoForScene(curScene);
            SceneManager.LoadScene(curScene);
        }

        public void LoadSpecificLevel(string sceneName)
        {
            Time.timeScale = 1f;
            IsGameOver = false;
            _isTransitioning = false;
            SetupAmmoForScene(sceneName);
            SceneManager.LoadScene(sceneName);
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            ResetRun();
            SceneManager.LoadScene(SceneMenu);
        }

        private void RefreshHud(string sceneName)
        {
            if (HUDManager.Instance == null) return;

            int objectivesLeft = sceneName == SceneLevel1 ? _targetsRemaining : _enemiesRemaining;
            string objLabel = sceneName == SceneLevel1 ? "Dianas" : "Enemigos";
            HUDManager.Instance.UpdateHUD(Score, Lives, Ammo, sceneName, objLabel, objectivesLeft);
        }
    }
}
