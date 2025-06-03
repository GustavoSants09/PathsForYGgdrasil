using UnityEngine;

public class ColorChange : MonoBehaviour, Interectable
{

    Material mat;

    private void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
    }

    public string GetDescription()
    {
        return "Change to a random colour";
    }

    public void Interact()
    {
        Color[] cores = { Color.grey, Color.green, Color.blue, Color.red, Color.yellow };
        mat.color = cores[Random.Range(0, cores.Length)];
    }

}
