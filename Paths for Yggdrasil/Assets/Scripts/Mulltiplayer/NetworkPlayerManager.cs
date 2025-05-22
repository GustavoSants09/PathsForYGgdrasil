using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NetworkPlayerManager : MonoBehaviourPunCallbacks
{
     public static GameObject localPlayerInstance;

     void Start()
    {
        if (photonView.IsMine)
        {
            localPlayerInstance = gameObject;
        }
         DontDestroyOnLoad(gameObject);
    }
}
