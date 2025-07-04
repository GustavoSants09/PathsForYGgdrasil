using UnityEngine;

public class BaseDetector : MonoBehaviour
{
    public string baseColor;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Statue"))
        {
            PickupObject pickup = other.GetComponent<PickupObject>();
            StatuePlacement statue = other.GetComponent<StatuePlacement>();

            if (statue != null && pickup != null && !pickup.IsLocked())
            {
                if (statue.statueColor == baseColor)
                {
                    // Chama o travamento pelo Photon
                    pickup.LockObject(transform.position);
                }
            }
        }
    }
}
