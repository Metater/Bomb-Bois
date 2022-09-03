using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerScript : NetworkBehaviour
{
    // Private Set Unity References
    private GameManager manager;
    private ItemManager itemManager;

    // Private Set Unity References
    [SerializeField] private CharacterController characterController;
    [Space]
    [SerializeField] private List<GameObject> invisibleToSelf;
    [Space]
    [SerializeField] private GameObject body;
    [SerializeField] private GameObject hands;
    [SerializeField] private GameObject grip;

    // Private Set Unity Variables
    [SerializeField] public float walkingSpeed;
    [SerializeField] public float runningSpeed;
    [SerializeField] public float jumpSpeed;
    [SerializeField] private float gravity;
    [SerializeField] private float lookSpeed;
    [SerializeField] private float lookXLimit;
    [SerializeField] private float momentumLerp;

    [SerializeField] private float drag;
    [SerializeField] private float punchForce;

    [SerializeField] private float reachDistance;
    [SerializeField] private int slotCount;

    // Private Variables
    private float rotationX = 0;
    private Vector3 moveDirection = Vector3.zero;
    private float lastSpeedX = 0;
    private float lastSpeedY = 0;
    private Vector3 velocity = Vector3.zero;

    private int selectedSlotLocal = 0;
    private Item[] slots;

    [SyncVar(hook = nameof(OnSlotChanged))]
    public int selectedSlotSynced = 0;

    #region Unity
    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);
        itemManager = FindObjectOfType<ItemManager>(true);

        manager.OnButtonStartPressed += OnButtonStartPressed;

        slots = new Item[slotCount];
    }

    private void FixedUpdate()
    {
        velocity *= 1 - drag * Time.fixedDeltaTime;
    }

    private void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorVisible(!Cursor.visible);
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out var hit, reachDistance))
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (hit.transform.gameObject.TryGetComponent<PlayerScript>(out var player))
                {
                    CmdPunch(player.netId);
                }
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (hit.transform.gameObject.TryGetComponent<Item>(out var item))
                {
                    CmdPickupItem(item.netIdentity);
                }
            }
        }

        Inventory();

        Move();
    }
    #endregion Unity

    #region NetworkBehaviour Callbacks
    public override void OnStartClient()
    {
        if (isLocalPlayer)
        {
            // Position Own Camera
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 1.6f, 0);

            // Make Own GameObjects invisible
            invisibleToSelf.ForEach(go => go.SetActive(false));
        }
        else
        {

        }

        if (isServer)
        {

        }
        else
        {
            SetCursorVisible(false);
        }
    }
    #endregion NetworkBehaviour Callbacks

    #region Commands
    [Command]
    public void CmdChangeSelectedSlot(int newSelectedSlot)
    {
        if (newSelectedSlot < 0 || newSelectedSlot > slotCount)
        {
            return;
        }

        selectedSlotSynced = newSelectedSlot;
    }

    [Command]
    public void CmdPickupItem(NetworkIdentity netId)
    {
        // TODO checks
        if (itemManager.TryGetItemByNetId(netId.netId, out var item))
        {
            netId.AssignClientAuthority(connectionToClient);
            RpcPickedUpItem(netId.netId);
        }
    }

    [Command]
    public void CmdPunch(uint netId)
    {
        if (NetworkServer.spawned.TryGetValue(netId, out var id))
        {
            if (id.gameObject.TryGetComponent(out PlayerScript player))
            {
                if (Vector3.Distance(player.transform.position, transform.position) < reachDistance * 2f)
                {
                    player.RpcAddInstantaneousForce((player.transform.position - transform.position).normalized * punchForce);
                }
            }
        }
    }
    #endregion Commands

    #region Client RPCs
    [ClientRpc]
    public void RpcAddInstantaneousForce(Vector3 force)
    {
        velocity += force;
    }
    [ClientRpc]
    public void RpcPickedUpItem(uint netId)
    {
        if (itemManager.TryGetItemByNetId(netId, out var item))
        {
            // check slot not occupied
            item.transform.SetParent(grip.transform);
            item.transform.localPosition = Vector3.zero;
            slots[selectedSlotLocal] = item;
            item.PickupInternal();
        }
    }
    #endregion Client RPCs

    #region Private
    private void OnButtonStartPressed()
    {
        SetCursorVisible(false);
    }

    private void OnSlotChanged(int oldSelectedSlot, int newSelectedSlot)
    {
        if (oldSelectedSlot >= 0 && oldSelectedSlot < slotCount && slots[oldSelectedSlot] != null)
        {
            slots[oldSelectedSlot].Deselect();
        }

        if (newSelectedSlot >= 0 && newSelectedSlot < slotCount && slots[newSelectedSlot] != null)
        {
            slots[newSelectedSlot].Select();
        }
    }

    private void SetCursorVisible(bool isVisible)
    {
        if (isVisible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Inventory()
    {
        int originalSelectedSlotLocal = selectedSlotLocal;

        float mouseScrollDelta = Input.mouseScrollDelta.y;
        if (mouseScrollDelta > 0) // scroll up
        {
            selectedSlotLocal++;
        }
        else if (mouseScrollDelta < 0) // scroll down
        {
            selectedSlotLocal--;
        }

        if (selectedSlotLocal < 0)
        {
            selectedSlotLocal = 0;
        }
        else if (selectedSlotLocal > slotCount)
        {
            selectedSlotLocal = slotCount - 1;
        }

        if (originalSelectedSlotLocal != selectedSlotLocal)
        {
            CmdChangeSelectedSlot(selectedSlotLocal);
        }
    }

    private void Move()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float speedX = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical");
        float speedY = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal");
        if (speedX < lastSpeedX)
            speedX = Mathf.Lerp(speedX, lastSpeedX, momentumLerp);
        if (speedY < lastSpeedY)
            speedY = Mathf.Lerp(speedY, lastSpeedY, momentumLerp);
        lastSpeedX = speedX;
        lastSpeedY = speedY;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * speedX) + (right * speedY);

        if (characterController.isGrounded && Input.GetButtonDown("Jump"))
            moveDirection.y = jumpSpeed;
        else
            moveDirection.y = movementDirectionY;

        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        Vector3 moveDelta = moveDirection * Time.deltaTime;
        Vector3 forceDelta = velocity * Time.deltaTime;
        characterController.Move(moveDelta + forceDelta);

        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        var rotation = Quaternion.Euler(rotationX, 0, 0);
        Camera.main.transform.localRotation = rotation;
        hands.transform.localRotation = rotation;

        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);

        if (transform.position.y < -25)
        {
            transform.position = new Vector3(0, 25, 0);
        }
    }

    private void PositionHeldItemsOnGrip()
    {
        foreach (var item in slots)
        {
            if (item is null)
            {
                continue;
            }

            item.transform.position = grip.transform.position;
        }
    }
    #endregion Private
}
