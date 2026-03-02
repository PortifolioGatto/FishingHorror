using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class LineNode
{
    public Vector3 position;
    public Vector3 previousPosition;
    public float mass;
    public bool isLocked;
}

public enum HookMode
{
    IDLE,
    CAST,
    WAITING_FOR_CATCH,
    CATCHING,
    HOLDING_FISH
}

public class FishingRod : MonoBehaviour
{
    [SerializeField] private Transform rodTip;
    [SerializeField] private Transform reelWheel;
    [SerializeField] private Rigidbody hookRb;
    public Transform HookPoint => hookRb.transform;
    [SerializeField] private HookMode hookMode = HookMode.IDLE;
    [SerializeField] private LayerMask waterMask;
    [SerializeField] private LayerMask boatMask;

    private FishInstance currentFish;
    public FishInstance CurrentFish => currentFish;

    public HookMode CurrentHookMode => hookMode;

    public void SetHookMode(HookMode mode)
    {
        hookMode = mode;
    }

    [Space]
    [SerializeField] private int startingCount = 3;
    [SerializeField] private int lineSegmentCount = 10;
    [SerializeField] private float lineSegmentLength = 0.5f;
    [SerializeField] private float lineSegmentGravity = 9.81f;
    [SerializeField] private float lineSegmentMass = .2f;
    [Space]
    [SerializeField, Range(0f,1f)] private float lineSegmentDamping = 1f;
    [SerializeField, Range(0f,1f)] private float lineSegmentStiffness = 1f;
    [Space]

    [SerializeField] private int iterationsSolver = 5;

    [Space]

    [Header("Casting Hook Settings")]
    [SerializeField] private float strengthMult = 1f;
    [SerializeField] private float addCooldown = 0.1f;
    private float addTimer = 0f;

    [Space]
    [SerializeField] private LineRenderer lineRenderer;

    private List<LineNode> lineNodes = new List<LineNode>();

    private bool isCasting = false;

    private void Start()
    {
        InitializeLine();
    }

    private void InitializeLine()
    {
        Vector3 startPosition = rodTip.position;

        lineSegmentCount = startingCount;

        for (int i = 0; i < startingCount; i++)
        {
            LineNode node = new LineNode
            {
                position = startPosition - new Vector3(0, lineSegmentLength * i, 0),
                previousPosition = startPosition - new Vector3(0, lineSegmentLength * i, 0),
                mass = lineSegmentMass,
                isLocked = false
            };

            if(i == 0)
            {
                node.position = rodTip.position; // Lock the first node to the rod tip
                node.previousPosition = rodTip.position; // Lock the first node to the rod tip
                node.isLocked = true; // Lock the first node to the rod tip
            }

            if(i == lineSegmentCount - 1)
            {
                node.mass = lineSegmentMass * 2f; // Make the last node heavier to simulate the hook's weight
            }


            lineNodes.Add(node);
        }

        hookRb.isKinematic = true;
        hookRb.linearVelocity = Vector3.zero;
        hookRb.transform.position = lineNodes[lineSegmentCount - 1].position;
    }

    private void Update()
    {
        UpdateHookState();
        RenderLine();
    }

    private void FixedUpdate()
    {
        SimulateLine();
    }


    private void OnDisable()
    {
        if(hookRb != null)
            hookRb.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        hookRb.gameObject.SetActive(true);
    }

