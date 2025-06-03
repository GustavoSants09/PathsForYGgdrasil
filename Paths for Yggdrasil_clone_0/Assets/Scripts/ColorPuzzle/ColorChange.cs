using UnityEngine;
using Photon.Pun;

public class ColorChange : MonoBehaviour, Interectable
{
    private Material mat;
    private PhotonView photonView;

    private void Start()
    {
        photonView = GetComponent<PhotonView>();
        mat = GetComponent<MeshRenderer>().material; // ou sharedMaterial, dependendo do seu caso
    }

    public string GetDescription()
    {
        return "Change to a random colour";
    }

    public void Interact()
    {
        if (photonView.IsMine)
        {
            Color[] cores = { Color.grey, Color.green, Color.blue, Color.red, Color.yellow };
            Color selectedColor = cores[Random.Range(0, cores.Length)];

            // Envia os componentes da cor (r,g,b,a) para todos os clientes
            photonView.RPC("ChangeColorRPC", RpcTarget.AllBuffered, selectedColor.r, selectedColor.g, selectedColor.b, selectedColor.a);
        }
    }

    [PunRPC]
    private void ChangeColorRPC(float r, float g, float b, float a)
    {
        Color newColor = new Color(r, g, b, a);
        mat.color = newColor;
    }
}