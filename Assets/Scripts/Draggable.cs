using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Draggable : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float forceProportionalConstant;

    [SyncVar]
    public bool isBeingDrug = false;

    public readonly SyncList<Dragger> draggers = new();

    private void Awake()
    {

    }

    private void Update()
    {
        if (!isServer)
        {
            return;
        }


    }

    private void OnDraggersUpdated(SyncList<Dragger>.Operation op, int index, Dragger oldDragger, Dragger newDragger)
    {
        switch (op)
        {
            case SyncList<Dragger>.Operation.OP_ADD:
                break;
            case SyncList<Dragger>.Operation.OP_INSERT:
                break;
            case SyncList<Dragger>.Operation.OP_REMOVEAT:
                break;
            case SyncList<Dragger>.Operation.OP_SET:
                break;
            case SyncList<Dragger>.Operation.OP_CLEAR:
                break;
        }
    }

    [Server]
    public void BeginDrag()
    {
        isBeingDrug = true;
        rb.useGravity = false;
    }

    [Server]
    public void EndDrag()
    {
        isBeingDrug = false;
        rb.useGravity = true;
    }

    [Client]
    public void AddForceTowardsPoint(Vector3 point)
    {
        //transform.position = point;
        Vector3 vector = (point - transform.position).normalized;
        float error = Vector3.Distance(transform.position, point);
        float force = error * forceProportionalConstant;
        rb.AddForce(Time.deltaTime * force * vector, ForceMode.VelocityChange);
    }

    public struct Dragger
    {
        public uint netId;
        public Vector3 point;
        [System.NonSerialized] public double time;

        public Dragger(uint netId, Vector3 point)
        {
            this.netId = netId;
            this.point = point;
            time = Time.timeAsDouble;
        }
    }
}