using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private bool actionMode = false;
    public void Start()
    {
        PokerGameManager.Instance.GameStateChanged += ActivateActionMode;
    }

    public void Update()
    {
        if (actionMode)
        {
            Debug.Log("Listening to player input");
        }
    }

    public void ActivateActionMode(object sender, EventArgs e)
    {
        actionMode = true;
    }
}