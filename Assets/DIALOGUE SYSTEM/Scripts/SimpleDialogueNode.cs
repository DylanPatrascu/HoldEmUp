using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using XNode;

public class SimpleDialogueNode : DialogueNode
{
	[Output] public Node nextNode;
}