using System.Collections.Generic;
using UnityEngine;
using XNode;

public class RandomOptionPoolNode : DialogueNode
{
    [Output(dynamicPortList = true)]
    public List<string> questions = new List<string>();

    [Output]
    public Node finished;

    [SerializeField]
    private int numberToShow = 2;

    private HashSet<int> usedOptions = new HashSet<int>();


    public List<RandomDialogueOption> GetRandomOptions()
    {
        List<int> availableIndexes = new List<int>();

        for (int i = 0; i < questions.Count; i++)
        {
            if (!usedOptions.Contains(i))
            {
                availableIndexes.Add(i);
            }
        }

        List<RandomDialogueOption> selected =
            new List<RandomDialogueOption>();

        int amount =
            Mathf.Min(numberToShow, availableIndexes.Count);

        for (int i = 0; i < amount; i++)
        {
            int randomIndex =
                Random.Range(0, availableIndexes.Count);

            int questionIndex =
                availableIndexes[randomIndex];

            selected.Add(
                new RandomDialogueOption(
                    questionIndex,
                    questions[questionIndex],
                    "questions " + questionIndex
                )
            );

            availableIndexes.RemoveAt(randomIndex);
        }

        return selected;
    }


    public void UseOption(RandomDialogueOption option)
    {
        usedOptions.Add(option.Index);
    }


    public void ResetPool()
    {
        usedOptions.Clear();
    }
}