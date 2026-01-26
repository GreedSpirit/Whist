using TMPro;
using UnityEngine;

public class Ranking : MonoBehaviour
{
    public TextMeshProUGUI rankText;    
    public TextMeshProUGUI nameText;    
    public TextMeshProUGUI recordText;
    public TextMeshProUGUI rateText;

    public void Setup(int rank, string nickname, int wins, int loses)
    {
        rankText.text = $"{rank}";
        nameText.text = nickname;
        recordText.text = $"{wins}W {loses}L";

        float total = wins + loses;
        float rate = (total == 0) ? 0 : (wins / total) * 100f;
        
        rateText.text = $"{rate:F1}%"; // 소수점 1자리까지
    }   
}
