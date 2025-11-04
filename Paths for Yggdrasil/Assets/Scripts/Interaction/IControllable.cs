using UnityEngine;

public interface IControllable
{
    void RequestPickUp(Transform holdPoint = null);
    void Drop();
}