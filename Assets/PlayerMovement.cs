using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float drag = 5f;

    [SerializeField] private float chanceToCreak = .25f;
    [SerializeField] private float stepSoundInterval = 0.5f;
    private float stepSoundTimer = 0f;

    private Rigidbody rb;
    private Blocker blocker;
    [SerializeField] private InputActionReference moveInput;

    private Vector3 moveVelocity;
    private Vector2 moveInputValue;

    public static PlayerMovement Instance { get; private set; }

    public bool IsMoving => GetIsMoving();

    private void Awake()
    {
        Instance = this;

        blocker = GetComponent<Blocker>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        moveInputValue = moveInput.action.ReadValue<Vector2>();

        float horizontal = moveInputValue.x;
        float vertical = moveInputValue.y;

        Vector3 forward = PlayerCamera.Orientation.forward;
        Vector3 right = PlayerCamera.Orientation.right;

        Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

        moveVelocity = moveDirection * moveSpeed * 10;

        HandleStepSound();
    }

    private void FixedUpdate()
    {
        rb.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration); // Aplicar gravidade

        if (blocker.isBlocking)
        {
            rb.linearVelocity = Vector3.zero; // Parar o movimento
            return;
        }

        rb.AddForce(moveVelocity, ForceMode.Acceleration);

        Vector3 curVel = rb.linearVelocity;
        curVel.y = 0f; // Manter a velocidade vertical inalterada

        if(curVel.magnitude > moveSpeed)
        {
            curVel = curVel.normalized * moveSpeed;
            
            rb.linearVelocity = new Vector3(curVel.x, rb.linearVelocity.y, curVel.z);
        }

        if(moveInputValue == Vector2.zero)
        {
            // Aplicar arrasto
            Vector3 dragForce = -curVel * drag;
            rb.AddForce(dragForce, ForceMode.Acceleration);
        }
        
    }

    private void HandleStepSound()
    {
        if (GetIsMoving())
        {
            stepSoundTimer += Time.deltaTime;
            if (stepSoundTimer >= stepSoundInterval)
            {
                stepSoundTimer = 0f;

                AudioManager.Instance.PlaySFX("woodstep", transform.position + Vector3.down, 1f);

                if (Random.value < chanceToCreak)
                {
                    AudioManager.Instance.PlaySFX("creaksfx", transform.position + Vector3.down, 1f);
                }
            }
        }
        else
        {

            stepSoundTimer = 0f; // Resetar o timer quando o jogador parar de se mover

        }
    }

    private bool GetIsMoving()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f; // Ignorar a componente vertical
        return horizontalVelocity.magnitude > 0.1f; // Considerar como movimento se a velocidade for maior que um pequeno valor
    }
}
