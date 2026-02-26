using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class FishingSpot : MonoBehaviour
{
    [SerializeField] private FishData[] fishData;
    [SerializeField] private float fishingAreaSize;
    [SerializeField] private ParticleSystem effect;


    [SerializeField] public int amountOfFishToCatch = 3;

    [SerializeField] private int maxVisualFishes = 5;
    [SerializeField] private float visualFishSpawnIntervalMin = 5f;
    [SerializeField] private float visualFishSpawnIntervalMax = 5f;

    private bool canSpawnVisualFish = true;

    private float visualFishSpawnTimer;

    private List<GameObject> visualFishes = new List<GameObject>();

    private SphereCollider fishingArea;

    public UnityEvent<FishData> OnFishCaught;
    public UnityEvent OnSpotIsTargeted;

    private void Awake()
    {
        fishingArea = GetComponent<SphereCollider>();
    }

    private void OnValidate()
    {
        if (fishingArea == null)
        {
            fishingArea = GetComponent<SphereCollider>();
        }

        fishingArea.radius = fishingAreaSize;

        if(effect != null)
        {
            var shape = effect.shape;
            shape.radius = fishingAreaSize;
        }
    }


    
    private void Start()
    {
        FishingSpotSpawner.Instance.RegisterFishingSpot(this);
    }

    public void MoveSpot(float radius)
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;

        Vector3 newPosition = new Vector3(randomCircle.x, transform.position.y, randomCircle.y);

        transform.DOMove(newPosition, 8f).onComplete += () =>
        {
            visualFishSpawnTimer = Random.Range(visualFishSpawnIntervalMin, visualFishSpawnIntervalMax);
        };
    }


    public FishData GetRandomFish()
    {
        if (fishData.Length == 0)
            return null;
        int randomIndex = Random.Range(0, fishData.Length);
        return fishData[randomIndex];
    }

    private void Update()
    {
        if(visualFishes.Count < maxVisualFishes)
        {
            visualFishSpawnTimer -= Time.deltaTime;
            if(visualFishSpawnTimer <= 0f)
            {
                SpawnVisualFish(GetRandomFish());
                visualFishSpawnTimer = Random.Range(visualFishSpawnIntervalMin, visualFishSpawnIntervalMax);
            }
        }
    }
    
    public void SpawnVisualFish(FishData data)
    {
        if (!canSpawnVisualFish) return;

        for (int i = visualFishes.Count - 1; i >= 0; i--)
        {
            if (visualFishes[i] == null)
            {
                visualFishes.RemoveAt(i);
            }
        }

        if (visualFishes.Count >= maxVisualFishes) return;
        GameObject fish = Instantiate(data.fishPrefab, transform.position + Vector3.down * 5, Quaternion.identity);

        fish.GetComponent<WorldFish>().Initialize(data);

        StartCoroutine(VisualFishMovement(fish, data));

        visualFishes.Add(fish);
    }
    private IEnumerator VisualFishMovement(GameObject fish, FishData data)
    {
        Vector3 randomPoint = transform.position + new Vector3(Random.Range(-fishingAreaSize, fishingAreaSize), data.visualHeightOffset, Random.Range(-fishingAreaSize, fishingAreaSize));

        float duration = Random.Range(3f, 6f);
        int maxStepsTillGetOut = Random.Range(3, 10);
        int curStep = 0;

        Vector3 direction = (randomPoint - fish.transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        float jumpChance = 0.33f;

        while(Vector3.Distance(fish.transform.position, randomPoint) >= 1f)
        {
            float t = Time.deltaTime / duration;
            Vector3 newPos = Vector3.Lerp(fish.transform.position, randomPoint, t);
            fish.transform.position = newPos;
            fish.transform.rotation = Quaternion.Slerp(fish.transform.rotation, targetRotation, t * 33f);
            yield return null;
        }

        bool fishAlreadyJumped = false;

        WorldFish worldFish = fish.GetComponent<WorldFish>();


        while (curStep < maxStepsTillGetOut)
        {
            if(fish == null) yield break;
            if(worldFish == null) yield break;

            while (worldFish.State != WorldFish.FishState.Swimming)
            {
                yield return null;
                continue;
            }

            if (fish == null) yield break;
            if(Random.value < jumpChance && !fishAlreadyJumped && worldFish.fishData.canJump)
            {
                worldFish.Jump();

                while(worldFish.IsJumping)
                {
                    yield return null;
                }

                fishAlreadyJumped = true;
            }
            else
            {
                randomPoint = transform.position + new Vector3(Random.Range(-fishingAreaSize, fishingAreaSize), data.visualHeightOffset, Random.Range(-fishingAreaSize, fishingAreaSize));

                direction = (randomPoint - fish.transform.position).normalized;
                targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                duration = Random.Range(3f, 6f);

                if(fish == null) yield break;


                while (Vector3.Distance(fish.transform.position, randomPoint) >= 1f)
                {
                    float t = Time.deltaTime / duration;
                    Vector3 newPos = Vector3.Lerp(fish.transform.position, randomPoint, t);
                    fish.transform.position = newPos;
                    fish.transform.rotation = Quaternion.Slerp(fish.transform.rotation, targetRotation, t * 33f);
                    yield return null;
                }

                
                curStep++;
            }

            yield return null;
        }


        randomPoint = transform.position + new Vector3(Random.Range(-fishingAreaSize, fishingAreaSize), -5f, Random.Range(-fishingAreaSize, fishingAreaSize)) * 2f;
        targetRotation = Quaternion.LookRotation(randomPoint - fish.transform.position, Vector3.up);


        while (fish != null && Vector3.Distance(fish.transform.position, randomPoint) > 1f)
        {
            float t = Time.deltaTime / duration;
            Vector3 newPos = Vector3.Lerp(fish.transform.position, randomPoint, t);
            fish.transform.position = newPos;
            fish.transform.rotation = Quaternion.Slerp(fish.transform.rotation, targetRotation, t * 33f);

            yield return null;
        }

        if (fish != null)
        {
            visualFishes.Remove(fish);
            Destroy(fish);
        }

    }

    public void CatchFish(FishData data)
    {
        OnFishCaught?.Invoke(data);
    }

    public void DisposeSpot()
    {
        StopAllCoroutines();

        canSpawnVisualFish = false;

        for (int i = visualFishes.Count - 1; i >= 0; i--)
        {
            if (visualFishes[i] != null)
            {
                int index = i;

                visualFishes[index].transform.DOMove(transform.position + Vector3.down * 10f, 2f).onComplete += () =>
                {
                    if (visualFishes[index] != null)
                    {
                        Destroy(visualFishes[index]);
                    }
                };

                Quaternion targetRotation = Quaternion.Euler(90f, visualFishes[index].transform.rotation.eulerAngles.y, visualFishes[index].transform.rotation.eulerAngles.z);

                visualFishes[index].transform.DORotate(targetRotation.eulerAngles, 2f);

            }
        }

        Destroy(gameObject, 6f);
    }
}
