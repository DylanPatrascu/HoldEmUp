using System;
using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerChair : MonoBehaviour, IInteractable
{
    private bool _interactable = false;
    [SerializeField] private SpriteRenderer _interactSprite;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_interactSprite == null)
        {
            throw new Exception("PlayerChair: _interactSprite is null");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetInteractable(bool state)
    {
        _interactable = state;
        _interactSprite.enabled = state;
    }

    public void Interact()
    {
        print("Player Chair Interacted");
    }
}
