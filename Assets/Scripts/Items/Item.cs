using Mirror;
using UnityEngine;

public abstract class Item : NetworkBehaviour
{
    [SerializeField] private NetworkTransform networkTransform;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float dropForce;

    public bool IsPickedUp { get; private set; } = false;

    // While held in a player's inventory
    public void PickupInternal()
    {
        IsPickedUp = true;
        rb.useGravity = false;
        rb.detectCollisions = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    public abstract void Pickup();
    public void DropInternal(Vector3 dropVector)
    {
        IsPickedUp = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.AddForce(dropVector * dropForce, ForceMode.Impulse);
    }
    public abstract void Drop();
    public abstract void Select();
    public abstract void Deselect();
    public abstract void LeftClick();
    public abstract void RightClick();
}