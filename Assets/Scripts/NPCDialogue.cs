using UnityEngine;
using Ink.Runtime;

// Este script ahora actúa como el intermediario entre el diálogo y los scripts de comportamiento.
public class NPCDialogue : MonoBehaviour
{
    [Header("Configuración de Diálogo")]
    [Tooltip("El archivo de diálogo (.ink.json) por defecto para este NPC si no tiene misiones.")]
    [SerializeField] private TextAsset inkJSON;

    // --- Referencias a los scripts de comportamiento ---
    private NPCMovement npcMovement;
    private DynamicIdleBehavior dynamicIdleBehavior;
    // No necesitamos una referencia a SetInitialAnimationState, ya que solo actúa una vez en Start.

    private Transform playerTransform;

    private void Awake()
    {
        // Obtener referencias a TODOS los posibles scripts de comportamiento en este NPC.
        // Si un script no existe en el NPC, su referencia será null y no dará error.
        npcMovement = GetComponent<NPCMovement>();
        dynamicIdleBehavior = GetComponent<DynamicIdleBehavior>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    /// <summary>
    /// Inicia el diálogo por defecto. Llamado por PlayerInteraction si no hay QuestGiver.
    /// </summary>
    public void StartDefaultDialogue()
    {
        StartDialogue(this.inkJSON);
    }

    /// <summary>
    /// Inicia un diálogo específico. Llamado por QuestGiver.
    /// </summary>
    public void StartDialogue(TextAsset dialogueToPlay)
    {
        if (dialogueToPlay == null || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialoguePlaying))
        {
            return;
        }

        // --- PAUSAR COMPORTAMIENTOS ---
        // Pausar el script de patrulla (si existe).
        if (npcMovement != null)
        {
            npcMovement.SetMovementPaused(true);
            npcMovement.SetIdleAndFaceTarget(playerTransform); // Le decimos que mire al jugador.
        }
        // Pausar el script de idle dinámico (si existe), usando su propio método.
        if (dynamicIdleBehavior != null)
        {
            dynamicIdleBehavior.FocusOnTarget(playerTransform);
        }

        // Iniciar el diálogo a través del DialogueManager.
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EnterDialogueMode(dialogueToPlay, this);
        }
    }

    /// <summary>
    /// Este método es llamado por DialogueManager cuando el diálogo termina.
    /// </summary>
    public void OnDialogueEnd()
    {
        // --- REANUDAR COMPORTAMIENTOS ---
        // Reanudar el script de patrulla (si existe).
        if (npcMovement != null)
        {
            npcMovement.SetMovementPaused(false);
        }
        // Reanudar el script de idle dinámico (si existe).
        if (dynamicIdleBehavior != null)
        {
            dynamicIdleBehavior.ResumeDynamicIdle();
        }
    }
}
