using UnityEngine;

[CreateAssetMenu(fileName = "NewCardDatabase", menuName = "Whist/CardDatabase")]
public class CardDatabaseSO : ScriptableObject
{
    [Tooltip("Spade(2~A) -> Diamond -> Heart -> Club")]
    public Sprite[] cardSprites; 

    public Sprite cardBackSprite; // 뒷면 이미지

    public Sprite GetCardSprite(int id)
    {
        if (id < 0 || id >= cardSprites.Length)
        {
            Debug.LogError($"잘못된 카드 ID : {id}");
            return null;
        }
        return cardSprites[id];
    }
}