using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;
using Random = UnityEngine.Random;
using UnityEngine.UIElements;

public class QuickTest : MonoBehaviourPunCallbacks
{
    public TMP_Text connectButtonText;


    bool quickTest;
    string quickTestRoomName = "quicktestroom";

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();

    }

    public override void OnJoinedLobby()
    {
        if (quickTest)
        {
            PhotonNetwork.JoinOrCreateRoom(quickTestRoomName, new RoomOptions() { MaxPlayers = 10, BroadcastPropsChangeToAll = true }, TypedLobby.Default);
        }
    }

    public override void OnJoinedRoom()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void OnClickQuickTest()
    {
        quickTest = true;
        PhotonNetwork.NickName = RandomString(5);
        connectButtonText.text = "Connecting...";
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }



    public static string RandomString(int length)
    {
        string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        string generated_string = "";

        for (int i = 0; i < length; i++)
            generated_string += characters[Random.Range(0, characters.Length)];

        return generated_string;
    }
}
