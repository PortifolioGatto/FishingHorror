using DG.Tweening;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEndController : MonoBehaviour
{
    [SerializeField] private WaveFollower boatFollowWave;
    [SerializeField] private BoatMovement boatMovement;
    [SerializeField] private VortexAffectedObject boatVortex;
    [SerializeField] private WakeTrailEmitter boatTrail;
    [SerializeField] private FishingSpotSpawner fishingSpotSpawner;
    [SerializeField] private JumpingFish_EventController jumpingFishEventController;
    [SerializeField] private UnderwaterLight monsterLight;
    [SerializeField] private Transform monsterTransform;
    [SerializeField] private Animator monsterAnimator;
    [Space]
    [SerializeField] private GameObject waterDefault;
    [SerializeField] private GameObject waterVortex;
    [SerializeField] private Material vortexMat;

    public void StartTheEnd()
    {
        StartCoroutine(EndSequence());
    }

    private IEnumerator EndSequence()
    {
        List<VortexAffectedObject> vortexObjects = new List<VortexAffectedObject>();

        vortexObjects.Add(boatVortex);

        jumpingFishEventController.StartJumpingFishEvent();
        jumpingFishEventController.ChanceChance(.25f);

        yield return new WaitForSeconds(5f);

        fishingSpotSpawner.SpawnFishingSpotAtMap();

        yield return new WaitForSeconds(3f);

        waterDefault.SetActive(false);
        waterVortex.SetActive(true);

        yield return new WaitForSeconds(3f);

        fishingSpotSpawner.RunToPlayer();

        yield return new WaitForSeconds(5f);

        jumpingFishEventController.ChanceChance(1f);

        yield return new WaitForSeconds(2f);

        boatMovement.movementEnabled = false;

        yield return new WaitForSeconds(1f);

        DOTween.To(() => monsterLight.intensity, x => monsterLight.intensity = x, 10f, 3f).SetEase(Ease.InOutSine);



        monsterTransform.DOMoveY(15, 10f);
        //monsterAnimator.SetBool("Pulsating", true);

        yield return new WaitForSeconds(1f);

        WeatherController.instance.PlayThunder();

        yield return new WaitForSeconds(10f);

        boatVortex.enabled = true;
        boatTrail.enabled = false;
        boatMovement.enabled = false;
        boatFollowWave.enabled = false;

        WorldFish[] worldFishes = FindObjectsOfType<WorldFish>();
        for (int i = 0; i < worldFishes.Length; i++)
        {
            worldFishes[i].enabled = false;
            vortexObjects.Add(worldFishes[i].gameObject.AddComponent<VortexAffectedObject>());
        }

        for (int i = 0; i < vortexObjects.Count; i++)
        {
            vortexObjects[i].orbitSpeed = 0f;
            vortexObjects[i].pullSpeed = 0f;

            vortexObjects[i].enabled = true;
            int index = i;

            DOTween.To(() => vortexObjects[index].orbitSpeed, x => vortexObjects[index].orbitSpeed = x, Random.Range(5f, 10f), 15f).SetEase(Ease.InOutSine);
        }

        yield return new WaitForSeconds(15.5f);

        VortexController vc = VortexController.Instance;

        vc.radius = 0f;
        vc.depth = 0f;
        vc.funnelPower = 6f;

        DOTween.To(() => vc.radius, x => vc.radius = x, 230f, 10f).SetEase(Ease.InOutSine);
        DOTween.To(() => vc.depth, x => vc.depth = x, 1500f, 20f).SetEase(Ease.InOutSine);

        for (int i = 0; i < vortexObjects.Count; i++)
        {
            int index = i;

            DOTween.To(() => vortexObjects[index].pullSpeed, x => vortexObjects[index].pullSpeed = x, Random.Range(5f, 10f), 15f).SetEase(Ease.InOutSine);
        }

        yield return new WaitForSeconds(5f);

        DOTween.To(() => vc.funnelPower, x => vc.funnelPower = x, 0f, 10f).SetEase(Ease.InOutSine);


        yield return null; // Just to keep the coroutine running if needed
    }
}
