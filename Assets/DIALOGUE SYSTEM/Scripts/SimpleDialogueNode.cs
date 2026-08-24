using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class SimpleDialogueNode : Node
{
	public Dialogue sentence;
	
	[Input] public Node prevNode;
	
	[Output] public Node nextNode;
}