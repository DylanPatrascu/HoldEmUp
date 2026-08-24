using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class DialogueControlNode : Node {

	public enum option {endDialogue, continueDialogue, restartDialogue};
	public option dialogueControl;
	
	[Input] public Node prevNode;
}