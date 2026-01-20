using System.Collections.Generic;
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
}
