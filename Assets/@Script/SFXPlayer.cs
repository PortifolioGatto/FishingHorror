using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    public string sfxName = string.Empty;

    public float volume = 1f;

    public void PlaySFX()
    {
        AudioManager.Instance.PlaySFX(sfxName, transform.position, volume);
    }
}