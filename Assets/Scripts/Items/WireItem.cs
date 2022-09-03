using Mirror;
using UnityEngine;

public class WireItem : Item
{
    // TODO May not need transforms just private vector3s or properties
    [SerializeField] private Transform a;
    [SerializeField] private Transform b;

    [SerializeField] private float maxLength;

    private bool isUnpacked = false;

    [Server]
    public bool SetPositionA(Vector3 position)
    {
        return false;
    }

    [Server]
    public bool SetPositionB(Vector3 position)
    {
        return false;
    }

    public override void Deselect()
    {
        throw new System.NotImplementedException();
    }

    public override void Drop()
    {
        throw new System.NotImplementedException();
    }

    public override void LeftClick()
    {
        throw new System.NotImplementedException();
    }

    public override void Pickup()
    {
        throw new System.NotImplementedException();
    }

    public override void RightClick()
    {
        throw new System.NotImplementedException();
    }

    public override void Select()
    {
        throw new System.NotImplementedException();
    }
}