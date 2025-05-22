using UnityEngine;

public class PickupObject : MonoBehaviour
{
    private Camera cam;
    private GameObject heldObject;
    private Vector3 offset;

    public float maxPickupDistance = 5f; // Max distance to pick up objects
    public LayerMask objectLayer; // Only interactive objects

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button click
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxPickupDistance, objectLayer))
            {
                if (hit.collider.CompareTag("Object"))
                {
                    heldObject = hit.collider.gameObject;
                    offset = heldObject.transform.position - hit.point;
                }
            }
        }

        if (Input.GetMouseButton(0) && heldObject) // While holding the mouse button
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Vector3 newPosition = ray.GetPoint(3f); // Adjust this distance as needed
            heldObject.transform.position = newPosition;
        }

        if (Input.GetMouseButtonUp(0) && heldObject) // Releasing the object
        {
            heldObject = null;
        }
    }
}
