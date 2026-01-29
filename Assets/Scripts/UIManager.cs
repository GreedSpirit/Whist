using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Data")]
    public CardDatabaseSO cardDatabase;
    public GameObject cardPrefab;

    [Header("Container")]
    public Transform myHandContainer;
    public Transform[] tableSlots;
    public Transform[] spawnPoints;

    [Header("UI")]
    public TextMeshProUGUI turnInfoText;
    public TextMeshProUGUI trumpSuitText;
    public TextMeshProUGUI leadSuitText;
    public TextMeshProUGUI team0InfoText; // "Nick1 & Nick2 : 0"
    public TextMeshProUGUI team1InfoText; // "Nick3 & Nick4 : 0"
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button backToLobbyBtn;
    public TextMeshProUGUI notificationText;

    private string team0Names;
    private string team1Names;
    private List<GameObject> myHandObjects = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        backToLobbyBtn.onClick.AddListener(OnClickBackToLobby);
    }

    // 손패 만들기
    public void UpdateHandUI(List<int> handIds)
    {
        foreach(var obj in myHandObjects)
        {
            Destroy(obj);
        }
        myHandObjects.Clear();

        foreach(int id in handIds)
        {
            GameObject card = Instantiate(cardPrefab, myHandContainer);
            CardController cardctrl = card.GetComponent<CardController>();
            
            Sprite sprite = cardDatabase.GetCardSprite(id);
            cardctrl.Setup(id, sprite, true); // true = 내 카드

            myHandObjects.Add(card);
        }
    }

    // 카드 내기
    public void ShowCardOnTable(int seatNum, int cardId)
    {
        GameObject cardObj = null;

        // 내가 낸 경우
        if (seatNum == GameManager.Instance.mySeatNum)
        {
            cardObj = myHandObjects.Find(x => x.GetComponent<CardController>().cardId == cardId);
            
            if (cardObj != null)
            {
                myHandObjects.Remove(cardObj);
                cardObj.transform.SetParent(transform);
            }
        }
        // 다른 유저가 낸 경우
        else
        {
            cardObj = Instantiate(cardPrefab, transform);
            
            Sprite sprite = cardDatabase.GetCardSprite(cardId);
            cardObj.GetComponent<CardController>().Setup(cardId, sprite, false);

            if(spawnPoints.Length > seatNum) 
                cardObj.transform.position = spawnPoints[seatNum].position;
        }

        if (cardObj != null)
        {
            cardObj.GetComponent<CardController>().OnMoveToTable();
            Transform targetTransform = tableSlots[seatNum];
            
            cardObj.transform.DOMove(targetTransform.position, 0.5f).SetEase(Ease.OutBack);
            cardObj.transform.DORotate(Vector3.zero, 0.5f);
            cardObj.transform.DOScale(1.0f, 0.5f);
            
            cardObj.transform.SetParent(targetTransform); 
        }
    }

    public void CleanTable()
    {
        foreach(Transform slot in tableSlots)
        {
            foreach(Transform child in slot)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void UpdateTurnText(int seatNum)
    {
        if (turnInfoText != null)
            turnInfoText.text = (seatNum == GameManager.Instance.mySeatNum) ? 
                "나의 턴!" : $"{GameManager.Instance.GetPlayerNameBySeat(seatNum)}({seatNum + 1})의 턴";
    }

    public void ShowResultPanel(string msg)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            if (resultText != null) resultText.text = msg;
        }
    }

    public void SetupGameInfo(CardSuit trumpSuit, string p0Name, string p1Name, string p2Name, string p3Name)
    {
        trumpSuitText.text = "Trump Suit : " + SetSuit(trumpSuit);
        leadSuitText.text = "Lead Suit : ?";

        team0Names = $"{p0Name} & {p2Name}";
        team1Names = $"{p1Name} & {p3Name}";
        
        UpdateScoreUI(0, 0);
    }

    private string SetSuit(CardSuit suit)
    {
        switch (suit)
        {
            case CardSuit.Spade:
                return "<color=black>♠</color>";
            case CardSuit.Heart:
                return "<color=red>♥</color>";
            case CardSuit.Diamond:
                return "<color=red>♦</color>";
            default:
                return "<color=black>♣</color>";
        }
    }

    public void UpdateLeadSuit(CardSuit leadSuit)
    {
        leadSuitText.text = "Lead Suit : " + SetSuit(leadSuit);
    }

    public void ResetLeadSuit()
    {
        leadSuitText.text = "Lead Suit : ?";
    }

    public void UpdateScoreUI(int score0, int score1)
    {
        team0InfoText.text = $"<color=green>[Team 0]</color>\n {team0Names} : <b>{score0} Wins</b>";
        team1InfoText.text = $"<color=orange>[Team 1]</color>\n {team1Names} : <b>{score1} Wins</b>";
    }

    public void RefreshHandInteractivity()
    {
        if(GameManager.Instance.currentTurnSeat != GameManager.Instance.mySeatNum)
        {
            foreach(var obj in myHandObjects)
            {
                obj.GetComponent<CardController>().SetPlayableState(false);
            }
            return;
        }

        foreach(var obj in myHandObjects)
        {
            CardController card = obj.GetComponent<CardController>();
            card.SetPlayableState(GameManager.Instance.IsValidPlay(card.cardId));
        }
    }

    private void ShowPopupText(string msg, float duration = 2.0f)
    {
        // 기존 연출이 있다면 중단하고 초기화
        notificationText.DOKill();
        notificationText.transform.DOKill();

        notificationText.text = msg;
        notificationText.alpha = 0;
        notificationText.transform.localScale = Vector3.one * 0.5f; // 작아진 상태에서 시작
        notificationText.gameObject.SetActive(true);

        // DOTween 시퀀스 생성
        Sequence seq = DOTween.Sequence();

        // 등장
        seq.Append(notificationText.DOFade(1.0f, 0.3f));
        seq.Join(notificationText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack)); // 1.2배로 팡!

        // 대기
        seq.Append(notificationText.transform.DOScale(1.0f, 0.2f));
        seq.AppendInterval(duration);

        // 퇴장
        seq.Append(notificationText.DOFade(0.0f, 0.5f));
        seq.Join(notificationText.transform.DOLocalMoveY(100f, 0.5f).SetRelative()); // 위로 둥둥

        // 종료 후 원위치
        seq.OnComplete(() => {
            notificationText.transform.localPosition = Vector3.zero; // 위치 초기화
        });
    }

    // 턴 알림용
    public void ShowTurnNotification(string nickname)
    {
        ShowPopupText($"<color=yellow>{nickname}</color>의 턴!", 1.5f);
    }

    public void ShowTrickResult(string winnerName, string cardName, int winningTeam)
    {
        string teamColor = (winningTeam == 0) ? "green" : "orange";
        string msg = $"<b>{winnerName}</b> 승리! ({cardName})\n<color={teamColor}>Team {winningTeam} 득점!</color>";
        
        ShowPopupText(msg, 2.5f); // 결과는 좀 더 길게 보여줌
    }

    public void LockAllHand()
    {
        foreach(var obj in myHandObjects) 
             obj.GetComponent<CardController>().SetPlayableState(false);
    }

    private void OnClickBackToLobby()
    {
        backToLobbyBtn.interactable = false;

        NetworkManager.Instance.LeaveRoom();
    }
}
