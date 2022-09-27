using Mirror;
using System;
using System.Collections.Generic;

public class NetRefLookup<T> where T : NetworkBehaviour
{
    // Could use dictionary to optimize

    private readonly RefLookup<T> lookup = new();

    public List<T> Refs => lookup.Refs;

    public bool TryGet(Predicate<T> predicate, out T reference)
    {
        return lookup.TryGet(predicate, out reference);
    }

    public bool TryGetWithNetId(uint netId, out T reference)
    {
        return lookup.TryGet(r => r.netId == netId, out reference);
    }
}