using System;
using UnityEngine;

[Serializable]
public class RandomDialogueOption
{
    public int Index { get; private set; }
    public string Text { get; private set; }
    public string PortName { get; private set; }

    public RandomDialogueOption(
        int index,
        string text,
        string portName)
    {
        Index = index;
        Text = text;
        PortName = portName;
    }
}