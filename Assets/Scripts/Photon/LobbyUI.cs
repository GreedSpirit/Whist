using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject roomPanel;

    [Header("Lobby Inputs")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>(); // 방 목록 갱신을 위한 딕셔너리

    [Header("Room Info")]
    [SerializeField] private TextMeshProUGUI roomTitleText;
    [SerializeField] private TextMeshProUGUI playerListText; // 접속자 목록 표시용 텍스트
    [SerializeField] private TextMeshProUGUI[] relativeSeatTexts; 
    [SerializeField] private Button startGameBtn;
    [SerializeField] private Button leaveRoomBtn;

    [Header("Ranking UI")]
    public GameObject rankingPanel;
    public Transform rankingContent;
    public GameObject rankingItemPrefab;
    public Button openRankingBtn;
    public Button closeRankingBtn;

    private void Start()
    {
        createRoomBtn.onClick.AddListener(OnClickCreateRoom);
        startGameBtn.onClick.AddListener(OnClickStartGame);
        leaveRoomBtn.onClick.AddListener(OnClickLeaveRoom);

        NetworkManager.Instance.OnRoomListUpdateAction += UpdateRoomList;
        NetworkManager.Instance.OnJoinRoomSuccessAction += ShowRoomPanel;

        NetworkManager.Instance.OnPlayerListUpdateAction += UpdateRoomUI; // 입장/ 퇴장
        NetworkManager.Instance.OnPlayerPropertiesUpdateAction += UpdateRoomUI; // 자리 변경

        openRankingBtn.onClick.AddListener(OnClickOpenRanking);
        closeRankingBtn.onClick.AddListener(() => rankingPanel.SetActive(false));

        ShowLobbyPanel();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRoomListUpdateAction -= UpdateRoomList;
            NetworkManager.Instance.OnPlayerListUpdateAction -= UpdateRoomUI;
            NetworkManager.Instance.OnJoinRoomSuccessAction -= ShowRoomPanel;
            NetworkManager.Instance.OnPlayerPropertiesUpdateAction -= UpdateRoomUI;
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

    private void UpdateRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                    cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }

        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var roomInfo in cachedRoomList.Values)
        {
            GameObject entry = Instantiate(roomItemPrefab, roomListContent);

            TextMeshProUGUI roomText = entry.GetComponentInChildren<TextMeshProUGUI>();
            if(roomText) roomText.text = $"{roomInfo.Name} ({roomInfo.PlayerCount}/{roomInfo.MaxPlayers})";

            Button btn = entry.GetComponent<Button>();
            if (btn)
            {
                string roomName = roomInfo.Name;
                btn.onClick.AddListener(() => NetworkManager.Instance.JoinRoom(roomName));
            }
        }
    }


    private void ShowLobbyPanel()
    {
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
    }

    private void ShowRoomPanel()
    {
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
        UpdateRoomUI();
    }

    private void UpdateRoomUI()
    {
        if(!PhotonNetwork.InRoom) return;
        roomTitleText.text = $"Room: {PhotonNetwork.CurrentRoom.Name}";

        foreach (var txt in relativeSeatTexts)
        {
            txt.text = "빈 자리";
        }

        // 내 자리 정보 확인 (아직 배정 전이면 리턴)
        if(PhotonNetwork.LocalPlayer == null) return;
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("SeatNum"))
            return;

        int mySeatIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties["SeatNum"];

        // 타 유저들을 내 기준으로 회전시켜 배치
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if(player == null) continue;
            if (player.IsLocal) continue;
            if (player.CustomProperties == null || !player.CustomProperties.ContainsKey("SeatNum")) continue;

            int targetSeatIndex = (int)player.CustomProperties["SeatNum"];

            // 상대 위치 계산
            int offset = (targetSeatIndex - mySeatIndex + 4) % 4;

            switch (offset)
            {
                case 1:
                    if(relativeSeatTexts.Length > 0) relativeSeatTexts[0].text = player.NickName;
                    break;
                case 2:
                    if(relativeSeatTexts.Length > 1) relativeSeatTexts[1].text = player.NickName;
                    break;
                case 3:
                    if(relativeSeatTexts.Length > 2) relativeSeatTexts[2].text = player.NickName;
                    break;
            }
        }

        string playerList = "";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerList += $"{player.NickName}\n";
        }
        playerListText.text = playerList;


        //TODO 방장만 시작 버튼 활성화 & 4명일 때만 (테스트 위해 일단 방장이면 활성)
        startGameBtn.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        // startGameBtn.interactable = PhotonNetwork.CurrentRoom.PlayerCount == 4; 
    }

    private void OnClickStartGame()
    {
        NetworkManager.Instance.StartGame();
    }

    private void OnClickLeaveRoom()
    {
        NetworkManager.Instance.LeaveRoom();
        ShowLobbyPanel();
    }

    void OnClickOpenRanking()
    {
        rankingPanel.SetActive(true);
        foreach (Transform child in rankingContent) Destroy(child.gameObject);

        AuthManager.Instance.FetchAllUsers(OnRankingDataLoaded);
    }

    void OnRankingDataLoaded(List<User> allUsers)
    {
        allUsers.Sort((a, b) => 
        {
            float rateA = (a.Wins + a.Loses == 0) ? 0 : (float)a.Wins / (a.Wins + a.Loses);
            float rateB = (b.Wins + b.Loses == 0) ? 0 : (float)b.Wins / (b.Wins + b.Loses);

            if (rateA != rateB) return rateB.CompareTo(rateA); // 승률 높은 순
            return b.Wins.CompareTo(a.Wins); // 승률 같으면 승수 높은 순
        });

        for (int i = 0; i < allUsers.Count; i++)
        {
            GameObject itemObj = Instantiate(rankingItemPrefab, rankingContent);
            Ranking item = itemObj.GetComponent<Ranking>();

            item.Setup(i + 1, allUsers[i].Nickname, allUsers[i].Wins, allUsers[i].Loses);
        }
    }


}
