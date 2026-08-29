using UnityEngine;

public class ChipStacks : MonoBehaviour
{
    public Transform Tens;
    public Transform Ones;

    public void ClearStacks()
    {
        for (int i = 0; i < Tens.transform.childCount - 1; i++)
        {
            Destroy(Tens.transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < Ones.transform.childCount - 1; i++)
        {
            Destroy(Ones.transform.GetChild(i).gameObject);
        }
    }
}