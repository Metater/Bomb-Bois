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
    private ItemManager itemManager;
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
    [Header("Crosshair Colors")]
    [SerializeField] private Color crosshairDefaultColor;
    [SerializeField] private Color crosshairHoverItemColor;
    #endregion Fields

    #region Player Callbacks
    public void PlayerAwake()
    {
        manager = FindObjectOfType<GameManager>(true);
        itemManager = FindObjectOfType<ItemManager>(true);

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

        Color crosshairColor = crosshairDefaultColor;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out var hit, reachDistance))
        {
            if (hit.transform.gameObject.TryGetComponent<Item>(out var item) && !item.IsPickedUp && selectedItem is null)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    CmdPickupItem(item.netId);
                }
                crosshairColor = crosshairHoverItemColor;
            }
        }
        manager.crosshairImage.color = crosshairColor;

        UpdateSlot();

        if (selectedItem is not null)
        {
            if (Input.GetKeyDown(KeyCode.G) && selectedSlotLocal == selectedSlotSynced)
            {
                CmdDropItem(selectedItem.netId, Camera.main.transform.forward);
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
        if (itemManager.TryGetItemByNetId(netId, out var item) && !item.IsPickedUp)
        {
            int slot = selectedSlotSynced;
            if (slots[slot] is null)
            {
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
        if (itemManager.TryGetItemByNetId(netId, out var item))
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
    public void CmdDropItem(uint netId, Vector3 dropVector)
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
                        SharedDropItem(item, i, dropVector);
                    }
                    RpcDropItem(netId, i, dropVector);
                    //item.netIdentity.RemoveClientAuthority();
                    break;
                }
            }
        }
    }

    [ClientRpc]
    public void RpcDropItem(uint netId, int slot, Vector3 dropVector)
    {
        if (itemManager.TryGetItemByNetId(netId, out var item))
        {
            SharedDropItem(item, slot, dropVector);
        }
    }

    private void SharedDropItem(Item item, int slot, Vector3 dropVector)
    {
        slots[slot] = null;
        item.DropInternal(dropVector, player.playerMovement.Velocity);
    }
    #endregion Drop
}
