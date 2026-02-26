using System.Collections.Generic;
using UnityEngine;

public class BoidsManager : MonoBehaviour
{
    public List<BoidSpecies> speciesList;
    public SpawnGroup[] spawnGroups;

    [System.Serializable]
    public struct SpawnGroup
    {
        public int speciesIndex;
        public int count;
    }

    [Header("Simulation")]
    public float simulationRadius = 50f;
    public float cellSize = 5f;

    private BoidData[] boids;
    private Transform[] visuals;

    private Dictionary<int, List<int>> spatialHash = new Dictionary<int, List<int>>(2048);
    private List<int> bucketPool = new List<int>(2048);
    private Stack<List<int>> reusableLists = new Stack<List<int>>(2048);

    void Awake()
    {
        for (int i = 0; i < 2048; i++)
            reusableLists.Push(new List<int>(16));
    }

    void Start()
    {
        SpawnBoids();
    }

    void Update()
    {
        UpdateSpatialHash();
        Simulate();
        UpdateVisuals();
    }

    // =========================================================
    // SPAWN
    // =========================================================

    void SpawnBoids()
    {
        int total = 0;
        foreach (var g in spawnGroups)
            total += g.count;

        boids = new BoidData[total];
        visuals = new Transform[total];

        int index = 0;

        foreach (var group in spawnGroups)
        {
            int speciesIndex = group.speciesIndex;

            BoidSpecies species = speciesList[speciesIndex];

            for (int i = 0; i < group.count; i++)
            {
                Vector3 pos = new Vector3(
                    transform.position.x + Random.Range(-simulationRadius, simulationRadius),
                    transform.position.y,
                    transform.position.z + Random.Range(-simulationRadius, simulationRadius)
                );

                Vector3 vel = Random.insideUnitSphere.normalized *
                              Random.Range(species.minSpeed, species.maxSpeed);

                boids[index] = new BoidData
                {
                    position = pos,
                    velocity = vel,
                    acceleration = Vector3.zero,
                    speciesIndex = speciesIndex
                };

                visuals[index] = Instantiate(species.prefab, pos, Quaternion.identity).transform;

                index++;
            }
        }
    }

    // =========================================================
    // SIMULATION
    // =========================================================

    void Simulate()
    {
        float deltaTime = Time.deltaTime;

        for (int i = 0; i < boids.Length; i++)
        {
            BoidData boid = boids[i];
            BoidSpecies species = speciesList[boid.speciesIndex];

            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            Vector3 avoidance = Vector3.zero;

            int count = 0;

            float neighborRadiusSqr = species.neighborRadius * species.neighborRadius;
            float avoidanceRadiusSqr = species.avoidanceRadius * species.avoidanceRadius;

            Vector3 offset = boid.position - transform.position;

            int baseX = Mathf.FloorToInt(offset.x / cellSize);
            int baseY = Mathf.FloorToInt(offset.z / cellSize);

            int maxNeighbors = 20;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int key = Hash(baseX + x, baseY + y);

                    if (!spatialHash.TryGetValue(key, out var list))
                        continue;

                    for (int j = 0; j < list.Count; j++)
                    {
                        int otherIndex = list[j];
                        if (otherIndex == i) continue;

                        BoidData other = boids[otherIndex];

                        Vector3 diff = boid.position - other.position;
                        float sqrDist = diff.sqrMagnitude;

                        

                        // Avoidance (sempre)
                        if (sqrDist < avoidanceRadiusSqr && sqrDist > 0)
                        {
                            avoidance += diff.normalized / Mathf.Sqrt(sqrDist);
                        }

                        count++;

                        // Social (mesma espécie)
                        if (sqrDist < neighborRadiusSqr &&
                            boid.speciesIndex == other.speciesIndex)
                        {
                            alignment += other.velocity;
                            cohesion += other.position;
                            

                            if (count >= maxNeighbors)
                                break;
                        }
                    }
                }
            }

            if (count > 0)
            {
                alignment /= count;
                alignment = alignment.normalized * species.maxSpeed - boid.velocity;

                cohesion /= count;
                cohesion = (cohesion - boid.position).normalized * species.maxSpeed - boid.velocity;
            }

            // Boundary
            Vector3 center = transform.position;
            Vector3 toCenter = center - boid.position;
            float dist = toCenter.magnitude;