    private void SimulateLine()
    {
        // Verlet Integration
        for (int i = 0; i < lineSegmentCount; i++)
        {
            LineNode node = lineNodes[i];
            if (!node.isLocked)
            {
                Vector3 velocity = node.position - node.previousPosition;

                velocity *= lineSegmentDamping;

                node.previousPosition += velocity;
                node.position += velocity;

                node.position += Vector3.down * lineSegmentGravity * (Time.deltaTime * Time.deltaTime);
            }
            else
            {
                lineNodes[0].position = rodTip.position;
                lineNodes[0].previousPosition = rodTip.position;

                if (hookMode == HookMode.IDLE || hookMode == HookMode.HOLDING_FISH)
                    hookRb.position = lineNodes[lineSegmentCount - 1].position;
                else
                    lineNodes[lineSegmentCount - 1].position = hookRb.position;
            }


        }

        for (int z = 0; z < iterationsSolver; z++)
        {
            lineNodes[0].position = rodTip.position;
            if(hookMode == HookMode.IDLE || hookMode == HookMode.HOLDING_FISH)
                hookRb.position = lineNodes[lineSegmentCount - 1].position;
            else
                lineNodes[lineSegmentCount - 1].position = hookRb.position;


            //Constraint Solving
            for (int i = 1; i < lineSegmentCount; i++)
            {
                float w1 = lineNodes[i - 1].isLocked ? 0f : 1f / lineNodes[i - 1].mass;
                float w2 = lineNodes[i].isLocked ? 0f : 1f / lineNodes[i].mass;

                float wSum = w1 + w2;

                if (wSum == 0f) continue;

                Vector3 dir = lineNodes[i].position - lineNodes[i - 1].position;
                float dist = dir.magnitude;

                if (dist == 0f) continue;

                float C = dist - lineSegmentLength;

                Vector3 gradient = dir / dist;

                Vector3 correction = lineSegmentStiffness * (C / wSum) * gradient;

                if (!lineNodes[i - 1].isLocked)
                    lineNodes[i - 1].position += w1 * correction;

                if (!lineNodes[i].isLocked)
                    lineNodes[i].position -= w2 * correction;
            }
        }

        if(hookMode == HookMode.CAST)
        {
            //Get tension
            float maxLength = lineSegmentCount * lineSegmentLength;
            float currentLength = 0f;

            for (int i = 1; i < lineSegmentCount; i++)
            {
                currentLength += Vector3.Distance(lineNodes[i].position, lineNodes[i - 1].position);
            }

            addTimer -= Time.deltaTime;

            if (currentLength >= maxLength * 0.99f && addTimer <= 0f && lineSegmentCount < 64)
            {
                AddSegment();
                addTimer = addCooldown;
            }
        }
    }

