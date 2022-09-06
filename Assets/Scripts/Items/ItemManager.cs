using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : NetworkBehaviour
{
    // Use picture on phone, add player and item layers
    // Give items and player those tags
    // Stop players and items from colliding with each other

    // Raycast: https://gamedevbeginner.com/raycasts-in-unity-made-easy/

    // Pass authority: https://mirror-networking.gitbook.io/docs/guides/authority

    // Public Set Unity References
    public Transform itemsTransform;

    // Private
    private List<Item> items;

    private void Awake()
    {
        items = new List<Item>(itemsTransform.GetComponentsInChildren<Item>(true));
    }

    public bool TryGetItemByNetId(uint netId, out Item item)
    {
        item = items.Find(i => i.netId == netId);
        return item != null;
    }
}