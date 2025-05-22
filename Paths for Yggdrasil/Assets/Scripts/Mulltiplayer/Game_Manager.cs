using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using UnityEngine.UI;
using TMPro;
public class Game_Manager : MonoBehaviourPunCallbacks
{
  public GameObject Player_1_Prefab;
  public GameObject Player_2_Prefab;
 
  public TMP_Text messageText;

  public static Game_Manager instance;
  
 private void Start()
 {
   StartCoroutine(DisplayMessage("Hello my friend"));//

  if (NetworkPlayerManager.localPlayerInstance == null)
  {
   int playerN = (int)PhotonNetwork.LocalPlayer.CustomProperties["PlayersN"];

   if (playerN == 1)
   {
     Vector3 spawn = new Vector3( 0, 10, -10);
  PhotonNetwork.Instantiate(Player_1_Prefab.name, spawn, Quaternion.Euler(0, 0, 0), 0);
   }
   if (playerN == 2)
   {
     Vector3 spawn = new Vector3( 0, 10, 10);
  PhotonNetwork.Instantiate(Player_2_Prefab.name, spawn, Quaternion.Euler(0, 180, 0), 0);
   }
  }
 }

 IEnumerator DisplayMessage(string message)
    {
      messageText.text = message;
      yield return new WaitForSeconds(3);
      messageText.text = "";
    }
 
    private void Awake()
    {
      instance = this;
    }
  
 public void QuitToLOBBY()
  {
    PhotonNetwork.LeaveRoom();
  }
 
 public override void OnLeftRoom()
  {
   SceneManager.LoadScene(0);
  }

}
