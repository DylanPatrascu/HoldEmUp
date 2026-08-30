using UnityEngine;
using UnityEngine.InputSystem;

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

    private InputAction moveAction;
    private InputAction interactAction;
    private InputAction clickAction;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerSprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerInputSystem == null)
        {
            Debug.LogWarning("Player2D enabled before GameManager/PlayerInput was ready.");
            return;
        }

        var actions = GameManager.Instance.PlayerInputSystem.actions;

        moveAction = actions.FindAction("Move");
        interactAction = actions.FindAction("Interact");
        clickAction = actions.FindAction("Click");

        if (interactAction != null) interactAction.performed += OnInteractPerformed;
        if (clickAction != null) clickAction.performed += OnClickPerformed;
    }

    private void OnDisable()
    {
        if (interactAction != null) interactAction.performed -= OnInteractPerformed;
        if (clickAction != null) clickAction.performed -= OnClickPerformed;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        currentInteractable?.Interact();
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        FindAnyObjectByType<DialogueManager>()?.OnClick();
    }

    private void FixedUpdate()
    {
        moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

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
        isMoving = moveInput.sqrMagnitude > 0f;
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
