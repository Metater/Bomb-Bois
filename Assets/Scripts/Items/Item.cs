using Mirror;
using UnityEngine;

public abstract class Item : NetworkBehaviour
{
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected float dropForce;
    [SerializeField] protected GameObject modelGameObject;

    [System.NonSerialized] public Vector3 smoothDampVelocity = Vector3.zero;

    public bool IsPickedUp { get; private set; } = false;

    // While held in a player's inventory
    public void PickupInternal()
    {
        IsPickedUp = true;
        rb.useGravity = false;
        rb.detectCollisions = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        smoothDampVelocity = Vector3.zero;
    }
    protected abstract void Pickup();
    public void DropInternal(Vector3 dropVector, Vector3 velocity)
    {
        IsPickedUp = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        // TODO messes with sync? could have no authority, hasAuthority check????
        rb.AddForce((dropVector * dropForce) + velocity, ForceMode.Impulse);
    }
    protected abstract void Drop();
    public void SelectInternal()
    {
        modelGameObject.SetActive(true);
        Select();
    }
    protected abstract void Select();
    public void DeselectInternal()
    {
        modelGameObject.SetActive(false);
        Deselect();
    }
    protected abstract void Deselect();
    protected abstract void LeftClick();
    protected abstract void RightClick();
}