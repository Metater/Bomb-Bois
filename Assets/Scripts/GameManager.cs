using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    // Private Set Unity References
    [SerializeField] private GameObject startButtonGO;

    // Public Properties
    public bool HasStarted { get; private set; } = false;
    public double StartTime { get; private set; }
    public double StartNetworkTime { get; private set; }

    // Public Method Properties
    public double TimeSinceStart => Time.timeAsDouble - StartTime;

    // Public Events
    public event Action OnButtonStartPressed;

    #region Unity
    private void Update()
    {
        double timeSinceStart = Time.timeAsDouble - StartTime;
    }
    #endregion Unity

    #region NetworkBehaviour Callbacks
    #endregion NetworkBehaviour Callbacks

    #region Buttons
    public void ButtonStart()
    {
        startButtonGO.SetActive(false);

        if (isServerOnly)
        {
            StartGame();
        }
        else
        {
            // TODO Not server only safe, client rpcs cant be called on only servers, i think...
            RpcButtonStart();
        }
    }
    #endregion Buttons

    #region Client RPCs
    [ClientRpc]
    public void RpcButtonStart()
    {
        StartGame();
    }
    #endregion Client RPCs

    #region Private
    private void StartGame()
    {
        HasStarted = true;
        StartTime = Time.timeAsDouble;
        StartNetworkTime = NetworkTime.time;

        OnButtonStartPressed?.Invoke();
    }
    #endregion Private
}
