using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ROOM : MonoBehaviourPunCallbacks
{
  [SerializeField]
  private GameObject lobbyPanel;

[SerializeField]
 private GameObject  roomPanel;

 
[SerializeField]
 private Text  roomNameDisplay;

[SerializeField]
 private Transform  playersContainer;
  
  [SerializeField]
  private GameObject Characters;


 [SerializeField]
 private GameObject  playeListingPrefab;

  void ClearPlayerListings()
  {
  for (int i = playersContainer.childCount - 1; i >= 0; i--)
  {
  Destroy(playersContainer.GetChild(i).gameObject);
  }
  }

  void ListPlayers ()
  {
  foreach (Player player in PhotonNetwork.PlayerList)
  {
  GameObject tempListing = Instantiate (playeListingPrefab, playersContainer );
  Text tempText = tempListing.transform.GetChild(0).GetComponent<Text>();
  tempText.text = player.NickName;
  }
  }

 public override void OnJoinedRoom()
 {
  ClearPlayerListings();
  ListPlayers();
  Characters.SetActive(true);
  roomPanel.SetActive(true);
  lobbyPanel.SetActive(false);
  roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name;
 }

 public override void OnPlayerEnteredRoom (Player newPlayer)
  {
  ClearPlayerListings  ();
  ListPlayers  ();
  }
 
  public override void OnPlayerLeftRoom (Player newPlayer)
  {
  ClearPlayerListings  ();
  ListPlayers  ();
   }

  public override void OnLeftRoom() 
 {
  ClearPlayerListings  ();
  ListPlayers  ();
   Debug.Log("OUT OF ROOM");//
  }

 public void StartGame ()
 {
  if (PhotonNetwork.IsMasterClient)
{
   PhotonNetwork.CurrentRoom.IsOpen = false;
   }
  }

 IEnumerator rejoinLobby()
 {
yield return new WaitForSeconds(1);
PhotonNetwork.JoinLobby();
 }

 public void BackOnClick ()
 {
  ClearPlayerListings (); 
  ListPlayers  ();
  lobbyPanel.SetActive(true);
  roomPanel.SetActive(false);
  PhotonNetwork.LeaveRoom  ();
  PhotonNetwork.LeaveLobby  ();
  StartCoroutine(rejoinLobby( )); 
  Characters.SetActive(false);// 
  }
 
 
  
}
