using UnityEngine;
using Photon.Pun;

public class Spawn : MonoBehaviour
{
    void Start()
    {
        PhotonNetwork.Instantiate("Player", new Vector3(0, 3, 0), Quaternion.identity);
    }
}
