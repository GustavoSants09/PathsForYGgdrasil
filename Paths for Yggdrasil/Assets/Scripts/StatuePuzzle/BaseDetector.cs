using UnityEngine;

public class BaseDetector : MonoBehaviour
{
    public string baseColor; // Ex: "Blue"

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Statue"))
        {
            StatuePlacement statue = other.GetComponent<StatuePlacement>();
            if (statue != null && statue.statueColor == baseColor)
            {
                // Trava a estátua na posição da base
                statue.LockStatue(transform.position);
            }
        }
    }
}
