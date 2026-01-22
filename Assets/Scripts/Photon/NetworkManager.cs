using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    // MonoBehaviourPunCallbacks를 상속받기 때문에 제네릭 싱글톤 사용 불가...
    public static NetworkManager Instance {get; private set;}

    public byte maxPlayers = 4; //최적화에 도움이 되는 데이터 형식이 아닐까

    public event Action<List<RoomInfo>> OnRoomListUpdateAction; // 방 목록 갱신 이벤트
    public event Action OnPlayerListUpdateAction; //플레이어 목록 갱신 이벤트
    public event Action OnJoinRoomSuccessAction;
    public event Action OnPlayerPropertiesUpdateAction;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 씬 전환을 방장과 동기화하는 코드
        PhotonNetwork.AutomaticallySyncScene = true;

        Connect();
    }

    public void Connect()
    {
        string nickname = AuthManager.Instance.CurrentUser.Nickname;

        PhotonNetwork.NickName = nickname; // 서버에 닉네임 등록
        PhotonNetwork.GameVersion = "1.0"; // 버전이 다르면 만날 수 없게 하는 용도

        Debug.Log($"2. Photon 접속 시도 (닉네임 : {nickname})");
         PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("3. 마스터 서버 접속 성공, 로비 진입 시도");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("4. 로비 진입 성공");
        //로비 관련 로직 실행하면 될 듯 (방 목록, 로비 화면 등)
    }

    // 여기부터 방 관련 로직 -------------------------------------------------------------------

    public void CreateRoom(string roomName)
    {
        if(string.IsNullOrEmpty(roomName)) return; //TODO 나중에는 경고 팝업?

        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 4 });
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"5. 방 참가 성공 : {PhotonNetwork.CurrentRoom.Name}");

        OnJoinRoomSuccessAction?.Invoke();

        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable() { { "SeatNum", 0 }};
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        OnPlayerListUpdateAction?.Invoke();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패 : {message}");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // UI와 네트워크를 분라하기 위해서 UI 쪽으로 갱신 리스트를 전달하는 이벤트 발행
        OnRoomListUpdateAction?.Invoke(roomList);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName} 입장");
        OnPlayerListUpdateAction?.Invoke();

        if (PhotonNetwork.IsMasterClient)
        {
            AssignSeatToNewPlayer(newPlayer);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} 퇴장");
        OnPlayerListUpdateAction?.Invoke();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        OnPlayerPropertiesUpdateAction?.Invoke();
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    // 게임 시작 로직 --------------------------------------------------------------
    
    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if(PhotonNetwork.CurrentRoom.PlayerCount == 4)
            {
                PhotonNetwork.LoadLevel("InGameScene");
            }
            else
            {
                Debug.LogWarning("4명이 모이지 않아 시작할 수 없습니다.");
                PhotonNetwork.LoadLevel("InGameScene"); //TODO 테스트를 위해서 일단 4명 아니여도 넘어가도록
            }
        }
    }

    private void AssignSeatToNewPlayer(Player newPlayer)
    {
        List<int> usedSeats = new List<int>();
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("SeatNum"))
            {
                usedSeats.Add((int)p.CustomProperties["SeatNum"]);
            }            
        }

        int emptySeat = -1;
        for (int i = 0; i < maxPlayers; i++)
        {
            if (!usedSeats.Contains(i))
            {
                emptySeat = i;
                break;
            }
        }

        if (emptySeat != -1)
        {
            Hashtable props = new Hashtable() { { "SeatNum", emptySeat } };
            newPlayer.SetCustomProperties(props); // 타겟 플레이어의 프로퍼티 설정
        }
    }

    public void LeaveRopom()
    {
        Debug.Log("방 나가기 시도");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("성공적으로 방 나가기 완료");
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

}
