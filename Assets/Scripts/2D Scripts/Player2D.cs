using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class Player2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    private SpriteRenderer playerSprite;
    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private IInteractable currentInteractable;
    private bool facingRight = true;
    private bool isMoving = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerSprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }
    
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnChipInteract(InputValue value)
    {
        if (value.isPressed)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
        }
    }

    public void OnClick(InputValue value)
    {
        FindAnyObjectByType<DialogueManager>().OnClick();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, moveInput.y * moveSpeed);
        if (moveInput.x > 0 && !facingRight)
        {
            playerSprite.flipX = false;
            facingRight = true;
        }
        else if (moveInput.x < 0 && facingRight)
        {
            playerSprite.flipX = true;
            facingRight = false;
        }
        animator.SetFloat("Speed", Mathf.Abs(moveInput.x));
        
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
