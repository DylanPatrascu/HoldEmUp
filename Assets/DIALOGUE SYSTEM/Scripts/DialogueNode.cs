using XNode;
using UnityEngine;

public class DialogueNode : Node
{
    [Input] public Node prevNode;
    public string LeadingQuestion;
    public string[] Sentences;
    public bool lying;
    public bool hasBeenAsked { get; set; } = false;
}