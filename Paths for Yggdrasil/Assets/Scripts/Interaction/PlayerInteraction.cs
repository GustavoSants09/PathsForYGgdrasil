using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{

    public Camera mainCam;
    public float interactionDistance = 2f;

    public GameObject interactionUI;
    public TextMeshProUGUI interactionText;


    private void Update()
    {
        InteractionRay();
    }

    void InteractionRay()
    {
        Ray ray = mainCam.ViewportPointToRay(Vector3.one / 2f);
        RaycastHit hit;

        bool hitSomething = false;

        if(Physics.Raycast(ray, out hit, interactionDistance))
        {
            Interectable interectable = hit.collider.GetComponent<Interectable>();

            if (interectable != null)
            {
                hitSomething = true;
                interactionText.text = interectable.GetDescription();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interectable.Interact();
                }
            }
        }
        interactionUI.SetActive(hitSomething);
    }
}
