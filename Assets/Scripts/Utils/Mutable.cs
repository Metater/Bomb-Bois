using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mutable<T>
{
    public T Value { get; set; }

    public Mutable(T value)
    {
        Value = value;
    }
}
