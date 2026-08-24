using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueTree tree;

    public void TriggerDialog()
    {
        FindAnyObjectByType<DialogueManager>().StartDialogue(tree.nodes[0]);
    }
}
