using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Keys keys;

    // Possible States for the Player //
    public enum PlayerState
    {
        player,
        station,
        ui,
        build,
    }
    public PlayerState playerState;

    public PlayerController playerController;
    public RaycastController raycastController;
    public BaseController baseController;

    public OptionManager optionManager;
    public UIManager uiManager;
    public InventoryManager inventoryManager;
    public BuildManager buildManager;
    public SaveSystem dataManager;

    public GameObject playerCamera;

    public Vector3 oldPlayerPos;
    public Vector3 oldPlayerRot;
    public GameObject baseBuildingBlueprint;

    public List<QuestMarker> quests = new List<QuestMarker>();

    public GameObject compass;
    float mouseX;
    float yRotation;

    //small save system part
    public void DoSave()
    {
        SaveSystem.instance.Save(SaveSystem.instance.slotToLoad);
    }

    void Start()
    {
        if(SaveSystem.instance != null)
        {
            SaveSystem.instance.gameManager = this;
            dataManager = SaveSystem.instance;

            if(SaveSystem.instance.Datastate == SaveSystem.SystemState.Loading)
            {
                Debug.Log(SaveSystem.instance.Datastate + " in Scene: " + SceneManager.GetActiveScene().name);
                StartCoroutine(SaveSystem.instance.LoadData());
            }
        }

        SwitchStatePlayer(PlayerState.player, UIManager.ExternalUIState.none);

        yRotation = compass.transform.eulerAngles.y;
    }

    void Update()
    {
        StatePlayer();
    }
    
    void CompassRot()
    {
        mouseX = Input.GetAxis("Mouse X") * OptionManager.playerMouseSens;
        yRotation += mouseX;
        compass.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
    }

    // State of the Player //
    void StatePlayer()
    {
        switch(playerState)
        {
            case PlayerState.player:
            {
                InputForSwitchStatePlayer();
                playerController.GetPlayerKeyInput(keys.playerForwardKey, keys.playerBackwardsKey, keys.playerLeftKey, keys.playerRightKey, keys.playerRunKey, keys.playerJumpKey);
                raycastController.GetInteractionKeyInput(keys.interactionKey);
                inventoryManager.PlayerUpdate();
                CompassRot();

                break;
            }
            case PlayerState.station:
            {
                baseController.GetBaseKeyInput(keys.baseForwardKey, keys.baseBackwardsKey, keys.baseLeftKey, keys.baseRightKey, keys.baseHandbrake, keys.baseSwitchCamKey, keys.interactionKey);
                inventoryManager.PlayerUpdate();
                CompassRot();

                break;
            }
            case PlayerState.ui:
            {
                if(uiManager.internalUIState != UIManager.InternalUIState.none)
                {
                    uiManager.InternalUIUpdate(keys.journalKey, keys.inventoryKey, keys.mapKey, keys.optionKey);
                    break;
                }
                else if(uiManager.externalUIState != UIManager.ExternalUIState.none)
                {
                    uiManager.ExternalUIUpdate(keys.interactionKey);
                    break;
                }

                break;
            }
            case PlayerState.build:
            {
                InputForSwitchStatePlayer();
                playerController.GetPlayerKeyInput(keys.playerForwardKey, keys.playerBackwardsKey, keys.playerLeftKey, keys.playerRightKey, keys.playerRunKey, keys.playerJumpKey);
                inventoryManager.PlayerUpdate();
                CompassRot();

                break;
            }
        }
    }

    // Make from this a cleaner Version //
    public void SwitchStatePlayer(PlayerState pPlayerState, UIManager.ExternalUIState eexternalUI)
    {
        PlayerState ppPlayerState = PlayerState.player;

        switch(playerState)
        {
            case PlayerState.player:
            {
                ppPlayerState = playerState;
                break;
            }
            case PlayerState.build:
            {
                ppPlayerState = playerState;
                break;
            }
        }

        playerState = pPlayerState;

        switch(playerState)
        {
            case PlayerState.player:
            {
                CursorModeLocked();
                if(oldPlayerPos != null)
                {
                    oldPlayerPos = playerController.gameObject.transform.localPosition;
                    playerController.transform.parent = baseController.transform;
                    playerController.gameObject.transform.localPosition = oldPlayerPos;
                    playerController.transform.eulerAngles = oldPlayerRot;

                    buildManager.TransferBuildings();
                }
                uiManager.SwitchStateUI(UIManager.InternalUIState.none, UIManager.ExternalUIState.none, ppPlayerState);
                uiManager.Player(true);

                playerController.UnfreezePlayer();

                break;
            }
            case PlayerState.station:
            {
                CursorModeLocked();
                playerController.StopMovement();

                uiManager.Player(false);

                playerController.FreezePlayer();

                break;
            }
            case PlayerState.ui:
            {
                CursorModeConfined();
                playerController.StopMovement();

                // Check which UI Button is pressed to Change to the correct UI State //
                if(Input.GetKeyDown(keys.journalKey))
                    uiManager.SwitchStateUI(UIManager.InternalUIState.journal, UIManager.ExternalUIState.none, ppPlayerState);
                else if(Input.GetKeyDown(keys.inventoryKey))
                    uiManager.SwitchStateUI(UIManager.InternalUIState.inventory, UIManager.ExternalUIState.none, ppPlayerState);
                else if(Input.GetKeyDown(keys.mapKey))
                    uiManager.SwitchStateUI(UIManager.InternalUIState.map, UIManager.ExternalUIState.none, ppPlayerState);
                else if(Input.GetKeyDown(keys.optionKey))
                    uiManager.SwitchStateUI(UIManager.InternalUIState.option, UIManager.ExternalUIState.none, ppPlayerState);
                else if(Input.GetKeyDown(keys.interactionKey))
                    uiManager.SwitchStateUI(UIManager.InternalUIState.none, eexternalUI, ppPlayerState);

                playerController.FreezePlayer();

                break;
            }
            case PlayerState.build:
            {
                CursorModeLocked();

                oldPlayerPos = playerController.gameObject.transform.localPosition;
                oldPlayerRot = playerController.gameObject.transform.eulerAngles;
                playerController.transform.eulerAngles = Vector3.zero;
                playerController.transform.parent = baseBuildingBlueprint.transform;
                playerController.gameObject.transform.localPosition = oldPlayerPos;
                break;
            }
        }
    }

    void InputForSwitchStatePlayer()
    {
        // Check if an UI is pressed and change to UI State //
        if(Input.GetKeyDown(keys.journalKey) || Input.GetKeyDown(keys.inventoryKey) || Input.GetKeyDown(keys.mapKey) || Input.GetKeyDown(keys.optionKey))
        {
            // Change only if PLayer State is Player //
            if(playerState == PlayerState.player || playerState == PlayerState.build)
                SwitchStatePlayer(PlayerState.ui, UIManager.ExternalUIState.none);
        }

        // Check if Build Button is pressed //
        if(Input.GetKeyDown(keys.buildKey))
        {
            if(playerState == PlayerState.player)
                SwitchStatePlayer(PlayerState.build, UIManager.ExternalUIState.none);
            else if(playerState == PlayerState.build)
                SwitchStatePlayer(PlayerState.player, UIManager.ExternalUIState.none);
        }
    }

    void CursorModeLocked()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        OptionManager.playerMouseSens = optionManager.mouseSens;
    }

    void CursorModeConfined()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;

        OptionManager.playerMouseSens = 0;
    }

    public bool CheckIfKeyCodeIsTrue(KeyCode key)
    {
        foreach(KeyCode kCode in Enum.GetValues(typeof(KeyCode)))
        {
            if(kCode == key)
            {
                Debug.Log($"{kCode}");
                return true;
            }
        }
        return false;
    }
}

//[System.Serializable]
//public class Keys
//{
//    [Header("Player Keys")]
//    public KeyCode playerForwardKey;
//    public KeyCode playerBackwardsKey;
//    public KeyCode playerLeftKey;
//    public KeyCode playerRightKey;
//    public KeyCode playerRunKey;
//    public KeyCode playerJumpKey;

//    [Header("Base Keys")]
//    public KeyCode baseForwardKey;
//    public KeyCode baseBackwardsKey;
//    public KeyCode baseLeftKey;
//    public KeyCode baseRightKey;
//    public KeyCode baseSwitchCamKey;

//    [Header("UI Keys")]
//    public KeyCode journalKey;
//    public KeyCode inventoryKey;
//    public KeyCode mapKey;
//    public KeyCode optionKey;

//    [Header("Interaction Key")]
//    public KeyCode interactionKey;

//    [Header("Build Mode Key")]
//    public KeyCode buildKey;
//}
