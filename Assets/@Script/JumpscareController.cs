using UnityEngine;
using UnityEngine.SceneManagement;

public class JumpscareController : MonoBehaviour
{
    [SerializeField] private Animator anim;

    [SerializeField] private AudioSource jumpscare_sfx;

    public static JumpscareController instance;

    private void Awake()
    {
        instance = this;
    }

    public void PlayJumpscare()
    {
        anim.SetTrigger("start");
    }

    public void PlayAudio()
    {
        jumpscare_sfx.Play();
    }
    
    public void GoToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
