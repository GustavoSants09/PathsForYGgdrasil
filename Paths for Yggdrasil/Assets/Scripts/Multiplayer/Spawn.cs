using UnityEngine;
using Photon.Pun;

public class Spawn : MonoBehaviour
{
    void Start()
    {
        PhotonNetwork.Instantiate("Player", new Vector3(28.3f, 3.21f, 444.312f), Quaternion.identity);
    }
}
