using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnetic : MonoBehaviour
{
    [SerializeField] private bool isMagneticForceRandomized;
    [SerializeField] private Vector2 randomizedMagneticForceRange;
    public float magneticForce;

    private void Awake()
    {
        if (isMagneticForceRandomized)
        {
            magneticForce = Random.Range(randomizedMagneticForceRange.x, randomizedMagneticForceRange.y);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other is null)
        {
            return;
        }

        MagnetItem magnet = other.GetComponentInParent<MagnetItem>();
        if (magnet is null)
        {
            return;
        }

        magnet.OnMagneticStay(this);
    }
}
