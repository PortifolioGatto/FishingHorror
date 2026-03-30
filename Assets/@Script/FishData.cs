using UnityEngine;
using UnityEngine.Localization;

public enum Oceans
{
    PACIFIC,
    ATLANTIC,
    INDIAN,
    SOUTHERN,
    ARCTIC
}

[CreateAssetMenu(fileName = "FishData", menuName = "ScriptableObjects/FishData", order = 1)]
public class FishData : ScriptableObject
{
    public LocalizedString fishName;
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


    [Header("Appearance")]

    public Oceans[] oceans;

    public float maxSizeCentimeters;
    public float maxWeightGrams;

    public int CalculatePrice(float size)
    {
        float sizeMultiplier = size / maxSizeVariation;
        return Mathf.RoundToInt(maxPrice * sizeMultiplier);
    }

    public float CalculateWeight(float size)
    {
        float sizeMultiplier = size / maxSizeVariation;

        return (maxWeightGrams * sizeMultiplier);
    }

    public float CalculateSize(float size)
    {
        float sizeMultiplier = size / maxSizeVariation;

        return (maxSizeCentimeters * sizeMultiplier);
    }

    public string GetOceans()
    {
        if (oceans == null) return "";

        return string.Join(", ", oceans);
    }

}
