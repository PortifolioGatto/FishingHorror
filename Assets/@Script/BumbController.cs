using UnityEngine;

public class BumbController : MonoBehaviour
{
    [SerializeField] private AudioSource bumpSound;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private BoatMovement boatMovement;

    public static BumbController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }


    public void TriggerBump(float magnitude)
    {
        // Toca o som do bump, ajustando o volume pela magnitude
        if (bumpSound != null)
        {
            bumpSound.volume = Mathf.Clamp01(magnitude);
            bumpSound.Play();
        }
        // Dispara o shake na câmera e no barco
        if (playerCamera != null)
        {
            playerCamera.ShakeCamera(magnitude, 0.25f); // Duração fixa de 0.5s, pode ser ajustada
        }
        if (boatMovement != null)
        {
            boatMovement.ShakeBoat(magnitude, 0.25f);
        }
    }
}
