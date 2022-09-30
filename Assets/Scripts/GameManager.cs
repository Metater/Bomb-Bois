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
    public GameObject playerDragIndicator;

    [Header("Prefabs")]

    public GameObject otherPlayerDragIndicatorPrefab;
    public GameObject indicator;

    public RectTransform test;


    // Lookup
    public NetRefLookup<Player> PlayerLookup { get; private set; }
    public NetRefLookup<Draggable> DraggableLookup { get; private set; }
    public NetRefLookup<Item> ItemLookup { get; private set; }

    // Private Set Unity References
    [SerializeField] private GameObject startButtonGO;

    // Public Properties
    public bool HasStarted { get; private set; } = false;
    public double StartTime { get; private set; }
    public double StartNetworkTime { get; private set; }
    public Player LocalPlayer { get; private set; }

    // Public Method Properties
    // TODO Is this a bad thing???
    public double TimeSinceStart => Time.timeAsDouble - StartTime;

    // Public Events
    public event Action OnButtonStartPressed;

    #region Unity
    private void Awake()
    {
        PlayerLookup = new();
        DraggableLookup = new();
        ItemLookup = new();

        DraggableLookup.Refs.AddRange(FindObjectsOfType<Draggable>(true));
        ItemLookup.Refs.AddRange(FindObjectsOfType<Item>(true));
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
        PlayerLookup.Refs.AddRange(FindObjectsOfType<Player>());

        OnButtonStartPressed?.Invoke();
    }
    #endregion Buttons

    public void InitLocalPlayer(Player localPlayer)
    {
        LocalPlayer = localPlayer;
    }
}
