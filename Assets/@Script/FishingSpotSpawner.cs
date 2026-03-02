using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class FishingSpotSpawner : MonoBehaviour
{
    [SerializeField] private FishingSpot fishingSpotPrefab;
    [SerializeField] private float spawnInterval = 30f;
    [SerializeField] private int spawnAmount = 1;
    [SerializeField] private float fishingSpotDuration = 180f;

    [SerializeField] private float minTimeToMoveSpot = 30f;
    [SerializeField] private float maxTimeToMoveSpot = 60f;
    [SerializeField] private float moveDistance = 10f;

    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private Vector2 spawnAreaMin;
    [SerializeField] private Vector2 spawnAreaMax;

    [SerializeField]
    private bool randomSpawningEnabled = false;

    private float spawnTimer;
    private bool anySpotExpired;
    private bool anySpotCanMove;

    [SerializeField]
    private List<SpawnedFishingSpot> spawnedFishingSpots;

    public static FishingSpotSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        spawnedFishingSpots = new List<SpawnedFishingSpot>();
    }

    private void Update()
    {
        if (!randomSpawningEnabled) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            for (int i = 0; i < spawnAmount; i++)
            {
                SpawnFishingSpot();
            }

            spawnTimer = spawnInterval;
        }

        anySpotExpired = false;
        anySpotCanMove = false;

        for (int i = spawnedFishingSpots.Count - 1; i >= 0; i--)
        {
            SpawnedFishingSpot spot = spawnedFishingSpots[i];
            spot.duration -= Time.deltaTime;
            spot.moveTimer -= Time.deltaTime;

            if (spot.duration <= 0f)
            {
                anySpotExpired = true;
            }

            if (spot.moveTimer <= 0f)
            {
                anySpotCanMove = true;
            }

        }

        if (anySpotExpired)
        {
            for (int i = spawnedFishingSpots.Count - 1; i >= 0; i--)
            {
                SpawnedFishingSpot spot = spawnedFishingSpots[i];
                if (spot.duration <= 0f)
                {
                    DisposeFishingSpot(spot);
                }
            }
        }

        if (anySpotCanMove)
        {
            for (int i = spawnedFishingSpots.Count - 1; i >= 0; i--)
            {
                SpawnedFishingSpot spot = spawnedFishingSpots[i];

                if(spot.fishingSpot == null)
                {
                    continue;
                }

                if (spot.moveTimer <= 0f)
                {
                    Vector3 newPosition = spot.fishingSpot.transform.position + new Vector3(Random.Range(-moveDistance, moveDistance), 0f, Random.Range(-moveDistance, moveDistance));
                    newPosition.x = Mathf.Clamp(newPosition.x, spawnAreaMin.x, spawnAreaMax.x);
                    newPosition.z = Mathf.Clamp(newPosition.z, spawnAreaMin.y, spawnAreaMax.y);
                    spot.moveTimer = Random.Range(minTimeToMoveSpot, maxTimeToMoveSpot);

                    ForceSmoothMove(spot, newPosition, 1f);

                }
            }
        }
    }

    public void EnableRandomSpawning(bool enabled)
    {
        randomSpawningEnabled = enabled;
        if (!enabled)
        {
            spawnTimer = 0f; // Reset timer when disabling
        }
    }

    public void SpawnFishingSpotAtMap()
    {
        for (int i = 0; i < 50; i++)
        {
            SpawnFishingSpot();
        }
    }

    [ContextMenu("Run To Player")]
    public void RunToPlayer()
    {
        for (int i = spawnedFishingSpots.Count - 1; i >= 0; i--)
            {
                SpawnedFishingSpot spot = spawnedFishingSpots[i];
                Vector3 playerPosition = PlayerFishingSystem.Instance.transform.position + new Vector3(Random.Range(-20f, 20f), 0f, Random.Range(-20f,20f));
                Vector3 targetPosition = new Vector3(playerPosition.x, heightOffset, playerPosition.z);
                ForceSmoothMove(spot, targetPosition, 5f);
        }
    }

    private void SpawnFishingSpot()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();
        FishingSpot newFishingSpot = Instantiate(fishingSpotPrefab, spawnPosition, Quaternion.identity);


        RegisterFishingSpot(newFishingSpot);
    }

    public void RegisterFishingSpot(FishingSpot spot)
    {
        if(spawnedFishingSpots.Exists(s => s.fishingSpot == spot))
        {
            return; // Spot already registered
        }

        SpawnedFishingSpot spawnedSpot = new SpawnedFishingSpot
        {
            fishingSpot = spot,
            availableFishCount = spot.amountOfFishToCatch,
            duration = fishingSpotDuration,
            moveTimer = Random.Range(minTimeToMoveSpot, maxTimeToMoveSpot)
        };

        spot.OnFishCaught.AddListener((fishData) =>
        {
            spawnedSpot.availableFishCount--;
            
            Debug.Log($"Fish caught at spot! Remaining fish: {spawnedSpot.availableFishCount}");

            if (spawnedSpot.availableFishCount <= 0)
            {
                if(spawnedSpot.fishingSpot != null)
                    spawnedSpot.fishingSpot.DisposeSpot();
            }
        });

        spawnedFishingSpots.Add(spawnedSpot);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float z = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        return new Vector3(x, heightOffset, z); // Assuming y is 0 for water level
    }

    public void ForceSmoothMove(SpawnedFishingSpot spot, Vector3 targetPosition, float duration)
    {
        if(spot.fishingSpot == null)
        {
            return;
        }
        StartCoroutine(SmoothMove(spot.fishingSpot.transform, targetPosition, duration));
    }

    private System.Collections.IEnumerator SmoothMove(Transform objTransform, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = objTransform.position;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            objTransform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        objTransform.position = targetPosition;
    }

    private void DisposeFishingSpot(SpawnedFishingSpot fishingSpot)
    {
        spawnedFishingSpots.Remove(fishingSpot);
        fishingSpot.fishingSpot.DisposeSpot();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 center = new Vector3((spawnAreaMin.x + spawnAreaMax.x) / 2f, heightOffset, (spawnAreaMin.y + spawnAreaMax.y) / 2f);
        Vector3 size = new Vector3(spawnAreaMax.x - spawnAreaMin.x, 0.1f, spawnAreaMax.y - spawnAreaMin.y);
        Gizmos.DrawWireCube(center, size);
    }

    [System.Serializable]
    public class SpawnedFishingSpot
    {
        public FishingSpot fishingSpot;
        public int availableFishCount;
        public float duration;
        public float moveTimer;
    }
}
