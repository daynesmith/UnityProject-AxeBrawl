using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror;
using System.Collections;  
public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuUI;
    public Button resumeButton;
    public Button backToMenuButton;
    public Button exitGameButton;

    private bool isPaused = false;

    void Start()
    {
        pauseMenuUI.SetActive(false);

        resumeButton.onClick.AddListener(ResumeGame);
        backToMenuButton.onClick.AddListener(ReturnToMainMenu);
        exitGameButton.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    void PauseGame()
    {
        isPaused = true;
        pauseMenuUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToMainMenu()
    {
        backToMenuButton.interactable = false;
        Debug.Log("Leaving lobby and disconnecting network...");

        Debug.Log("Leaving Steam lobby...");
        SteamLobby.instance?.Leave();
        Destroy(SteamLobby.instance);

        if (NetworkServer.active)
        {
            NetworkClient.Shutdown();
            NetworkServer.Shutdown();
        }
        else 
        {
            NetworkClient.Shutdown();
        }
        
        Destroy(NetworkManager.singleton);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Leaving Steam lobby...");
        SteamLobby.instance?.Leave();
        Destroy(SteamLobby.instance);

        if (NetworkManager.singleton == null)
        {
            Debug.LogWarning("No NetworkManager found!");
            return;
        }

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            // Host (server + client)
            NetworkManager.singleton.StopHost(); // Cleans up both client and server
        }
        else if (NetworkClient.isConnected)
        {
            // Client only
            NetworkManager.singleton.StopClient(); // Disconnects client
        }
        else if (NetworkServer.active)
        {
            // Dedicated server
            NetworkManager.singleton.StopServer();
        }

        // Cleanup
        Destroy(NetworkManager.singleton.gameObject); // Remove manager if needed
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Quitting game");
        Application.Quit();
    }

    public bool IsPaused() => isPaused;
}

