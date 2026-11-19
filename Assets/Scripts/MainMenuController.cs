using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoFPS
{
    /// <summary>
    /// Controlador del Menú Principal: botones Jugar y Salir,
    /// y panel selector de Nivel 1, Nivel 2 y Nivel 3.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panel Principal")]
        public GameObject mainPanel;
        public Button playButton;
        public Button quitButton;

        [Header("Panel Selección de Nivel")]
        public GameObject levelSelectPanel;
        public Button level1Button;
        public Button level2Button;
        public Button level3Button;
        public Button backButton;

        private void Start()
        {
            PlayerControllerFPS.LockCursor(false);

            ShowMainPanel();

            if (playButton != null)
            {
                playButton.onClick.AddListener(ShowLevelSelect);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }

            if (level1Button != null)
            {
                level1Button.onClick.AddListener(() => LoadLevel(GameManager.SceneLevel1));
            }

            if (level2Button != null)
            {
                level2Button.onClick.AddListener(() => LoadLevel(GameManager.SceneLevel2));
            }

            if (level3Button != null)
            {
                level3Button.onClick.AddListener(() => LoadLevel(GameManager.SceneLevel3));
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(ShowMainPanel);
            }
        }

        public void ShowMainPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        }

        public void ShowLevelSelect()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        }

        public void LoadLevel(string sceneName)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetRun();
            }
            SceneManager.LoadScene(sceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
