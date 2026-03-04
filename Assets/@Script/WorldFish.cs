using DG.Tweening;
using UnityEngine;


public class WorldFish : MonoBehaviour, IInteractable
{
    public enum FishState
    {
        Swimming,
        Landed,
        Caught
    }

    private const string SPLASH_SFX_NAME = "splashinsmall";
    private const string SPLASH_OUT_SFX_NAME = "splashout";
    private const string FLOP_SFX_NAME = "fishsplashup";
    private const string HIT_BOAT_SFX_NAME = "fishhitboat";

    [Header("Landed Settings")]
    [SerializeField] private float minFloppingTime = .15f;
    [SerializeField] private float maxFloppingTime = 3f;
    private float floppingTimer;

    [Header("Jump Settings")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private LayerMask boatLayer;

    private bool isJumping = false;
    public bool IsJumping => isJumping;

    private FishState state = FishState.Swimming;

    public FishState State => state;

    public FishData fishData;

    private FishingSpot owner;

    private Rigidbody rb;
    private Collider col;

    public bool isHovering { get; set; }

    [field: SerializeField]public bool canInteract { get; set; } = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Initialize(FishData data)
    {
        fishData = data;
        SetState(FishState.Swimming);
    }
    public void InitializeFishingSpot(FishingSpot spot)
    {
        owner = spot;
    }

    private void Update()
    {
        if(state == FishState.Landed)
        {
            if(floppingTimer > 0f)
            {
                floppingTimer -= Time.deltaTime;
                return;
            }
            
            if(Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, .5f, boatLayer))
            {
                Vector3 randomForce = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f), Random.Range(-1f, 1f)) * 5f;
                rb.AddForce(randomForce, ForceMode.Impulse);

                floppingTimer = Random.Range(minFloppingTime, maxFloppingTime);
                
                AudioManager.Instance.PlaySFX(FLOP_SFX_NAME, transform.position, 0.5f);
                
            }

            
            if(Physics.OverlapSphere(transform.position, 0.5f, waterLayer).Length > 0)
            {
                AudioManager.Instance.PlaySFX(SPLASH_SFX_NAME, transform.position, 0.5f);

                SetState(FishState.Swimming);
                if(owner == null)
                {
                    transform.DOMove(transform.position + Vector3.down * 7f, 1f).onComplete += () =>
                    {
                        Destroy(gameObject);
                    };
                }
            }
        }
    }

    public void SetState(FishState state)
    {
        this.state = state;
        switch (state)
        {
            case FishState.Swimming:
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                canInteract = false;
                break;
            case FishState.Landed:
                canInteract = true;
                rb.isKinematic = false;
                col.enabled = true;
                transform.rotation = Quaternion.Euler(0, 0, 90);
                break;
            case FishState.Caught:
                canInteract = false;
                rb.isKinematic = true;
                col.enabled = false;
                break;
        }
    }

    public void JumpTo(Vector3 force)
    {
        isJumping = true;
        rb.angularDamping = 0;
        rb.isKinematic = false;

        rb.AddForce(force, ForceMode.Impulse);
        Collider[] cols = new Collider[1];
        Collider[] colsBoat = new Collider[1];

        Quaternion velocityRotation = Quaternion.LookRotation(force.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, velocityRotation, Time.deltaTime * 10f);

        AudioManager.Instance.PlaySFX(SPLASH_OUT_SFX_NAME, transform.position, 0.5f);

        StartCoroutine(CheckForWaterOrBoat(cols, colsBoat));
    }

    public void Jump()
    {
        JumpTo(Vector3.up * Random.Range(7f, 10f) + Vector3.forward * Random.Range(-2f, 2f));
    }

    private System.Collections.IEnumerator CheckForWaterOrBoat(Collider[] cols, Collider[] colsBoat)
    {
        yield return new WaitForSeconds(0.15f); // Wait a moment for the fish to be in the air before checking collisions

        while (true)
        {
            Quaternion velocityRotation = Quaternion.LookRotation(rb.linearVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, velocityRotation, Time.deltaTime * 10f);

            if (rb.linearVelocity.y >= 0) // Only check when the fish is falling down
            {
                yield return null;
                continue;
            }

            int waterHits = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 1f, 0.5f, cols, waterLayer);
            int boatHits = Physics.OverlapSphereNonAlloc(transform.position, 1f, colsBoat, boatLayer);
    
            if (waterHits > 0)
            {
                transform.SetParent(null);
                AudioManager.Instance.PlaySFX(SPLASH_SFX_NAME, transform.position, 0.5f);
                isJumping = false;
                SetState(FishState.Swimming);
                yield break;
            }
            else if (boatHits > 0)
            {
                transform.SetParent(colsBoat[0].transform);
                AudioManager.Instance.PlaySFX(HIT_BOAT_SFX_NAME, transform.position, 0.5f);
                Debug.Log("Fish landed on the boat!");
                isJumping = false;
                SetState(FishState.Landed);
                yield break;
            }
    
            yield return null;
        }
    }

    public void OnHover()
    {
        
    }

    public void OnHoverExit()
    {
        
    }

    public void Interact()
    {
        if(PlayerFishingSystem.Instance != null && !PlayerFishingSystem.Instance.HasCurrentFish())
        {
            PlayerFishingSystem.Instance.CatchWorldFish(this);
        }
    }

    public string GetInteractionText()
    {
        return $"Catch {fishData.fishName}";
    }
}
