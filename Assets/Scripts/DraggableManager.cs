using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class DraggableManager : NetworkBehaviour
{
    private List<Draggable> draggables;

    private void Awake()
    {
        draggables = new List<Draggable>(FindObjectsOfType<Draggable>(true));
    }

    public bool TryGetDraggableByNetId(uint netId, out Draggable draggable)
    {
        draggable = draggables.Find(i => i.netId == netId);
        return draggable != null;
    }
}
