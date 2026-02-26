using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [SerializeField] private Blocker playerBlocker;

    private void Awake()
    {
        Instance = this;
    }

    public void SetBlocker(bool blocker)
    {
        playerBlocker.isBlocking = blocker;
    }
}
