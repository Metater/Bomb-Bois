using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : NetworkBehaviour
{
    #region Fields
    // References
    [Header("General")]
    private GameManager manager;
    public PlayerMovement playerMovement;
    public PlayerInteraction playerInteraction;
    public PlayerAudio playerAudio;
    [SerializeField] private List<GameObject> invisibleToSelf;
    private bool isCursorVisible = false;
    #endregion Fields

    #region Mirror Callbacks
    public override void OnStartClient()
    {
        if (isLocalPlayer)
        {
            // Position own camera
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 1.6f, 0);

            // Make own GameObjects invisible
            invisibleToSelf.ForEach(go => go.SetActive(false));

            manager.InitLocalPlayer(this);
        }
    }
    #endregion Mirror Callbacks

    #region Unity Callbacks
    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);

        playerMovement.PlayerAwake();
        playerInteraction.PlayerAwake();

        UpdateCursorVisibility();
    }
    private void Start()
    {
        playerMovement.PlayerStart();
        playerInteraction.PlayerStart();
    }
    private void Update()
    {
        playerMovement.PlayerUpdate();
        playerInteraction.PlayerUpdate();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isCursorVisible = !isCursorVisible;
            UpdateCursorVisibility();
        }
    }
    #endregion Unity Callbacks

    private void UpdateCursorVisibility()
    {
        if (isCursorVisible)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        Cursor.visible = isCursorVisible;
    }
}
