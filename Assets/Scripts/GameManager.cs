using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public enum CardSuit { Spade, Diamond, Heart, club }
public enum GameState { Ready, Dealing, Playing, Result }
 
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Ready;
    public int currentTurnSeat = -1;
    public CardSuit currentLeadSuit; // 이번 턴의 으뜸 문양
    public CardSuit currentTrumpSuit; // 이번 게임의 으뜸 문양
    public bool isFirstCardOfTrick = true;

    [Header("Table")]
    public int[] fieldCards = new int[4]; // 인덱스를 의자 번호와 일치시켜서 판정시킬 예정
    public int cardsPlayedCount = 0; // 테이블 위의 카드가 몇개 나왔는지 나타내는 변수
    public int currentTrickNumber = 0; // 몇 턴째인지 나타내는 변수

    public int[] teamTricks = new int[2];

    public int mySeatNum;
    public List<int> myHand = new List<int>();

    private void Awake()
    {
        Instance = this;
        mySeatNum = (int)PhotonNetwork.LocalPlayer.CustomProperties["SeatNum"];

        for(int i = 0; i < 4; i++)
        {
            fieldCards[i] = -1;
        }
    }

    private void Start()
    {
        // 마스터 클라이언트한테 권한?을 몰아줌으로써 동기화에 유리하게 만들기
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitAndStartGame());
        }
    }

    private IEnumerator WaitAndStartGame()
    {
        yield return new WaitForSeconds(1.5f);

        //여기서 덱 만들고 섞기
        int[] deck = CreateShuffledDeck();

        int trumpVal = UnityEngine.Random.Range(0, 4);

        //여기서 게임 시작 로직 RPC로 호출
        photonView.RPC(nameof(RPC_StartGame), RpcTarget.All, deck, trumpVal);
    }

    // RPC -----------------------------------------------------------------------
    
    [PunRPC]
    private void RPC_StartGame(int[] shuffledDeck, int trumpVal)
    {
        currentState = GameState.Playing;
        currentTrumpSuit = (CardSuit)trumpVal;
        currentTrickNumber = 1;
        teamTricks[0] = 0;
        teamTricks[1] = 0;
        string[] playerNames = new string[4];
        foreach(Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("SeatNum"))
            {
                int seat = (int)p.CustomProperties["SeatNum"];
                if(seat >= 0 && seat < 4)
                {
                    playerNames[seat] = p.NickName;
                }
            }
        }

        Debug.Log($"1. 게임 시작. 으뜸 문양 -> {currentTrumpSuit}");

        myHand.Clear();
        int startIndex = mySeatNum * 13; //13장씩 나눠가질 예정
        for(int i = 0; i < 13; i++)
        {
            myHand.Add(shuffledDeck[startIndex + i]);
        }

        UIManager.Instance.UpdateHandUI(myHand);
        UIManager.Instance.SetupGameInfo((CardSuit)trumpVal, playerNames[0], playerNames[1], playerNames[2], playerNames[3]);
        UIManager.Instance.RefreshHandInteractivity();
        
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_ChangeTurn), RpcTarget.All, 0, true);
        }
    }

    [PunRPC]
    private void RPC_ChangeTurn(int nextSeat, bool isNewTrick)
    {
        currentTurnSeat = nextSeat;

        if (isNewTrick)
        {
            isFirstCardOfTrick = true;
            cardsPlayedCount = 0;

            for(int i = 0; i < 4; i++)
            {
                fieldCards[i] = -1;
            }
            UIManager.Instance.CleanTable();
            UIManager.Instance.ResetLeadSuit();
        }

        UIManager.Instance.UpdateTurnText(nextSeat);
        UIManager.Instance.RefreshHandInteractivity();
        Debug.Log("Turn Changed : " + nextSeat);
    }

    [PunRPC]
    private void RPC_PlayCard(int seatNum, int cardId)
    {
        fieldCards[seatNum] = cardId;
        cardsPlayedCount++;

        if(seatNum == mySeatNum)
        {
            myHand.Remove(cardId);
        }

        if (isFirstCardOfTrick)
        {
            currentLeadSuit = GetSuit(cardId);
            UIManager.Instance.UpdateLeadSuit(currentLeadSuit);
            isFirstCardOfTrick = false;
        }

        UIManager.Instance.ShowCardOnTable(seatNum, cardId);

        // 마스터 클라이언트가 턴 종료 여부 판단
        if (PhotonNetwork.IsMasterClient)
        {
            if (cardsPlayedCount == 4)
            {
                StartCoroutine(ProcessTrickResult());
            }
            else
            {
                // 다음 사람 턴 (0 -> 1 -> 2 -> 3 -> 0)
                int nextSeat = (seatNum + 1) % 4;
                photonView.RPC(nameof(RPC_ChangeTurn), RpcTarget.All, nextSeat, false);
            }
        }
    }

    [PunRPC]
    void RPC_TrickEnd(int winnerSeat, int winningTeam)
    {
        teamTricks[winningTeam]++;

        UIManager.Instance.UpdateScoreUI(teamTricks[0], teamTricks[1]);

        Debug.Log($"트릭 승자: Player {winnerSeat} (Team {winningTeam})");
    }

    [PunRPC]
    void RPC_GameSet(int score0, int score1, string msg)
    {
        currentState = GameState.Result;
        Debug.Log($"게임 종료 : {msg}, {score0} : {score1}");

        UIManager.Instance.ShowResultPanel(msg);
    }

    // RPC Method 종료 ----------------------------------------------------------------

    private IEnumerator ProcessTrickResult()
    {
        yield return new WaitForSeconds(2.0f); // 연출 및 동기화 시간 확보

        int winnerSeat = CalculateTrickWinner();
        int winningTeam = winnerSeat % 2;

        photonView.RPC(nameof(RPC_TrickEnd), RpcTarget.All, winnerSeat, winningTeam);

        yield return new WaitForSeconds(1.0f);

        if(currentTrickNumber >= 13)
        {
            EndGame();
        }
        else
        {
            currentTrickNumber++;
            photonView.RPC(nameof(RPC_ChangeTurn), RpcTarget.All, winnerSeat, true);   
        }
    }

    private int CalculateTrickWinner()
    {
        int winner = -1;
        int highestRank = -1;
        bool trumpPlayed = false;

        for(int i = 0; i < 4; i++)
        {
            int cardId = fieldCards[i];
            CardSuit suit = GetSuit(cardId);
            int rank = GetRank(cardId); // A = 12, 2 = 0;

            bool isTrump = (suit == currentTrumpSuit);
            bool isLead = (suit == currentLeadSuit);

            // 현재 최고 카드가 트럼프이고 지금 카드도 트럼프면 랭크 비교
            if (trumpPlayed && isTrump)
            {
                if (rank > highestRank)
                {
                    highestRank = rank;
                    winner = i;
                }
            }
            // 현재 최고 카드가 트럼프가 아닌데 (리드 슈트 상태) 내가 트럼프를 낸 경우
            else if (!trumpPlayed && isTrump)
            {
                trumpPlayed = true;
                highestRank = rank;
                winner = i;
            }
            // 트럼프가 아직 안 나왔고, 내가 리드 슈트를 내면 랭크 비교
            else if (!trumpPlayed && isLead)
            {
                if (rank > highestRank)
                {
                    highestRank = rank;
                    winner = i;
                }
            }
            // 그 외(리드 슈트도 아니고 트럼프도 아님)는 승리 불가
        }
        return winner;
    }

    private int[] CreateShuffledDeck()
    {
        int[] deck = new int[52];
        for (int i = 0; i < 52; i++) deck[i] = i;
        
        for (int i = 0; i < deck.Length; i++)
        {
            int randomRank = UnityEngine.Random.Range(0, deck.Length);
            int temp = deck[i];
            deck[i] = deck[randomRank];
            deck[randomRank] = temp;
        }
        return deck;
    }

    //유저가 카드를 클릭했을 때 호출할 함수
    public void TryPlayCard(int cardId)
    {
        if(currentTurnSeat != mySeatNum) return;
        if(currentState != GameState.Playing) return;

        if (!IsValidPlay(cardId))
        {
            Debug.LogWarning("로직 확인, 낼 수 없는 카드");
            return;
        }

        photonView.RPC(nameof(RPC_PlayCard), RpcTarget.All, mySeatNum, cardId);
    }

    public bool IsValidPlay(int cardId)
    {   
        // 첫턴은 아무거나 괜찮음
        if(isFirstCardOfTrick) return true;

        CardSuit myCardSuit = GetSuit(cardId);

        if(myCardSuit == currentLeadSuit) return true;

        bool hasLeadSuit = myHand.Exists(id => GetSuit(id) == currentLeadSuit);
        if(hasLeadSuit) return false;

        return true;
    }

    void EndGame()
    {
        int scoreTeam0 = (teamTricks[0] > 6) ? teamTricks[0] - 6 : 0;
        int scoreTeam1 = (teamTricks[1] > 6) ? teamTricks[1] - 6 : 0;

        string resultMsg = "";
        if (scoreTeam0 > scoreTeam1) resultMsg = "Team 0 승리!";
        else if (scoreTeam1 > scoreTeam0) resultMsg = "Team 1 승리!";
        else resultMsg = "무승부";

        // 결과 RPC 전송
        photonView.RPC(nameof(RPC_GameSet), RpcTarget.All, scoreTeam0, scoreTeam1, resultMsg);

        int myTeam = (mySeatNum % 2);
        bool isMyTeamWin = false;

        if (myTeam == 0 && scoreTeam0 > scoreTeam1) isMyTeamWin = true;
        else if (myTeam == 1 && scoreTeam1 > scoreTeam0) isMyTeamWin = true;
        
        if (scoreTeam0 != scoreTeam1)
        {
            AuthManager.Instance.UpdatePlayerStats(isMyTeamWin);
        }
    }

    public CardSuit GetSuit(int cardId)
    {
        return (CardSuit)(cardId /13);
    }

    public int GetRank(int cardId)
    {
        return cardId %13;
    }

}
