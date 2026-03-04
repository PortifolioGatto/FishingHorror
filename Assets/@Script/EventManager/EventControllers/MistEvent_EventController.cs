using DG.Tweening;
using System.Collections;
using UnityEngine;
using VolumetricFogAndMist2;

public class MistEvent_EventController : EventController
{
    [SerializeField] private VolumetricFog mistFog;

    [Space]

    [SerializeField] private RadioDialogue dialogue;
    [Space]
    [SerializeField] private GameObject playerBoat;
    [Space]
    [SerializeField] private GameObject uncleBoat;
    [SerializeField] private GameObject destroyedBoat;

    [Header("Monster Circle Settings")]

    [SerializeField] private GameObject centerObj;
    [SerializeField] private GameObject monsterPrefab;

    [SerializeField] private float circleRadius = 5f;
    [SerializeField] private int circleSpotCount = 8;

    private GameObject[] spawnedMonsters;

    [ContextMenu("Start Mist Event")]
    public override void StartEvent()
    {
        StartCoroutine(EMistEvent());
    }

    public override void UpdateEvent() { }
    public override void EndEvent() { }

    private IEnumerator EMistEvent()
    {
        BoatMovement.Instance.respectLimit = false;

        mistFog.profile.albedo.a = 0f;

        float targetDensity = 1f;

        yield return null;

        float diff = Mathf.Abs(mistFog.profile.albedo.a - targetDensity);
        Debug.Log($"Increasing mist density... Current: {mistFog.profile.albedo.a}, Target: {targetDensity}, Diff: {diff}");

        while (diff > 0.1f)
        {
            diff = Mathf.Abs(mistFog.profile.albedo.a - targetDensity);
            mistFog.profile.albedo.a = Mathf.Lerp(mistFog.profile.albedo.a, targetDensity, Time.deltaTime );

            Debug.Log($"Increasing mist density... Current: {mistFog.profile.albedo.a}, Target: {targetDensity}, Diff: {diff}");

            yield return null;
        }
        
        mistFog.profile.albedo.a = targetDensity;

        yield return new WaitForSeconds(30f);

        PlayerBoatManager.Instance.StopBoatMovement();

        yield return new WaitForSeconds(2f);

        SpawnMonstersInCircle();

        yield return new WaitForSeconds(15f);


        uncleBoat.GetComponent<BoatScenario>().enabled = false;

        while (Vector3.Distance(uncleBoat.transform.position, playerBoat.transform.position) > 130f)
        {
            Vector3 direction = (playerBoat.transform.position - uncleBoat.transform.position).normalized;
            uncleBoat.transform.position += direction * 20f * Time.deltaTime;
            yield return null;
        }

        RadioManager.Instance.PlayDialogue(dialogue);

        PlayerFishingSystem.Instance.UnequipFishingRod();

        //Swap uncleBot to Destroyed UncleBot

        destroyedBoat.transform.position = uncleBoat.transform.position;
        uncleBoat.SetActive(false);
        destroyedBoat.SetActive(true);


        yield return new WaitForSeconds(30f);

        PlayerBoatManager.Instance.StartBoatMovement();

        for (int i = 0; i < spawnedMonsters.Length; i++)
        {
            if (spawnedMonsters[i] != null)
            {
                Destroy(spawnedMonsters[i]);
            }
        }

        targetDensity = 0f;

        diff = Mathf.Abs(mistFog.profile.albedo.a - targetDensity);
        Debug.Log($"Increasing mist density... Current: {mistFog.profile.albedo.a}, Target: {targetDensity}, Diff: {diff}");

        while (diff > 0.1f)
        {
            diff = Mathf.Abs(mistFog.profile.albedo.a - targetDensity);
            mistFog.profile.albedo.a = Mathf.Lerp(mistFog.profile.albedo.a, targetDensity, Time.deltaTime);

            Debug.Log($"Increasing mist density... Current: {mistFog.profile.albedo.a}, Target: {targetDensity}, Diff: {diff}");

            yield return null;
        }

        while (Vector3.Distance(destroyedBoat.transform.position, playerBoat.transform.position) > 70)
        {
            yield return null;
        }

        //Player is checking out the destroyed boat, so we can end the event here

        GameObject chaser = Instantiate(monsterPrefab, playerBoat.transform.right * 150f + playerBoat.transform.position, Quaternion.identity);

        float maxChaseTime = 30f;
        float chaseTime = maxChaseTime;

        float chaseSpeed = 5f;
        float maxChaseSpeed = 5.5f;

        int maxHits = 5;
        int hits = 0;

        float waitingTime = 3f;
        float waitingTimer = 0f;

        bool waiting = false;

        while( hits < maxHits && chaseTime > 0)
        {
            if(waiting)
            {
                if( waitingTimer > waitingTime )
                {
                    waitingTimer += Time.deltaTime;
                    waiting = false;
                }

                yield return null;
            }

            if(Vector3.Distance(chaser.transform.position, playerBoat.transform.position) > 20f)
            {
                Vector3 direction = (playerBoat.transform.position - chaser.transform.position).normalized;

                Quaternion targetRotation = Quaternion.LookRotation(direction);

                chaser.transform.rotation = Quaternion.Slerp(chaser.transform.rotation, targetRotation, Time.deltaTime * 2f);

                chaser.transform.position += direction * chaseSpeed * Time.deltaTime;

                chaseSpeed += Time.deltaTime * 0.5f;
                chaseSpeed = Mathf.Min(chaseSpeed, maxChaseSpeed);

                chaseTime -= Time.deltaTime;
            }
            else
            {
                hits++;
                waiting = true;
                waitingTimer = waitingTime;
            
                //Boat Hit
            }

            yield return null;
        }

        if(hits >= maxHits)
        {
            //Boat Died

            PlayerMovement.Instance.gameObject.GetComponent<Blocker>().isBlocking = true;
            PlayerCamera.Instance.cameraEnabled = false;
            JumpscareController.instance.PlayJumpscare();
            yield break;
        }


        

        if(chaseTime > 0)
        {
            Debug.Log("Chaser caught the player! Ending event.");

            PlayerMovement.Instance.gameObject.GetComponent<Blocker>().isBlocking = true;
            PlayerCamera.Instance.cameraEnabled = false;
            JumpscareController.instance.PlayJumpscare();
            yield break;

        }
        else
        {
            Debug.Log("Chaser failed to catch the player in time. Ending event.");

            chaser.transform.DOMoveY(chaser.transform.position.y - 50f, 2f).OnComplete(() => Destroy(chaser));
            yield return new WaitForSeconds(10f);

            PlayerMovement.Instance.gameObject.GetComponent<Blocker>().isBlocking = true;
            PlayerCamera.Instance.cameraEnabled = false;
            JumpscareController.instance.PlayJumpscare();
            yield break;

        }

    }

    private void SpawnMonstersInCircle()
    {
        spawnedMonsters = new GameObject[circleSpotCount];

        Debug.Log("Spawning monsters in a circle around the player!");

        for (int i = 0; i < circleSpotCount; i++)
        {
            float angle = i * Mathf.PI * 2f / circleSpotCount;
            Vector3 spawnPos = centerObj.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * circleRadius;
            spawnPos += Vector3.down * 30f;
            spawnedMonsters[i] = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (centerObj != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centerObj.transform.position, circleRadius);

            for (int i = 0; i < circleSpotCount; i++)
            {
                float angle = i * Mathf.PI * 2f / circleSpotCount;
                Vector3 spawnPos = centerObj.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * circleRadius;
                Gizmos.DrawSphere(spawnPos, 0.2f);
            }
        }
    }
}
