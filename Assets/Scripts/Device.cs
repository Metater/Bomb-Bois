using Mirror;
using UnityEngine;

public class Device : NetworkBehaviour
{
    [Header("General")]
    private GameManager manager;
    [Header("Shockwave")]
    [SerializeField] private Transform shockwave;
    [SerializeField] private float shockwaveMaxScale;
    [SerializeField] private double shockwaveTime;
    [SerializeField] private AudioSource activatedSound;
    private bool isActivated = false;
    private double shockwaveStartTime;

    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);

        //shockwave.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isServer || isActivated)
        {
            return;
        }

        //float shockwaveStep = (float)((manager.TimeSinceStart - shockwaveStartTime) / shockwaveTime);
        //float shockwaveScale = Mathf.Lerp(0, shockwaveMaxScale, shockwaveStep);
        //shockwave.localScale = new Vector3(shockwaveScale, shockwaveScale, shockwaveScale);
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
        activatedSound.Play();
        //shockwaveStartTime = manager.TimeSinceStart;
    }
    #endregion Activation

    private void OnCollisionEnter(Collision collision)
    {
        if (!isServer)
        {
            return;
        }

        Activate();
    }
}