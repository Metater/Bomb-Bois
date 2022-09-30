using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Draggable : NetworkBehaviour
{
    private GameManager manager;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private float forceProportionalConstant;
    [SerializeField] private float forceDerivitiveConstant;
    [SerializeField] private double draggerTimeout;

    private List<(uint netId, double time, Vector3 point, Vector3 target, GameObject indicator, Mutable<float> lastError)> draggers;

    private void Awake()
    {
        manager = FindObjectOfType<GameManager>(true);

        draggers = new();
    }

    private void Update()
    {
        draggers.RemoveAll(dragger =>
        {
            bool shouldRemove = Time.timeAsDouble >= dragger.time + draggerTimeout;
            if (dragger.indicator is not null)
            {
                if (shouldRemove)
                {
                    Destroy(dragger.indicator);
                }
                else
                {
                    Vector3 indicatorPosition = transform.position + dragger.point;
                    dragger.indicator.transform.position = indicatorPosition;
                    dragger.indicator.SetActive(true);
                }
            }
            return shouldRemove;
        });

        if (!isServer)
        {
            return;
        }

        rb.useGravity = draggers.Count == 0;

        Vector3 netForce = Vector3.zero;
        foreach ((uint netId, double time, Vector3 point, Vector3 target, GameObject indicator, Mutable<float> lastError) in draggers)
        {
            Vector3 vector = (target - transform.position).normalized;
            float error = Vector3.Distance(transform.position, target);
            float force = error * forceProportionalConstant;
            if (!float.IsNaN(lastError.Value))
            {
                force += ((error - lastError.Value) / Time.deltaTime) * forceDerivitiveConstant;
            }
            netForce += force * vector;

            lastError.Value = error;
        }
        rb.AddForce(Time.deltaTime * netForce, ForceMode.VelocityChange);
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            Destroy(rb);
        }
    }

    #region Dragger
    [Server]
    public void UpdateDragger(uint netId, Vector3 point, Vector3 target)
    {
        if (isServerOnly)
        {
            SharedUpdateDragger(netId, point, target);
        }

        RpcUpdateDragger(netId, point, target);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    public void RpcUpdateDragger(uint netId, Vector3 point, Vector3 target)
    {
        SharedUpdateDragger(netId, point, target);
    }

    public void SharedUpdateDragger(uint netId, Vector3 point, Vector3 target)
    {
        for (int i = 0; i < draggers.Count; i++)
        {
            if (draggers[i].netId == netId)
            {
                draggers[i] = (netId, Time.timeAsDouble, point, target, draggers[i].indicator, draggers[i].lastError);
                return;
            }
        }

        GameObject indicator = null;
        if (manager.LocalPlayer is not null && manager.LocalPlayer.netId != netId)
        {
            indicator = Instantiate(manager.otherPlayerDragIndicatorPrefab, Vector3.zero, Quaternion.identity);
            indicator.SetActive(false);
        }
        draggers.Add((netId, Time.timeAsDouble, point, target, indicator, new Mutable<float>(float.NaN)));
    }
    #endregion Dragger
}