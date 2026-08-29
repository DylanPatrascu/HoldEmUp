using XNode;

public class DialogueNode : Node
{
    [Input] public Node prevNode;
    public string LeadingQuestion;
    public string[] Sentences;
    public bool hasBeenAsked { get; set; } = false;
}