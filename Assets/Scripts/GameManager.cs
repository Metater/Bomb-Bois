using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
    Items will be deleted when client that last touched them disconnects, not good
*/

public class GameManager : NetworkBehaviour
{
    // Public Set Unity References
    public Image crosshairImage;
    public PhoneManager phoneManager;
    public GameObject draggableIndicator;
    public GameObject indicator;

    // Lookup
    public NetRefLookup<Player> playerLookup;
    public NetRefLookup<Draggable> draggableLookup;
    public NetRefLookup<Item> itemLookup;

    // Private Set Unity References
    [SerializeField] private GameObject startButtonGO;

    // Public Properties
    public bool HasStarted { get; private set; } = false;
    public double StartTime { get; private set; }
    public double StartNetworkTime { get; private set; }
    public Player LocalPlayer { get; private set; }

    // Public Method Properties
    public double TimeSinceStart => Time.timeAsDouble - StartTime;

    // Public Events
    public event Action OnButtonStartPressed;

    #region Unity
    private void Awake()
    {
        playerLookup = new();
        draggableLookup = new();
        itemLookup = new();

        draggableLookup.Refs.AddRange(FindObjectsOfType<Draggable>(true));
        itemLookup.Refs.AddRange(FindObjectsOfType<Item>(true));
    }

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
        playerLookup.Refs.AddRange(FindObjectsOfType<Player>());

        OnButtonStartPressed?.Invoke();
    }
    #endregion Buttons

    public void InitLocalPlayer(Player localPlayer)
    {
        LocalPlayer = localPlayer;
    }
}
