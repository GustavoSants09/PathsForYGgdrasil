using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun.UtilityScripts;

public class LOBBY : MonoBehaviourPunCallbacks
{
   [SerializeField]
   private GameObject  JoinLobby;

   public TMP_InputField playerNameInput;
 
   public Animator AnimaNickName_Field;
   public Animator AnimaJoinButton;

   [SerializeField]
   private GameObject lobbyPanel;

   public  TMP_Text Displayplayername;

   private string roomName;
   private int roomSize;

   private List<RoomInfo> roomListings;
 
   [SerializeField]
   private Transform roomsContainer;
    
   public static  bool char_1;
   public static  bool char_2;
   public static  bool FasesButtons;
 
   public Animator AnimaPlayer_1_Button;
   public Animator AnimaPlayer_2_Button;
 
   [SerializeField]
   private GameObject roomListingPrefab;

  public override void OnConnectedToMaster()
  {
   PhotonNetwork.AutomaticallySyncScene = true;
   roomListings = new List<RoomInfo>(); 
   if (PlayerPrefs.HasKey("NickName"))
   {
   if(PlayerPrefs.GetString("NickName") == "")
   {
   PhotonNetwork.NickName = "Player" + Random.Range(0, 100);
   }
   else
   {
   PhotonNetwork.NickName = PlayerPrefs.GetString("NickName");
   }
   }
   else
   {
   PhotonNetwork.NickName = "Player" + Random.Range(0, 100);
   }
   playerNameInput.text = PhotonNetwork.NickName;
  }

  public  void PlayerNameUpdate (string nameInput)
  {
   PhotonNetwork.NickName = nameInput;
   PlayerPrefs.SetString("NickName", nameInput);
  }

  public void JoinLobbyOnClick()
  {     
   JoinLobby.SetActive(false);
   lobbyPanel.SetActive(true);
   PhotonNetwork.JoinLobby();
  }

  public override void OnRoomListUpdate(List<RoomInfo> roomList)
  {
    int tempIndex;
   foreach (RoomInfo room in roomList)
   {
      if (roomListings != null)
     {
      tempIndex = roomListings.FindIndex(ByName(room.Name));
     }
     else
     {
      tempIndex = -1;
     }
     if (tempIndex != -1)
     {
      roomListings.RemoveAt(tempIndex);
      Destroy(roomsContainer.GetChild(tempIndex).gameObject);
     }
     if (room.PlayerCount > 0)
     {
      roomListings.Add(room);
      ListRoom(room);
     }
     }
   Displayplayername.text =  PhotonNetwork.NickName; //
   AnimaNickName_Field.SetBool("ON", true);//
   AnimaJoinButton.SetBool("ON", true);//
  }

  static System.Predicate<RoomInfo> ByName (string name)
  {
    return delegate (RoomInfo room)
    {
    return room.Name == name;
    }; 
  }

  void ListRoom (RoomInfo room)
  {
   if (room.IsOpen && room.IsVisible)
   {
    GameObject tempListing = Instantiate(roomListingPrefab, roomsContainer);
    RoomButton tempButton = tempListing.GetComponent<RoomButton>();
    tempButton.SetRoom(room.Name, room.MaxPlayers, room.PlayerCount);
   }
  }

  public void OnRoomNameChanged (string nameIn)
  {
   roomName = nameIn;
  }

  public void OnRoomSizeChanged (string sizeIn)
  {
   roomSize = int.Parse(sizeIn);
  }

  public void CreateRoom()
  {
   RoomOptions roomOps = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)roomSize };
   PhotonNetwork.CreateRoom(roomName, roomOps);
   AnimaPlayer_1_Button.SetBool("ON",false); //
  }

  public override void OnCreateRoomFailed (short returnCode, string message)
  {
        Debug.Log("vasco");
  }

  public void MatchmakingCancel()
  {
    JoinLobby.SetActive(true);
    lobbyPanel.SetActive(false);
    PhotonNetwork.LeaveLobby();
    PhotonNetwork.JoinLobby (); 
  }

  public override void OnLeftRoom() 
 {
  CharsIsFalse();   
  AnimaPlayer_1_Button.SetBool("ON", false);//
  AnimaPlayer_2_Button.SetBool("ON", false);//
 }
 
 public void  Char_1 ()//
  {
   photonView.RPC("Char1IsTrue", RpcTarget.AllBuffered);
        Char1IsTrue();
  } 
 
  [PunRPC]
  public void Char1IsTrue ()//
  {
   AnimaPlayer_1_Button.SetBool("ON", true);
   char_1 = true;
   FasesButtons = true;
  }

  public void Char_2 ()//
  {
    photonView.RPC("Char2IsTrue", RpcTarget.AllBuffered);
  }

  [PunRPC]
  private void Char2IsTrue ()//
  {
    AnimaPlayer_2_Button.SetBool("ON", true);
    char_2 = true;
    FasesButtons = true;
  }

  public void ClearClicks ()//
  {
   photonView.RPC("CharsIsFalse", RpcTarget.All );
  }   
 
  [PunRPC]
  private void CharsIsFalse ()//
  {
   char_1 = false;
   char_2 = false;

   }

}


