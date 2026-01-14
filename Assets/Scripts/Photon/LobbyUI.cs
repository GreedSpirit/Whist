using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject roomPanel;

    [Header("Lobby Inputs")]
    public TMP_InputField roomNameInput;
    public Button createRoomBtn;
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    [Header("Room Info")]
    public TextMeshProUGUI roomTitleText;
    public TextMeshProUGUI playerListText; // 접속자 목록 표시용 텍스트
    public Button startGameBtn;
    public Button leaveRoomBtn;

    private void Start()
    {
        createRoomBtn.onClick.AddListener(OnClickCreateRoom);
        startGameBtn.onClick.AddListener(OnClickStartGame);
        leaveRoomBtn.onClick.AddListener(OnClickLeaveRoom);

        NetworkManager.Instance.OnRoomListUpdateAction += UpdateRoomList;
        NetworkManager.Instance.OnPlayerListUpdateAction += UpdateRoomUI;
        NetworkManager.Instance.OnJoinRoomSuccessAction += ShowRoomPanel;

        ShowLobbyPanel();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoomListUpdateAction -= UpdateRoomList;
            NetworkManager.Instance.OnPlayerListUpdateAction -= UpdateRoomUI;
        }
    }

    // 로비 관련 로직 -----------------------------------------------------------

    private void OnClickCreateRoom()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.Log("입력 칸이 비어있습니다");
            return;
        }

        NetworkManager.Instance.CreateRoom(roomName);
    }

    private void OnClickQuickJoinRoom() //TODO 빠른 참가 버튼과 연결할 예정 
    {
        NetworkManager.Instance.JoinRandomRoom();
    }

    private void UpdateRoomList(List<RoomInfo> roomList)
    {
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        foreach(RoomInfo info in roomList)
        {
            if(info.RemovedFromList) continue;

            GameObject entry = Instantiate(roomItemPrefab, roomListContent);

            TextMeshProUGUI txt = entry.GetComponentInChildren<TextMeshProUGUI>();
            if(txt) txt.text = $"{info.Name} ({info.PlayerCount}/{info.MaxPlayers})";

            // 클릭 시 해당 방 참가
            Button btn = entry.GetComponent<Button>();
            if (btn)
            {
                string roomName = info.Name;
                btn.onClick.AddListener(() => NetworkManager.Instance.JoinRoom(roomName));
            }
        }
    }


    void ShowLobbyPanel()
    {
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
    }

    void ShowRoomPanel()
    {
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
        UpdateRoomUI();
    }

    private void UpdateRoomUI()
    {
        if(!PhotonNetwork.InRoom) return;

        roomTitleText.text = $"Room: {PhotonNetwork.CurrentRoom.Name}";

        string playerList = "";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerList += $"{player.NickName}\n";
        }
        playerListText.text = playerList;

        //TODO 방장만 시작 버튼 활성화 & 4명일 때만 (테스트 위해 일단 방장이면 활성)
        if (PhotonNetwork.IsMasterClient)
        {
            startGameBtn.gameObject.SetActive(true);
            // startGameBtn.interactable = PhotonNetwork.CurrentRoom.PlayerCount == 4; 
        }
        else
        {
            startGameBtn.gameObject.SetActive(false);
        }
    }

    void OnClickStartGame()
    {
        NetworkManager.Instance.StartGame();
    }

    void OnClickLeaveRoom()
    {
        NetworkManager.Instance.LeaveRoom();
        ShowLobbyPanel();
    }
}
