using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    #region Fields
    // References
    [Header("General")]
    private GameManager manager;
    [SerializeField] private Player player;
    [Header("Transforms")]
    [SerializeField] private Transform gripTransform;
    [Header("Interaction")]
    [SerializeField] private float reachDistance;
    // TODO item should be smoothed relative to player body position???
    // TODO It would probably look more smooth then
    [SerializeField] private float itemSmoothTime;
    // TODO is there a better way to do this?
    [SerializeField] private float itemRotationSlerpMultiplier;
    private Item[] slots;
    private int selectedSlotLocal = 0;
    [SyncVar(hook = nameof(OnSlotChanged))] public int selectedSlotSynced = 0;
    [Header("Dragging")]
    [SerializeField] private float dragDistance;
    [SerializeField] private double draggerUpdateSendFrequency;
    private (Draggable draggable, Vector3 point, float distance)? currentDrag = null;
    private double lastDraggerUpdateSentTime = -1;
    [Header("Crosshair Colors")]
    [SerializeField] private Color crosshairDefaultColor;
    [SerializeField] private Color crosshairHoverItemColor;
    [SerializeField] private Color crosshairHoverDraggableColor;
    #endregion Fields

    #region Player Callbacks
    public void PlayerAwake()
    {
        manager = FindObjectOfType<GameManager>(true);

        slots = new Item[3];
    }
    public void PlayerStart()
    {

    }
    public void PlayerUpdate()
    {
        if (!isLocalPlayer || !manager.HasStarted)
        {
            return;
        }

        Item selectedItem = slots[selectedSlotLocal];

        bool shouldSkipCurrentDragLogic = false;
        Color crosshairColor = crosshairDefaultColor;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (selectedItem is null && Physics.Raycast(ray, out var hit, reachDistance))
        {
            if (hit.transform.gameObject.TryGetComponent<Item>(out var item) && !item.IsPickedUp)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    CmdPickupItem(item.netId);
                }
                crosshairColor = crosshairHoverItemColor;
            }
            else
            {
                Draggable draggable = hit.transform.gameObject.GetComponentInParent<Draggable>();
                if (draggable is not null)
                {
                    if (currentDrag is null)
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            Vector3 point = hit.point - draggable.transform.position;
                            // Where the heck does 0.3 come from and why does it help?
                            currentDrag = (draggable, point, hit.distance + 0.3f);
                            manager.playerDragStartIndicator.SetActive(true);
                            manager.playerDragStartIndicator.transform.position = hit.point;
                        }
                        else
                        {
                            shouldSkipCurrentDragLogic = true;
                            manager.playerDragIndicator.SetActive(true);
                            manager.playerDragIndicator.transform.position = hit.point;
                        }
                    }

                    crosshairColor = crosshairHoverDraggableColor;
                }
            }
        }
        manager.crosshairImage.color = crosshairColor;

        if (!shouldSkipCurrentDragLogic)
        {
            if (currentDrag is null)
            {
                manager.playerDragIndicator.SetActive(false);
                manager.playerDragStartIndicator.SetActive(false);
            }
            else
            {
                Vector3 dragPoint = currentDrag.Value.draggable.transform.position + currentDrag.Value.point;
                float distance = Vector3.Distance(Camera.main.transform.position, dragPoint);
                if (Input.GetMouseButton(0) && distance < dragDistance)
                {
                    manager.playerDragIndicator.SetActive(true);
                    manager.playerDragIndicator.transform.position = dragPoint;

                    // This method of slowing updates will be inaccurate, doesnt account for overflow!!!
                    if (lastDraggerUpdateSentTime + (1d / draggerUpdateSendFrequency) <= Time.timeAsDouble)
                    {
                        lastDraggerUpdateSentTime = Time.timeAsDouble;
                        Vector3 desiredPosition = Camera.main.transform.position + (Camera.main.transform.forward * currentDrag.Value.distance);
                        Vector3 target = desiredPosition - currentDrag.Value.point;
                        CmdUpdateDragger(currentDrag.Value.draggable.netId, currentDrag.Value.point, target);
                    }
                }
                else
                {
                    currentDrag = null;
                    manager.playerDragIndicator.SetActive(false);
                }
            }
        }

        if (currentDrag is null)
        {
            manager.playerDragStartIndicator.SetActive(false);
            manager.playerDragLineIndicator.gameObject.SetActive(false);
        }
        else
        {
            manager.playerDragLineIndicator.gameObject.SetActive(true);
            manager.playerDragLineIndicator.SetPosition(0, manager.playerDragStartIndicator.transform.position);
            manager.playerDragLineIndicator.SetPosition(1, manager.playerDragIndicator.transform.position);
        }

        UpdateSlot();

        if (selectedItem is not null)
        {
            if (Input.GetKeyDown(KeyCode.G) && selectedSlotLocal == selectedSlotSynced)
            {
                CmdDropItem(selectedItem.netId, Camera.main.transform.forward, player.playerMovement.Velocity);
            }
        }

        foreach (var item in slots)
        {
            if (item is null || !item.hasAuthority)
            {
                continue;
            }

            item.transform.position = Vector3.SmoothDamp(item.transform.position, gripTransform.position, ref item.smoothDampVelocity, itemSmoothTime);
            item.transform.rotation = Quaternion.Slerp(item.transform.rotation, gripTransform.rotation, Time.deltaTime * itemRotationSlerpMultiplier);
        }
    }
    #endregion Player Callbacks

    #region Dragging
    [Command(channel = Channels.Unreliable)]
    public void CmdUpdateDragger(uint draggerNetId, Vector3 point, Vector3 target)
    {
        if (manager.DraggableLookup.TryGetWithNetId(draggerNetId, out var draggable))
        {
            draggable.UpdateDragger(netId, point, target);
        }
    }
    #endregion Dragging

    #region Slot
    private void OnSlotChanged(int oldSelectedSlot, int newSelectedSlot)
    {
        if (oldSelectedSlot >= 0 && oldSelectedSlot < 3 && slots[oldSelectedSlot] != null)
        {
            slots[oldSelectedSlot].DeselectInternal();
        }

        if (newSelectedSlot >= 0 && newSelectedSlot < 3 && slots[newSelectedSlot] != null)
        {
            slots[newSelectedSlot].SelectInternal();
        }
    }

    private void UpdateSlot()
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
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedSlotLocal = 2;
        }

        if (selectedSlotLocal < 0)
        {
            selectedSlotLocal = 3 - 1;
        }
        else if (selectedSlotLocal >= 3)
        {
            selectedSlotLocal = 0;
        }

        if (originalSelectedSlotLocal != selectedSlotLocal)
        {
            CmdChangeSelectedSlot(selectedSlotLocal);
        }
    }

    [Command]
    public void CmdChangeSelectedSlot(int newSelectedSlot)
    {
        if (newSelectedSlot < 0 || newSelectedSlot > 3)
        {
            return;
        }

        selectedSlotSynced = newSelectedSlot;
    }
    #endregion Slot

    #region Pickup
    [Command]
    public void CmdPickupItem(uint netId)
    {
        if (manager.ItemLookup.TryGetWithNetId(netId, out var item) && !item.IsPickedUp)
        {
            int slot = selectedSlotSynced;
            if (slots[slot] is null)
            {
                // Unnessarily removes and reassigns client authority when client picks up same item 2 times in a row
                if (item.netIdentity.connectionToClient is not null)
                {
                    item.netIdentity.RemoveClientAuthority();
                }

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
    public void RpcPickupItem(uint netId, int slot)
    {
        if (manager.ItemLookup.TryGetWithNetId(netId, out var item))
        {
            SharedPickupItem(item, slot);
        }
    }

    private void SharedPickupItem(Item item, int slot)
    {
        slots[slot] = item;
        item.PickupInternal();

        if (isLocalPlayer)
        {
            item.transform.position = gripTransform.position;
            item.transform.rotation = gripTransform.rotation;
        }
    }
    #endregion Pickup

    #region Drop
    [Command]
    public void CmdDropItem(uint netId, Vector3 dropVector, Vector3 velocity)
    {
        if (manager.ItemLookup.TryGetWithNetId(netId, out var item))
        {
            for (int i = 0; i < 3; i++)
            {
                Item slot = slots[i];
                if (slot is null)
                {
                    continue;
                }

                if (slot.netId == netId)
                {
                    if (isServerOnly)
                    {
                        SharedDropItem(item, i, dropVector, velocity);
                    }
                    RpcDropItem(netId, i, dropVector, velocity);
                    //item.netIdentity.RemoveClientAuthority();
                    break;
                }
            }
        }
    }

    [ClientRpc]
    public void RpcDropItem(uint netId, int slot, Vector3 dropVector, Vector3 velocity)
    {
        if (manager.ItemLookup.TryGetWithNetId(netId, out var item))
        {
            SharedDropItem(item, slot, dropVector, velocity);
        }
    }

    private void SharedDropItem(Item item, int slot, Vector3 dropVector, Vector3 velocity)
    {
        slots[slot] = null;
        item.DropInternal(dropVector, velocity);
    }
    #endregion Drop
}
