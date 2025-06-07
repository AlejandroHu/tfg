using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ink.Runtime;
using System.Collections;
using System.Collections.Generic; // Necesario para List
using TopDown;


public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    [Header("UI de Diálogo")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("UI de Opciones")] // --- NUEVA SECCIÓN ---
    [Tooltip("El GameObject padre donde se instanciarán los botones de opción. Debe tener un VerticalLayoutGroup.")]
    [SerializeField] private GameObject choicesContainer;
    [Tooltip("El prefab del botón que se usará para cada opción de diálogo.")]
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("Configuración de Input")]
    [SerializeField] private KeyCode advanceDialogueKey = KeyCode.Space;

    public bool IsDialoguePlaying { get; private set; }

    private Story currentStory;
    private NPCDialogue currentNpcDialogue;
    private PlayerMovement playerMovement;

    void Start()
    {
        IsDialoguePlaying = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        playerMovement = FindObjectOfType<PlayerMovement>();
    }

    void Update()
    {
        // Solo escuchar la tecla de avanzar si un diálogo está activo Y no hay opciones que mostrar.
        if (IsDialoguePlaying && currentStory.currentChoices.Count == 0 && Input.GetKeyDown(advanceDialogueKey))
        {
            ContinueStory();
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON, NPCDialogue npc)
    {
        if (inkJSON == null) { Debug.LogError("El archivo Ink JSON es nulo.", npc.gameObject); return; }

        currentStory = new Story(inkJSON.text);
        currentNpcDialogue = npc;
        IsDialoguePlaying = true;
        dialoguePanel.SetActive(true);

        if (playerMovement != null) playerMovement.SetCanMove(false);

        if (npc.TryGetComponent<QuestGiver>(out QuestGiver questGiver))
        {
            currentStory.BindExternalFunction("AcceptQuest", () => questGiver.AcceptThisQuest());
            currentStory.BindExternalFunction("ClaimRewards", () => questGiver.ClaimRewardsForThisQuest());
        }

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

        if (playerMovement != null) playerMovement.SetCanMove(true);
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            dialogueText.text = currentStory.Continue();
            // Después de mostrar la línea, comprobar si hay opciones
            DisplayChoices();
        }
        else
        {
            // Si no puede continuar Y no hay opciones, el diálogo ha terminado
            ExitDialogueMode();
        }
    }

    // --- NUEVO: Método para mostrar las opciones ---
    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        // Limpiar botones de opciones anteriores
        foreach (Transform child in choicesContainer.transform)
        {
            Destroy(child.gameObject);
        }

        if (currentChoices.Count > 0)
        {
            choicesContainer.SetActive(true); // Mostrar el contenedor
                                              // Crear un botón por cada opción
            foreach (Choice choice in currentChoices)
            {
                GameObject choiceButtonGO = Instantiate(choiceButtonPrefab, choicesContainer.transform);
                TextMeshProUGUI choiceText = choiceButtonGO.GetComponentInChildren<TextMeshProUGUI>();
                Button choiceButton = choiceButtonGO.GetComponent<Button>();

                choiceText.text = choice.text;
                choiceButton.onClick.AddListener(() => MakeChoice(choice));
            }
        }
        else
        {
            choicesContainer.SetActive(false); // Ocultar si no hay opciones
        }
    }

    // --- NUEVO: Método para manejar la selección de una opción ---
    public void MakeChoice(Choice choice)
    {
        currentStory.ChooseChoiceIndex(choice.index);
        ContinueStory(); // Continuar la historia después de haber elegido
    }


}