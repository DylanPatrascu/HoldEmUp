using System;
using UnityEngine;

public class Suspect : MonoBehaviour, IInteractable
{
    private bool _interactable = false;
    [SerializeField] private SpriteRenderer _interactSprite;
    [SerializeField] private DialogueTree _dialogueTree;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        if (_interactSprite == null)
        {
            throw new Exception(name + ": _interactSprite is null");
        }
        if (_dialogueTree == null || _dialogueTree.nodes[0] == null)
        {
            throw new Exception(name + ": _dialogueTree is null");
        }
    }

    public void SetInteractable(bool state)
    {
        _interactable = state;
        _interactSprite.enabled = state;
    }

    public void Interact()
    {
        StartDialogue();
    }

    private void StartDialogue()
    {
        FindAnyObjectByType<DialogueManager>().StartDialogue(_dialogueTree.nodes[0]);
    }
}
