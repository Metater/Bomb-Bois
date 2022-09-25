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
    [SerializeField] private Transform handsTransform;
    [Header("Interaction")]
    [SerializeField] private float reachDistance;
    private int selectedSlotLocal = 0;
    private Item[] slots;
    [SyncVar(hook = nameof(OnSlotChanged))]
    public int selectedSlotSynced = 0;
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

        UpdateSlot();
    }
    #endregion Player Callbacks

    private void OnSlotChanged(int oldSelectedSlot, int newSelectedSlot)
    {
        if (oldSelectedSlot >= 0 && oldSelectedSlot < 3 && slots[oldSelectedSlot] != null)
        {
            slots[oldSelectedSlot].Deselect();
        }

        if (newSelectedSlot >= 0 && newSelectedSlot < 3 && slots[newSelectedSlot] != null)
        {
            slots[newSelectedSlot].Select();
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
        else if (!Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedSlotLocal = 2;
        }

        if (selectedSlotLocal < 0)
        {
            selectedSlotLocal = 0;
        }
        else if (selectedSlotLocal >= 3)
        {
            selectedSlotLocal = 3 - 1;
        }

        if (originalSelectedSlotLocal != selectedSlotLocal)
        {
            CmdChangeSelectedSlot(selectedSlotLocal);
        }
    }
}
