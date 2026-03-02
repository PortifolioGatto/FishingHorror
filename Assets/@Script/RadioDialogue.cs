using UnityEngine;

[CreateAssetMenu(fileName = "RadioDialogue", menuName = "ScriptableObjects/RadioDialogue", order = 1)]
public class RadioDialogue : ScriptableObject
{
    public string name;
    [TextArea(1, 10)]
    public string[] lines;
}
