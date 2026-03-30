using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishDataPaperManager : MonoBehaviour
{
    [SerializeField] private FishDataPaper paper;

    [SerializeField] private Transform visiblePosition;
    [SerializeField] private Transform hiddenPosition;

    [SerializeField] private Ease inEase;
    [SerializeField] private Ease outEase;


    private List<FishInstance> fishesQueue = new List<FishInstance>();

    private Coroutine fishQueueRoutine;
    private bool fishRunning = false;

    public static FishDataPaperManager Instance;

    private void Awake()
    {
        Instance = this;
        fishesQueue = new List<FishInstance>();
    }

    public void ShowFishData(FishInstance fish)
    {
        if(fish != null && !fishesQueue.Contains(fish))
            fishesQueue.Add(fish);

        if(!fishRunning)
        {
            fishQueueRoutine = StartCoroutine(RunFishQueue());
        }
    }

    public void HideFishData(FishInstance fish)
    {
        if(fishesQueue.Contains(fish))
            fishesQueue.Remove(fish);
    }

    private IEnumerator RunFishQueue()
    {
        fishRunning = true;
        
        paper.gameObject.transform.DOKill();

        if (fishesQueue.Count > 0) paper.FillData(fishesQueue[0]);

        yield return paper.gameObject.transform.DOMove(visiblePosition.position, .5f).SetEase(inEase).WaitForCompletion();

        while(fishesQueue.Count > 0)
        {
            if (fishesQueue.Count > 0) paper.FillData(fishesQueue[0]);
            yield return null;
        }
        fishRunning = false;

        paper.gameObject.transform.DOMove(hiddenPosition.position, .5f).SetEase(outEase).WaitForCompletion();

    }

    [ContextMenu("TestIn")]
    public void TestIn()
    {
        paper.gameObject.transform.DOMove(visiblePosition.position, .5f).SetEase(inEase);
    }

    [ContextMenu("TestOut")]
    public void TestOut()
    {
        paper.gameObject.transform.DOMove(hiddenPosition.position, .5f).SetEase(outEase);
    }
}
