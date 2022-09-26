using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    // Public Set Unity References
    public Image crosshairImage;
    public PhoneManager phoneManager;
    public GameObject draggableIndicator;
    public GameObject indicator;

    // Private Set Unity References
    [SerializeField] private GameObject startButtonGO;

    // Public Properties
    public bool HasStarted { get; private set; } = false;
    public double StartTime { get; private set; }
    public double StartNetworkTime { get; private set; }
    public PlayerOld LocalPlayer { get; private set; }

    // Public Method Properties
    public double TimeSinceStart => Time.timeAsDouble - StartTime;

    // Public Events
    public event Action OnButtonStartPressed;

    #region Unity
    private void Start()
    {

    }

    private void Update()
    {
        double timeSinceStart = Time.timeAsDouble - StartTime;
    }
    #endregion Unity

    #region NetworkBehaviour Callbacks
    #endregion NetworkBehaviour Callbacks

    #region Button Start
    public void ButtonStart()
    {
        startButtonGO.SetActive(false);

        if (isServerOnly)
        {
            SharedButtonStart();
        }

        RpcButtonStart();
    }

    [ClientRpc]
    public void RpcButtonStart()
    {
        SharedButtonStart();
    }

    private void SharedButtonStart()
    {
        HasStarted = true;
        StartTime = Time.timeAsDouble;
        StartNetworkTime = NetworkTime.time;

        OnButtonStartPressed?.Invoke();
    }
    #endregion Buttons

    public void InitLocalPlayer(PlayerOld localPlayer)
    {
        LocalPlayer = localPlayer;
    }

    public bool TryGetPlayerWithNetId(uint netId, out PlayerOld player)
    {
        // TODO IMPROVE EFF????
        PlayerOld[] players = FindObjectsOfType<PlayerOld>();
        foreach (var p in players)
        {
            if (p.netId == netId)
            {
                player = p;
                return true;
            }
        }
        player = null;
        return false;
    }
}
