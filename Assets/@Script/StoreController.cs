using UnityEngine;

public class StoreController : MonoBehaviour
{
    public void SailButton()
    {
        GameManager.Instance.NextDay();
    }
}
