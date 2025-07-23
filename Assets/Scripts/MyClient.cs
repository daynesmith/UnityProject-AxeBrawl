using Mirror;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct PlayerInfoData 
{
    public string username;
    public ulong steamId;

    public PlayerInfoData(string username, ulong steamId)
    {
        this.username = username;
        this.steamId = steamId;
    }
}
public class MyClient : NetworkBehaviour
{
    [SyncVar(hook = nameof(PlayerInfoUpdate))]
    public PlayerInfoData playerInfo;

    [SyncVar(hook = nameof(IsReadyUpdate))]
    public bool IsReady;

    [Header("Controller")]
    [SerializeField] private GameObject controllerObj;
    [SerializeField] private GameObject meshObj;
    [SerializeField] private GameObject camHolder;
    [SerializeField] private Behaviour[] controllerComponents;

    public Sprite icon { get; private set; }
    public CharacterSkinElement characterInstance { get; set; }

    #region Steam PFP
    protected Callback<AvatarImageLoaded_t> avatarImageLoaded;
    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        Debug.Log("Avatar loaded " + callback.m_steamID);
        if (callback.m_steamID.m_SteamID != playerInfo.steamId) return;
        SetIcon(callback.m_steamID);
    }

    void SetIcon(CSteamID steamId)
    {
        Texture2D tex = SteamHelper.GetAvatar(steamId);
        if (tex)
            icon = SteamHelper.ConvertTextureToSprite(tex);
    }
    #endregion

    private void Start()
    {
        ((MyNetworkManager)NetworkManager.singleton).allClients.Add(this);

        if(CharacterSkinHandler.instance) CharacterSkinHandler.instance.SpawnCharacterMesh(this);
        avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            ToggleController(true);
        }
        else
        { 
            ToggleController(false);
        }

        if (isLocalPlayer)
        {
            // Set this player and children to LocalPlayer layer
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("LocalPlayerHidden"));

            // Then override just the arms to LocalArms so they render
            Transform armLeft = transform.Find("Controller/Barbarian/Barbarian_ArmLeft");
            Transform armRight = transform.Find("Controller/Barbarian/Barbarian_ArmRight");
            Transform axe_1H = transform.Find("Controller/Barbarian/Rig/root/hips/spine/chest/upperarm.r/lowerarm.r/wrist.r/hand.r/handslot.r/1H_Axe");

            if (armLeft != null)
                SetLayerRecursively(armLeft.gameObject, LayerMask.NameToLayer("LocalPlayerShow"));

            if (armRight != null)
                SetLayerRecursively(armRight.gameObject, LayerMask.NameToLayer("LocalPlayerShow"));

            if (axe_1H != null)
                SetLayerRecursively(axe_1H.gameObject, LayerMask.NameToLayer("LocalPlayerShow"));

            // Find your camera (adjust if camera is elsewhere)
            Camera localCam = Camera.main;

            if (localCam != null)
            {
                // Remove LocalPlayer layer from camera's culling mask
                localCam.cullingMask &= ~(1 << LayerMask.NameToLayer("LocalPlayerHidden"));
                localCam.cullingMask |= (1 << LayerMask.NameToLayer("LocalPlayerShow"));
            }
            else
            {
                Debug.LogWarning("No Main Camera found for local player");
            }
        }

    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    void ToggleController(bool value) 
    {
        controllerObj.SetActive(value);
        meshObj.SetActive(value ? !isLocalPlayer : false);
        camHolder.SetActive(value ? isLocalPlayer : false);

        if (!isLocalPlayer) 
            value = false;

        
        foreach (var component in controllerComponents)
        {
            component.enabled = value;
        }

        GetComponent<CharacterController>().enabled = value;
    }

    #region Ready Up
    public void ToggleReady() => Cmd_ToggleReady();

    [Command]
    private void Cmd_ToggleReady() 
    {
        IsReady = !IsReady;
    }
    #endregion

    #region SyncVar Hooks
    private void PlayerInfoUpdate(PlayerInfoData _, PlayerInfoData data)
    {
        if (characterInstance)
            characterInstance.Initialize(this, IsReady);

            SetIcon(new CSteamID(data.steamId));
    }

    public void IsReadyUpdate(bool _, bool value) 
    {
        if (characterInstance)
            characterInstance.Initialize(this, value);
        if (isLocalPlayer) 
        {
            MainMenu.instance.UpdateReadyButton(value);
        }
    }

    #endregion

    

    private void OnDestroy()
    {
        if (this && ((MyNetworkManager)NetworkManager.singleton))
            ((MyNetworkManager)NetworkManager.singleton).allClients.Remove(this);

        if (characterInstance && !isLocalPlayer)
        {
            CharacterSkinHandler.instance.DestroyCharacterMesh(this);
        }
    }
}
