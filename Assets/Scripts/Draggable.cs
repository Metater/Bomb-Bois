using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Draggable : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float forceProportionalConstant;

    [SyncVar]
    public bool isBeingDrug = false;

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
}