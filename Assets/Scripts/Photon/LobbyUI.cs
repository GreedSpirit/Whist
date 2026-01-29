using System.Collections.Generic;
using DG.Tweening;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _roomPanel;

    [Header("Lobby Inputs")]
    [SerializeField] private TMP_InputField _roomNameInput;
    [SerializeField] private Button _createRoomBtn;
    [SerializeField] private Button _gameExitButton;
    [SerializeField] private Transform _roomListContent;
    [SerializeField] private GameObject _roomItemPrefab;
    [SerializeField] private Dictionary<string, RoomInfo> _cachedRoomList = new Dictionary<string, RoomInfo>(); // 방 목록 갱신을 위한 딕셔너리

    [Header("Room Info")]
    [SerializeField] private TextMeshProUGUI _roomTitleText;
    [SerializeField] private TextMeshProUGUI _playerListText; // 접속자 목록 표시용 텍스트
    [SerializeField] private TextMeshProUGUI[] _relativeSeatTexts; 
    [SerializeField] private Button _startGameBtn;
    [SerializeField] private Button _leaveRoomBtn;

    [Header("Notification UI")]
    [SerializeField] private CanvasGroup _notificationPanel; // 로비 씬에도 똑같이 만든 패널 연결
    [SerializeField] private TextMeshProUGUI _notificationText;

    [Header("Ranking UI")]
    public GameObject rankingPanel;
    public Transform rankingContent;
    public GameObject rankingItemPrefab;
    public Button openRankingBtn;
    public Button closeRankingBtn;

    private void Start()
    {
        _createRoomBtn.onClick.AddListener(OnClickCreateRoom);
        _startGameBtn.onClick.AddListener(OnClickStartGame);
        _leaveRoomBtn.onClick.AddListener(OnClickLeaveRoom);
        _gameExitButton.onClick.AddListener(ExitGame);

        NetworkManager.Instance.OnRoomListUpdateAction += UpdateRoomList;
        NetworkManager.Instance.OnJoinRoomSuccessAction += ShowRoomPanel;

        NetworkManager.Instance.OnPlayerListUpdateAction += UpdateRoomUI; // 입장/ 퇴장
        NetworkManager.Instance.OnPlayerPropertiesUpdateAction += UpdateRoomUI; // 자리 변경

        openRankingBtn.onClick.AddListener(OnClickOpenRanking);
        closeRankingBtn.onClick.AddListener(() => rankingPanel.SetActive(false));

        if(_notificationPanel != null)
        {
            _notificationPanel.alpha = 0;
            _notificationPanel.gameObject.SetActive(false);
        }

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
        string roomName = _roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            ShowNotification("방 제목을 입력하고 버튼을 눌러주세요!");
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
                if (_cachedRoomList.ContainsKey(info.Name))
                    _cachedRoomList.Remove(info.Name);
            }
            else
            {
                _cachedRoomList[info.Name] = info;
            }
        }

        foreach (Transform child in _roomListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var roomInfo in _cachedRoomList.Values)
        {
            GameObject entry = Instantiate(_roomItemPrefab, _roomListContent);

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
        _lobbyPanel.SetActive(true);
        _roomPanel.SetActive(false);
    }

    private void ShowRoomPanel()
    {
        _lobbyPanel.SetActive(false);
        _roomPanel.SetActive(true);
        UpdateRoomUI();
    }

    private void UpdateRoomUI()
    {
        if(!PhotonNetwork.InRoom) return;
        _roomTitleText.text = $"Room: {PhotonNetwork.CurrentRoom.Name}";

        foreach (var txt in _relativeSeatTexts)
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
                    if(_relativeSeatTexts.Length > 0) _relativeSeatTexts[0].text = player.NickName;
                    break;
                case 2:
                    if(_relativeSeatTexts.Length > 1) _relativeSeatTexts[1].text = player.NickName;
                    break;
                case 3:
                    if(_relativeSeatTexts.Length > 2) _relativeSeatTexts[2].text = player.NickName;
                    break;
            }
        }

        string playerList = "";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerList += $"{player.NickName}\n";
        }
        _playerListText.text = playerList;

        _startGameBtn.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        _startGameBtn.interactable = PhotonNetwork.CurrentRoom.PlayerCount == 4; 
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

    private void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void ShowNotification(string message)
    {
        if (_notificationPanel == null) return;

        _notificationPanel.DOKill();
        _notificationText.text = message;
        _notificationPanel.gameObject.SetActive(true);
        _notificationPanel.alpha = 0;

        Sequence seq = DOTween.Sequence();
        seq.Append(_notificationPanel.DOFade(1.0f, 0.3f));
        seq.AppendInterval(1.5f);
        seq.Append(_notificationPanel.DOFade(0.0f, 0.5f));
        seq.OnComplete(() => _notificationPanel.gameObject.SetActive(false));
    }


}
