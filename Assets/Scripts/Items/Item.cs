using Mirror;
using UnityEngine;

public abstract class Item : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float dropForce;

    public bool IsPickedUp { get; private set; } = false;

    // While held in a player's inventory
    public void PickupInternal()
    {
        IsPickedUp = true;
        Destroy(rb);
    }
    public abstract void Pickup();
    public void DropInternal(Vector3 dropVector)
    {
        IsPickedUp = false;
        rb = gameObject.AddComponent<Rigidbody>();
        rb.AddForce(dropVector * dropForce, ForceMode.Impulse);
    }
    public abstract void Drop();
    public abstract void Select();
    public abstract void Deselect();
    public abstract void LeftClick();
    public abstract void RightClick();
}