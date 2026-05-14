using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TowerDefense.Manager
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private string menuSceneName = "Menu_Inicial";

        private bool isPaused;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            Time.timeScale = 1f;
            isPaused = false;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (isPaused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            if (panel != null) panel.SetActive(true);
        }

        public void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
        }

        public void BackToMenu()
        {
            Time.timeScale = 1f;
            isPaused = false;
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
