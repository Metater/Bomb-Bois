using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : NetworkBehaviour
{
    // May want to disable character controller on non-owner clients??? why, idk

    // Disable the rigidbodies on clients that arent being used somehow, if it doesnt own it,
    // That also includes the rigidbody on the server

    //https://docs.unity3d.com/ScriptReference/Mathf.SmoothDamp.html
    //https://mirror-networking.gitbook.io/docs/guides/gameobjects/pickups-drops-and-child-objects

    #region Fields
    // References
    [Header("General")]
    private GameManager manager;
    private ItemManager itemManager;
    [SerializeField] private List<GameObject> invisibleToSelf;
    [SerializeField] private CharacterController controller;
    [Header("Transforms")]
    [SerializeField] private Transform handsTransform;
    [SerializeField] private Transform gripTransfrom;

    // Variables
    [Header("General")]
    [Header("Velocity")]
    [SerializeField] private int velocityAveragingQueueSize;
    private Vector3 lastPosition = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private Queue<Vector3> velocities;
    [Header("Crosshair Colors")]
    [SerializeField] private Color crosshairDefaultColor;
    [SerializeField] private Color crosshairHoverPlayerColor;
    [SerializeField] private Color crosshairHoverItemColor;
    [Header("Move")]
    [SerializeField] private float walkingSpeed;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float gravity;
    [SerializeField] private float momentumLerp;
    private Vector3 moveVelocity = Vector3.zero;
    private float moveX = 0;
    private float moveY = 0;
    private float moveXSmoothVelocity = 0;
    private float moveYSmoothVelocity = 0;
    private float moveSmoothTime = 0;
    [Header("Look")]
    [SerializeField] private float lookSpeed;
    [SerializeField] private float lookXLimit;
    private float rotationX = 0;
    [Header("Interaction")]
    [SerializeField] private float reachDistance;
    [Header("Inventory")]
    [SerializeField] private byte slotCount;
    private Item[] slots;
    private byte selectedSlotLocal = 0;
    [Header("Punch")]
    [SerializeField] private float punchForce;
    [SerializeField] private float punchVerticalForce;
    [SerializeField] private float punchDrag;
    [SerializeField] private float punchLerpVelocityToZeroStartTime;
    [SerializeField] private float punchLerpVelocityToZeroEndTime;
    private Vector2 punchVector = Vector2.zero;
    private double punchTime = 0f;
    private double punchableAgainTime = 0f;
    #endregion Fields

    #region Sync Vars
    [SyncVar(hook = nameof(OnSlotChanged))]
    public byte selectedSlotSynced = 0;
    #endregion Sync Vars

    #region Unity
    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);
        itemManager = FindObjectOfType<ItemManager>(true);

        velocities = new();

        manager.OnButtonStartPressed += OnButtonStartPressed;
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        // Break if not own player or not started
        if (!isLocalPlayer || !manager.HasStarted)
        {
            return;
        }

        #region Update Velocity
        Vector3 rawVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        velocities.Enqueue(rawVelocity);
        while (velocities.Count > velocityAveragingQueueSize)
        {
            velocities.Dequeue();
        }
        velocity = velocities.Average();
        #endregion Update Velocity

        #region Update Movement Speeds
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float lastMoveX = moveX;
        float lastMoveY = moveY;
        moveX = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical");
        moveY = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal");
        moveX = Mathf.SmoothDamp(lastMoveX, moveX, ref moveXSmoothVelocity, moveSmoothTime);
        moveY = Mathf.SmoothDamp(lastMoveY, moveY, ref moveYSmoothVelocity, moveSmoothTime);
        #endregion Update Movement Speeds

        #region Update Cursor Visibility
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorVisibility(!Cursor.visible);
        }
        #endregion Update Cursor Visibility

        UpdateSelectedSlot();

        Item selectedItem = slots[selectedSlotSynced];

        #region Update Interaction
        Color crosshairColor = crosshairDefaultColor;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out var hit, reachDistance))
        {
            // Player interaction
            if (hit.transform.gameObject.TryGetComponent<Player>(out var player))
            {
                if (player.IsPunchable())
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        CmdPunch(player.netId);
                    }
                    crosshairColor = crosshairHoverPlayerColor;
                }
            }
            // Item interaction
            else if (hit.transform.gameObject.TryGetComponent<Item>(out var item) && !item.IsPickedUp)
            {
                if (selectedItem is null
                && selectedSlotLocal == selectedSlotSynced
                && Input.GetKeyDown(KeyCode.E))
                {
                    CmdPickupItem(item.netId);
                }
                crosshairColor = crosshairHoverItemColor;
            }
        }
        manager.crosshairImage.color = crosshairColor;

        if (selectedItem is not null
        && selectedSlotLocal == selectedSlotSynced
        && Input.GetKeyDown(KeyCode.G))
        {
            //CmdDropItem(selectedItem.netId, Camera.main.transform.forward);
        }
        #endregion Interaction
    
        UpdateMovement();
    }
    #endregion Unity

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
        }
    }
    #endregion Mirror Callbacks

    #region Pickup
    [Command]
    public void CmdPickupItem(uint netId)
    {
        if (itemManager.TryGetItemByNetId(netId, out var item) && item.CanBePickedup)
        {
            if (slots[selectedSlotSynced] is null)
            {
                // Possible lerp from origin fix????
                //item.networkTransform.RpcTeleport(grip.transform.position);
                item.netIdentity.AssignClientAuthority(connectionToClient);
                if (isServerOnly)
                {
                    SharedPickupItem(item, selectedSlotSynced);
                }
                RpcPickupItem(netId, selectedSlotSynced);
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
        item.transform.SetParent(gripTransfrom);
        item.transform.localPosition = Vector3.zero;

        slots[slot] = item;
        item.PickupInternal();
    }
    #endregion Pickup

    #region Drop
    // normalize camera drop thing on command side
    // Keep client authority over item until next player tries to pick it up
    #endregion Drop

    #region Slot
    private void UpdateSelectedSlot()
    {
        byte originalSelectedSlotLocal = selectedSlotLocal;

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
            selectedSlotLocal = (byte)(slotCount - 1);
        }
        else if (selectedSlotLocal >= slotCount)
        {
            selectedSlotLocal = 0;
        }

        if (originalSelectedSlotLocal != selectedSlotLocal)
        {
            CmdChangeSelectedSlot(selectedSlotLocal);
        }
    }

    [Command]
    public void CmdChangeSelectedSlot(byte newSelectedSlot)
    {
        if (newSelectedSlot < 0 || newSelectedSlot > slotCount)
        {
            return;
        }

        selectedSlotSynced = newSelectedSlot;
    }

    private void OnSlotChanged(byte oldSelectedSlot, byte newSelectedSlot)
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
    #endregion Slot

    #region Punch
    // For future things like this, just make the server say yes or no, the fewer checks the better
    // You do get highlighting only when you can punch this way though
    [Command]
    public void CmdPunch(uint netId)
    {
        if (NetworkServer.spawned.TryGetValue(netId, out var id)
        && id.gameObject.TryGetComponent(out Player player)
        && player.IsPunchable()
        // You checked for cheating in a coop game, WHY?
        && Vector3.Distance(player.transform.position, transform.position) < reachDistance * 2f)
        {
            Vector3 vector = (player.transform.position - transform.position);
            vector.y = 0;
            vector = vector.normalized;

            player.RpcPunch(player.connectionToClient, new Vector2(vector.x, vector.z));
            if (isServerOnly)
            {
                SharedHasBeenPunched();
            }
            RpcHasBeenPunched();
        }
    }

    [TargetRpc]
    public void RpcPunch(NetworkConnection player, Vector2 vector)
    {
        if (punchVector == Vector2.zero)
        {
            punchVector = vector;
            punchTime = manager.TimeSinceStart;
            if (moveVelocity.y < 0 && controller.isGrounded)
            {
                moveVelocity.y = 0;
            }
            moveVelocity.y += punchVerticalForce;
        }
    }

    [ClientRpc]
    public void RpcHasBeenPunched()
    {
        SharedHasBeenPunched();
    }
    private void SharedHasBeenPunched()
    {
        punchableAgainTime = manager.TimeSinceStart + punchLerpVelocityToZeroEndTime;
    }

    public bool IsPunchable()
    {
        return manager.TimeSinceStart >= punchableAgainTime;
    }

        private Vector3 GetPunchVelocity()
    {
        if (punchVector == Vector2.zero)
        {
            return Vector3.zero;
        }

        float timeSincePunch = (float)(manager.TimeSinceStart - punchTime);

        if (timeSincePunch > punchLerpVelocityToZeroStartTime)
        {
            timeSincePunch = punchLerpVelocityToZeroStartTime;
        }

        float GetAxialPunchVelocity(float force)
        {
            return force * Mathf.Pow(1 - (punchDrag * 0.02f), 50 * timeSincePunch);
        }

        float x = GetAxialPunchVelocity(punchVector.x * punchForce);
        float z = GetAxialPunchVelocity(punchVector.y * punchForce);

        if (timeSincePunch < punchLerpVelocityToZeroStartTime)
        {

        }
        else if (timeSincePunch >= punchLerpVelocityToZeroStartTime && timeSincePunch < punchLerpVelocityToZeroEndTime)
        {
            float step = (timeSincePunch - punchLerpVelocityToZeroStartTime) / (punchLerpVelocityToZeroEndTime - punchLerpVelocityToZeroStartTime);
            x = Mathf.Lerp(x, 0, step);
            z = Mathf.Lerp(z, 0, step);
        }
        else
        {
            punchVector = Vector2.zero;
            return Vector3.zero;
        }

        return new Vector3(x, 0, z);
    }
    #endregion Punch

    #region Move
    private void UpdateMovement()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float moveVelocityY = moveVelocity.y;
        moveVelocity = (forward * moveX) + (right * moveY);

        if (controller.isGrounded && Input.GetButtonDown("Jump"))
            moveVelocity.y = jumpSpeed;
        else
            moveVelocity.y = moveVelocityY;

        if (!controller.isGrounded)
            moveVelocity.y -= gravity * Time.deltaTime;

        Vector3 punchVelocity = GetPunchVelocity();
        Vector3 moveDelta = moveVelocity * Time.deltaTime;
        Vector3 punchDelta = punchVelocity * Time.deltaTime;
        controller.Move(moveDelta + punchDelta);

        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        Quaternion rotation = Quaternion.Euler(rotationX, 0, 0);
        Camera.main.transform.localRotation = rotation;
        handsTransform.transform.localRotation = rotation;

        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);

        // Teleport to center if you fall off of the map
        if (transform.position.y < -25)
        {
            // May need to do networkTransform.CmdTeleport instead, to avoid lerp?
            transform.position = new Vector3(0, 25, 0);
            moveVelocity.y = 0f;
        }
    }
    #endregion Move

    #region Misc
    private void OnButtonStartPressed()
    {
        SetCursorVisibility(false);
    }

    private void SetCursorVisibility(bool isVisible)
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
    #endregion Misc
}