using DG.Tweening;
using TMPro;
using UnityEngine;

public class TaskSystem : MonoBehaviour
{
    public GameObject trackObjectPrefab;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI taskNameText;
    [SerializeField] private CanvasGroup canvasGroup;

    private TaskComponent currentTask;

    public static TaskSystem instance => _instance;
    private static TaskSystem _instance;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        canvasGroup.alpha = 0f;
    }

    public void InitializeTask(TaskComponent task)
    {
        DisposeTask();

        currentTask = task;

        currentTask.InitializeTask();

        taskNameText.text = task.GetTaskName();

        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, .5f);
    }

    public void DisposeTask()
    {
        if (currentTask != null)
        {
            currentTask.DisposeTask();
        }

        currentTask = null;

        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, .5f);

    }
}
