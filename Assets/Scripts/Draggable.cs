using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Draggable : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float forceProportionalConstant;

    private List<(double time, uint netId, Vector3 point)> draggers;

    private void Awake()
    {
        draggers = new();
    }

    private void Update()
    {
        if (!isServer)
        {
            return;
        }

        rb.useGravity = draggers.Count == 0;
    }

    [Server]
    public void NotifyDragger(uint netId, Vector3 point)
    {
        RpcNotifyDragger(netId, point);
    }

    [ClientRpc]
    public void RpcNotifyDragger(uint netId, Vector3 point)
    {
        for (int i = 0; i < draggers.Count; i++)
        {
            if (draggers[i].netId == netId)
            {
                draggers[i] = (Time.timeAsDouble, netId, point);
                return;
            }
        }

        draggers.Add((Time.timeAsDouble, netId, point));
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