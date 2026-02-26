using UnityEngine;

[CreateAssetMenu(menuName = "EventData", fileName = "New Event Data")]
public class EventData : ScriptableObject
{
    public string eventName;
    public string description;

    public EventController eventController_prefab;
}
