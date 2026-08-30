using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class DialogueControlNode : DialogueNode {

	public enum option {endDialogue, continueDialogue, restartDialogue};
	public option dialogueControl;
}