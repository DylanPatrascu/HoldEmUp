using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using XNode;

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")] public TMP_Text nameText;
    public TMP_Text sentenceText;

    public Button optionAButton;
    public Button optionBButton;
    public Button optionCButton;

    public TMP_Text optionAText;
    public TMP_Text optionBText;
    public TMP_Text optionCText;

    public Image bribeImage;
    public TMP_Text bribeText;

    public Image portrait;
    public Image nextElement;

    [Header("Audio")] public AudioClip panelOpen;
    public AudioClip panelClose;

    [Header("Dialogue Settings")] public Vector3 showPanelPos = new Vector3(0, -140, 0);
    public Vector3 hidePanelPos = new Vector3(0, -800, 0);
    public float panelAnimationTime = 1f;
    public float textSpeed = 0.01f;

    [Header("Club Scene Manager")] public ClubSceneManager clubSceneManager;


    // Current dialogue state
    private Node curNode;
    private readonly Queue<string> sentences = new();
    private List<DialogueNode> curUnaskedQuestions;
    private DialogueTree currentDialogueTree;

    // Audio
    private AudioSource source;
    private AudioClip talkingClip;

    // Text/page state
    private int currentPage = 1;
    private int totalPages = 1;
    private bool isRenderingText;

    private Coroutine renderCoroutine;

    private Button[] optionButtons;
    private TMP_Text[] optionText;
    private Tween panelTween;
    private bool active = false;


    /// <summary>
    /// Initializes the dialogue manager and verifies its required components.
    /// </summary>
    private void Start()
    {
        optionButtons = new Button[] { optionAButton, optionBButton, optionCButton };
        optionText = new TMP_Text[] { optionAText, optionBText, optionCText };
        curUnaskedQuestions = new List<DialogueNode>();
        source = GetComponent<AudioSource>();

        if (source == null)
        {
            Debug.LogError(
                "DialogueManager requires an AudioSource component.",
                this
            );
        }

        if (!ValidateUIReferences())
        {
            Debug.LogError(
                "DialogueManager is missing required UI references.",
                this
            );

            enabled = false;
            return;
        }

        nextElement.enabled = false;

        HideOptionButtons();
        HideBribe();
    }


    /// <summary>
    /// Checks that all required UI references have been assigned in the Inspector.
    /// </summary>
    private bool ValidateUIReferences()
    {
        return nameText != null &&
               sentenceText != null &&
               optionAButton != null &&
               optionBButton != null &&
               optionCButton != null &&
               optionAText != null &&
               optionBText != null &&
               optionCText != null &&
               portrait != null &&
               nextElement != null &&
               bribeImage != null &&
               bribeText != null;
    }

    public void StartDialogueTree(DialogueTree dialogueTree)
    {
        if (active) return;
        currentDialogueTree = dialogueTree;
        StartDialogue(dialogueTree.nodes[0]);
        active = true;
    }


    /// <summary>
    /// Starts displaying the supplied dialogue node and dispatches it
    /// to the appropriate handler based on its node type.
    /// </summary>
    public void StartDialogue(Node node)
    {
        curUnaskedQuestions.Clear();
        
        panelTween?.Kill();
        panelTween = null;

        if (node == null)
        {
            Debug.LogError("Cannot start dialogue from a null node.");
            EndDialogue();
            return;
        }

        StopTextRendering();

        curNode = node;

        nextElement.enabled = false;

        HideOptionButtons();
        HideBribe();

        switch (curNode)
        {
            case OptionDialogueNode optionNode:
                DisplayOptionNode(optionNode);
                break;

            case SimpleDialogueNode simpleNode:
                DisplaySimpleNode(simpleNode);
                break;

            case DialogueControlNode controlNode:
                DisplayControlNode(controlNode);
                break;

            default:
                Debug.LogWarning(
                    $"Unknown dialogue node type: {curNode.GetType().Name}",
                    this
                );
                break;
        }
    }


    // -------------------------------------------------------------------------
    // NODE DISPLAY
    // -------------------------------------------------------------------------


    /// <summary>
    /// Displays a simple dialogue node containing normal sequential dialogue.
    /// </summary>
    private void DisplaySimpleNode(SimpleDialogueNode node)
    {
        if (!TryPrepareDialogueNode(node))
            return;

        EnqueueSentences(node);
    }


    /// <summary>
    /// Displays a normal option node and prepares its two response choices.
    /// </summary>
    private void DisplayOptionNode(OptionDialogueNode node)
    {
        if (!clubSceneManager.CanAskQuestion())
        {
            NodePort finishedPort =
                node.GetOutputPort("finished")?.Connection;

            if (finishedPort != null)
            {
                StartDialogue(finishedPort.node);
            }
            else Debug.LogWarning("Finished Dialogue node is missing");

            return;
        }
        
        if (!TryPrepareDialogueNode(node))
            return;

        AssignUnaskedQuestions(node);

        // AssignUnaskedQuestions may have moved us to the finished node.
        if (curNode != node)
            return;

        EnqueueSentences(node);
    }


    /// <summary>
    /// Executes a dialogue control node such as ending, continuing,
    /// or restarting dialogue.
    /// </summary>
    private void DisplayControlNode(DialogueControlNode node)
    {
        switch (node.dialogueControl)
        {
            case DialogueControlNode.option.endDialogue:
                EndDialogue();
                break;

            case DialogueControlNode.option.continueDialogue:
                Debug.LogWarning(
                    $"Continue dialogue is not yet implemented on [{node.name}]."
                );
                break;

            default:
                Debug.LogWarning(
                    $"Restart dialogue is not yet implemented on [{node.name}]."
                );
                break;
        }
    }


    /// <summary>
    /// Validates a dialogue node's speaker and prepares the shared UI.
    /// Returns false if the node cannot safely be displayed.
    /// </summary>
    private bool TryPrepareDialogueNode(DialogueNode node)
    {
        if (node == null)
        {
            Debug.LogError("Attempted to display a null dialogue node.");
            return false;
        }

        RenderUI();

        return true;
    }


    // -------------------------------------------------------------------------
    // PLAYER INPUT / NODE TRANSITIONS
    // -------------------------------------------------------------------------


    /// <summary>
    /// Handles the player's general dialogue click.
    /// A click first finishes currently typing text, then advances pages,
    /// sentences, or simple dialogue nodes.
    /// </summary>
    public void OnClick()
    {
        // Ignore general dialogue clicks while the player
        // is expected to choose an option.
        if (AreOptionButtonsVisible())
            return;

        nextElement.enabled = false;

        // First click while typing instantly reveals the current page.
        if (isRenderingText)
        {
            FinishCurrentPage();
            return;
        }

        // Advance to the next page of the current sentence.
        if (currentPage < totalPages)
        {
            currentPage++;
            StartPageRendering();
            return;
        }

        // Advance to the next sentence in the current node.
        if (sentences.Count > 0)
        {
            DisplaySentence();
            return;
        }

        // Simple nodes wait for one final click before moving on.
        if (curNode is SimpleDialogueNode)
        {
            DisplayNextSimple();
        }
    }


    /// <summary>
    /// Handles selecting option A or B and follows the corresponding
    /// graph connection. Random options are also marked as used.
    /// </summary>
    public void DisplayNextOption(int index)
    {
        if (index < 0 || index >= curUnaskedQuestions.Count)
        {
            Debug.LogWarning(
                $"Invalid dialogue option index [{index}]. " +
                $"There are {curUnaskedQuestions.Count} available questions."
            );
            return;
        }

        DialogueNode selectedQuestion = curUnaskedQuestions[index];

        if (selectedQuestion is BribeDialogueNode bN)
        {
            clubSceneManager.Bribed(bN.bribeAmount);
        }

        selectedQuestion.hasBeenAsked = true;
        if (curNode == currentDialogueTree.nodes[0]) clubSceneManager.AskedAQuestion();

        StartDialogue(selectedQuestion);
    }

    /// <summary>
    /// Advances from a SimpleDialogueNode through its nextNode output.
    /// Ends dialogue if no next node is connected.
    /// </summary>
    private void DisplayNextSimple()
    {
        if (curNode is not SimpleDialogueNode simpleNode)
        {
            Debug.LogWarning(
                "DisplayNextSimple called while current node " +
                "is not a SimpleDialogueNode."
            );

            return;
        }

        NodePort port =
            simpleNode.GetOutputPort("nextNode")?.Connection;

        if (port == null)
        {
            EndDialogue();
            return;
        }

        StartDialogue(port.node);
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------


    /// <summary>
    /// Updates the shared dialogue UI for the supplied speaker
    /// and hides choice-related UI until it is needed.
    /// </summary>
    private void RenderUI()
    {
        nameText.text = currentDialogueTree.CharacterName;
        if (curNode is DialogueNode dN && dN.lying)
        {
            print("portrait wasnt null");
            portrait.sprite = currentDialogueTree.CharacterLyingIcon;
        }
        else portrait.sprite = currentDialogueTree.CharacterIcon;
        talkingClip = currentDialogueTree.TalkingClip;

        sentenceText.text = "";
        sentenceText.maxVisibleCharacters = 0;

        nextElement.enabled = false;

        HideOptionButtons();
    }


    /// <summary>
    /// Handles what should happen automatically once the final sentence
    /// and final page of the current node have finished rendering.
    /// </summary>
    private void OnDialogueTextFinished()
    {
        switch (curNode)
        {
            case OptionDialogueNode node:
                nextElement.enabled = false;
                ShowOptionButtons(node);
                break;

            case SimpleDialogueNode:
                // Simple dialogue requires another click to advance.
                nextElement.enabled = true;
                break;
        }
    }

    
    /// <summary>
    /// Displays one button for each remaining unasked question.
    /// Questions that have already been asked are not shown.
    /// </summary>
    private void ShowOptionButtons(OptionDialogueNode node)
    {
        HideOptionButtons();

        int questionCount =
            Mathf.Min(curUnaskedQuestions.Count, optionButtons.Length);

        for (int i = 0; i < questionCount; i++)
        {
            DialogueNode question = curUnaskedQuestions[i];
            
            // NOTE: ONLY WORKS WHEN BRIBE IS FIRST OPTION

            optionButtons[i].gameObject.SetActive(true);
            optionText[i].text =
                question.LeadingQuestion;
            if (question is BribeDialogueNode bribeNode)
            {
                ShowBribe(bribeNode.bribeAmount);
                if (!clubSceneManager.CanAffordBribe(bribeNode.bribeAmount))
                {
                    optionButtons[i].interactable = false;
                }
            }
        }
    }

    /// <summary>
    /// Hides all dialogue option buttons.
    /// </summary>
    private void HideOptionButtons()
    {
        optionAButton.gameObject.SetActive(false);
        optionBButton.gameObject.SetActive(false);
        optionCButton.gameObject.SetActive(false);
    }


    /// <summary>
    /// Returns true while one or more dialogue choices are visible.
    /// </summary>
    private bool AreOptionButtonsVisible()
    {
        return optionAButton.gameObject.activeSelf ||
               optionBButton.gameObject.activeSelf ||
               optionCButton.gameObject.activeSelf;
    }


    // -------------------------------------------------------------------------
    // SENTENCE / PAGE RENDERING
    // -------------------------------------------------------------------------


    /// <summary>
    /// Loads a dialogue's sentences into the queue and begins displaying
    /// its first sentence after the dialogue panel has opened.
    /// </summary>
    private void EnqueueSentences(DialogueNode dialogueNode)
    {
        sentences.Clear();

        if (dialogueNode.Sentences == null ||
            dialogueNode.Sentences.Length == 0)
        {
            Debug.LogWarning(
                $"Dialogue [{dialogueNode.name}] contains no sentences."
            );

            OnDialogueTextFinished();
            return;
        }

        foreach (string sentence in dialogueNode.Sentences)
        {
            if (!string.IsNullOrEmpty(sentence))
            {
                sentences.Enqueue(sentence);
            }
        }

        if (sentences.Count == 0)
        {
            Debug.LogWarning(
                $"Dialogue [{dialogueNode.name}] contains only empty sentences."
            );

            OnDialogueTextFinished();
            return;
        }

        if (source != null && panelOpen != null)
        {
            source.PlayOneShot(panelOpen);
        }

        panelTween = transform
            .DOLocalMove(showPanelPos, panelAnimationTime).SetUpdate(true)
            .OnComplete(() =>
            {
                panelTween = null;
                DisplaySentence();
            });
        
        clubSceneManager.Pause();
    }


    /// <summary>
    /// Loads the next sentence from the queue, calculates its TextMeshPro
    /// pagination, and begins rendering its first page.
    /// </summary>
    private void DisplaySentence()
    {
        if (sentences.Count == 0)
        {
            OnDialogueTextFinished();
            return;
        }

        StopTextRendering();

        string sentence = sentences.Dequeue();

        sentenceText.text = sentence;

        currentPage = 1;
        sentenceText.pageToDisplay = currentPage;

        sentenceText.ForceMeshUpdate();

        totalPages = Mathf.Max(
            1,
            sentenceText.textInfo.pageCount
        );

        StartPageRendering();
    }


    /// <summary>
    /// Starts the typewriter coroutine for the currently selected page.
    /// </summary>
    private void StartPageRendering()
    {
        StopTextRendering();

        renderCoroutine = StartCoroutine(RenderPage());
    }


    /// <summary>
    /// Reveals the current TMP page one character at a time.
    /// When finished, either displays a continuation indicator or
    /// completes the current dialogue node.
    /// </summary>
    private IEnumerator RenderPage()
    {
        isRenderingText = true;
        nextElement.enabled = false;

        sentenceText.pageToDisplay = currentPage;
        sentenceText.ForceMeshUpdate();

        if (!TryGetCurrentPageInfo(out TMP_PageInfo pageInfo))
        {
            isRenderingText = false;
            yield break;
        }

        int firstCharacter = pageInfo.firstCharacterIndex;
        int lastCharacter = pageInfo.lastCharacterIndex;

        sentenceText.maxVisibleCharacters = firstCharacter;

        for (int i = firstCharacter; i <= lastCharacter; i++)
        {
            sentenceText.maxVisibleCharacters = i + 1;

            if (i % 4 == 0 &&
                source != null &&
                talkingClip != null)
            {
                source.PlayOneShot(talkingClip);
            }

            yield return new WaitForSecondsRealtime(textSpeed);
        }

        renderCoroutine = null;
        isRenderingText = false;

        HandlePageFinished();
    }


    /// <summary>
    /// Immediately reveals the remainder of the current page when the
    /// player clicks while text is still being rendered.
    /// </summary>
    private void FinishCurrentPage()
    {
        print("TRYING TO FINISH PAGE");
        StopTextRendering();

        if (!TryGetCurrentPageInfo(out TMP_PageInfo pageInfo))
            return;

        sentenceText.maxVisibleCharacters =
            pageInfo.lastCharacterIndex + 1;

        isRenderingText = false;

        HandlePageFinished();
    }


    /// <summary>
    /// Determines what should happen after a page finishes rendering.
    /// Shows the continuation indicator when more text remains, otherwise
    /// completes the current dialogue node.
    /// </summary>
    private void HandlePageFinished()
    {
        bool finalPage =
            currentPage >= totalPages;

        bool finalSentence =
            sentences.Count == 0;

        if (finalPage && finalSentence)
        {
            OnDialogueTextFinished();
        }
        else
        {
            nextElement.enabled = true;
        }
    }


    /// <summary>
    /// Safely retrieves TMP page information for the current page.
    /// </summary>
    private bool TryGetCurrentPageInfo(
        out TMP_PageInfo pageInfo)
    {
        pageInfo = default;

        sentenceText.ForceMeshUpdate();

        int pageIndex = currentPage - 1;

        if (pageIndex < 0 ||
            pageIndex >= sentenceText.textInfo.pageCount)
        {
            Debug.LogError(
                $"Invalid dialogue page {currentPage}. " +
                $"Text contains {sentenceText.textInfo.pageCount} pages."
            );

            return false;
        }

        pageInfo =
            sentenceText.textInfo.pageInfo[pageIndex];

        return true;
    }


    /// <summary>
    /// Stops only the active text-rendering coroutine without affecting
    /// unrelated coroutines on the DialogueManager.
    /// </summary>
    private void StopTextRendering()
    {
        if (renderCoroutine != null)
        {
            StopCoroutine(renderCoroutine);
            renderCoroutine = null;
        }

        isRenderingText = false;
    }


    // -------------------------------------------------------------------------
    // BRIBES
    // -------------------------------------------------------------------------

    /// <summary>
    /// Displays the bribe cost associated with option A.
    /// </summary>
    private void ShowBribe(int bribeAmount)
    {
        bribeText.text = bribeAmount.ToString();

        bribeImage.gameObject.SetActive(true);
        bribeText.gameObject.SetActive(true);
    }


    /// <summary>
    /// Hides both bribe displays and restores option button interactability.
    /// </summary>
    private void HideBribe()
    {
        bribeImage.gameObject.SetActive(false);
        bribeText.gameObject.SetActive(false);

        ResetOptionInteractability();
    }


    /// <summary>
    /// Restores both option buttons to their default interactable state.
    /// </summary>
    private void ResetOptionInteractability()
    {
        optionAButton.interactable = true;
        optionBButton.interactable = true;
        optionCButton.interactable = true;
    }


    // -------------------------------------------------------------------------
    // DIALOGUE END
    // -------------------------------------------------------------------------


    /// <summary>
    /// Stops the current dialogue, hides its interaction UI,
    /// and animates the dialogue panel off-screen.
    /// </summary>
    public void EndDialogue()
    {
        StopTextRendering();
        
        panelTween?.Kill();
        panelTween = null;

        sentences.Clear();
        
        sentenceText.text = "";

        nextElement.enabled = false;

        HideOptionButtons();
        HideBribe();

        if (source != null && panelClose != null)
        {
            source.PlayOneShot(panelClose);
        }

        panelTween = transform.DOLocalMove(
            hidePanelPos,
            panelAnimationTime
        ).SetUpdate(true).OnComplete(() => { panelTween = null; });
        
        currentDialogueTree = null;
        active = false;
        
        clubSceneManager.Resume();
    }

    /// <summary>
    /// Finds all connected questions that have not yet been asked.
    /// Once every question has been asked, follows the finished output.
    /// </summary>
    private void AssignUnaskedQuestions(OptionDialogueNode node)
    {
        curUnaskedQuestions.Clear();

        AddQuestionIfUnasked(node, "optionA");
        AddQuestionIfUnasked(node, "optionB");
        AddQuestionIfUnasked(node, "optionC");

        if (curUnaskedQuestions.Count == 0)
        {
            NodePort finishedPort =
                node.GetOutputPort("finished")?.Connection;

            if (finishedPort != null)
            {
                StartDialogue(finishedPort.node);
            }
            else
            {
                Debug.LogWarning(
                    $"Option node [{node.name}] has no remaining questions " +
                    "and no finished node connected."
                );

                EndDialogue();
            }
        }
    }
    
    /// <summary>
    /// Adds a connected dialogue question to the available-question list
    /// if it has not already been asked.
    /// </summary>
    private void AddQuestionIfUnasked(
        OptionDialogueNode node,
        string portName)
    {
        NodePort connection =
            node.GetOutputPort(portName)?.Connection;

        if (connection?.node is not DialogueNode question)
            return;

        if (!question.hasBeenAsked)
        {
            curUnaskedQuestions.Add(question);
        }
    }
}