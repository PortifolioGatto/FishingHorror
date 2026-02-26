using DG.Tweening;
using UnityEngine;

public class AcceleratorPoints : MonoBehaviour
{
    [SerializeField] private Vector3[] points = new Vector3[3];
    [SerializeField] private int targetPointIndex = 0;

    [ContextMenu("Register Point")]
    private void RegisterPoint()
    {
        points[targetPointIndex] = transform.localPosition;
    }

    [ContextMenu("Go To Point")]
    public void GoToPoint(int point)
    {
        transform.DOKill();
        transform.DOLocalMove(points[point], 0.5f).SetEase(Ease.InOutSine);
    }

    private void Update()
    {
        
    }
}
