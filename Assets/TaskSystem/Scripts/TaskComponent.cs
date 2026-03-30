using UnityEngine;
using UnityEngine.Localization;

public class TaskComponent : MonoBehaviour
{
    [SerializeField] private LocalizedString taskName;

    [SerializeField] private Transform[] trackedObjects;

    private GameObject[] trackObjectSpawned;

    public string GetTaskName() => taskName.GetLocalizedString();

    public void SelfStartTask()
    {
        TaskSystem.instance.InitializeTask(this);
    }

    public void InitializeTask()
    {
        trackObjectSpawned = new GameObject[trackedObjects.Length];
        for (int i = 0; i < trackedObjects.Length; i++)
        {
            trackObjectSpawned[i] = Instantiate(TaskSystem.instance.trackObjectPrefab, trackedObjects[i].transform.position, Quaternion.identity);       
        }
    }
    public void DisposeTask()
    {
        for (int i = 0; i < trackObjectSpawned.Length; i++)
        {
            if(trackObjectSpawned[i] != null) Destroy(trackObjectSpawned[i]);
        }

        trackObjectSpawned = null;
    }

}