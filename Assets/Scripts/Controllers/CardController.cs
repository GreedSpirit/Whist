
using DG.Tweening;
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

    private bool isInteractable = true;
    private Vector3 originalScale;



    void Awake()
    {
        originalScale = visualTransform.localScale;
    }

    public void Setup(int id, Sprite faceSprite, bool isMyCard)
    {
        this.cardId = id;
        this.cardImage.sprite = faceSprite;
        this.isMine = isMyCard;

        //todo 내 카드가 아니면 뒷면을 보여주거나 아예 안보여주기
        if (backImage != null) backImage.gameObject.SetActive(!isMyCard);
    }

    public void SetInteractable(bool state)
    {
        isInteractable = state;
        
        cardImage.color = state ? Color.white : Color.gray;
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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isMine) return;

        visualTransform.DOScale(originalScale, 0.2f);        
    }
}
