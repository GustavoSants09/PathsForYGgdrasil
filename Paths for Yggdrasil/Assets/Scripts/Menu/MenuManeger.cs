using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManeger : MonoBehaviour
{

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void LoadScene(string name)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(name);
        }
        else
        {
            Debug.LogWarning("Apenas o MasterClient pode carregar a cena.");
        }
    }

    public void Tp(string tp)
    {
        SceneManager.LoadScene(tp);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
