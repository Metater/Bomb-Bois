using System;
using System.Collections.Generic;

public class RefLookup<T>
{
    public List<T> Refs { get; private set; } = new();

    public bool TryGet(Predicate<T> predicate, out T reference)
    {
        reference = Refs.Find(predicate);
        return reference is not null;
    }
}