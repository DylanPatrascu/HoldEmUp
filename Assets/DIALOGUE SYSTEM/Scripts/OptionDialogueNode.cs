using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class OptionDialogueNode : DialogueNode
{
	[Output] public DialogueNode optionA;
	[Output] public DialogueNode optionB;
	[Output] public DialogueNode optionC;
	[Output] public DialogueNode finished;

}
