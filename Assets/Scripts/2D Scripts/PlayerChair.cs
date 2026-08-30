using System;
using UnityEngine;

public class PlayerChair : MonoBehaviour, IInteractable
{
    private bool _interactable = false;
    [SerializeField] private GameObject interact;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (interact == null)
        {
            throw new Exception("PlayerChair: interactSprite is null");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetInteractable(bool state)
    {
        _interactable = state;
        interact.SetActive(state);
    }

    public void Interact()
    {
        GameManager.Instance.DisplayConfirmationMenu();
    }
}
