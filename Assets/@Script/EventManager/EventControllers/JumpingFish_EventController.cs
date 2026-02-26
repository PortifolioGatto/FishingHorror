using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class JumpingFish_EventController : EventController
{
    [SerializeField] private float chanceToHappen = 0.5f;
    [SerializeField] private float minInterval;
    [SerializeField] private float maxInterval;

    [SerializeField] private FishData[] possibleFishes;

    [SerializeField] private float boatRadiusSpawn = 5f;
    [SerializeField] private float boatRadiusInside = 2f;
    [SerializeField] private float timeToHitBoat = 2f;

    private void Start()
    {
        if(possibleFishes.Length == 0)
        {
                Debug.LogWarning("No fishes assigned to JumpingFish_EventController.");
        }

        if(startOnStart)
            StartCoroutine(DelayedStart());
    }

    public void StartJumpingFishEvent()
    {
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        while(true)
        {
            float interval = Random.Range(minInterval, maxInterval);

            yield return new WaitForSeconds(interval);
            if(Random.value < chanceToHappen) 
                StartEvent();
        }
    }

    [ContextMenu("Start Jumping Fish Event")]
    public override void StartEvent()
    {
        FishData randomFish = possibleFishes[Random.Range(0, possibleFishes.Length)];

        BoatMovement boatMovement = FindAnyObjectByType<BoatMovement>();

        if (boatMovement == null) return;

        Transform boatTransform = boatMovement.transform;
        boatMovement = null;


        Vector3 startPoint = boatTransform.position + (Random.onUnitSphere * boatRadiusSpawn);
        startPoint.y = OceanManager.Instance.GetWaveHeight(boatTransform.position);


        Vector3 targetPoint = boatTransform.position + (Random.insideUnitSphere * boatRadiusInside);

        GameObject fishInstance = Instantiate(randomFish.fishPrefab, startPoint, Quaternion.identity);

        Vector3 forceToApply = CalculateForceToApply(startPoint, targetPoint, timeToHitBoat);

        WorldFish worldFish = fishInstance.GetComponent<WorldFish>();

        fishInstance.transform.SetParent(boatTransform);

        worldFish.Initialize(randomFish);
        worldFish.JumpTo(forceToApply);

    }

    public override void UpdateEvent()
    {
        
    }

    public override void EndEvent()
    {

    }

    private Vector3 CalculateForceToApply(Vector3 startPos, Vector3 endPos, float timeToHit)
    {
        Vector3 displacement = endPos - startPos;

        Vector3 displacementXZ = new Vector3(displacement.x, 0, displacement.z);

        float displacementY = displacement.y;

        Vector3 velocityXZ = displacementXZ / timeToHit;

        float gravity = Physics.gravity.y;

        float velocityY = (displacementY - 0.5f * gravity * timeToHit * timeToHit) / timeToHit;

        return velocityXZ + Vector3.up * velocityY;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            BoatMovement boatMovement = FindAnyObjectByType<BoatMovement>();
            if (boatMovement != null)
            {
                Transform boatTransform = boatMovement.transform;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(boatTransform.position, boatRadiusSpawn);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(boatTransform.position, boatRadiusInside);
            }
        }
    }
}
