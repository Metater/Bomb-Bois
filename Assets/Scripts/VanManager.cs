using Mirror;
using UnityEngine;

public class VanManager : MonoBehaviour
{
    // Private Set Unity References
    private GameManager manager;

    // Private Set Unity References
    [SerializeField] private Transform door;
    [Space]
    [SerializeField] private AudioSource screech;
    [SerializeField] private AudioSource intro;

    // Private Set Unity Variables
    [SerializeField] private float screechDelayTime;
    [SerializeField] private float introDelayTime;
    [Space]
    [SerializeField] private float slideDoorTime;
    [SerializeField] private float timeToSlideDoor;

    #region Unity
    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);

        manager.OnButtonStartPressed += OnButtonStartPressed;
    }

    private void Start()
    {

    }

    private void Update()
    {
        if (!manager.HasStarted)
        {
            return;
        }

        float timeSinceStart = (float)manager.TimeSinceStart;

        float doorStep = (timeSinceStart - slideDoorTime) / timeToSlideDoor;
        Vector3 doorPosition = Vector3.Lerp(new Vector3(0, 1.31f, -3.375f), new Vector3(0, 3.75f, -3.375f), doorStep);
        door.localPosition = doorPosition;
    }
    #endregion Unity

    #region Private
    private void OnButtonStartPressed()
    {
        screech.PlayDelayed(screechDelayTime);
        intro.PlayDelayed(introDelayTime);
    }
    #endregion Private
}
