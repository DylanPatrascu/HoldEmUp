using UnityEngine;

public enum ChipLocation
{
    Stack,
    Table
}

public class Chip : MonoBehaviour
{
    public int chipValue = 1;
    public ChipLocation location = ChipLocation.Stack;

    [SerializeField]
    private int interactableLayer;

    [SerializeField]
    private int lockedLayer;

    public bool IsLocked { get; private set; }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        gameObject.layer = locked ? lockedLayer : interactableLayer; 
    }
}