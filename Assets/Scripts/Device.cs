public class Device : NetworkBehaviour
{
    [Header("General")]
    private GameManager manager;
    [Header("Shockwave")]
    [SerializeField] private Transform shockwave;
    [SerializeField] private float shockwaveMaxScale;
    [SerializeField] private float shockwaveTime;
    private bool isActivated = false;
    private float shockwaveStartTime;

    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);

        shockwave.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isServer || isActivated)
        {
            return;
        }

        float shockwaveStep = (manager.TimeSinceStart - shockwaveStartTime) / shockwaveTime;
        float shockwaveScale = Mathf.Lerp(0, shockwaveMaxScale, shockwaveStep);
        shockwave.localScale = new Vector3(shockwaveScale, shockwaveScale, shockwaveScale);
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