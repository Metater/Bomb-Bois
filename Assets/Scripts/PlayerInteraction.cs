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
    [SerializeField] private float itemSmoothTime;
    [SerializeField] private float itemRotationSlerpMultiplier;
    private int selectedSlotLocal = 0;
    private Item[] slots;
    [SyncVar(hook = nameof(OnSlotChanged))]
    public int selectedSlotSynced = 0;
    private Draggable currentDraggable = null;
    private float currentDraggableDistance = 0;
    [Header("Crosshair Colors")]
    [SerializeField] private Color crosshairDefaultColor;
    [SerializeField] private Color crosshairHoverItemColor;
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

        bool isDragging = false;
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
                    manager.draggableIndicator.transform.position = hit.point;

                    isDragging = true;

                    if (Input.GetMouseButton(0))
                    {
                        if (!draggable.isBeingDrug && currentDraggable is null)
                        {
                            // begin drag
                            // TODO IS THIS REPEATEDLY CALLED????!??!?!?
                            CmdBeginDrag(draggable.netId, hit.distance);
                        }
                        else if (currentDraggable is not null)
                        {
                            // take into account original click point, use that somehow, you need to

                            // continue drag
                            Vector3 draggablePosition = draggable.transform.position;
                            Vector3 draggableDragPosition = hit.point; // here???????
                            Vector3 desiredDragPosition = Camera.main.transform.position + (Camera.main.transform.forward * currentDraggableDistance);
                            Vector3 point = (draggableDragPosition - draggablePosition) + desiredDragPosition;
                            manager.indicator.transform.position = point;
                            draggable.AddForceTowardsPoint(point);
                        }
                    }
                }
            }
        }
        manager.crosshairImage.color = crosshairColor;
        if (!isDragging)
        {
            manager.draggableIndicator.SetActive(false);
            manager.indicator.SetActive(false);

            if (currentDraggable is not null)
            {
                // TODO STOP DRAGGING!       *(*(DASY*(D*(ASY((DSAY*(YDA
            }
        }
        else
        {
            manager.draggableIndicator.SetActive(true);
            manager.indicator.SetActive(true);
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
    [Command]
    public void CmdBeginDrag(uint netId, float distance)
    {
        if (draggableManager.TryGetDraggableByNetId(netId, out var draggable)
            && !draggable.isBeingDrug
            && draggable.netIdentity.connectionToClient is null)
        {
            draggable.netIdentity.AssignClientAuthority(connectionToClient);
            draggable.BeginDrag();
            RpcBeginDrag(netId, distance);
        }
    }
    [ClientRpc]
    public void RpcBeginDrag(uint netId, float distance)
    {
        if (draggableManager.TryGetDraggableByNetId(netId, out var draggable))
        {
            currentDraggable = draggable;
            currentDraggableDistance = distance;
        }
    }
    [Command]
    public void CmdEndDrag(uint netId)
    {
        if (draggableManager.TryGetDraggableByNetId(netId, out var draggable)
            && draggable.isBeingDrug
            && draggable.netIdentity.connectionToClient is not null)
        {
            draggable.netIdentity.RemoveClientAuthority();
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
        if (manager.itemLookup.TryGetWithNetId(netId, out var item) && !item.IsPickedUp)
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
        if (manager.itemLookup.TryGetWithNetId(netId, out var item))
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
        if (itemManager.TryGetItemByNetId(netId, out var item))
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
        if (itemManager.TryGetItemByNetId(netId, out var item))
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
