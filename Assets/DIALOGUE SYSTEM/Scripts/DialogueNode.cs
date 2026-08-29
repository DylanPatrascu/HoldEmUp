using XNode;

public class DialogueNode : Node
{
    [Input] public Node prevNode;
    public Dialogue speaker;
    public bool hasBeenAsked { get; set; } = false;
}