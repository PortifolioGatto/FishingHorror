using UnityEngine;

[CreateAssetMenu(fileName = "FishData", menuName = "ScriptableObjects/FishData", order = 1)]
public class FishData : ScriptableObject
{
    public string fishName;
    public GameObject fishPrefab;
    public float fishBaseDifficulty;

    public int minTriesToCatch = 1;
    public int maxTriesToCatch = 3;

    public float minSizeVariation = 1f;
    public float maxSizeVariation = 1f;

    public int maxPrice = 100;

    public int sizeInBox = 1;

    public float visualHeightOffset = 0f;

    public bool canJump = false;
    public bool canBite = true;

    public int CalculatePrice(float size)
    {
        float sizeMultiplier = size / maxSizeVariation;
        return Mathf.RoundToInt(maxPrice * sizeMultiplier);
    }
}
