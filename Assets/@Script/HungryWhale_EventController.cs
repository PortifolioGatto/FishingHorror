using Unity.VisualScripting;
using UnityEngine;

public class HungryWhale_EventController : EventController
{
    [SerializeField] private GameObject hungryWhale;
    [SerializeField] private float spawnRadius = 50f;

    private Transform boatTransform;

    private void Awake()
    {
        
    }

    private void Start()
    {
        boatTransform = GameObject.FindGameObjectWithTag("Boat").transform;
    }

    [ContextMenu("Spawn Hungry Whale")]
    public override void StartEvent()
    {
        Vector3 spawnPosition = boatTransform.position + Random.insideUnitSphere * spawnRadius;

        spawnPosition.y = hungryWhale.transform.position.y;

        //Rotate whale to face the boat at y axis
        Vector3 directionToBoat = boatTransform.position - spawnPosition;
        directionToBoat.y = 0; // Keep only the horizontal direction
        Quaternion rotationToBoat = Quaternion.LookRotation(directionToBoat);


        GameObject hungryWhaleInstance = Instantiate(hungryWhale, spawnPosition, rotationToBoat);

        Destroy(hungryWhaleInstance, 30f);
    }

    public override void UpdateEvent()
    {
        
    }

    public override void EndEvent()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.purple;
        if(boatTransform != null)
        {
            Gizmos.DrawWireSphere(boatTransform.position, spawnRadius);
        }
    }
}
