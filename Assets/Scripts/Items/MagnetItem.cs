using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetItem : Item
{
    [SerializeField] private float magneticForce;
    [SerializeField] private float forceAtZeroDistance;

    // make magnet fly out of hands when force too strong

    // some trashcans are slightly magnetic too troll
    // or all are, but in random amounts that are lower than the bomb???

    // constant = (magneticForce1 * magneticForce2)
    // shift = sqrt(constant / forceAtZeroDistance)
    // f(distance) = constant / ((distance + shift)^2)

    //https://www.desmos.com/calculator/jar9rvpkp8

    private List<(int frame, Vector3 force)> forces;

    private void Awake()
    {
        forces = new();
    }

    protected override void Pickup()
    {

    }
    protected override void Drop()
    {

    }
    protected override void Select()
    {

    }
    protected override void Deselect()
    {

    }
    protected override void LeftClick()
    {

    }
    protected override void RightClick()
    {
        
    }

    public void OnMagneticStay(Magnetic magnetic)
    {
        if (!hasAuthority)
        {
            return;
        }
        
        if (IsPickedUp)
        {
            print("does this print!!!");
            return;
        }

        float magnitude = GetAttractionForce(magnetic);
        Vector3 vector = magnetic.transform.position - transform.position;
        Vector3 force = magnitude * vector;

        rb.AddForce(Time.deltaTime * force, ForceMode.VelocityChange);
    }

    private float GetAttractionForce(Magnetic magnetic)
    {
        float distance = Vector3.Distance(transform.position, magnetic.transform.position);
        float constant = magneticForce * magnetic.magneticForce;
        float shift = Mathf.Sqrt(constant / forceAtZeroDistance);
        return constant / Mathf.Pow(distance + shift, 2);
    }
}
