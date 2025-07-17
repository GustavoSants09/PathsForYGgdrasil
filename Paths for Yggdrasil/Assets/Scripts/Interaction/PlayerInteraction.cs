using TMPro;
using UnityEngine;
using Photon.Pun;

public class PlayerInteraction : MonoBehaviourPun
{
    public Camera mainCam;
    public float interactionDistance = 2f;

    public GameObject interactionUI;
    public TextMeshProUGUI interactionText;

    void Start()
    {
        // Garante que só o dono do Player tenha a câmera ativada
        if (!photonView.IsMine)
        {
            mainCam.enabled = false;
            this.enabled = false;
            return;
        }

        if (mainCam == null)
        {
            mainCam = GetComponentInChildren<Camera>();
        }
    }

    private void Update()
    {
        InteractionRay();
    }

    void InteractionRay()
    {
        Ray ray = mainCam.ViewportPointToRay(Vector3.one / 2f);
        RaycastHit hit;

        bool hitSomething = false;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            Interectable interectable = hit.collider.GetComponent<Interectable>();

            if (interectable != null)
            {
                hitSomething = true;
                interactionText.text = interectable.GetDescription();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interectable.Interact();

                    // Se o objeto for PickupObject, passa a câmera corretamente
                    PickupObject pickup = hit.collider.GetComponent<PickupObject>();
                    if (pickup != null && pickup.photonView.IsMine)
                    {
                        pickup.SetCamera(mainCam);
                    }
                }
            }
        }

        interactionUI.SetActive(hitSomething);
    }
}