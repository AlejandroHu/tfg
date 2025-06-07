using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ink.Runtime;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    [Header("UI de Diálogo")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button continueButton;

    public bool IsDialoguePlaying { get; private set; }

    private Story currentStory;
    private NPCDialogue currentNpcDialogue;

    void Start()
    {
        IsDialoguePlaying = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueStory);
    }

    public void EnterDialogueMode(TextAsset inkJSON, NPCDialogue npc)
    {
        currentStory = new Story(inkJSON.text);
        currentNpcDialogue = npc;
        IsDialoguePlaying = true;
        dialoguePanel.SetActive(true);

        // Aquí podrías pausar el PlayerMovement si lo necesitas
        // PlayerMovement.Instance.SetCanMove(false);

        ContinueStory();
    }

    private void ExitDialogueMode()
    {
        IsDialoguePlaying = false;
        dialoguePanel.SetActive(false);
        dialogueText.text = "";

        if (currentNpcDialogue != null)
        {
            currentNpcDialogue.OnDialogueEnd();
            currentNpcDialogue = null;
        }

        // Aquí podrías reanudar el PlayerMovement
        // PlayerMovement.Instance.SetCanMove(true);
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            dialogueText.text = currentStory.Continue();
        }
        else
        {
            ExitDialogueMode();
        }
    }
}
