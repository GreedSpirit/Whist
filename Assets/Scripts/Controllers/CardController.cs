
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


    private bool isInteractable = true;
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
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

        //DOTween으로 0.2초 동안 1.2배 커지는 코드
        transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutBack);

        transform.SetAsLastSibling(); // z축 정렬을 앞으로 당겨서 다른 카드에 가려지지 않게 하기(만약 layoutgroup 쓰면 안됨 저번 프로젝트에서 배운대로)
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isMine) return;

        transform.DOScale(originalScale, 0.2f);
    }
}
