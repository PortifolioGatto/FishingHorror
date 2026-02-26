using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private TextMeshProUGUI interactionText;

    [SerializeField] private InputActionReference interactionInput;
    private IInteractable interactable;

    private RaycastHit hit;

    private Blocker blocker;

    private void Awake()
    {
        blocker = GetComponent<Blocker>();
    }

    private void Start()
    {
        interactionInput.action.performed += OnInteractionPerformed;
        interactionInput.action.Enable();
    }

    private void Update()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, interactionRange, interactableLayer))
        {
            IInteractable hitInteractable = hit.collider.GetComponent<IInteractable>();

            if (hitInteractable != interactable)
            {
                interactable?.OnHoverExit();
            }


            interactable = hitInteractable;
            
            if(interactable != null)
            {
                interactionText?.SetText(interactable.GetInteractionText());
                interactable?.OnHover();
            }
        }
        else
        {
            if (interactable != null)
            {
                interactionText?.SetText("");
                interactable?.OnHoverExit();
            }

            interactable = null;
        }
    }

    private void OnInteractionPerformed(InputAction.CallbackContext context)
    {
        if (interactable != null)
        {
            interactable.Interact();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * interactionRange);
    }
}
