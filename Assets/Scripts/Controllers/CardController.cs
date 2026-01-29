
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image cardImage;
    public Image backImage;

    public int cardId;
    public bool isMine = false;
    public Transform visualTransform;
    public GameObject highlightBorder;
    private Canvas cardCanvas;
    private bool isInteractable = true;
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = visualTransform.localScale;
        cardCanvas = GetComponent<Canvas>();
    }

    public void Setup(int id, Sprite faceSprite, bool isMyCard)
    {
        this.cardId = id;
        this.cardImage.sprite = faceSprite;
        this.isMine = isMyCard;

        if (backImage != null) backImage.gameObject.SetActive(!isMyCard);
    }

    public void SetInteractable(bool state)
    {
        isInteractable = state;
        
        cardImage.color = state ? Color.white : Color.gray;
    }

    public void SetPlayableState(bool isPlayable)
    {
        if(highlightBorder != null)
        {
            highlightBorder.SetActive(isPlayable);
        }

        if(cardImage != null)
        {
            cardImage.color = isPlayable ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        SetInteractable(isPlayable);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isMine || !isInteractable) return;

        GameManager.Instance.TryPlayCard(cardId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!isMine || !isInteractable) return;

        visualTransform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutBack);

        if (cardCanvas != null)
        {
        cardCanvas.overrideSorting = true;
        cardCanvas.sortingOrder = 10;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isMine) return;

        visualTransform.DOScale(originalScale, 0.2f);     
        if (cardCanvas != null)
        {
        cardCanvas.overrideSorting = false;
        cardCanvas.sortingOrder = 0;
        }   
    }

    public void OnMoveToTable()
    {
        if(highlightBorder != null) highlightBorder.SetActive(false);

        isInteractable = false;

        if(cardImage != null)
        {
            cardImage.color = Color.white;
        }
    }
}
