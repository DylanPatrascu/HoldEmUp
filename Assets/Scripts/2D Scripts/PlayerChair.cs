using System;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerChair : MonoBehaviour, IInteractable
{
    private bool _interactable = false;
    [SerializeField] private SpriteRenderer interactSprite;
    [SerializeField] private ClubSceneManager manager;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (manager == null)
        {
            throw new Exception("PlayerChair: manager is null");
        }
        if (interactSprite == null)
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
        interactSprite.enabled = state;
    }

    public void Interact()
    {
        manager.DisplayConfirmationMenu();
    }
    
    
}
