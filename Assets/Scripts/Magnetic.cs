using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnetic : MonoBehaviour
{
    public float magneticForce;

    private void OnTriggerStay(Collider other)
    {
        // BEWARE, THIS IS NOT FLEXIBLE, parent.parent
        // TODO LEFT OFF
        MagnetItem magnet = other.transform.parent.parent.GetComponent<MagnetItem>();
        if (magnet is null)
        {
            return;
        }

        magnet.OnMagneticStay(this);
    }
}
