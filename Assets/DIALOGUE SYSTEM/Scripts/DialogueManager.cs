using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.VisualScripting;
using XNode;
using Debug = UnityEngine.Debug;

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text nameText;
    public TMP_Text sentenceText;
    public Button optionAButton, optionBButton;
    public TMP_Text optionAText, optionBText;
    public Image portrait;
    public AudioClip panelOpen, panelClose;
    public Image nextElement;
    
    [Space]
    [Header("Variables")]
    public Vector3 showPanelPos = new Vector3(0,-140,0);
    public Vector3 hidePanelPos = new Vector3(0, -800, 0);
    public float panelAnimationTime = 1;
    public float textSpeed = 0.01f;
    
    [Space]
    [Header("Club Scene Manager")]
    public ClubSceneManager clubSceneManager;

    Node curNode;
    Queue<string> sentences = new Queue<string>();
    AudioSource source;
    AudioClip talkingClip;
    
    private List<RandomDialogueOption> currentRandomOptions;
    
    // Overflow page variables
    private int currentPage = 1;
    private int totalPages = 1;
    private bool isRenderingText = false;

    void Start()
    {
        source = GetComponent<AudioSource>();
        nextElement.enabled = false;
    }

    public void StartDialogue(Node rootNode)
    {
        StopAllCoroutines();

        currentRandomOptions = null;
        curNode = rootNode;

        switch (curNode)
        {
            // RANDOM OPTION POOL NODE
            case RandomOptionPoolNode randomPool:
                print("random option pool");
                DisplayRandomOptionPoolNode(randomPool);
                break;
            case OptionDialogueNode optionNode:
                print("option node");
                DisplayOptionNode(optionNode);
                break;
            case SimpleDialogueNode simpleNode:
                print("simple node");
                DisplaySimpleNode(simpleNode);
                break;
            case DialogueControlNode controlNode:
                print("control node");
                DisplayControlNode(controlNode);
                break;
            default:
                Debug.LogWarning("Unknown Dialogue Node in tree");
                break;
        }
    }
    
    // ---- DISPLAY CURRENT NODE ----
    private void DisplayRandomOptionPoolNode(RandomOptionPoolNode pool)
    {
        //load node for speaker
        RandomOptionPoolNode options = curNode as RandomOptionPoolNode;
        Dialogue dialogue = options.speaker;
            
        RenderUI(pool);
        
        currentRandomOptions = pool.GetRandomOptions();

        // No questions left
        if (currentRandomOptions.Count == 0 ||
            (clubSceneManager is not null && !clubSceneManager.CanAskQuestion()))
        {
            NodePort finishedPort = pool.GetOutputPort("finished")?.Connection;

            if (finishedPort != null)
            {
                StartDialogue(finishedPort.node);
            }
            else
            {
                EndDialogue();
            }

            return;
        }

        optionAText.text = currentRandomOptions[0].Text;

        if (currentRandomOptions.Count > 1)
        {
            optionBText.text = currentRandomOptions[1].Text;
        }
        else
        {
            optionBText.text = "";
        }
        
        enqueueSentences(dialogue);
    }

    private void DisplaySimpleNode(SimpleDialogueNode node)
    {
        Dialogue dialogue = node.speaker;
        
        RenderUI(node);
        
        enqueueSentences(dialogue);
    }

    private void DisplayOptionNode(OptionDialogueNode node)
    {
        Dialogue dialogue = node.speaker;
            
        RenderUI(node);
            
        //load responses
        optionAText.text = node.responses.sentences[0];
        optionBText.text = node.responses.sentences[1];

        enqueueSentences(dialogue);
    }

    private void DisplayControlNode(DialogueControlNode node)
    {
        //load node for speaker
            
        if (node.dialogueControl == DialogueControlNode.option.endDialogue)
        {
            EndDialogue();
        }
        else if (node.dialogueControl == DialogueControlNode.option.continueDialogue)
        {
            //continue Dialogue
        }
        else
        {
            //restart Dialogue
        }
    }

    // ---- TRIGGER FUNCTIONS ----

    public void OnClick()
    {
        print("click");
        // Don't advance dialogue when the player is choosing an option
        if (enabled && (optionAButton.gameObject.activeSelf ||
            optionBButton.gameObject.activeSelf))
        {
            return;
        }
        
        nextElement.enabled = false;

        // Currently typing -> reveal the rest of this page
        if (isRenderingText)
        {
            FinishCurrentPage();
            return;
        }

        // More pages in the current sentence
        if (currentPage < totalPages)
        {
            currentPage++;
            StartCoroutine(RenderPage());
            return;
        }

        // More sentences in the current node
        if (sentences.Count > 0)
        {
            DisplaySentence();
            return;
        }

        // Simple node is completely finished.
        // Player clicked again, so advance.
        if (curNode is SimpleDialogueNode)
        {
            DisplayNextSimple();
        }
    }
    
    // Triggered by either option A or option B button. Switches to next node and removes the option from the random pool.
    public void DisplayNextOption(string option)
    {
        if (clubSceneManager is not null)
        {
            clubSceneManager.AskedAQuestion();
        }
        // RANDOM OPTION POOL
        if (curNode is RandomOptionPoolNode randomPool)
        {
            int index = option == "A" ? 0 : 1;

            if (currentRandomOptions == null ||
                index >= currentRandomOptions.Count)
            {
                return;
            }

            RandomDialogueOption selectedOption =
                currentRandomOptions[index];

            // Permanently mark this question as used
            randomPool.UseOption(selectedOption);

            // Follow the output belonging to this question
            NodePort port =
                randomPool.GetOutputPort(selectedOption.PortName)?.Connection;

            if (port != null)
            {
                StartDialogue(port.node);
            }

            return;
        }

        // NORMAL OPTION NODE
        if (curNode is OptionDialogueNode optionNode)
        {
            NodePort port;

            if (option == "A")
            {
                port = optionNode.GetOutputPort("optionA")?.Connection;
            }
            else
            {
                port = optionNode.GetOutputPort("optionB")?.Connection;
            }

            if (port != null)
            {
                StartDialogue(port.node);
            }
        }
    }

    // Triggered by clicking the next button on a simple dialogue node, switches to next node
    public void DisplayNextSimple()
    {
        if (curNode is not SimpleDialogueNode simpleNode)
            return;

        NodePort port =
            simpleNode.GetOutputPort("nextNode")?.Connection;

        if (port != null)
        {
            StartDialogue(port.node);
        }
        else
        {
            EndDialogue();
        }
    }
    
    // ---- UI RENDERING ----
    private void RenderUI(DialogueNode dialogueNode)
    {
        if (dialogueNode.speaker is null)
        {
            Debug.LogError("Dialogue node [" + dialogueNode.name + "] has no speaker");
            return;
        }

        Dialogue dialogue = dialogueNode.speaker;

        nameText.text = dialogue.name;
        portrait.sprite = dialogue.portrait;
        talkingClip = dialogue.talkingClip;
        sentenceText.text = "";

        // Options only appear once dialogue text is finished.
        displayOptionButtons(false);
    }

    private void OnDialogueTextFinished()
    {
        switch (curNode)
        {
            case RandomOptionPoolNode:
                // Player should choose an option, not click to continue
                nextElement.enabled = false;

                optionAButton.gameObject.SetActive(true);

                optionBButton.gameObject.SetActive(
                    currentRandomOptions != null &&
                    currentRandomOptions.Count > 1
                );

                break;

            case OptionDialogueNode:
                // Player should choose an option
                nextElement.enabled = false;
                displayOptionButtons(true);
                break;

            case SimpleDialogueNode:
                // Player must click to advance to the next node
                nextElement.enabled = true;
                break;
        }
    }

    private void displayOptionButtons(bool value)
    {
        optionAButton.gameObject.SetActive(value);
        optionBButton.gameObject.SetActive(value);
    }

    // ---- SENTENCE RENDERING ----

    private void enqueueSentences(Dialogue dialogue)
    {
        sentences.Clear();
        for (int i = 0; i < dialogue.sentences.Length; i++)
        {
            sentences.Enqueue(dialogue.sentences[i]);
        }

        source.PlayOneShot(panelOpen);
        transform.DOLocalMove(showPanelPos, panelAnimationTime).OnComplete(() => DisplaySentence());
    }
    public void DisplaySentence()
    {
        if (sentences.Count == 0)
            return;

        StopAllCoroutines();

        string sentence = sentences.Dequeue();

        sentenceText.text = sentence;

        currentPage = 1;

        sentenceText.pageToDisplay = currentPage;

        sentenceText.ForceMeshUpdate();

        totalPages = sentenceText.textInfo.pageCount;

        StartCoroutine(RenderPage());
    }
    
    private IEnumerator RenderPage()
    {
        isRenderingText = true;
        nextElement.enabled = false;

        sentenceText.pageToDisplay = currentPage;
        sentenceText.ForceMeshUpdate();

        TMP_PageInfo pageInfo =
            sentenceText.textInfo.pageInfo[currentPage - 1];

        int firstCharacter = pageInfo.firstCharacterIndex;
        int lastCharacter = pageInfo.lastCharacterIndex;

        sentenceText.maxVisibleCharacters = firstCharacter;

        for (int i = firstCharacter; i <= lastCharacter; i++)
        {
            sentenceText.maxVisibleCharacters = i + 1;

            if (i % 4 == 0)
            {
                source.PlayOneShot(talkingClip);
            }

            yield return new WaitForSeconds(textSpeed);
        }

        isRenderingText = false;

        bool finalPage = currentPage == totalPages;
        bool finalSentence = sentences.Count == 0;

        if (finalPage && finalSentence)
        {
            OnDialogueTextFinished();
        }
        else
        {
            // There is another page or sentence,
            // so prompt the player to continue.
            nextElement.enabled = true;
        }
    }
    
    

    private void FinishCurrentPage()
    {
        StopAllCoroutines();

        TMP_PageInfo pageInfo =
            sentenceText.textInfo.pageInfo[currentPage - 1];

        sentenceText.maxVisibleCharacters =
            pageInfo.lastCharacterIndex + 1;

        isRenderingText = false;

        bool finalPage = currentPage == totalPages;
        bool finalSentence = sentences.Count == 0;

        if (finalPage && finalSentence)
        {
            OnDialogueTextFinished();
        }
        else
        {
            nextElement.enabled = true;
        }
    }

    public void EndDialogue()
    {
        StopAllCoroutines();
        source.PlayOneShot(panelClose);
        transform.DOLocalMove(hidePanelPos, panelAnimationTime);
    }
    
}
