using UnityEngine;

public class FishInstance : MonoBehaviour
{
    public FishData fishData;

    public float size;
    public int price;

    public void Initialize(FishData data)
    {
        fishData = data;

        size = UnityEngine.Random.Range(data.minSizeVariation, data.maxSizeVariation);
        price = data.CalculatePrice(size);

        GameObject fishPrefab = Instantiate(fishData.fishPrefab, transform.position, Quaternion.identity, transform);

        fishPrefab.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

        if (fishPrefab.TryGetComponent(out Rigidbody rb))
        {
            Destroy(rb);
        }

        if(fishPrefab.TryGetComponent(out Collider col))
        {
            Destroy(col);
        }

        fishPrefab.transform.localScale = Vector3.one * size;
    }
}