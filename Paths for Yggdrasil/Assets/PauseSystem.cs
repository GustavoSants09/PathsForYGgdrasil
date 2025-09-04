using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseSystem : MonoBehaviour
{
    [SerializeField] GameObject painel;
    bool canOpen;

    void Update()
    {
        if (Input.GetButtonDown("Cancel") && canOpen == true)
        {
            painel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            canOpen = false;
        }

        else if (Input.GetButtonDown("Cancel") && canOpen == false)
        {
            painel.SetActive(false);
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
            canOpen = true;
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Lobby()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Connect");
    }
    public void Menu()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("Connect");
    }
}
