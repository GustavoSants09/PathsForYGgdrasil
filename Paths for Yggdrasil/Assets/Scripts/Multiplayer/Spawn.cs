using UnityEngine;
using Photon.Pun;

public class Spawn : MonoBehaviour
{
    void Start()
    {
        PhotonNetwork.Instantiate("Player", new Vector3(-57.4f, 2.77f, 429.1f), Quaternion.identity);
    }
}
