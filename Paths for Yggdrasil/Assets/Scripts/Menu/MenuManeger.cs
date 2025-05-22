using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManeger : MonoBehaviour
{
    public void Tp(string tp)
    {
        SceneManager.LoadScene(tp);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
