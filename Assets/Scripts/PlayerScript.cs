using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : NetworkBehaviour
{
    // Private Runtime Set Unity References
    private GameManager manager;
    private ItemManager itemManager;

    // Private Set Unity References
    [SerializeField] private CharacterController characterController;
    [Space]
    [SerializeField] private List<GameObject> invisibleToSelf;
    [Header("GameObjects")]
    [SerializeField] private GameObject body;
    [SerializeField] private GameObject hands;
    [SerializeField] private GameObject grip;
    [Header("Colors")]
    [SerializeField] private Color crosshairDefaultColor;
    [SerializeField] private Color crosshairHoverPlayerColor;
    [SerializeField] private Color crosshairHoverItemColor;

    // Private Set Unity Variables
    [Header("Movement")]
    [SerializeField] public float walkingSpeed;
    [SerializeField] public float runningSpeed;
    [SerializeField] public float jumpSpeed;
    [SerializeField] private float gravity;
    [SerializeField] private float lookSpeed;
    [SerializeField] private float lookXLimit;
    [SerializeField] private float momentumLerp;
    [Header("Interaction")]
    [SerializeField] private float punchDrag;
    [SerializeField] private float punchForce;
    [SerializeField] private double punchCooldownTime;
    [SerializeField] private float reachDistance;
    [SerializeField] private int slotCount;

    // Private Variables
    private float rotationX = 0;
    private Vector3 moveDirection = Vector3.zero;
    private float lastSpeedX = 0;
    private float lastSpeedY = 0;

    private Vector3 punchVelocity = Vector3.zero;
    private double punchableTime = 0;

    private int selectedSlotLocal = 0;
    private Item[] slots;

    [SyncVar(hook = nameof(OnSlotChanged))]
    public int selectedSlotSynced = 0;

    public Vector3 lastPosition = Vector3.zero;
    public Vector3 velocity = Vector3.zero;

    #region Unity
    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);
        itemManager = FindObjectOfType<ItemManager>(true);

        manager.OnButtonStartPressed += OnButtonStartPressed;

        slots = new Item[slotCount];
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // Break if not own player or not started
        if (!isLocalPlayer || !manager.HasStarted)
        {
            return;
        }

        // Apply drag to punch velocity
        punchVelocity *= 1 - (punchDrag * Time.fixedDeltaTime);
    }

    private void Update()
    {
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        // Break if not own player or not started
        if (!isLocalPlayer || !manager.HasStarted)
        {
            return;
        }

        // Handle cursor visibility
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorVisible(!Cursor.visible);
        }

        // Handle interaction
        Color crosshairColor = crosshairDefaultColor;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out var hit, reachDistance))
        {
            // Player interaction
            if (hit.transform.gameObject.TryGetComponent<PlayerScript>(out var player))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    CmdPunch(player.netId);
                }
                crosshairColor = crosshairHoverPlayerColor;
            }
            // Item interaction
            else if (hit.transform.gameObject.TryGetComponent<Item>(out var item) && !item.IsPickedUp)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    CmdPickupItem(item.netId);
                }
                crosshairColor = crosshairHoverItemColor;
            }
        }
        manager.crosshairImage.color = crosshairColor;

        SelectSlot();

        Item selectedItem = slots[selectedSlotLocal];
        if (selectedItem is not null)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                CmdDropItem(selectedItem.netId, Camera.main.transform.forward);
            }
        }

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
    }
    #endregion NetworkBehaviour Callbacks

    #region Punch
    [Command]
    public void CmdPunch(uint netId)
    {
        if (NetworkServer.spawned.TryGetValue(netId, out var id))
        {
            if (id.gameObject.TryGetComponent(out PlayerScript player))
            {
                if (Vector3.Distance(player.transform.position, transform.position) < reachDistance * 2f)
                {
                    player.RpcPunch((player.transform.position - transform.position).normalized * punchForce);
                }
            }
        }
    }

    [ClientRpc]
    public void RpcPunch(Vector3 force)
    {
        if (manager.TimeSinceStart >= punchableTime)
        {
            punchableTime = manager.TimeSinceStart + punchCooldownTime;
            punchVelocity += force;
        }
    }
    #endregion Punch

    #region Slot
    // Add server confirming slot changes, client makes sure server has confirmed slot change before doing any interactions
    // ^^ Dont queue
    #endregion Slot

    #region Pickup
    [Command]
    public void CmdPickupItem(uint netId)
    {
        if (itemManager.TryGetItemByNetId(netId, out var item) && !item.IsPickedUp)
        {
            byte slot = (byte)selectedSlotSynced;
            if (slots[slot] is null)
            {
                // Possible lerp from origin fix????
                //item.networkTransform.RpcTeleport(grip.transform.position);
                item.netIdentity.AssignClientAuthority(connectionToClient);
                if (isServerOnly)
                {
                    SharedPickupItem(item, slot);
                }
                RpcPickupItem(netId, slot);
            }
        }
    }

    [ClientRpc]
    public void RpcPickupItem(uint netId, byte slot)
    {
        if (itemManager.TryGetItemByNetId(netId, out var item))
        {
            SharedPickupItem(item, slot);
        }
    }

    private void SharedPickupItem(Item item, byte slot)
    {
        item.transform.SetParent(grip.transform);
        item.transform.localPosition = Vector3.zero;

        slots[slot] = item;
        item.PickupInternal();
    }
    #endregion Pickup

    #region Drop
    [Command]
    public void CmdDropItem(uint netId, Vector3 dropVector)
    {
        if (itemManager.TryGetItemByNetId(netId, out var item))
        {
            for (int i = 0; i < slotCount; i++)
            {
                Item slot = slots[i];
                if (slot is null)
                {
                    continue;
                }

                if (slot.netId == netId)
                {
                    byte slotIndex = (byte)i;
                    if (isServerOnly)
                    {
                        SharedDropItem(item, slotIndex, dropVector);
                    }
                    RpcDropItem(netId, slotIndex, dropVector);
                    item.netIdentity.RemoveClientAuthority();
                    break;
                }
            }
        }
    }

    [ClientRpc]
    public void RpcDropItem(uint netId, byte slot, Vector3 dropVector)
    {
        if (itemManager.TryGetItemByNetId(netId, out var item))
        {
            SharedDropItem(item, slot, dropVector);
        }
    }

    private void SharedDropItem(Item item, byte slot, Vector3 dropVector)
    {
        //item.transform.SetParent(itemManager.itemsTransform);

        slots[slot] = null;
        item.DropInternal(dropVector, velocity);
    }
    #endregion Drop

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
    #endregion Commands

    #region Client RPCs
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
            slots[oldSelectedSlot].DeselectInternal();
        }

        if (newSelectedSlot >= 0 && newSelectedSlot < slotCount && slots[newSelectedSlot] != null)
        {
            slots[newSelectedSlot].SelectInternal();
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

    private void SelectSlot()
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

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedSlotLocal = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedSlotLocal = 1;
        }
        else if (!Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedSlotLocal = 2;
        }

        if (selectedSlotLocal < 0)
        {
            selectedSlotLocal = 0;
        }
        else if (selectedSlotLocal >= slotCount)
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
        Vector3 punchDelta = punchVelocity * Time.deltaTime;
        characterController.Move(moveDelta + punchDelta);

        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        var rotation = Quaternion.Euler(rotationX, 0, 0);
        Camera.main.transform.localRotation = rotation;
        hands.transform.localRotation = rotation;

        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);

        if (transform.position.y < -25)
        {
            transform.position = new Vector3(0, 25, 0);
            moveDirection.y = 0f;
        }
    }
    #endregion Private
}
