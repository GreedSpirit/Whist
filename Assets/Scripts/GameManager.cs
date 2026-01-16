using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Photon.Pun;
using Unity.VisualScripting;
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

    [Header("Table")]
    public int[] fieldCards = new int[4]; // 인덱스를 의자 번호와 일치시켜서 판정시킬 예정
    public int cardsPlayedCount = 0;

    public int mySeatNum;
    public List<int> myHand = new List<int>();

    private void Awake()
    {
        Instance = this;
        mySeatNum = (int)PhotonNetwork.LocalPlayer.CustomProperties["SeatNum"];
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            
        }
    }

    private IEnumerator WaitAndStartGame()
    {
        //TODO waituntil로 모두의 로딩이 끝나는 것을 기다리게 만들 예정
        yield return new WaitForSeconds(2.0f);

        //여기서 덱 만들고 섞기
        fieldCards = CreateShuffledDeck();

        //여기서 게임 시작 로직 RPC로 호출
        photonView.RPC(nameof(RPC_StartGame), RpcTarget.All, 0);
    }

    // RPC -----------------------------------------------------------------------
    
    [PunRPC]
    private void RPC_StartGame(int[] shuffledDeck)
    {
        currentState = GameState.Playing;

        myHand.Clear();
        int startIndex = mySeatNum * 13; //13장씩 나눠가질 예정
        for(int i = 0; i < 13; i++)
        {
            myHand.Add(shuffledDeck[startIndex + i]);
        }

        //TODO : UI상에 카드 생성과 정렬 로직 호출
        // UIManager.Instance.SpawnCards(myHand);

        if (PhotonNetwork.IsMasterClient)
        {
            //TODO 0번부터 시작으로 일단 설정, 실제 규칙에서는 뭐였는지 확인 필요
            photonView.RPC(nameof(RPC_ChangeTurn), RpcTarget.All, 0);
        }
    }

    [PunRPC]
    private void RPC_ChangeTurn(int nextSeat)
    {
        currentTurnSeat = nextSeat;
        cardsPlayedCount = 0;
        Array.Clear(fieldCards, 0, fieldCards.Length);

        // TODO UI 갱신 (누구 턴인지, 테이블의 카드 지우기)
        Debug.Log("Turn Changed : " + nextSeat);
    }

    [PunRPC]
    private void RPC_PlayCard(int seatNum, int cardId)
    {
        fieldCards[seatNum] = cardId;
        cardsPlayedCount++;

        if(cardsPlayedCount == 1) // 첫번째 낸 카드가 으뜸 카드 문양
        {
            currentLeadSuit = (CardSuit)(cardId / 13);
        }

        //Todo 추후 UI 연출로 중앙에 카드가 가도록 혹은 낸 사람의 앞으로
        // UIManager.Instance.AnimateCardPlay(seatNum, cardId);

        // 마스터 클라이언트가 턴 종료 여부 판단
        if (PhotonNetwork.IsMasterClient)
        {
            if (cardsPlayedCount == 4)
            {
                StartCoroutine(ProcessTurnResult());
            }
            else
            {
                // 다음 사람 턴 (0 -> 1 -> 2 -> 3 -> 0)
                int nextTurn = (seatNum + 1) % 4;
                photonView.RPC("RPC_NextPlayer", RpcTarget.All, nextTurn);
            }
        }
    }










    // RPC Method 종료 ----------------------------------------------------------------

    private IEnumerator ProcessTurnResult()
    {
        yield return new WaitForSeconds(2.0f); // 연출 및 동기화 시간 확보

        int winnerSeat = CalculateWinner();

    }

    private int CalculateWinner()
    {
        int winner = -1;
        int maxRank = -1;

        for(int i = 0; i < 4; i++)
        {
            int cardId = fieldCards[i];
            CardSuit suit = (CardSuit)(cardId / 13);
            int rank = cardId % 13; // A = 12, 2 = 0;

            if(suit == currentLeadSuit)
            {
                if(rank > maxRank)
                {
                    maxRank = rank;
                    winner = i;
                }
            }
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
}
