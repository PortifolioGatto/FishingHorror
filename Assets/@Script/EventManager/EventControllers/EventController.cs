using UnityEngine;

public abstract class EventController : MonoBehaviour
{
    public bool startOnStart = false;


    public abstract void StartEvent();
    public abstract void UpdateEvent();
    public abstract void EndEvent();
}
