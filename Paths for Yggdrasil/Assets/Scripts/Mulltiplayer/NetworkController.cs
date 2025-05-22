using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class NetworkController : MonoBehaviourPunCallbacks
{
 private PhotonView myphotonview;
    
 private bool startingGame;
 
 [SerializeField] GameObject Fase_1_Button;
 
 public Animator Anima_Fase_1_Button;
 
  void Start()
  {
   myphotonview = GetComponent<PhotonView>();
   Connect();
    LOBBY.FasesButtons = false;//
   }
 void Update()
 { 
  if(LOBBY.FasesButtons==true)
  {
    Anima_Fase_1_Button.SetBool("ON",true);//
  }else{
    Anima_Fase_1_Button.SetBool("ON",false);//
  }
  }

 public void Connect()
  {
    PhotonNetwork.ConnectUsingSettings();
  }

  public override void OnConnectedToMaster ()
  {
    PhotonNetwork.JoinLobby ();
  }
  
 public void FASE_1 ()//
   {
    startingGame = true;
   if (!PhotonNetwork.IsMasterClient)
     return;
     PhotonNetwork.CurrentRoom.IsOpen = false;
     PhotonNetwork.LoadLevel(1);
   }


 public void DelayCancel ()
 {
   PhotonNetwork.LeaveRoom();
   PhotonNetwork.LoadLevel(0);
 }

 public override void OnJoinedRoom ()
   {
   Fase_1_Button.SetActive(PhotonNetwork.IsMasterClient);// 
   }

 public override void OnMasterClientSwitched (Player newMasterClient)
   {
   Fase_1_Button.SetActive(PhotonNetwork.IsMasterClient);//
   }
  
public void PlayersNumber(int playerNumber)
  {
   if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("PlayersN"))
   {
     PhotonNetwork.LocalPlayer.CustomProperties["PlayersN"] = playerNumber;
   }
   else
   {
   ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable
   {
   { "PlayersN", playerNumber }
   };
   PhotonNetwork.SetPlayerCustomProperties(playerProps);
   }
   }
 

}
