using UnityEngine;
using UnityEngine.InputSystem;

public class Player2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private IInteractable currentInteractable;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnInteract(InputValue value)
    {
        print(currentInteractable);
        if (value.isPressed)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, moveInput.y * moveSpeed);
    }    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            if (currentInteractable != null)
            {
                Debug.LogWarning(
                    $"Player entered another interactable while one is already active. " +
                    $"Incoming collider: {other.name}"
                );

                return;
            }

            currentInteractable = interactable;
            currentInteractable.SetInteractable(true);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            if (interactable == currentInteractable)
            {
                currentInteractable.SetInteractable(false);
                currentInteractable = null;
            }
        }
    }
}
