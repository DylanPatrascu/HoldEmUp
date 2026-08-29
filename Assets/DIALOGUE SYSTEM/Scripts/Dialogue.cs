
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Scriptable Objects/Dialogue")]
public class Dialogue : ScriptableObject
{
    public string dialogueName;
    public Sprite portrait;
    public AudioClip talkingClip;

    [TextArea(3,10)]
    public string questionLeadingToDialogue = "";
    [TextArea(3,10)]
    public string[] sentences;
}
