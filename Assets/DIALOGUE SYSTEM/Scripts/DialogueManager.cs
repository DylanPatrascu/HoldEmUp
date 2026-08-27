using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using XNode;

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text nameText;
    public TMP_Text sentenceText;

    public Button optionAButton;
    public Button optionBButton;

    public TMP_Text optionAText;
    public TMP_Text optionBText;

    public Image bribeImageA;
    public Image bribeImageB;

    public TMP_Text bribeTextA;
    public TMP_Text bribeTextB;

    public Image portrait;
    public Image nextElement;

    [Header("Audio")]
    public AudioClip panelOpen;
    public AudioClip panelClose;

    [Header("Dialogue Settings")]
    public Vector3 showPanelPos = new Vector3(0, -140, 0);
    public Vector3 hidePanelPos = new Vector3(0, -800, 0);
    public float panelAnimationTime = 1f;
    public float textSpeed = 0.01f;

    [Header("Club Scene Manager")]
    public ClubSceneManager clubSceneManager;


    // Current dialogue state
    private Node curNode;
    private readonly Queue<string> sentences = new();

    // Audio
    private AudioSource source;
    private AudioClip talkingClip;

    // Random option pool state
    private List<RandomDialogueOption> currentRandomOptions;

    // Text/page state
    private int currentPage = 1;
    private int totalPages = 1;
    private bool isRenderingText;

    private Coroutine renderCoroutine;


    /// <summary>
    /// Initializes the dialogue manager and verifies its required components.
    /// </summary>
    private void Start()
    {
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
        HideBribes();
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
               optionAText != null &&
               optionBText != null &&
               portrait != null &&
               nextElement != null &&
               bribeImageA != null &&
               bribeImageB != null &&
               bribeTextA != null &&
               bribeTextB != null;
    }


    /// <summary>
    /// Starts displaying the supplied dialogue node and dispatches it
    /// to the appropriate handler based on its node type.
    /// </summary>
    public void StartDialogue(Node node)
    {
        if (node == null)
        {
            Debug.LogError("Cannot start dialogue from a null node.");
            EndDialogue();
            return;
        }

        StopTextRendering();

        curNode = node;
        currentRandomOptions = null;

        nextElement.enabled = false;

        HideOptionButtons();
        HideBribes();

        switch (curNode)
        {
            case RandomOptionPoolNode randomPool:
                DisplayRandomOptionPoolNode(randomPool);
                break;

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
    /// Displays a random option pool node, chooses its available questions,
    /// and prepares their text for the option buttons.
    /// </summary>
    private void DisplayRandomOptionPoolNode(RandomOptionPoolNode pool)
    {
        if (!TryPrepareDialogueNode(pool, out Dialogue dialogue))
            return;

        currentRandomOptions = pool.GetRandomOptions();

        if (currentRandomOptions == null)
        {
            Debug.LogError(
                $"Random option pool [{pool.name}] returned a null option list.",
                this
            );

            EndDialogue();
            return;
        }

        bool noQuestionsRemaining = currentRandomOptions.Count == 0;
        bool questionLimitReached =
            clubSceneManager != null &&
            !clubSceneManager.CanAskQuestion();

        if (noQuestionsRemaining || questionLimitReached)
        {
            FollowFinishedPort(pool);
            return;
        }

        // Prepare the option text now.
        // Buttons stay hidden until the dialogue text has finished.
        optionAText.text = currentRandomOptions[0].Text;

        optionBText.text =
            currentRandomOptions.Count > 1
                ? currentRandomOptions[1].Text
                : "";

        EnqueueSentences(dialogue);
    }


    /// <summary>
    /// Displays a simple dialogue node containing normal sequential dialogue.
    /// </summary>
    private void DisplaySimpleNode(SimpleDialogueNode node)
    {
        if (!TryPrepareDialogueNode(node, out Dialogue dialogue))
            return;

        EnqueueSentences(dialogue);
    }


    /// <summary>
    /// Displays a normal option node and prepares its two response choices.
    /// </summary>
    private void DisplayOptionNode(OptionDialogueNode node)
    {
        if (!TryPrepareDialogueNode(node, out Dialogue dialogue))
            return;

        if (node.responses == null ||
            node.responses.sentences == null ||
            node.responses.sentences.Length < 2)
        {
            Debug.LogError(
                $"Option node [{node.name}] requires at least two response sentences.",
                this
            );

            EndDialogue();
            return;
        }

        optionAText.text = node.responses.sentences[0];
        optionBText.text = node.responses.sentences[1];

        EnqueueSentences(dialogue);
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
    private bool TryPrepareDialogueNode(
        DialogueNode node,
        out Dialogue dialogue)
    {
        dialogue = null;

        if (node == null)
        {
            Debug.LogError("Attempted to display a null dialogue node.");
            return false;
        }

        if (node.speaker == null)
        {
            Debug.LogError(
                $"Dialogue node [{node.name}] has no speaker assigned.",
                this
            );

            return false;
        }

        dialogue = node.speaker;

        RenderUI(dialogue);

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
    public void DisplayNextOption(string option)
    {
        if (option != "A" && option != "B")
        {
            Debug.LogWarning(
                $"Invalid dialogue option [{option}]. Expected A or B."
            );

            return;
        }

        if (curNode is RandomOptionPoolNode randomPool)
        {
            SelectRandomOption(randomPool, option);
            return;
        }

        if (curNode is OptionDialogueNode optionNode)
        {
            SelectNormalOption(optionNode, option);
            return;
        }

        Debug.LogWarning(
            $"DisplayNextOption was called while current node " +
            $"[{curNode?.name}] is not an option node."
        );
    }


    /// <summary>
    /// Selects a question from a random option pool, consumes it,
    /// processes any bribe cost, and follows its output connection.
    /// </summary>
    private void SelectRandomOption(
        RandomOptionPoolNode pool,
        string option)
    {
        int index = option == "A" ? 0 : 1;

        if (currentRandomOptions == null ||
            index >= currentRandomOptions.Count)
        {
            Debug.LogWarning(
                $"Random option [{option}] is not currently available."
            );

            return;
        }

        RandomDialogueOption selectedOption =
            currentRandomOptions[index];

        NodePort port =
            pool.GetOutputPort(selectedOption.PortName)?.Connection;

        if (port == null)
        {
            Debug.LogWarning(
                $"Random option [{selectedOption.Text}] on node " +
                $"[{pool.name}] has no connected output."
            );

            return;
        }

        // Only count the question after confirming it has a valid destination.
        if (clubSceneManager != null)
        {
            clubSceneManager.AskedAQuestion();

            if (port.node is BribeDialogueNode bribeNode)
            {
                clubSceneManager.Bribed(bribeNode.bribeAmount);
            }
        }

        pool.UseOption(selectedOption);

        StartDialogue(port.node);
    }


    /// <summary>
    /// Follows option A or B from a standard OptionDialogueNode.
    /// </summary>
    private void SelectNormalOption(
        OptionDialogueNode node,
        string option)
    {
        string portName =
            option == "A"
                ? "optionA"
                : "optionB";

        NodePort port =
            node.GetOutputPort(portName)?.Connection;

        if (port == null)
        {
            Debug.LogWarning(
                $"Option [{option}] on node [{node.name}] " +
                $"has no connected output."
            );

            return;
        }

        StartDialogue(port.node);
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


    /// <summary>
    /// Follows the finished output of a random option pool.
    /// Ends dialogue if no finished connection exists.
    /// </summary>
    private void FollowFinishedPort(RandomOptionPoolNode pool)
    {
        NodePort port =
            pool.GetOutputPort("finished")?.Connection;

        if (port == null)
        {
            Debug.LogWarning(
                $"Random option pool [{pool.name}] has no finished connection."
            );

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
    private void RenderUI(Dialogue dialogue)
    {
        nameText.text = dialogue.dialogueName;
        portrait.sprite = dialogue.portrait;
        talkingClip = dialogue.talkingClip;

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
            case RandomOptionPoolNode pool:
                nextElement.enabled = false;

                ShowRandomOptionButtons();
                CheckForBribes(pool);
                break;

            case OptionDialogueNode:
                nextElement.enabled = false;
                ShowOptionButtons();
                break;

            case SimpleDialogueNode:
                // Simple dialogue requires another click to advance.
                nextElement.enabled = true;
                break;
        }
    }


    /// <summary>
    /// Shows both standard dialogue option buttons.
    /// </summary>
    private void ShowOptionButtons()
    {
        optionAButton.gameObject.SetActive(true);
        optionBButton.gameObject.SetActive(true);
    }


    /// <summary>
    /// Shows the currently available random option buttons.
    /// Option B remains hidden when only one option is available.
    /// </summary>
    private void ShowRandomOptionButtons()
    {
        if (currentRandomOptions == null ||
            currentRandomOptions.Count == 0)
        {
            HideOptionButtons();
            return;
        }

        optionAButton.gameObject.SetActive(true);

        optionBButton.gameObject.SetActive(
            currentRandomOptions.Count > 1
        );
    }


    /// <summary>
    /// Hides all dialogue option buttons.
    /// </summary>
    private void HideOptionButtons()
    {
        optionAButton.gameObject.SetActive(false);
        optionBButton.gameObject.SetActive(false);
    }


    /// <summary>
    /// Returns true while one or more dialogue choices are visible.
    /// </summary>
    private bool AreOptionButtonsVisible()
    {
        return optionAButton.gameObject.activeSelf ||
               optionBButton.gameObject.activeSelf;
    }


    // -------------------------------------------------------------------------
    // SENTENCE / PAGE RENDERING
    // -------------------------------------------------------------------------


    /// <summary>
    /// Loads a dialogue's sentences into the queue and begins displaying
    /// its first sentence after the dialogue panel has opened.
    /// </summary>
    private void EnqueueSentences(Dialogue dialogue)
    {
        sentences.Clear();

        if (dialogue.sentences == null ||
            dialogue.sentences.Length == 0)
        {
            Debug.LogWarning(
                $"Dialogue [{dialogue.dialogueName}] contains no sentences."
            );

            OnDialogueTextFinished();
            return;
        }

        foreach (string sentence in dialogue.sentences)
        {
            if (!string.IsNullOrEmpty(sentence))
            {
                sentences.Enqueue(sentence);
            }
        }

        if (sentences.Count == 0)
        {
            Debug.LogWarning(
                $"Dialogue [{dialogue.dialogueName}] contains only empty sentences."
            );

            OnDialogueTextFinished();
            return;
        }

        if (source != null && panelOpen != null)
        {
            source.PlayOneShot(panelOpen);
        }

        transform
            .DOLocalMove(showPanelPos, panelAnimationTime)
            .OnComplete(DisplaySentence);
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

            yield return new WaitForSeconds(textSpeed);
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
    /// Checks each currently displayed random option to determine whether
    /// it leads to a bribe node, then displays and validates its cost.
    /// </summary>
    private void CheckForBribes(RandomOptionPoolNode pool)
    {
        if (currentRandomOptions == null)
            return;

        ResetOptionInteractability();

        CheckOptionForBribe(
            pool,
            0,
            optionAButton,
            ShowBribeA
        );

        CheckOptionForBribe(
            pool,
            1,
            optionBButton,
            ShowBribeB
        );
    }


    /// <summary>
    /// Checks one random option for a BribeDialogueNode and configures
    /// its bribe UI and button interactability when necessary.
    /// </summary>
    private void CheckOptionForBribe(
        RandomOptionPoolNode pool,
        int optionIndex,
        Button button,
        System.Action<int> showBribe)
    {
        if (optionIndex >= currentRandomOptions.Count)
            return;

        RandomDialogueOption option =
            currentRandomOptions[optionIndex];

        NodePort port =
            pool.GetOutputPort(option.PortName)?.Connection;

        if (port == null)
        {
            Debug.LogWarning(
                $"Random option [{option.Text}] on [{pool.name}] " +
                $"has no connected output."
            );

            return;
        }

        if (port.node is not BribeDialogueNode bribeNode)
            return;

        showBribe(bribeNode.bribeAmount);

        if (clubSceneManager is not null &&
            !clubSceneManager.CanAffordBribe(bribeNode.bribeAmount))
        {
            button.interactable = false;
        }
    }


    /// <summary>
    /// Displays the bribe cost associated with option A.
    /// </summary>
    private void ShowBribeA(int bribeAmount)
    {
        bribeTextA.text = bribeAmount.ToString();

        bribeImageA.gameObject.SetActive(true);
        bribeTextA.gameObject.SetActive(true);
    }


    /// <summary>
    /// Displays the bribe cost associated with option B.
    /// </summary>
    private void ShowBribeB(int bribeAmount)
    {
        bribeTextB.text = bribeAmount.ToString();

        bribeImageB.gameObject.SetActive(true);
        bribeTextB.gameObject.SetActive(true);
    }


    /// <summary>
    /// Hides both bribe displays and restores option button interactability.
    /// </summary>
    private void HideBribes()
    {
        bribeImageA.gameObject.SetActive(false);
        bribeTextA.gameObject.SetActive(false);

        bribeImageB.gameObject.SetActive(false);
        bribeTextB.gameObject.SetActive(false);

        ResetOptionInteractability();
    }


    /// <summary>
    /// Restores both option buttons to their default interactable state.
    /// </summary>
    private void ResetOptionInteractability()
    {
        optionAButton.interactable = true;
        optionBButton.interactable = true;
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

        sentences.Clear();
        currentRandomOptions = null;

        nextElement.enabled = false;

        HideOptionButtons();
        HideBribes();

        if (source != null && panelClose != null)
        {
            source.PlayOneShot(panelClose);
        }

        transform.DOLocalMove(
            hidePanelPos,
            panelAnimationTime
        );
    }
}