    private void RenderLine()
    {
        lineRenderer.positionCount = lineSegmentCount;

        lineRenderer.SetPosition(0, rodTip.position);

        for (int i = 1; i < lineSegmentCount; i++)
        {
            Vector3 linePos = lineRenderer.GetPosition(i);
            Vector3 nodePosition = lineNodes[i].position;

            Vector3 smoothedPosition = Vector3.Lerp(linePos, nodePosition, 0.5f);

            lineRenderer.SetPosition(i, smoothedPosition);
        }

        if (hookMode == HookMode.IDLE || hookMode == HookMode.HOLDING_FISH)
        {
            Vector3 lastPos = lineNodes[lineSegmentCount - 1].position;
            Vector3 prevPos = lineNodes[lineSegmentCount - 2].position;

            hookRb.transform.position = lastPos;

            Vector3 dir = lastPos - prevPos;

            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

                hookRb.transform.rotation = Quaternion.Slerp(
                    hookRb.transform.rotation,
                    targetRot,
                    8f * Time.deltaTime
                );
            }
        }
        else
            lineRenderer.SetPosition(lineSegmentCount - 1, hookRb.position);
    }


    public void AddSegment()
    {
        if (lineNodes.Count < 2) return;

        

        LineNode last = lineNodes[lineNodes.Count - 1]; 

        // Calcula a "velocidade" implícita do último nó
        Vector3 inheritedVelocity = last.position - last.previousPosition; 
        LineNode newNode = new LineNode(); 
        newNode.position = last.position; 
        newNode.previousPosition = last.position - inheritedVelocity; 
        newNode.isLocked = false; 
        newNode.mass = lineSegmentMass; 
        lineNodes.Insert(lineNodes.Count - 1, newNode); 
        lineSegmentCount = lineNodes.Count; 
        lineRenderer.positionCount = lineSegmentCount;

        RenderLine(); 
    } 
    public void RemoveSegment()
    { 
        if (lineNodes.Count <= 2) return; 
        lineNodes.RemoveAt(lineNodes.Count - 2); 
        lineSegmentCount = lineNodes.Count; 
        lineRenderer.positionCount = lineSegmentCount;
        RenderLine(); 
    }


    public void ResetHook()
    {
        while(lineSegmentCount > startingCount)
        {
            RemoveSegment();
        }

        reelWheel.transform.DOKill();
        reelWheel.transform.DOLocalRotate(new Vector3(0f,0f,0f), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.OutQuad);

        lineRenderer.positionCount = lineSegmentCount;

        isCasting = false;
        hookRb.isKinematic = true;
        hookRb.transform.position = rodTip.position;
        hookMode = HookMode.IDLE;
    }

    private AudioSource reelSource;

    public void CastHook(Vector3 force)
    {
        if (hookMode == HookMode.IDLE)
        {
            reelWheel.transform.DOKill();
            reelWheel.transform.DOLocalRotate(new Vector3(360, 0f, 0f), .25f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);

            reelSource = AudioManager.Instance.PlaySFXLoop("reel", transform.position, .25f);
            reelSource.pitch = 1f + (force.magnitude * 0.05f);
            reelSource.transform.parent = transform;

            hookRb.transform.position = rodTip.position;
            hookRb.isKinematic = false;
            hookRb.AddForce(force * strengthMult, ForceMode.Impulse);
            hookMode = HookMode.CAST;

            isCasting = true;
        }
    }
    
    private void UpdateHookState()
    {
        switch (hookMode)
        {
            case HookMode.IDLE:
                if (reelSource != null)
                {
                    reelSource.Stop();
                    Destroy(reelSource.gameObject, 0.1f);
                }
                break;
            case HookMode.CAST:
                bool hitting = Physics.SphereCast(hookRb.position, 0.1f, Vector3.down, out RaycastHit hit, 0.2f, waterMask);
                if (isCasting && hitting)
                {
                    reelWheel.transform.DOKill();
                    if (reelSource != null)
                    {
                        reelSource.Stop();
                        Destroy(reelSource.gameObject, 0.1f);
                    }

                    AudioManager.Instance.PlaySFX("splashinsmall", hookRb.position, 0.25f);

                    isCasting = false;
                    hookRb.isKinematic = true;
                    hookRb.linearVelocity = Vector3.zero;
                    hookMode = HookMode.WAITING_FOR_CATCH;

                    hookRb.transform.position = hit.point;

                    OceanManager.Instance.SpawnRipple(hit.point);
                }


                bool hittingBoat = Physics.SphereCast(hookRb.transform.position, 0.2f, Vector3.down, out RaycastHit boatHit, 0.2f, boatMask);
                if (isCasting && hittingBoat)
                {
                    reelWheel.transform.DOKill();
                    if (reelSource != null)
                    {
                        reelSource.Stop();
                        Destroy(reelSource.gameObject, 0.1f);
                    }
                    isCasting = false;
                    hookRb.isKinematic = true;
                    hookRb.linearVelocity = Vector3.zero;
                    hookMode = HookMode.IDLE;
                    hookRb.transform.position = boatHit.point;

                    ResetHook();
                }

                break;
            case HookMode.WAITING_FOR_CATCH:

                float dist = Vector3.Distance(hookRb.position, rodTip.position);
                if (dist > 50f)
                {
                    ResetHook();
                }

                break;
            case HookMode.CATCHING:
                break;
        }
    }

    public void SetCurrentFish(FishInstance fish)
    {
        currentFish = fish;
    }

    public void ReleaseFish()
    {
        Destroy(currentFish.gameObject);
        currentFish = null;
    }

    public void DisposeFish()
    {
        currentFish = null;
    }

    public bool TryGetPredictedWaterHit(Vector3 force, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        float mass = hookRb.mass;
        float drag = hookRb.linearDamping; // se estiver usando Rigidbody padrão use hookRb.drag

        Vector3 velocity = (force * strengthMult) / mass;
        Vector3 position = rodTip.position;

        float waterHeight = OceanManager.Instance.transform.position.y;

        float simulationTime = 0f;
        float maxSimulationTime = 10f;
        float step = Time.fixedDeltaTime;

        while (simulationTime < maxSimulationTime)
        {
            // Aplica gravidade
            velocity += Physics.gravity * step;

            // Aplica drag (igual Unity)
            velocity *= 1f / (1f + drag * step);

            // Move
            position += velocity * step;

            // Checa se cruzou a água
            if (position.y <= waterHeight)
            {
                hitPoint = new Vector3(position.x, waterHeight, position.z);
                return true;
            }

            simulationTime += step;
        }

        return false;
    }
}
