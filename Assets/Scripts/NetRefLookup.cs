public class NetRefManager<T> where T : NetworkBehaviour
{
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