            if (dist > simulationRadius * 0.8f)
            {
                float t = (dist - simulationRadius * 0.8f) / (simulationRadius * 0.2f);
                boid.acceleration += toCenter.normalized * species.boundaryWeight * t;
            }

            boid.acceleration += alignment * species.alignmentWeight;
            boid.acceleration += cohesion * species.cohesionWeight;
            boid.acceleration += avoidance * species.avoidanceWeight;

            boid.acceleration = Vector3.ClampMagnitude(boid.acceleration, species.maxForce);

            boid.velocity += boid.acceleration * deltaTime;
            boid.velocity = Vector3.ClampMagnitude(boid.velocity, species.maxSpeed);

            if (boid.velocity.magnitude < species.minSpeed)
                boid.velocity = boid.velocity.normalized * species.minSpeed;

            boid.position += boid.velocity * deltaTime;
            boid.acceleration = Vector3.zero;

            boids[i] = boid;
        }
    }

    // =========================================================
    // VISUALS
    // =========================================================

    void UpdateVisuals()
    {
        for (int i = 0; i < visuals.Length; i++)
        {
            visuals[i].position = boids[i].position;

            if (boids[i].velocity.sqrMagnitude > 0.01f)
                visuals[i].rotation = Quaternion.LookRotation(boids[i].velocity);
        }
    }

    // =========================================================
    // SPATIAL HASH
    // =========================================================

    void UpdateSpatialHash()
    {
        // Devolve listas para pool
        foreach (var pair in spatialHash)
        {
            pair.Value.Clear();
            reusableLists.Push(pair.Value);
        }

        spatialHash.Clear();

        for (int i = 0; i < boids.Length; i++)
        {
            Vector3 offset = boids[i].position - transform.position;

            int key = Hash(
                Mathf.FloorToInt(offset.x / cellSize),
                Mathf.FloorToInt(offset.z / cellSize)
            );

            if (!spatialHash.TryGetValue(key, out var list))
            {
                if (reusableLists.Count > 0)
                    list = reusableLists.Pop();
                else
                    list = new List<int>(16); // fallback raro

                spatialHash[key] = list;
            }

            if (list.Count < 50)
                list.Add(i);
        }
    }

    int Hash(int x, int y)
    {
        return (x * 73856093) ^ (y * 19349663);
    }

    Color GetColorFromName(string name)
    {
        int hash = name.GetHashCode();
        Random.InitState(hash);
        return new Color(
            Random.Range(0.3f, 1f),
            Random.Range(0.3f, 1f),
            Random.Range(0.3f, 1f)
        );
    }

    void OnDrawGizmos()
    {
        if (!enabled)
            return;

        Vector3 center = transform.position;

        // =============================
        // Simulation Radius
        // =============================
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(center, simulationRadius);

        // =============================
        // Spatial Grid Preview
        // =============================
        Gizmos.color = new Color(1f, 1f, 1f, 0.1f);

        int cells = Mathf.CeilToInt(simulationRadius / cellSize);

        for (int x = -cells; x <= cells; x++)
        {
            for (int y = -cells; y <= cells; y++)
            {
                Vector3 cellCenter = center + new Vector3(
                    x * cellSize + cellSize * 0.5f,
                    0,
                    y * cellSize + cellSize * 0.5f
                );

                Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.01f, cellSize));
            }
        }

        // =============================
        // Species Radius Preview
        // =============================
        if (speciesList != null)
        {
            float offset = 0f;

            foreach (var species in speciesList)
            {
                if (species == null) continue;

                Gizmos.color = GetColorFromName(species.name);

                Vector3 previewPos = center + Vector3.right * offset;

                Gizmos.DrawWireSphere(previewPos, species.neighborRadius);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(previewPos, species.avoidanceRadius);

                offset += species.neighborRadius * 2f + 2f;
            }
        }
    }
}

public struct BoidData
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 acceleration;
    public int speciesIndex;
}

[System.Serializable]
public class BoidSpecies
{
    public string name;
    public GameObject prefab;

    [Header("Movement")]
    public float minSpeed = 2f;
    public float maxSpeed = 6f;
    public float maxForce = 10f;

    [Header("Weights")]
    public float alignmentWeight = 1f;
    public float cohesionWeight = 0.6f;
    public float avoidanceWeight = 2f;
    public float boundaryWeight = 5f;

    public float neighborRadius = 5f;
    public float avoidanceRadius = 2f;
}