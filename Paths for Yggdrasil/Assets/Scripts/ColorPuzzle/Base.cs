using UnityEngine;

public class Base : MonoBehaviour
{
    public Color correctColor;
    public GameObject currentObject; // The object currently on the base
    private Renderer objectRenderer;

    public bool IsCorrect()
    {
        if (currentObject == null) return false;

        objectRenderer = currentObject.GetComponent<Renderer>();
        return objectRenderer != null && objectRenderer.material.color == correctColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Object"))
        {
            // Snap the object into place
            currentObject = other.gameObject;
            other.transform.position = transform.position;
            other.transform.rotation = Quaternion.identity;

            // Notify the GameController to check status
            PuzzleController.Instance.CheckPuzzleState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Object") && other.gameObject == currentObject)
        {
            currentObject = null;
            PuzzleController.Instance.CheckPuzzleState();
        }
    }
}
