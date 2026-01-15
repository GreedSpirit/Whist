using System;
using System.Collections;
using System.Collections.Generic;
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
}
