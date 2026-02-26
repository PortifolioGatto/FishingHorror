using UnityEngine;

[CreateAssetMenu(fileName = "SFXGroup", menuName = "Audio/SFX Group", order = 1)]
public class SFXGroup : ScriptableObject
{
    public string groupName;
    public AudioClip[] audioClips;

    public AudioClip GetRandomClip()
    {
        if (audioClips == null || audioClips.Length == 0)
        {
            Debug.LogWarning($"SFX Group '{groupName}' has no audio clips assigned.");
            return null;
        }
        int randomIndex = Random.Range(0, audioClips.Length);
        return audioClips[randomIndex];
    }
}
