public class Device : NetworkBehaviour
{
    [Header("General")]
    private GameManager manager;
    [Header("Shockwave")]
    [SerializeField] private Transform shockwave;
    [SerializeField] private float shockwaveTime;
    private bool isActivated = false;
    private float shockwaveStartTime;

    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);
    }

    private void Update()
    {
        if (!isServer)
        {
            return;
        }


    }

    #region Activation
    [Server]
    public void Activate()
    {
        if (isServerOnly)
        {
            SharedHasBeenActivated();
        }
        RpcHasBeenActivated();
    }

    [ClientRpc]
    public void RpcHasBeenActivated()
    {
        SharedHasBeenActivated();
    }

    private void SharedHasBeenActivated()
    {
        isActivated = true;
        shockwaveStartTime = manager.TimeSinceStart;
    }
    #endregion Activation
}