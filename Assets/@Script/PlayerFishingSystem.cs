using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFishingSystem : MonoBehaviour
{
    [SerializeField] private float minCastForce = 2f;
    [SerializeField] private float maxCastForce = 10f;
    [SerializeField] private float minCastToThrow = 1f;
    [SerializeField] private float maxCastTime = 4f;
    [SerializeField] private float minRotationAngle = 0f;
    [SerializeField] private float maxRotationAngle = 0f;

    [Space]
    [SerializeField] private LayerMask fishingSpotLayer;
    [Space]

    [Header("Waiting for catch settings")]
    [SerializeField] private FishInstance fishInstance;
    [SerializeField] private GameObject previewForce;
    [SerializeField] private Material previewMat;
    [SerializeField] private SplashEffect splashEffect;
    [Space]
    [SerializeField] private float timeToBeReadyToCatch = 2f;
    [SerializeField] private float randomFishBiteTimeMin = 1f;
    [SerializeField] private float randomFishBiteTimeMax = 5f;
    [SerializeField] private float catchWindowDuration = 1f;
    private float readyToCatchTimer = 0f;
    private float fishBiteTimer = 0f;
    private float catchWindowTimer = 0f;

    private bool catchAttempted = false;
    private bool atemptingCatch = false;

    private bool isCharging = false;
    private float currentChargeTime = 0f;

    [Header("Ice Box Configuration")]
    [SerializeField] private int maxFishInIceBox = 10;
    private int currentFishInIceBox = 0;

    public string CurrentFishInIceBox => currentFishInIceBox + "/" + maxFishInIceBox;

    [SerializeField] private List<FishInstance> iceBoxContents = new List<FishInstance>();

    [Space]
    [SerializeField] private FishingRod fishingRod;
    [SerializeField] private InputActionReference throwFishingRodInput;

    private FishingSpot currentFishingSpot;

    private Blocker blocker;

    public static PlayerFishingSystem Instance;

    private void Awake()
    {
        Instance = this;
        blocker = GetComponent<Blocker>();
    }

    private void Start()
    {
        throwFishingRodInput.action.performed += OnThrowFishingRod;
        throwFishingRodInput.action.canceled += OnThrowFishingRodCanceled;
        throwFishingRodInput.action.Enable();
    }

    private void Update()
    {
        if(fishingRod.CurrentHookMode == HookMode.WAITING_FOR_CATCH)
        {
            if(readyToCatchTimer < timeToBeReadyToCatch)
            {
                readyToCatchTimer += Time.deltaTime;

                fishBiteTimer = UnityEngine.Random.Range(randomFishBiteTimeMin, randomFishBiteTimeMax);
            }
            else
            {
                if(fishBiteTimer > 0)
                {
                    fishBiteTimer -= Time.deltaTime;
                }
                else
                {
                    if(!atemptingCatch)
                    {
                        Collider[] cols = Physics.OverlapSphere(fishingRod.HookPoint.position, .5f, fishingSpotLayer);

                        int inSpot = -1;

                        for (int i = 0; i < cols.Length; i++)
                        {
                            if(cols[i] != null && cols[i].TryGetComponent(out FishingSpot fishingSpot))
                            {
                                inSpot = i;
                                break;
                            }
                        }

                        if (inSpot != -1)
                        {
                            FishingSpot spot = cols[inSpot].GetComponent<FishingSpot>();
                            if(spot != null)
                            {
                                StartCoroutine(EHandleFishBite(spot));
                            }
                        }
                    }
                }
            }
        }
    }

    private IEnumerator EHandleFishBite(FishingSpot spot)
    {
        atemptingCatch = true;
        catchAttempted = false;
        catchWindowTimer = 0f;
        readyToCatchTimer = 0f;

        float splashTimer = 0f;

        while (catchWindowTimer < catchWindowDuration)
        {
            catchWindowTimer += Time.deltaTime;

            if(splashEffect != null)
            {
                splashTimer -= Time.deltaTime;
    
                if(splashTimer <= 0f)
                {
                    AudioManager.Instance.PlaySFX("fishingbell", fishingRod.HookPoint.position, 0.5f);
                    SplashEffect splash = Instantiate(splashEffect, fishingRod.HookPoint.position, Quaternion.identity);
                    splash.Play();
                    splashTimer = UnityEngine.Random.Range(0.1f,0.4f);
                }
        }

            if (catchAttempted)
            {
                readyToCatchTimer = 0f;
                catchAttempted = false;
                currentFishingSpot = spot;
                fishingRod.SetHookMode(HookMode.CATCHING);

                currentFishingSpot.OnSpotIsTargeted?.Invoke();

                FishData fishData = spot.GetRandomFish();

                int attempts = 0;
                int maxAttempts = 10;

                while (fishData.canBite == false && attempts < maxAttempts)
                {
                    fishData = spot.GetRandomFish();
                    attempts++;
                }

                FishingMinigameManager.Instance.StartFishingMinigame(fishData, GetTheFish, fishingRod.ResetHook);

                break;
            }
            yield return null;
        }

        atemptingCatch = false;

        catchWindowTimer = 0f;

        fishBiteTimer = UnityEngine.Random.Range(randomFishBiteTimeMin, randomFishBiteTimeMax);
    }

    public bool HasCurrentFish()
    {
        return fishingRod.CurrentFish != null;
    }

    public void CatchWorldFish(WorldFish fish)
    {
        FishData fishData = fish.fishData;

        Destroy(fish.gameObject);

        GetTheFish(fishData);
    }

    public void GetTheFish(FishData fishData)
    {
        Debug.Log("Caught a " + fishData.fishName);

        FishInstance fishInstanced = Instantiate(fishInstance, fishingRod.HookPoint.position, Quaternion.identity, fishingRod.HookPoint);

        fishInstanced.Initialize(fishData);
        fishingRod.SetCurrentFish(fishInstanced);


        fishingRod.ResetHook();
        fishingRod.SetHookMode(HookMode.HOLDING_FISH);

        currentFishingSpot?.CatchFish(fishData);
    }

   


    public void SetVisible(bool visible)
    {
        fishingRod.gameObject.SetActive(visible);
    }


    private void OnThrowFishingRod(InputAction.CallbackContext context)
    {
        if(blocker.isBlocking) return;

        if (fishingRod.CurrentHookMode == HookMode.IDLE)
        {
            isCharging = true;

            fishingRod.ResetHook();

            readyToCatchTimer = 0f;

            StartCoroutine(ChargeCast());
        }
        else if(fishingRod.CurrentHookMode == HookMode.WAITING_FOR_CATCH)
        {
            if(atemptingCatch)
            {
                catchAttempted = true;
            }
            else
            {
                fishingRod.ResetHook();
            }
                
        }else if(fishingRod.CurrentHookMode == HookMode.CAST)
        {
            fishingRod.ResetHook();
        }

    }

    private void OnThrowFishingRodCanceled(InputAction.CallbackContext context)
    {
        isCharging = false;
    }

    private IEnumerator ChargeCast()
    {
        this.previewForce.SetActive(true);
        while (isCharging)
        {
            currentChargeTime += Time.deltaTime;
            currentChargeTime = Mathf.Clamp(currentChargeTime, 0f, maxCastTime);

            float probableCastForce = Mathf.Lerp(minCastForce, maxCastForce, currentChargeTime / maxCastTime);

            Vector3 previewForce = PlayerCamera.Orientation.forward * probableCastForce;

            if (fishingRod.TryGetPredictedWaterHit(previewForce, out Vector3 point))
            {
                this.previewForce.transform.position = point;

                bool isHittingSpot = Physics.OverlapSphere(point, .5f, fishingSpotLayer).Length > 0;

                previewMat.color = isHittingSpot ? Color.green : Color.red;
            }

            fishingRod.transform.localRotation = Quaternion.Euler(Mathf.Lerp(minRotationAngle, maxRotationAngle, currentChargeTime / maxCastTime), 0f, 0f);

            yield return null;
        }

        this.previewForce.SetActive(false);

        float chargePercent = currentChargeTime / maxCastTime;
        float castForce = Mathf.Lerp(minCastForce, maxCastForce, chargePercent);


        fishingRod.transform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack);

        if (currentChargeTime >= minCastToThrow)
        {
            AudioManager.Instance.PlaySFX("rodwhoosh", transform.position, 0.5f);
            fishingRod.CastHook(PlayerCamera.Orientation.forward * castForce);
        }
        currentChargeTime = 0f;
    }

    public bool IsHoldingFish()
    {
        return fishingRod.CurrentHookMode == HookMode.HOLDING_FISH;
    }

    public bool CanStoreFish(FishInstance fish)
    {
        return IsHoldingFish() && currentFishInIceBox + fishingRod.CurrentFish.fishData.sizeInBox  <= maxFishInIceBox;
    }

    public void StoreFishInIceBox()
    {
        currentFishInIceBox += fishingRod.CurrentFish.fishData.sizeInBox;
        iceBoxContents.Add(fishingRod.CurrentFish);

        fishingRod.CurrentFish.transform.SetParent(FishIceBox.Instance.transform);

        Vector3 targetPosition = new Vector3(0f, 0f, 0f); // You can adjust this position as needed
        targetPosition.x = UnityEngine.Random.Range(-0.75f, 0.75f);
        targetPosition.z = UnityEngine.Random.Range(-0.5f, 0.5f);
        targetPosition.y = UnityEngine.Random.Range(-0.275f, 0.126f);

        Vector3 randomRotation = new Vector3(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f,360f));


        fishingRod.CurrentFish.transform.DOLocalMove(targetPosition, 0.5f).SetEase(Ease.OutQuad);
        fishingRod.CurrentFish.transform.DOLocalRotate(randomRotation, 0.5f).SetEase(Ease.OutQuad);
        Vector3 scaling = fishingRod.CurrentFish.transform.localScale * .33f;
        fishingRod.CurrentFish.transform.DOScale(scaling, 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            fishingRod.CurrentFish.transform.localScale = scaling;
        });

        fishingRod.DisposeFish();
        fishingRod.ResetHook();

        Debug.Log("Fish stored in icebox");
    }

    public SerializedFishData[] GetSerializedFish()
    {
        SerializedFishData[] serializedFish = new SerializedFishData[iceBoxContents.Count];
        for (int i = 0; i < iceBoxContents.Count; i++)
        {
            serializedFish[i] = new SerializedFishData
            {
                fishData = iceBoxContents[i].fishData,
                size = iceBoxContents[i].size
            };
        }

        return serializedFish;
    }
}
