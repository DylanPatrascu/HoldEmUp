using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class OptionDialogueNode : DialogueNode
{
	public Dialogue responses;
	
	[Output] public Node optionA;
	[Output] public Node optionB;
}
