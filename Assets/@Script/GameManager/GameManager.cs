using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int currentMoney = 0;

    [Header("Game Settings")]
    public string[] gameScenesInOrder;

    [Header("Day Settings")]
    public int currentDay = 1;
    public int totalDays = 3;

    [Header("End Day Settings")]
    public string endDaySceneName = "Store";
    public SerializedFishData[] caughtFishData = new SerializedFishData[0];

    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGame()
    {
        currentMoney = 0;
        currentDay = 1;

        caughtFishData = new SerializedFishData[0];
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
    }
    public void RemoveMoney(int amount)
    {
        currentMoney -= amount;
    }

    public void EndDay()
    {
        if (PlayerFishingSystem.Instance == null) return;


        caughtFishData = PlayerFishingSystem.Instance.GetSerializedFish();

        UnityEngine.SceneManagement.SceneManager.LoadScene(endDaySceneName);
    }

    public void NextDay()
    {
        currentDay++;

        int nextSceneIndex = currentDay - 1;

        nextSceneIndex = Mathf.Clamp(nextSceneIndex, 0, gameScenesInOrder.Length - 1);


        if (currentDay > totalDays)
        {
            Debug.Log("Game Over! Final Money: " + currentMoney);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(gameScenesInOrder[nextSceneIndex]);
    }
}

[System.Serializable]
public class SerializedFishData
{
    public FishData fishData;
    public float size;
}
