using System.Collections;
using UnityEngine;

public class SpawnObserver_EventController : EventController
{
    [SerializeField] private GameObject theObserverPrefab;

    [Space]

    [SerializeField] private float intervalBetweenSpawns = 60f;
    private float timeSinceLastSpawn = 0f;
    private bool observerSpawned = false;

    private bool canSpawnObserver = false;

    [Space]
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private float spawnLength = 8f;  // eixo maior (comprimento do barco)
    [SerializeField] private float spawnWidth = 3f;    // eixo menor (largura do barco)
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 3f;


    [SerializeField] private float minimumDistanceFromOtherBoats = 150f;
    [SerializeField] private LayerMask boatLayerMask;

    private void Start()
    {
        if (startOnStart)
        {
            StartEvent();
        }
    }

    [ContextMenu("Start Event")]
    public override void StartEvent()
    {
        canSpawnObserver = true;
    }

    private void SpawnObserver()
    {
        // Ponto aleatório na BORDA do círculo unitário
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float x = Mathf.Cos(angle) * spawnWidth;
        float z = Mathf.Sin(angle) * spawnLength;

        Vector3 localOffset = new Vector3(x, Random.Range(minHeight, maxHeight), z);
        Vector3 worldPosition = spawnCenter.TransformPoint(localOffset);

        worldPosition.y = Random.Range(minHeight, maxHeight);

        // Verifica se há outros barcos próximos
        Collider[] nearbyBoats = Physics.OverlapSphere(worldPosition, minimumDistanceFromOtherBoats, boatLayerMask);

        while (nearbyBoats.Length > 0)
        {
            // Gera um novo ponto aleatório
            angle = Random.Range(0f, Mathf.PI * 2f);
            x = Mathf.Cos(angle) * spawnWidth;
            z = Mathf.Sin(angle) * spawnLength;
            localOffset = new Vector3(x, Random.Range(minHeight, maxHeight), z);
            worldPosition = spawnCenter.TransformPoint(localOffset);
            worldPosition.y = Random.Range(minHeight, maxHeight);
            nearbyBoats = Physics.OverlapSphere(worldPosition, minimumDistanceFromOtherBoats, boatLayerMask);
        }


        GameObject observer = Instantiate(theObserverPrefab, worldPosition, Quaternion.identity);

        StartCoroutine(EObserverBehaviour(observer));
    }

    private IEnumerator EObserverBehaviour(GameObject observer)
    {
        GameObject player = PlayerController.Instance.gameObject;

        bool playerSawObserver = false;

        float distanceToPlayer = Vector3.Distance(observer.transform.position, player.transform.position);

        Vector3 directionToPlayer = (player.transform.position - observer.transform.position).normalized;
        float playerCanSeeObserver = Vector3.Dot(player.transform.forward, directionToPlayer);

        while (!playerSawObserver)
        {
            directionToPlayer = (observer.transform.position - player.transform.position).normalized;
            playerCanSeeObserver = Vector3.Dot(Camera.main.transform.forward, directionToPlayer);
            // O jogador pode ver o observador se estiver dentro de um certo ângulo e distância

            Debug.Log($"Distance to player: {distanceToPlayer}, Player cannot see observer: {playerCanSeeObserver}");

            if (playerCanSeeObserver > 0.9f)
            {
                playerSawObserver = true;
                Debug.Log("Player saw the observer!");
            }
            yield return null;
        }

        while(playerSawObserver)
        {
            directionToPlayer = (observer.transform.position - player.transform.position).normalized;
            playerCanSeeObserver = Vector3.Dot(Camera.main.transform.forward, directionToPlayer);
            // O jogador pode ver o observador se estiver dentro de um certo ângulo e distância

            Debug.Log($"Distance to player: {distanceToPlayer}, Player can see observer: {playerCanSeeObserver}");

            if (playerCanSeeObserver < 0.4f)
            {
                playerSawObserver = false;
                Debug.Log("Player saw the observer!");
            }
            yield return null;
        }

        observerSpawned = false;
        Destroy(observer);
    }

    private void Update()
    {
        if(canSpawnObserver && !observerSpawned)
        {
            timeSinceLastSpawn += Time.deltaTime;
            if (timeSinceLastSpawn >= intervalBetweenSpawns)
            {
                SpawnObserver();
                observerSpawned = true;
                timeSinceLastSpawn = 0f;
            }
        }
    }

    public override void UpdateEvent() { }
    public override void EndEvent() { }

    private void OnDrawGizmosSelected()
    {
        if (spawnCenter == null) return;

        Gizmos.color = Color.white;
        Gizmos.matrix = spawnCenter.localToWorldMatrix;

        // Desenha a elipse aproximada com linhas
        int segments = 32;
        Vector3 prevPoint = new Vector3(spawnWidth, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 nextPoint = new Vector3(
                Mathf.Cos(angle) * spawnWidth,
                0f,
                Mathf.Sin(angle) * spawnLength
            );
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        // Linhas verticais nos cantos para indicar a faixa de altura
        float[] angles = { 0f, Mathf.PI * 0.5f, Mathf.PI, Mathf.PI * 1.5f };
        foreach (float a in angles)
        {
            Vector3 basePoint = new Vector3(Mathf.Cos(a) * spawnWidth, minHeight, Mathf.Sin(a) * spawnLength);
            Vector3 topPoint = new Vector3(Mathf.Cos(a) * spawnWidth, maxHeight, Mathf.Sin(a) * spawnLength);
            Gizmos.DrawLine(basePoint, topPoint);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}