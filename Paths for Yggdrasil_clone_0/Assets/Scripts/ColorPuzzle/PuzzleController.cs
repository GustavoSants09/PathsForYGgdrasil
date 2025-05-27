using UnityEngine;

public class PuzzleController : MonoBehaviour
{
    public static PuzzleController Instance;
    public Base[] bases;
    public GameObject wireObject; // The wire object to activate or change color
    public Color successColor = Color.green;
    private Renderer wireRenderer;

    void Awake()
    {
        Instance = this;
        wireRenderer = wireObject.GetComponent<Renderer>();
    }

    public void CheckPuzzleState()
    {
        foreach (Base b in bases)
        {
            if (!b.IsCorrect())
            {
                // If any base is wrong, reset the wire
                wireRenderer.material.color = Color.red;
                return;
            }
        }

        // All bases are correct
        wireRenderer.material.color = successColor;
    }
}
