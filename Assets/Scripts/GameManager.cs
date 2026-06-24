using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public static GameManager Instance { get; private set; }
    public static GameState bootState = GameState.MainMenu;

    [Header("Current State")]
    public GameState currentState;
    private GameState previousState;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;

    [Header("Buttons")]
    public Button playButton;
    public Button quitButton;
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public Button settingsButtonMainMenu;
    public Button settingsButtonPauseMenu;
    public Button settingsBackButton;
    public Button gameOverRestartButton;
    public Button gameOverMainMenuButton;

    [Header("HUD Elements")]
    public TextMeshProUGUI scoreText;

    [Header("Game Over Elements")]
    public TextMeshProUGUI gameOverScoreText;

    [Header("Settings Elements")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("Gameplay References")]
    public PlayerMovement player;
    public Spawner spawner;

    private int score = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Setup button listeners
        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (settingsButtonMainMenu != null) settingsButtonMainMenu.onClick.AddListener(OpenSettings);
        if (settingsButtonPauseMenu != null) settingsButtonPauseMenu.onClick.AddListener(OpenSettings);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);
        if (gameOverRestartButton != null) gameOverRestartButton.onClick.AddListener(RestartGame);
        if (gameOverMainMenuButton != null) gameOverMainMenuButton.onClick.AddListener(GoToMainMenu);

        // Setup settings listeners
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        SetState(bootState);
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                PauseGame();
            }

            if (player != null && player.currentHealth <= 0)
            {
                SetState(GameState.GameOver);
            }
        }
        else if (currentState == GameState.Paused)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                ResumeGame();
            }
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;

        // Hide all panels initially
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        switch (newState)
        {
            case GameState.MainMenu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                Time.timeScale = 0f;
                break;

            case GameState.Playing:
                if (hudPanel != null) hudPanel.SetActive(true);
                Time.timeScale = 1f;
                UpdateScoreUI();
                break;

            case GameState.Paused:
                if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
                Time.timeScale = 0f;
                break;

            case GameState.GameOver:
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                if (gameOverScoreText != null)
                {
                    gameOverScoreText.text = "Final Score: " + score;
                }
                Time.timeScale = 0f;
                break;
        }
    }

    public void PlayGame()
    {
        bootState = GameState.Playing;
        SetState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            SetState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            SetState(GameState.Playing);
        }
    }

    public void OpenSettings()
    {
        previousState = currentState;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        SetState(previousState);
    }

    public void RestartGame()
    {
        bootState = GameState.Playing;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        bootState = GameState.MainMenu;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}