using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MenuState { Home, InParty }
public class MainMenu : MonoBehaviour
{
    public static MainMenu instance;
    public Button startGameButton;
    public MenuState state = MenuState.Home;
    [SerializeField] private GameObject homeUI, partyUI;

    [Header("Ready Button")]
    [SerializeField] private Image readyButton_Image;
    [SerializeField] private TMP_Text readyButton_Text;
    public Color readyColor, notReadyColor;

    public Button quitButton;

    [Header("Stage Selection")]
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button prevStageButton;

    private string[] stageSceneNames = { "PunchGame", "TagGame"};
    private int selectedStageIndex = 0;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        quitButton.onClick.AddListener(QuitGame);
        UpdateStageText();
        if (!NetworkServer.active || !NetworkClient.ready || !NetworkClient.localPlayer.isServer)
        {
            startGameButton.gameObject.SetActive(false);
        }
        else
        {
            startGameButton.gameObject.SetActive(true);
        }

    }
    public void Update()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
    }
    public void QuitGame() 
    {
        Application.Quit();
    }
    public void SetMenuState(MenuState state)
    {
        this.state = state;

        homeUI.SetActive(state == MenuState.Home);
        partyUI.SetActive(state == MenuState.InParty);
    }

    public void CreateParty()
    {
        PopupManager.instance.Popup_Show("Creating Party");

        ((MyNetworkManager)NetworkManager.singleton).SetMultiplayer(true);
        SteamLobby.instance.CreateLobby();
    }

    public void StartSinglePlayer()
    {
        LobbyController.instance.StartGameSolo();
    }

    public void LeaveParty()
    {
        if (!NetworkClient.active) return;

        if (NetworkClient.localPlayer.isServer)
            NetworkManager.singleton.StopHost();
        else
            NetworkManager.singleton.StopClient();

        SteamLobby.instance.Leave();
    }

    public void FindMatch()
    {
        SteamLobby.instance.FindMatch();
    }

    public void StartGame()
    {
        LobbyController.instance.StartGameWithParty();
        RefreshStartButton();  
    }

    public void StartLocalClient()
    {
        ((MyNetworkManager)NetworkManager.singleton).SetMultiplayer(true);
        NetworkManager.singleton.StartClient();
        RefreshStartButton();
    }

    public void StartLocalHost()
    {
        ((MyNetworkManager)NetworkManager.singleton).SetMultiplayer(true);
        NetworkManager.singleton.StartHost();
        RefreshStartButton();
    }

    public void ToggleReady()
    {
        if (!NetworkClient.active) return;

        NetworkClient.localPlayer.GetComponent<MyClient>().ToggleReady();
    }

    public void UpdateReadyButton(bool value)
    {
        readyButton_Text.text = value ? "Ready" : "Not Ready";
        readyButton_Image.color = value ? readyColor : notReadyColor;
        RefreshStartButton();
    }

    public void NextStage()
    {
        if (!NetworkServer.active) return;

        selectedStageIndex = (selectedStageIndex + 1) % stageSceneNames.Length;
        UpdateStageText();
    }

    public void PreviousStage()
    {
        if (!NetworkServer.active) return;

        selectedStageIndex = (selectedStageIndex - 1 + stageSceneNames.Length) % stageSceneNames.Length;
        UpdateStageText();
    }

    private void UpdateStageText()
    {
        if (stageNameText != null)
            stageNameText.text = $"Selected Stage: {stageSceneNames[selectedStageIndex]}";
    }

    // This method lets other classes ask the currently selected stage
    public string GetSelectedStage()
    {
        return stageSceneNames[selectedStageIndex];
    }

    public void RefreshStartButton() {
        if (!NetworkServer.active || !NetworkClient.ready || !NetworkClient.localPlayer.isServer)
        {
            startGameButton.gameObject.SetActive(false);
        }
        else
        {
            startGameButton.gameObject.SetActive(true);
        }
    }
}
