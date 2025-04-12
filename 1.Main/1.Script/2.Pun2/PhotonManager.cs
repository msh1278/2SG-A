using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    private static PhotonManager instance = null;
    private void Awake()
    {
        //전송 횟수
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
        Singleton();
    }
    private void Singleton()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(this.gameObject);

        }
        else
        {
            if (instance == this)
            {
                Destroy(this.gameObject);
            }
        }
    }
    public void Connect()
    {
        PhotonNetwork.ConnectUsingSettings();

    }

    public override void OnConnectedToMaster()
    {
        //꽉차면 이곳이 호출 안됨
        PhotonNetwork.LocalPlayer.NickName = "닉네임";
        PhotonNetwork.JoinOrCreateRoom(SceneManager.GetActiveScene().name, new RoomOptions { MaxPlayers = 20 }, null);
        //접속 시 (방생성 < 현재 씬 이름으로 생성)
    }

    public override void OnJoinedRoom()
    {

        //룸 참여 시
        Spawn();
    }

    public void Spawn()
    {
        //캐릭터 소환
        PhotonNetwork.Instantiate("Player", new Vector3(0, 0, 0), Quaternion.identity);
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        //연결 종료 시
    }
}
