using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

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
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    private List<GameObject> myHandObjects = new List<GameObject>();

    void Awake()
    {
        Instance = this;
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
                "나의 턴!" : $"Player {seatNum}의 턴";
    }

    public void ShowResultPanel(string msg)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            if (resultText != null) resultText.text = msg;
        }
    }
}
