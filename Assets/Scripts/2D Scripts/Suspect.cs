using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Suspect : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interact;
    [SerializeField] private ClubSceneManager clubSceneManager;
    
    [Space]
    [Header("Dialogue Trees")]
    [SerializeField] private DialogueTree roundOneDialogueTree;
    [SerializeField] private DialogueTree roundTwoDialogueTree;
    [SerializeField] private DialogueTree roundThreeDialogueTree;
    [SerializeField] private DialogueTree roundFourDialogueTree;
    
    private bool _interactable = false;
    private DialogueTree[] _trees;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        _trees = new DialogueTree[]
            { roundOneDialogueTree, roundTwoDialogueTree, roundThreeDialogueTree, roundFourDialogueTree };
        
        if (interact == null)
        {
            throw new Exception(name + ": _interactSprite is null");
        }

        foreach (DialogueTree tree in _trees)
        {
            if (tree == null || tree.nodes[0] == null)
            {
                throw new Exception(name + ": _dialogueTree "+ tree.name +" is null");
            }
        }

        if (clubSceneManager == null)
        {
            throw new Exception(name + ": clubSceneManager is null");
        }

        foreach (DialogueTree tree in _trees)
        {
            foreach (var node in tree.nodes)
            {
                DialogueNode dNode = node as DialogueNode;
                dNode.hasBeenAsked = false;
            }
        }
    }

    public void SetInteractable(bool state)
    {
        _interactable = state;
        interact.SetActive(state);
    }

    public void Interact()
    {
        StartDialogue();
    }

    private void StartDialogue()
    {
        int round = clubSceneManager.CurrentClubRound;
        DialogueTree currentDialogueTree = _trees[round - 1];
        DialogueManager dialogueManager = FindAnyObjectByType<DialogueManager>();
        print(currentDialogueTree + ", round: " + round + ", dialoguemanager: " + dialogueManager);
        
        dialogueManager.StartDialogueTree(currentDialogueTree);
    }
}
