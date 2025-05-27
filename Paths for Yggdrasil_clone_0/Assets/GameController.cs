using UnityEngine;
using Photon.Pun;
public class GameController : MonoBehaviour
{
    void Start()
    {
        PhotonNetwork.Instantiate("Player", new Vector3(0, 8, 0), Quaternion.identity);
    }

}
